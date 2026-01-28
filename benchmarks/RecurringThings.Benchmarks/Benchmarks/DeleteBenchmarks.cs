namespace RecurringThings.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using RecurringThings.Benchmarks.Infrastructure;
using RecurringThings.Engine;
using RecurringThings.Models;

/// <summary>
/// Benchmarks for delete operations.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class DeleteBenchmarks
{
    private IRecurrenceEngine _engine = null!;
    private Queue<CalendarEntry> _occurrencesToDelete = null!;
    private Queue<Guid> _recurrencesToDelete = null!;

    /// <summary>
    /// Gets or sets the database provider to benchmark.
    /// </summary>
    [Params(BenchmarkProvider.MongoDB, BenchmarkProvider.PostgreSQL)]
    public BenchmarkProvider Provider { get; set; }

    private const string TimeZone = "America/New_York";

    /// <summary>
    /// Setup that runs once before all benchmark iterations for each parameter combination.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        Console.WriteLine();
        Console.WriteLine($"[DeleteBenchmarks] GlobalSetup: Provider={Provider}");
        _engine = ProviderFactory.CreateEngine(Provider);
        Console.WriteLine($"[DeleteBenchmarks] Setup complete.");
    }

    /// <summary>
    /// Setup that runs before each benchmark iteration.
    /// Pre-creates entries to be deleted during the benchmark.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        _occurrencesToDelete = new Queue<CalendarEntry>();
        _recurrencesToDelete = new Queue<Guid>();

        // Pre-create entries to delete during benchmark iteration
        for (int i = 0; i < 10; i++)
        {
            var occurrence = _engine.CreateOccurrenceAsync(
                DataSeeder.Organization, DataSeeder.ResourcePath, "delete-test",
                DateTime.UtcNow.AddDays(i), TimeSpan.FromHours(1), TimeZone)
                .GetAwaiter().GetResult();
            _occurrencesToDelete.Enqueue(occurrence);

            var rrule = $"FREQ=DAILY;UNTIL={DateTime.UtcNow.AddMonths(1):yyyyMMdd}T235959Z";
            var recurrence = _engine.CreateRecurrenceAsync(
                DataSeeder.Organization, DataSeeder.ResourcePath, "delete-test",
                DateTime.UtcNow.AddDays(i), TimeSpan.FromHours(1), rrule, TimeZone)
                .GetAwaiter().GetResult();
            _recurrencesToDelete.Enqueue(recurrence.RecurrenceId!.Value);
        }
    }

    /// <summary>
    /// Benchmarks deleting a standalone occurrence.
    /// </summary>
    [Benchmark(Description = "Delete standalone occurrence")]
    public async Task DeleteOccurrenceAsync()
    {
        if (_occurrencesToDelete.TryDequeue(out var entry))
        {
            await _engine.DeleteOccurrenceAsync(entry);
        }
    }

    /// <summary>
    /// Benchmarks deleting a recurrence with cascade delete.
    /// </summary>
    [Benchmark(Description = "Delete recurrence (cascade)")]
    public async Task DeleteRecurrenceAsync()
    {
        if (_recurrencesToDelete.TryDequeue(out var recurrenceId))
        {
            await _engine.DeleteRecurrenceAsync(DataSeeder.Organization, DataSeeder.ResourcePath, recurrenceId);
        }
    }

    /// <summary>
    /// Cleanup that runs after each benchmark iteration.
    /// Removes any entries that weren't deleted during the benchmark.
    /// </summary>
    [IterationCleanup]
    public void IterationCleanup()
    {
        // Clean up any entries that weren't deleted during the benchmark
        while (_occurrencesToDelete.TryDequeue(out var entry))
        {
            try
            {
                _engine.DeleteOccurrenceAsync(entry).GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        while (_recurrencesToDelete.TryDequeue(out var id))
        {
            try
            {
                _engine.DeleteRecurrenceAsync(DataSeeder.Organization, DataSeeder.ResourcePath, id)
                    .GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Cleanup that runs once after all benchmark iterations for each parameter combination.
    /// </summary>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        Console.WriteLine();
        Console.WriteLine($"[DeleteBenchmarks] GlobalCleanup: Ensuring all test data is removed...");
        await DataSeeder.CleanupAllAsync(_engine);
        Console.WriteLine($"[DeleteBenchmarks] Cleanup complete.");
    }
}
