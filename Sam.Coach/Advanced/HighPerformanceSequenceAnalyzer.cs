using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sam.Coach.Advanced
{
    public sealed class HighPerformanceSequenceAnalyzer<T> : ISequenceAnalyzer<T>, IDisposable 
        where T : struct, IComparable<T>
    {
        private readonly ArrayPool<T> _arrayPool;
        private readonly SemaphoreSlim _concurrencySemaphore;
        private readonly int _maxConcurrency;
        private bool _disposed;

        public HighPerformanceSequenceAnalyzer(int maxConcurrency = -1)
        {
            _arrayPool = ArrayPool<T>.Shared;
            _maxConcurrency = maxConcurrency == -1 ? Environment.ProcessorCount : maxConcurrency;
            _concurrencySemaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        }

        public async ValueTask<SequenceAnalysisResult<T>> AnalyzeAsync(
            ReadOnlyMemory<T> sequence, 
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (sequence.IsEmpty)
                return new SequenceAnalysisResult<T>(
                    ReadOnlyMemory<T>.Empty, 
                    ReadOnlyMemory<T>.Empty, 
                    0, 
                    TimeSpan.Zero);

            await _concurrencySemaphore.WaitAsync(cancellationToken);
            try
            {
                return await AnalyzeCore(sequence, cancellationToken);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }

        public async ValueTask<SequenceAnalysisResult<T>> AnalyzeStreamAsync(
            IAsyncEnumerable<T> sequence, 
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            var buffer = _arrayPool.Rent(4096);
            try
            {
                var count = 0;
                await foreach (var item in sequence.WithCancellation(cancellationToken))
                {
                    if (count >= buffer.Length)
                    {
                        var newBuffer = _arrayPool.Rent(buffer.Length * 2);
                        Array.Copy(buffer, newBuffer, count);
                        _arrayPool.Return(buffer);
                        buffer = newBuffer;
                    }
                    buffer[count++] = item;
                }

                return await AnalyzeAsync(buffer.AsMemory(0, count), cancellationToken);
            }
            finally
            {
                _arrayPool.Return(buffer);
            }
        }

        private ValueTask<SequenceAnalysisResult<T>> AnalyzeCore(
            ReadOnlyMemory<T> sequence, 
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var span = sequence.Span;
            var length = span.Length;

            if (length <= 1)
            {
                stopwatch.Stop();
                return ValueTask.FromResult(new SequenceAnalysisResult<T>(
                    sequence, sequence, 0, stopwatch.Elapsed));
            }

            var (risingSequence, risingComparisons) = FindLongestSequence(span, ascending: true);
            var (descendingSequence, descendingComparisons) = FindLongestSequence(span, ascending: false);
            
            stopwatch.Stop();

            var risingMemory = sequence.Slice(risingSequence.start, risingSequence.length);
            var descendingMemory = sequence.Slice(descendingSequence.start, descendingSequence.length);

            return ValueTask.FromResult(new SequenceAnalysisResult<T>(
                risingMemory,
                descendingMemory,
                risingComparisons + descendingComparisons,
                stopwatch.Elapsed));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private ((int start, int length), int comparisons) FindLongestSequence(
            ReadOnlySpan<T> span, 
            bool ascending)
        {
            var length = span.Length;
            var longestStart = 0;
            var longestLength = 1;
            var currentStart = 0;
            var currentLength = 1;
            var comparisons = 0;

            ref var spanRef = ref MemoryMarshal.GetReference(span);

            for (var i = 1; i < length; i++)
            {
                comparisons++;
                ref var current = ref Unsafe.Add(ref spanRef, i);
                ref var previous = ref Unsafe.Add(ref spanRef, i - 1);

                var isIncreasing = current.CompareTo(previous) > 0;
                var shouldContinue = ascending ? isIncreasing : !isIncreasing;

                if (shouldContinue)
                {
                    currentLength++;
                }
                else
                {
                    if (currentLength > longestLength)
                    {
                        longestStart = currentStart;
                        longestLength = currentLength;
                    }
                    currentStart = i;
                    currentLength = 1;
                }
            }

            if (currentLength > longestLength)
            {
                longestStart = currentStart;
                longestLength = currentLength;
            }

            return ((longestStart, longestLength), comparisons);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HighPerformanceSequenceAnalyzer<T>));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _concurrencySemaphore?.Dispose();
                _disposed = true;
            }
        }
    }
}