using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sam.Coach.Advanced
{
    public interface ISequenceAnalyzer<T> where T : IComparable<T>
    {
        ValueTask<SequenceAnalysisResult<T>> AnalyzeAsync(
            ReadOnlyMemory<T> sequence, 
            CancellationToken cancellationToken = default);
        
        ValueTask<SequenceAnalysisResult<T>> AnalyzeStreamAsync(
            IAsyncEnumerable<T> sequence, 
            CancellationToken cancellationToken = default);
    }
    
    public readonly record struct SequenceAnalysisResult<T>(
        ReadOnlyMemory<T> LongestRising,
        ReadOnlyMemory<T> LongestDescending,
        int TotalComparisons,
        TimeSpan ProcessingTime) where T : IComparable<T>;
}