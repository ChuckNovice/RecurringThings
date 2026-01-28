namespace RecurringThings.Benchmarks.Infrastructure;

using RecurringThings.Engine;
using RecurringThings.Models;

/// <summary>
/// Seeds test data for benchmarks with varied recurrence patterns.
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Organization used for all benchmark data.
    /// </summary>
    public const string Organization = "benchmark-org";

    /// <summary>
    /// Resource path used for all benchmark data.
    /// </summary>
    public const string ResourcePath = "benchmark/calendar";

    private const string TimeZone = "America/New_York";

    // Varied recurrence patterns with different computational complexities
    private static readonly string[] RRulePatterns =
    [
        // Heavy: Multiple days per week (generates many occurrences)
        "FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR",
        "FREQ=WEEKLY;BYDAY=MO,WE,FR",
        "FREQ=DAILY",
        // Medium: Weekly patterns
        "FREQ=WEEKLY;BYDAY=MO",
        "FREQ=WEEKLY;BYDAY=TU,TH",
        "FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,WE,FR",
        // Light: Monthly and yearly patterns (fewer occurrences)
        "FREQ=MONTHLY;BYMONTHDAY=1",
        "FREQ=MONTHLY;BYMONTHDAY=15",
        "FREQ=MONTHLY;BYDAY=1MO",
        "FREQ=YEARLY;BYMONTH=1;BYMONTHDAY=1"
    ];

    /// <summary>
    /// Seeds recurrences with varied patterns.
    /// </summary>
    public static async Task<List<CalendarEntry>> SeedRecurrencesAsync(
        IRecurrenceEngine engine,
        int count,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<CalendarEntry>(count);
        var baseDate = DateTime.UtcNow.Date;
        var untilDate = baseDate.AddYears(1);

        Console.WriteLine($"  Seeding {count} recurrences with varied patterns...");
        var lastProgress = 0;

        for (int i = 0; i < count; i++)
        {
            // Report progress every 10%
            var progress = (i + 1) * 100 / count;
            if (progress >= lastProgress + 10)
            {
                Console.WriteLine($"    Progress: {progress}% ({i + 1}/{count})");
                lastProgress = progress;
            }

            var startTime = baseDate.AddDays(i % 365).AddHours(9);

            // Select varied pattern based on index
            var patternIndex = i % RRulePatterns.Length;
            var baseRRule = RRulePatterns[patternIndex];
            var rrule = $"{baseRRule};UNTIL={untilDate:yyyyMMdd}T235959Z";

            var entry = await engine.CreateRecurrenceAsync(
                Organization,
                ResourcePath,
                $"meeting-type-{i % 10}",
                startTime,
                TimeSpan.FromHours(1),
                rrule,
                TimeZone,
                new Dictionary<string, string>
                {
                    ["index"] = i.ToString(),
                    ["pattern"] = patternIndex.ToString()
                },
                cancellationToken: cancellationToken);

            entries.Add(entry);
        }

        Console.WriteLine($"  Seeding complete: {count} recurrences created");
        return entries;
    }

    /// <summary>
    /// Seeds standalone occurrences.
    /// </summary>
    public static async Task<List<CalendarEntry>> SeedOccurrencesAsync(
        IRecurrenceEngine engine,
        int count,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<CalendarEntry>(count);
        var baseDate = DateTime.UtcNow.Date;

        Console.WriteLine($"  Seeding {count} standalone occurrences...");
        var lastProgress = 0;

        for (int i = 0; i < count; i++)
        {
            var progress = (i + 1) * 100 / count;
            if (progress >= lastProgress + 10)
            {
                Console.WriteLine($"    Progress: {progress}% ({i + 1}/{count})");
                lastProgress = progress;
            }

            var startTime = baseDate.AddDays(i % 365).AddHours(10 + (i % 8));

            var entry = await engine.CreateOccurrenceAsync(
                Organization,
                ResourcePath,
                $"appointment-type-{i % 5}",
                startTime,
                TimeSpan.FromMinutes(30 + (i % 60)),
                TimeZone,
                new Dictionary<string, string> { ["index"] = i.ToString() },
                cancellationToken: cancellationToken);

            entries.Add(entry);
        }

        Console.WriteLine($"  Seeding complete: {count} occurrences created");
        return entries;
    }

    /// <summary>
    /// Cleans up all benchmark data from the database.
    /// </summary>
    public static async Task CleanupAllAsync(IRecurrenceEngine engine, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("  Cleaning up benchmark data...");

        // Query all recurrences and delete them (cascades to exceptions/overrides)
        var start = DateTime.UtcNow.AddYears(-5);
        var end = DateTime.UtcNow.AddYears(5);

        var recurrenceIds = new List<Guid>();
        await foreach (var entry in engine.GetRecurrencesAsync(
            Organization, ResourcePath, start, end, cancellationToken: cancellationToken))
        {
            if (entry.RecurrenceId.HasValue)
            {
                recurrenceIds.Add(entry.RecurrenceId.Value);
            }
        }

        if (recurrenceIds.Count > 0)
        {
            Console.WriteLine($"    Deleting {recurrenceIds.Count} recurrences...");
            foreach (var id in recurrenceIds)
            {
                await engine.DeleteRecurrenceAsync(Organization, ResourcePath, id, cancellationToken: cancellationToken);
            }
        }

        // Query and delete standalone occurrences
        var occurrences = new List<CalendarEntry>();
        await foreach (var entry in engine.GetOccurrencesAsync(
            Organization, ResourcePath, start, end, cancellationToken: cancellationToken))
        {
            if (entry.EntryType == CalendarEntryType.Standalone)
            {
                occurrences.Add(entry);
            }
        }

        if (occurrences.Count > 0)
        {
            Console.WriteLine($"    Deleting {occurrences.Count} standalone occurrences...");
            foreach (var entry in occurrences)
            {
                await engine.DeleteOccurrenceAsync(entry, cancellationToken: cancellationToken);
            }
        }

        Console.WriteLine("  Cleanup complete");
    }
}
