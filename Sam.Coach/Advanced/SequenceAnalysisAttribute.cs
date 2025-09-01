using System;

namespace Sam.Coach.Advanced
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class GenerateSequenceAnalysisAttribute : Attribute
    {
        public string AnalyzerName { get; }
        public Type ElementType { get; }
        public bool UseMemoryPooling { get; set; } = true;
        public bool EnableBenchmarking { get; set; } = false;

        public GenerateSequenceAnalysisAttribute(string analyzerName, Type elementType)
        {
            AnalyzerName = analyzerName ?? throw new ArgumentNullException(nameof(analyzerName));
            ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        }
    }
}