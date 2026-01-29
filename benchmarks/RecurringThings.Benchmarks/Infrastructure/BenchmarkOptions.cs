namespace RecurringThings.Benchmarks.Infrastructure;

/// <summary>
/// Configurable options for benchmark parameters.
/// </summary>
public static class BenchmarkOptions
{
    /// <summary>
    /// Data volume steps - each represents a different seeded record count.
    /// </summary>
    public static readonly int[] DataVolumes = [1_00, 1_000, 10_000, 100_000];

    /// <summary>
    /// Concurrent request counts to test.
    /// Note: High values (50+) may require increasing PostgreSQL max_connections.
    /// </summary>
    public static readonly int[] ConcurrentRequests = [1, 5, 10, 25, 50];

    /// <summary>
    /// Query range start (UTC). Recurrences will be seeded to produce occurrences within this range.
    /// </summary>
    public static readonly DateTime QueryStartUtc = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Query range end (UTC). Recurrences will be seeded to produce occurrences within this range.
    /// </summary>
    public static readonly DateTime QueryEndUtc = new(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);

    /// <summary>
    /// Maximum number of recurrences that produce occurrences within the query range.
    /// Remaining recurrences are seeded outside the range to test index selectivity.
    /// </summary>
    public static readonly int MaxRecurrencesInRange = 20;

    /// <summary>
    /// Maximum number of standalone occurrences that fall within the query range.
    /// Remaining occurrences are seeded outside the range to test index selectivity.
    /// </summary>
    public static readonly int MaxOccurrencesInRange = 100;
}
