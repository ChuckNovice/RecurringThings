namespace RecurringThings.Benchmarks.Infrastructure;

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Plotting;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

/// <summary>
/// Custom BenchmarkDotNet configuration for RecurringThings benchmarks.
/// </summary>
public class BenchmarkConfig : ManualConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BenchmarkConfig"/> class.
    /// </summary>
    public BenchmarkConfig()
    {
        // Job configuration - fewer iterations for database benchmarks
        AddJob(Job.Default
            .WithWarmupCount(2)
            .WithIterationCount(5)
            .WithInvocationCount(1)
            .WithUnrollFactor(1));

        // Diagnosers
        AddDiagnoser(MemoryDiagnoser.Default);

        // Columns
        AddColumnProvider(DefaultColumnProviders.Instance);

        // Exporters for graphical output
        AddExporter(HtmlExporter.Default);
        AddExporter(CsvExporter.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(new ScottPlotExporter(1920, 1080));

        // Logger
        AddLogger(ConsoleLogger.Default);

        // Output folder
        WithArtifactsPath("./BenchmarkResults");
    }
}
