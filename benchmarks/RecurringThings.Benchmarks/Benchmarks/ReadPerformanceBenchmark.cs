namespace RecurringThings.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using RecurringThings.Benchmarks.Infrastructure;
using RecurringThings.Engine;

/// <summary>
/// Benchmark for measuring read performance with varying data volumes and concurrent requests.
/// BenchmarkDotNet measures execution time (response time) automatically.
/// Each (Provider, DataVolume) combination uses its own persistent database.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class ReadPerformanceBenchmark
{
    /// <summary>
    /// Data volume for the current benchmark run.
    /// </summary>
    [ParamsSource(nameof(GetDataVolumes))]
    public int DataVolume { get; set; }

    /// <summary>
    /// Number of concurrent requests to execute.
    /// </summary>
    [ParamsSource(nameof(GetConcurrentRequests))]
    public int ConcurrentRequests { get; set; }

    /// <summary>
    /// Database provider for the current benchmark run.
    /// </summary>
    [ParamsSource(nameof(GetProviders))]
    public BenchmarkProvider Provider { get; set; }

    /// <summary>
    /// Gets the configured data volumes.
    /// </summary>
    public static int[] GetDataVolumes() => BenchmarkOptions.DataVolumes;

    /// <summary>
    /// Gets the configured concurrent request counts.
    /// </summary>
    public static int[] GetConcurrentRequests() => BenchmarkOptions.ConcurrentRequests;

    /// <summary>
    /// Gets the available database providers.
    /// </summary>
    public static BenchmarkProvider[] GetProviders() => ProviderFactory.GetAvailableProviders().ToArray();

    private IRecurrenceEngine _engine = null!;

    /// <summary>
    /// Sets up the benchmark by initializing the provider and seeding data if needed.
    /// Uses persistent databases - data is only seeded if not already present.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var dbName = ProviderFactory.GetDatabaseName(Provider, DataVolume);
        Console.WriteLine($"\n=== Setup: {Provider}, Volume={DataVolume}, Concurrency={ConcurrentRequests} ===");
        Console.WriteLine($"  Database: {dbName}");

        // Initialize provider (creates database if needed, runs migrations/indexes)
        await ProviderFactory.InitializeProviderAsync(Provider, DataVolume);

        // Create engine
        _engine = ProviderFactory.CreateEngine(Provider, DataVolume);

        // Check if data is already seeded
        var isSeeded = await ProviderFactory.IsDataSeededAsync(Provider, DataVolume);

        if (isSeeded)
        {
            Console.WriteLine($"  Data already seeded - reusing existing data");
        }
        else
        {
            Console.WriteLine($"  Seeding {DataVolume} records...");
            await DataSeeder.SeedDeterministicAsync(_engine, DataVolume);
            Console.WriteLine($"  Seeding complete");
        }
    }

    /// <summary>
    /// Benchmark method - BenchmarkDotNet measures execution time (response time).
    /// Launches N concurrent queries and waits for all to complete.
    /// </summary>
    [Benchmark(Description = "Read")]
    public async Task QueryOccurrencesAsync()
    {
        var tasks = Enumerable.Range(0, ConcurrentRequests)
            .Select(_ => ExecuteQueryAsync())
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteQueryAsync()
    {
        try
        {
            // Consume all results to measure full query time
            await foreach (var _ in _engine.GetOccurrencesAsync(
                DataSeeder.Organization,
                DataSeeder.ResourcePath,
                BenchmarkOptions.QueryStartUtc,
                BenchmarkOptions.QueryEndUtc))
            {
                // Results consumed to measure full iteration time
            }
        }
        catch (Exception ex)
        {
            // Log but don't rethrow - allow other concurrent queries to complete
            Console.WriteLine($"  [Query failed: {ex.GetType().Name}: {ex.Message}]");
        }
    }

    /// <summary>
    /// No cleanup needed - databases persist for reuse across benchmark runs.
    /// </summary>
    [GlobalCleanup]
    public Task GlobalCleanup()
    {
        // Databases persist for reuse - no cleanup needed
        return Task.CompletedTask;
    }
}
