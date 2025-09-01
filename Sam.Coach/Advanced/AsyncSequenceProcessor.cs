using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sam.Coach.Advanced
{
    public sealed class AsyncSequenceProcessor<T> : IAsyncDisposable 
        where T : struct, IComparable<T>
    {
        private readonly ChannelWriter<T> _writer;
        private readonly ChannelReader<T> _reader;
        private readonly CancellationTokenSource _processingCts;
        private readonly Task _processingTask;
        private readonly HighPerformanceSequenceAnalyzer<T> _analyzer;

        public AsyncSequenceProcessor(int capacity = 1000)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            var channel = Channel.CreateBounded<T>(options);
            _writer = channel.Writer;
            _reader = channel.Reader;
            _processingCts = new CancellationTokenSource();
            _analyzer = new HighPerformanceSequenceAnalyzer<T>();
            
            _processingTask = ProcessSequencesAsync(_processingCts.Token);
        }

        public async ValueTask<bool> TryAddAsync(T item, CancellationToken cancellationToken = default)
        {
            try
            {
                await _writer.WriteAsync(item, cancellationToken);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        public void CompleteAdding() => _writer.Complete();

        public async IAsyncEnumerable<SequenceAnalysisResult<T>> GetResultsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var result in ProcessBatchesAsync(cancellationToken))
            {
                yield return result;
            }
        }

        private async IAsyncEnumerable<SequenceAnalysisResult<T>> ProcessBatchesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            const int batchSize = 1024;
            var buffer = ArrayPool<T>.Shared.Rent(batchSize);
            
            try
            {
                while (await _reader.WaitToReadAsync(cancellationToken))
                {
                    var count = 0;
                    
                    while (_reader.TryRead(out var item) && count < batchSize)
                    {
                        buffer[count++] = item;
                    }

                    if (count > 0)
                    {
                        var result = await _analyzer.AnalyzeAsync(
                            buffer.AsMemory(0, count), 
                            cancellationToken);
                        yield return result;
                    }
                }
            }
            finally
            {
                ArrayPool<T>.Shared.Return(buffer);
            }
        }

        private async Task ProcessSequencesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var result in GetResultsAsync(cancellationToken))
                {
                    await OnSequenceProcessedAsync(result, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        protected ValueTask OnSequenceProcessedAsync(
            SequenceAnalysisResult<T> result, 
            CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            _writer.Complete();
            _processingCts.Cancel();
            
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
            }
            
            _processingCts.Dispose();
            _analyzer.Dispose();
        }
    }
}