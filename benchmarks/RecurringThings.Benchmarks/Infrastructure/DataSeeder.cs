namespace RecurringThings.Benchmarks.Infrastructure;

using Ical.Net;
using Ical.Net.DataTypes;
using RecurringThings.Engine;

/// <summary>
/// Seeds test data for benchmarks with deterministic, varied recurrence patterns.
/// Separates data into "in-range" (within query range) and "out-of-range" (historical).
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
    private const int Seed = 42;

    /// <summary>
    /// Seeds deterministic test data with the specified total count.
    /// Data composition:
    /// - 90% recurrences (cycling through frequency types)
    /// - 10% standalone occurrences
    /// In-range vs out-of-range controlled by MaxRecurrencesInRange and MaxOccurrencesInRange.
    /// </summary>
    /// <param name="engine">The recurrence engine to seed data into.</param>
    /// <param name="totalCount">Total number of records to seed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Seeded data information.</returns>
    public static async Task<SeededData> SeedDeterministicAsync(
        IRecurrenceEngine engine,
        int totalCount,
        CancellationToken cancellationToken = default)
    {
        var random = new Random(Seed);

        // Calculate counts based on composition
        var recurrenceCount = (int)(totalCount * 0.9);
        var occurrenceCount = totalCount - recurrenceCount;

        // Calculate in-range vs out-of-range
        var inRangeRecurrences = Math.Min(recurrenceCount, BenchmarkOptions.MaxRecurrencesInRange);
        var outOfRangeRecurrences = recurrenceCount - inRangeRecurrences;
        var inRangeOccurrences = Math.Min(occurrenceCount, BenchmarkOptions.MaxOccurrencesInRange);
        var outOfRangeOccurrences = occurrenceCount - inRangeOccurrences;

        Console.WriteLine($"  Seeding {totalCount} total records (seed={Seed}):");
        Console.WriteLine($"    - {recurrenceCount} recurrences (90%):");
        Console.WriteLine($"        {inRangeRecurrences} in-range, {outOfRangeRecurrences} out-of-range");
        Console.WriteLine($"    - {occurrenceCount} standalone occurrences (10%):");
        Console.WriteLine($"        {inRangeOccurrences} in-range, {outOfRangeOccurrences} out-of-range");

        // Calculate query range
        var queryStart = BenchmarkOptions.QueryStartUtc;
        var queryEnd = BenchmarkOptions.QueryEndUtc;
        var rangeDays = (queryEnd - queryStart).TotalDays;

        // Calculate historical period for out-of-range data (1 year before query start)
        var historicalEnd = queryStart.AddDays(-1);
        var historicalStart = historicalEnd.AddYears(-1);
        var historicalDays = (historicalEnd - historicalStart).TotalDays;

        // Seed in-range recurrences
        Console.WriteLine("  Seeding in-range recurrences...");
        var lastProgress = 0;

        for (var i = 0; i < inRangeRecurrences; i++)
        {
            ReportProgress(i, inRangeRecurrences, ref lastProgress, "in-range recurrences");

            // Distribute start times within the first third of the query range
            var dayOffset = (i % (int)(rangeDays / 3)) + random.Next(0, 7);
            var startTime = queryStart.AddDays(dayOffset).AddHours(9 + (i % 12));

            // Build RRule with UNTIL = query end
            var rrule = BuildRRule(i, queryEnd);

            await engine.CreateRecurrenceAsync(
                Organization,
                ResourcePath,
                $"type-{i % 10}",
                startTime,
                TimeSpan.FromHours(1),
                rrule,
                TimeZone,
                new Dictionary<string, string>
                {
                    ["index"] = i.ToString(),
                    ["seed"] = Seed.ToString(),
                    ["location"] = "in-range"
                },
                cancellationToken: cancellationToken);
        }

        Console.WriteLine($"    In-range recurrences: {inRangeRecurrences} created");

        // Seed out-of-range recurrences (historical data)
        if (outOfRangeRecurrences > 0)
        {
            Console.WriteLine("  Seeding out-of-range recurrences (historical)...");
            lastProgress = 0;

            for (var i = 0; i < outOfRangeRecurrences; i++)
            {
                ReportProgress(i, outOfRangeRecurrences, ref lastProgress, "out-of-range recurrences");

                // Distribute throughout the historical period
                var dayOffset = i % (int)historicalDays;
                var startTime = historicalStart.AddDays(dayOffset).AddHours(9 + (i % 12));

                // Build RRule with UNTIL = before query start (ends within historical period)
                var untilDate = startTime.AddMonths(1); // Recurrence runs for about a month
                if (untilDate > historicalEnd)
                    untilDate = historicalEnd;

                var rrule = BuildRRule(i, untilDate);

                await engine.CreateRecurrenceAsync(
                    Organization,
                    ResourcePath,
                    $"type-{i % 10}",
                    startTime,
                    TimeSpan.FromHours(1),
                    rrule,
                    TimeZone,
                    new Dictionary<string, string>
                    {
                        ["index"] = (inRangeRecurrences + i).ToString(),
                        ["seed"] = Seed.ToString(),
                        ["location"] = "out-of-range"
                    },
                    cancellationToken: cancellationToken);
            }

            Console.WriteLine($"    Out-of-range recurrences: {outOfRangeRecurrences} created");
        }

        // Seed in-range standalone occurrences
        Console.WriteLine("  Seeding in-range standalone occurrences...");
        lastProgress = 0;

        for (var i = 0; i < inRangeOccurrences; i++)
        {
            ReportProgress(i, inRangeOccurrences, ref lastProgress, "in-range occurrences");

            // Distribute throughout the query range
            var dayOffset = i % (int)rangeDays;
            var startTime = queryStart.AddDays(dayOffset).AddHours(10 + (i % 8));

            await engine.CreateOccurrenceAsync(
                Organization,
                ResourcePath,
                $"standalone-type-{i % 5}",
                startTime,
                TimeSpan.FromMinutes(30 + (i % 60)),
                TimeZone,
                new Dictionary<string, string>
                {
                    ["index"] = i.ToString(),
                    ["seed"] = Seed.ToString(),
                    ["location"] = "in-range"
                },
                cancellationToken: cancellationToken);
        }

        Console.WriteLine($"    In-range occurrences: {inRangeOccurrences} created");

        // Seed out-of-range standalone occurrences (historical data)
        if (outOfRangeOccurrences > 0)
        {
            Console.WriteLine("  Seeding out-of-range standalone occurrences (historical)...");
            lastProgress = 0;

            for (var i = 0; i < outOfRangeOccurrences; i++)
            {
                ReportProgress(i, outOfRangeOccurrences, ref lastProgress, "out-of-range occurrences");

                // Distribute throughout the historical period
                var dayOffset = i % (int)historicalDays;
                var startTime = historicalStart.AddDays(dayOffset).AddHours(10 + (i % 8));

                await engine.CreateOccurrenceAsync(
                    Organization,
                    ResourcePath,
                    $"standalone-type-{i % 5}",
                    startTime,
                    TimeSpan.FromMinutes(30 + (i % 60)),
                    TimeZone,
                    new Dictionary<string, string>
                    {
                        ["index"] = (inRangeOccurrences + i).ToString(),
                        ["seed"] = Seed.ToString(),
                        ["location"] = "out-of-range"
                    },
                    cancellationToken: cancellationToken);
            }

            Console.WriteLine($"    Out-of-range occurrences: {outOfRangeOccurrences} created");
        }

        Console.WriteLine($"  Seeding complete: {totalCount} total records");
        Console.WriteLine($"    In-range: {inRangeRecurrences} recurrences, {inRangeOccurrences} occurrences");
        Console.WriteLine($"    Out-of-range: {outOfRangeRecurrences} recurrences, {outOfRangeOccurrences} occurrences");

        return new SeededData(
            totalCount,
            recurrenceCount,
            inRangeRecurrences,
            0,
            0,
            occurrenceCount,
            inRangeOccurrences);
    }

    /// <summary>
    /// Builds an RRule string using Ical.Net RecurrencePattern.
    /// Cycles through Daily, Weekly, Monthly frequencies based on index.
    /// </summary>
    private static string BuildRRule(int index, DateTime untilUtc)
    {
        var pattern = (index % 3) switch
        {
            0 => new RecurrencePattern(FrequencyType.Daily) { Interval = 1 },
            1 => new RecurrencePattern(FrequencyType.Weekly)
            {
                Interval = 1,
                ByDay = [new WeekDay(DayOfWeek.Monday)]
            },
            _ => new RecurrencePattern(FrequencyType.Monthly)
            {
                Interval = 1,
                ByMonthDay = [1]
            }
        };

        // Set UNTIL to the specified date - use CalDateTime for proper conversion
        pattern.Until = new CalDateTime(untilUtc);

        // Convert to string - RecurrencePattern.ToString() gives us the RRULE
        return pattern.ToString() ?? throw new InvalidOperationException("RecurrencePattern.ToString() returned null");
    }

    private static void ReportProgress(int current, int total, ref int lastProgress, string itemType)
    {
        if (total == 0) return;

        var progress = (current + 1) * 100 / total;
        if (progress >= lastProgress + 20)
        {
            Console.WriteLine($"    {itemType}: {progress}% ({current + 1}/{total})");
            lastProgress = progress;
        }
    }

}

/// <summary>
/// Information about seeded data.
/// </summary>
/// <param name="TotalCount">Total number of records seeded.</param>
/// <param name="RecurrenceCount">Number of recurrences seeded.</param>
/// <param name="InRangeRecurrenceCount">Number of recurrences within query range.</param>
/// <param name="OverrideCount">Number of overrides applied.</param>
/// <param name="ExceptionCount">Number of exceptions applied.</param>
/// <param name="OccurrenceCount">Number of standalone occurrences seeded.</param>
/// <param name="InRangeOccurrenceCount">Number of occurrences within query range.</param>
public record SeededData(
    int TotalCount,
    int RecurrenceCount,
    int InRangeRecurrenceCount,
    int OverrideCount,
    int ExceptionCount,
    int OccurrenceCount,
    int InRangeOccurrenceCount);
