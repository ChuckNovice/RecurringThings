namespace RecurringThings.Benchmarks.Infrastructure;

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

/// <summary>
/// Custom BenchmarkDotNet configuration for RecurringThings read performance benchmarks.
/// Outputs HTML, CSV, and Markdown reports. Use CSV for custom chart visualization.
/// </summary>
public class BenchmarkConfig : ManualConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BenchmarkConfig"/> class.
    /// </summary>
    public BenchmarkConfig()
    {
        // Job configuration - optimized for database benchmarks
        // Use fewer iterations since database operations have inherent variability
        // WithId hides verbose job config from chart legends
        AddJob(Job.Default
            .WithId("Bench")
            .WithWarmupCount(2)
            .WithIterationCount(5)
            .WithInvocationCount(16)
            .WithUnrollFactor(16));

        // Hide job column since we only have one job configuration
        HideColumns(Column.Job);

        // Diagnosers - memory tracking
        AddDiagnoser(MemoryDiagnoser.Default);

        // Note: HTML, CSV, and Markdown exporters are included by default
        // Adding them explicitly causes duplicate warnings

        // Logger
        AddLogger(ConsoleLogger.Default);

        // Output folder
        WithArtifactsPath("./BenchmarkResults");

        // Summary style - show mean and error
        WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(50));
    }
}
