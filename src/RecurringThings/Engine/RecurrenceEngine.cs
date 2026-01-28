namespace RecurringThings.Engine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ical.Net.DataTypes;
using NodaTime;
using RecurringThings.Domain;
using RecurringThings.Engine.Virtualization;
using RecurringThings.Models;
using RecurringThings.Options;
using RecurringThings.Repository;
using RecurringThings.Validation;
using Transactional.Abstractions;

/// <summary>
/// Virtualizes recurring occurrences on-demand using Ical.Net and NodaTime.
/// </summary>
/// <remarks>
/// <para>
/// This engine is the core of the RecurringThings library. It:
/// </para>
/// <list type="bullet">
/// <item>Queries recurrences, occurrences, exceptions, and overrides from repositories</item>
/// <item>Converts UTC times to local times using NodaTime for correct DST handling</item>
/// <item>Uses Ical.Net to generate virtualized occurrences from RRule patterns</item>
/// <item>Applies exceptions (cancellations) and overrides (modifications)</item>
/// <item>Streams results as <see cref="CalendarEntry"/> objects</item>
/// </list>
/// </remarks>
internal sealed class RecurrenceEngine : IRecurrenceEngine
{
    private readonly IRecurrenceRepository _recurrenceRepository;
    private readonly IOccurrenceRepository _occurrenceRepository;
    private readonly IOccurrenceExceptionRepository _exceptionRepository;
    private readonly IOccurrenceOverrideRepository _overrideRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurrenceEngine"/> class.
    /// </summary>
    /// <param name="recurrenceRepository">The recurrence repository.</param>
    /// <param name="occurrenceRepository">The occurrence repository.</param>
    /// <param name="exceptionRepository">The exception repository.</param>
    /// <param name="overrideRepository">The override repository.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public RecurrenceEngine(
        IRecurrenceRepository recurrenceRepository,
        IOccurrenceRepository occurrenceRepository,
        IOccurrenceExceptionRepository exceptionRepository,
        IOccurrenceOverrideRepository overrideRepository)
    {
        ArgumentNullException.ThrowIfNull(recurrenceRepository);
        ArgumentNullException.ThrowIfNull(occurrenceRepository);
        ArgumentNullException.ThrowIfNull(exceptionRepository);
        ArgumentNullException.ThrowIfNull(overrideRepository);

        _recurrenceRepository = recurrenceRepository;
        _occurrenceRepository = occurrenceRepository;
        _exceptionRepository = exceptionRepository;
        _overrideRepository = overrideRepository;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<CalendarEntry> GetOccurrencesAsync(
        string organization,
        string resourcePath,
        DateTime start,
        DateTime end,
        string[]? types = null,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Validate that start and end have a specified Kind (UTC or Local, not Unspecified)
        if (start.Kind == DateTimeKind.Unspecified)
        {
            throw new ArgumentException(
                "start must have a specified Kind (Utc or Local). DateTimeKind.Unspecified is not allowed.",
                nameof(start));
        }

        if (end.Kind == DateTimeKind.Unspecified)
        {
            throw new ArgumentException(
                "end must have a specified Kind (Utc or Local). DateTimeKind.Unspecified is not allowed.",
                nameof(end));
        }

        // Convert to UTC if needed (using system timezone for Local times)
        var startUtc = start.Kind == DateTimeKind.Utc ? start : start.ToUniversalTime();
        var endUtc = end.Kind == DateTimeKind.Utc ? end : end.ToUniversalTime();

        // Validate types filter
        Validator.ValidateTypesFilter(types);

        // Query recurrences and standalone occurrences in parallel
        var recurrencesTask = MaterializeAsync(
            _recurrenceRepository.GetInRangeAsync(organization, resourcePath, startUtc, endUtc, types, transactionContext, cancellationToken),
            cancellationToken);

        var occurrencesTask = MaterializeAsync(
            _occurrenceRepository.GetInRangeAsync(organization, resourcePath, startUtc, endUtc, types, transactionContext, cancellationToken),
            cancellationToken);

        await Task.WhenAll(recurrencesTask, occurrencesTask).ConfigureAwait(false);

        var recurrences = recurrencesTask.Result;
        var occurrences = occurrencesTask.Result;

        // Query exceptions and overrides for the recurrences found
        var recurrenceIds = recurrences.Select(r => r.Id).ToList();

        Dictionary<Guid, List<OccurrenceException>> exceptionsByRecurrenceId;
        Dictionary<Guid, List<OccurrenceOverride>> overridesByRecurrenceId;

        if (recurrenceIds.Count > 0)
        {
            var exceptionsTask = MaterializeAsync(
                _exceptionRepository.GetByRecurrenceIdsAsync(organization, resourcePath, recurrenceIds, transactionContext, cancellationToken),
                cancellationToken);

            var overridesTask = MaterializeAsync(
                _overrideRepository.GetInRangeAsync(organization, resourcePath, recurrenceIds, startUtc, endUtc, transactionContext, cancellationToken),
                cancellationToken);

            await Task.WhenAll(exceptionsTask, overridesTask).ConfigureAwait(false);

            exceptionsByRecurrenceId = exceptionsTask.Result
                .GroupBy(e => e.RecurrenceId)
                .ToDictionary(g => g.Key, g => g.ToList());

            overridesByRecurrenceId = overridesTask.Result
                .GroupBy(o => o.RecurrenceId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        else
        {
            exceptionsByRecurrenceId = [];
            overridesByRecurrenceId = [];
        }

        // Process recurrences and generate virtualized occurrences
        foreach (var recurrence in recurrences)
        {
            var exceptions = exceptionsByRecurrenceId.GetValueOrDefault(recurrence.Id, []);
            var overrides = overridesByRecurrenceId.GetValueOrDefault(recurrence.Id, []);

            // Build lookup sets for efficient checking
            var exceptionTimes = exceptions
                .Select(e => e.OriginalTimeUtc)
                .ToHashSet();

            var overridesByOriginalTime = overrides
                .ToDictionary(o => o.OriginalTimeUtc);

            // Generate virtualized occurrences
            var virtualizedTimes = GenerateOccurrences(recurrence, startUtc, endUtc);

            foreach (var occurrenceTimeUtc in virtualizedTimes)
            {
                // Skip if this occurrence has an exception (cancelled)
                if (exceptionTimes.Contains(occurrenceTimeUtc))
                {
                    continue;
                }

                // Check if there's an override for this occurrence
                if (overridesByOriginalTime.TryGetValue(occurrenceTimeUtc, out var @override))
                {
                    // Skip override if it was moved completely outside the query range
                    if (@override.EndTime < startUtc || @override.StartTime > endUtc)
                    {
                        continue;
                    }

                    yield return CreateOverriddenEntry(recurrence, @override);
                }
                else
                {
                    yield return CreateVirtualizedEntry(recurrence, occurrenceTimeUtc);
                }
            }

            // Include overrides that were moved INTO the query range from outside
            foreach (var @override in overrides)
            {
                // Skip if the original time is in the query range (already processed above)
                if (@override.OriginalTimeUtc >= startUtc && @override.OriginalTimeUtc <= endUtc)
                {
                    continue;
                }

                // Skip if the override time is outside the query range
                if (@override.EndTime < startUtc || @override.StartTime > endUtc)
                {
                    continue;
                }

                // This override was moved from outside the range into the range
                yield return CreateOverriddenEntry(recurrence, @override);
            }
        }

        // Yield standalone occurrences
        foreach (var occurrence in occurrences)
        {
            yield return CreateStandaloneEntry(occurrence);
        }
    }

    /// <inheritdoc/>
    public async Task<CalendarEntry> CreateRecurrenceAsync(
        string organization,
        string resourcePath,
        string type,
        DateTime startTime,
        TimeSpan duration,
        string rrule,
        string timeZone,
        Dictionary<string, string>? extensions = null,
        CreateRecurrenceOptions? options = null,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        Validator.ValidateRecurrenceCreate(organization, resourcePath, type, startTime, duration, rrule, timeZone, extensions);

        // Convert input time to UTC if it's local
        var startTimeUtc = ConvertToUtc(startTime, timeZone);

        // Parse RRule ONCE and reuse
        var pattern = new RecurrencePattern(rrule);

        // Extract RecurrenceEndTime from RRule UNTIL clause
        var recurrenceEndTimeUtc = pattern.Until is not null
            ? DateTime.SpecifyKind(pattern.Until.Value, DateTimeKind.Utc)
            : throw new ArgumentException("RRule must contain UNTIL clause.", nameof(rrule));

        // Validate monthly day bounds - returns strategy or null, throws if Throw strategy
        var monthDayBehavior = Validator.ValidateMonthlyDayBounds(pattern, startTimeUtc, options);

        // Create the recurrence entity
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = organization,
            ResourcePath = resourcePath,
            Type = type,
            StartTime = startTimeUtc,
            Duration = duration,
            RecurrenceEndTime = recurrenceEndTimeUtc,
            RRule = rrule,
            TimeZone = timeZone,
            Extensions = extensions,
            MonthDayBehavior = monthDayBehavior
        };

        // Persist via repository
        var created = await _recurrenceRepository.CreateAsync(
            recurrence,
            transactionContext,
            cancellationToken).ConfigureAwait(false);

        // Convert to CalendarEntry
        return CreateRecurrenceEntry(created);
    }

    /// <inheritdoc/>
    public async Task<CalendarEntry> CreateOccurrenceAsync(
        string organization,
        string resourcePath,
        string type,
        DateTime startTime,
        TimeSpan duration,
        string timeZone,
        Dictionary<string, string>? extensions = null,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        // Validate parameters
        Validator.ValidateOccurrenceCreate(organization, resourcePath, type, startTime, duration, timeZone, extensions);

        // Convert input time to UTC if it's local
        var startTimeUtc = ConvertToUtc(startTime, timeZone);

        // Create the occurrence entity
        var occurrence = new Domain.Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = organization,
            ResourcePath = resourcePath,
            Type = type,
            TimeZone = timeZone,
            Extensions = extensions
        };

        // Initialize with StartTime and Duration (auto-computes EndTime)
        occurrence.Initialize(startTimeUtc, duration);

        // Persist via repository
        var created = await _occurrenceRepository.CreateAsync(
            occurrence,
            transactionContext,
            cancellationToken).ConfigureAwait(false);

        // Convert to CalendarEntry
        return CreateStandaloneEntry(created);
    }

    /// <summary>
    /// Generates virtualized occurrence times from a recurrence pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method delegates to the appropriate occurrence generator based on the
    /// recurrence's <see cref="Recurrence.MonthDayBehavior"/> setting:
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="MonthDayOutOfBoundsStrategy.Clamp"/> uses <see cref="ClampedMonthlyOccurrenceGenerator"/></item>
    /// <item>All other cases (null, Skip) use <see cref="IcalNetOccurrenceGenerator"/></item>
    /// </list>
    /// </remarks>
    private static IEnumerable<DateTime> GenerateOccurrences(Recurrence recurrence, DateTime startUtc, DateTime endUtc)
    {
        // Parse RRule ONCE and reuse
        var pattern = new RecurrencePattern(recurrence.RRule);

        // Get the appropriate generator based on MonthDayBehavior
        var generator = OccurrenceGeneratorFactory.GetGenerator(recurrence);

        return generator.GenerateOccurrences(recurrence, pattern, startUtc, endUtc);
    }

    /// <summary>
    /// Creates a CalendarEntry for a virtualized occurrence (no override).
    /// </summary>
    private static CalendarEntry CreateVirtualizedEntry(Recurrence recurrence, DateTime occurrenceTimeUtc)
    {
        var startTimeLocal = ConvertToLocal(occurrenceTimeUtc, recurrence.TimeZone);
        var endTimeUtc = occurrenceTimeUtc + recurrence.Duration;
        var endTimeLocal = ConvertToLocal(endTimeUtc, recurrence.TimeZone);

        return new CalendarEntry
        {
            Organization = recurrence.Organization,
            ResourcePath = recurrence.ResourcePath,
            Type = recurrence.Type,
            StartTime = startTimeLocal,
            EndTime = endTimeLocal,
            Duration = recurrence.Duration,
            TimeZone = recurrence.TimeZone,
            Extensions = recurrence.Extensions,
            RecurrenceId = recurrence.Id,
            EntryType = CalendarEntryType.Virtualized,
            RecurrenceDetails = new RecurrenceDetails
            {
                RRule = recurrence.RRule,
                MonthDayBehavior = recurrence.MonthDayBehavior
            },
            Original = new OriginalDetails
            {
                StartTime = occurrenceTimeUtc,
                Duration = recurrence.Duration,
                Extensions = recurrence.Extensions
            }
        };
    }

    /// <summary>
    /// Creates a CalendarEntry for an overridden virtualized occurrence.
    /// </summary>
    private static CalendarEntry CreateOverriddenEntry(Recurrence recurrence, OccurrenceOverride @override)
    {
        var startTimeLocal = ConvertToLocal(@override.StartTime, recurrence.TimeZone);
        var endTimeLocal = ConvertToLocal(@override.EndTime, recurrence.TimeZone);

        return new CalendarEntry
        {
            Organization = recurrence.Organization,
            ResourcePath = recurrence.ResourcePath,
            Type = recurrence.Type,
            StartTime = startTimeLocal,
            EndTime = endTimeLocal,
            Duration = @override.Duration,
            TimeZone = recurrence.TimeZone,
            Extensions = @override.Extensions,
            RecurrenceId = recurrence.Id,
            OverrideId = @override.Id,
            EntryType = CalendarEntryType.Virtualized,
            RecurrenceDetails = new RecurrenceDetails
            {
                RRule = recurrence.RRule,
                MonthDayBehavior = recurrence.MonthDayBehavior
            },
            Original = new OriginalDetails
            {
                StartTime = @override.OriginalTimeUtc,
                Duration = @override.OriginalDuration,
                Extensions = @override.OriginalExtensions
            }
        };
    }

    /// <summary>
    /// Creates a CalendarEntry for a standalone occurrence.
    /// </summary>
    private static CalendarEntry CreateStandaloneEntry(Domain.Occurrence occurrence)
    {
        var startTimeLocal = ConvertToLocal(occurrence.StartTime, occurrence.TimeZone);
        var endTimeLocal = ConvertToLocal(occurrence.EndTime, occurrence.TimeZone);

        return new CalendarEntry
        {
            Organization = occurrence.Organization,
            ResourcePath = occurrence.ResourcePath,
            Type = occurrence.Type,
            StartTime = startTimeLocal,
            EndTime = endTimeLocal,
            Duration = occurrence.Duration,
            TimeZone = occurrence.TimeZone,
            Extensions = occurrence.Extensions,
            OccurrenceId = occurrence.Id,
            EntryType = CalendarEntryType.Standalone,
            Original = null
        };
    }

    /// <summary>
    /// Creates a CalendarEntry for a recurrence pattern.
    /// </summary>
    private static CalendarEntry CreateRecurrenceEntry(Recurrence recurrence)
    {
        var startTimeLocal = ConvertToLocal(recurrence.StartTime, recurrence.TimeZone);
        var endTimeLocal = ConvertToLocal(recurrence.StartTime + recurrence.Duration, recurrence.TimeZone);

        return new CalendarEntry
        {
            Organization = recurrence.Organization,
            ResourcePath = recurrence.ResourcePath,
            Type = recurrence.Type,
            StartTime = startTimeLocal,
            EndTime = endTimeLocal,
            Duration = recurrence.Duration,
            TimeZone = recurrence.TimeZone,
            Extensions = recurrence.Extensions,
            RecurrenceId = recurrence.Id,
            EntryType = CalendarEntryType.Recurrence,
            RecurrenceDetails = new RecurrenceDetails
            {
                RRule = recurrence.RRule,
                MonthDayBehavior = recurrence.MonthDayBehavior
            },
            Original = null
        };
    }

    /// <summary>
    /// Materializes an async enumerable to a list.
    /// </summary>
    private static async Task<List<T>> MaterializeAsync<T>(
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken)
    {
        var result = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            result.Add(item);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<CalendarEntry> UpdateOccurrenceAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Block recurrence updates
        if (entry.EntryType == CalendarEntryType.Recurrence)
        {
            throw new InvalidOperationException(
                "Cannot update a recurrence pattern. Delete and recreate the recurrence instead.");
        }

        // Determine entry type and delegate to appropriate handler
        if (entry.OccurrenceId.HasValue)
        {
            // Standalone occurrence
            return await UpdateStandaloneOccurrenceAsync(entry, transactionContext, cancellationToken).ConfigureAwait(false);
        }

        if (entry.EntryType == CalendarEntryType.Virtualized)
        {
            // Virtualized occurrence (from recurrence)
            if (entry.OverrideId.HasValue)
            {
                // Has existing override - update it
                return await UpdateVirtualizedOccurrenceWithOverrideAsync(entry, transactionContext, cancellationToken).ConfigureAwait(false);
            }

            // No override yet - create one
            return await CreateOverrideForVirtualizedOccurrenceAsync(entry, transactionContext, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            "Cannot determine entry type. Entry must have OccurrenceId set (standalone) or EntryType set to Virtualized.");
    }

    /// <summary>
    /// Updates a standalone occurrence. StartTime, Duration, Extensions, Type, and ResourcePath are mutable.
    /// </summary>
    private async Task<CalendarEntry> UpdateStandaloneOccurrenceAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        // Use OriginalResourcePath if ResourcePath was modified
        var lookupResourcePath = entry.OriginalResourcePath ?? entry.ResourcePath;

        var occurrence = await _occurrenceRepository.GetByIdAsync(
            entry.OccurrenceId!.Value,
            entry.Organization,
            lookupResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException(
                $"Occurrence with ID '{entry.OccurrenceId}' not found.");

        // Validate immutable fields
        ValidateImmutableOccurrenceFields(entry, occurrence);

        // Apply mutable changes - setters auto-compute EndTime
        occurrence.StartTime = entry.StartTime;
        occurrence.Duration = entry.Duration;
        occurrence.Extensions = entry.Extensions;
        occurrence.Type = entry.Type;
        occurrence.ResourcePath = entry.ResourcePath;

        var updated = await _occurrenceRepository.UpdateAsync(
            occurrence,
            transactionContext,
            cancellationToken).ConfigureAwait(false);

        return CreateStandaloneEntry(updated);
    }

    /// <summary>
    /// Validates that immutable fields on a standalone occurrence have not been changed.
    /// </summary>
    private static void ValidateImmutableOccurrenceFields(CalendarEntry entry, Domain.Occurrence existing)
    {
        if (entry.Organization != existing.Organization)
        {
            throw new InvalidOperationException(
                "Cannot modify Organization. This field is immutable after creation.");
        }

        if (entry.TimeZone != existing.TimeZone)
        {
            throw new InvalidOperationException(
                "Cannot modify TimeZone. This field is immutable after creation.");
        }
    }

    /// <summary>
    /// Creates a new override for a virtualized occurrence that has not been overridden before.
    /// </summary>
    private async Task<CalendarEntry> CreateOverrideForVirtualizedOccurrenceAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        var recurrenceId = entry.RecurrenceId
            ?? throw new InvalidOperationException("Cannot create override: RecurrenceId is missing.");

        // Use OriginalResourcePath if ResourcePath was modified (though this should fail validation)
        var lookupResourcePath = entry.OriginalResourcePath ?? entry.ResourcePath;

        var recurrence = await _recurrenceRepository.GetByIdAsync(
            recurrenceId,
            entry.Organization,
            lookupResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException(
                $"Parent recurrence with ID '{recurrenceId}' not found.");

        // Validate immutable fields against the parent recurrence
        ValidateImmutableVirtualizedFields(entry, recurrence);

        // Get the original time from Original.StartTime
        // This is populated by CreateVirtualizedEntry when the entry is first queried
        var originalTime = entry.Original?.StartTime
            ?? throw new InvalidOperationException(
                "Cannot create override: Original start time is missing.");

        // Create new override with denormalized original values
        var newOverride = new OccurrenceOverride
        {
            Id = Guid.NewGuid(),
            Organization = recurrence.Organization,
            ResourcePath = recurrence.ResourcePath,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = originalTime,
            OriginalDuration = recurrence.Duration,
            OriginalExtensions = recurrence.Extensions is not null
                ? new Dictionary<string, string>(recurrence.Extensions)
                : null,
            Extensions = entry.Extensions
        };

        // Initialize with new values (auto-computes EndTime)
        newOverride.Initialize(entry.StartTime, entry.Duration);

        var created = await _overrideRepository.CreateAsync(
            newOverride,
            transactionContext,
            cancellationToken).ConfigureAwait(false);

        return CreateOverriddenEntry(recurrence, created);
    }

    /// <summary>
    /// Updates an existing override for a virtualized occurrence.
    /// </summary>
    private async Task<CalendarEntry> UpdateVirtualizedOccurrenceWithOverrideAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        // Use OriginalResourcePath if ResourcePath was modified (though this should fail validation)
        var lookupResourcePath = entry.OriginalResourcePath ?? entry.ResourcePath;

        var @override = await _overrideRepository.GetByIdAsync(
            entry.OverrideId!.Value,
            entry.Organization,
            lookupResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException(
                $"Override with ID '{entry.OverrideId}' not found.");

        // Get parent recurrence for immutability validation
        var recurrence = await _recurrenceRepository.GetByIdAsync(
            @override.RecurrenceId,
            entry.Organization,
            lookupResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException(
                $"Parent recurrence with ID '{@override.RecurrenceId}' not found.");

        // Validate immutable fields
        ValidateImmutableVirtualizedFields(entry, recurrence);

        // Apply mutable changes - setters auto-compute EndTime
        @override.StartTime = entry.StartTime;
        @override.Duration = entry.Duration;
        @override.Extensions = entry.Extensions;

        var updated = await _overrideRepository.UpdateAsync(
            @override,
            transactionContext,
            cancellationToken).ConfigureAwait(false);

        return CreateOverriddenEntry(recurrence, updated);
    }

    /// <summary>
    /// Validates that immutable fields on a virtualized occurrence have not been changed.
    /// </summary>
    private static void ValidateImmutableVirtualizedFields(CalendarEntry entry, Recurrence parentRecurrence)
    {
        if (entry.Organization != parentRecurrence.Organization)
        {
            throw new InvalidOperationException(
                "Cannot modify Organization. This field is immutable after creation.");
        }

        if (entry.ResourcePath != parentRecurrence.ResourcePath)
        {
            throw new InvalidOperationException(
                "Cannot modify ResourcePath. This field is immutable after creation.");
        }

        if (entry.Type != parentRecurrence.Type)
        {
            throw new InvalidOperationException(
                "Cannot modify Type. This field is inherited from the parent recurrence.");
        }

        if (entry.TimeZone != parentRecurrence.TimeZone)
        {
            throw new InvalidOperationException(
                "Cannot modify TimeZone. This field is inherited from the parent recurrence.");
        }
    }

    /// <inheritdoc/>
    public async Task DeleteOccurrenceAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Block recurrence deletion - use DeleteRecurrenceAsync instead
        if (entry.EntryType == CalendarEntryType.Recurrence)
        {
            throw new InvalidOperationException(
                "Cannot delete a recurrence pattern using DeleteOccurrenceAsync. Use DeleteRecurrenceAsync instead.");
        }

        // Determine entry type and delegate to appropriate handler
        if (entry.OccurrenceId.HasValue)
        {
            // Standalone occurrence - direct delete
            await DeleteStandaloneOccurrenceAsync(entry, transactionContext, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (entry.EntryType == CalendarEntryType.Virtualized)
        {
            // Virtualized occurrence (from recurrence)
            if (entry.OverrideId.HasValue)
            {
                // Has existing override - delete override and create exception
                await DeleteVirtualizedOccurrenceWithOverrideAsync(entry, transactionContext, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // No override - create exception to cancel the occurrence
                await CreateExceptionForVirtualizedOccurrenceAsync(entry, transactionContext, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        throw new InvalidOperationException(
            "Cannot determine entry type. Entry must have OccurrenceId set (standalone) or EntryType set to Virtualized.");
    }

    /// <inheritdoc/>
    public async Task DeleteRecurrenceAsync(
        string organization,
        string resourcePath,
        Guid recurrenceId,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        // Verify recurrence exists and belongs to organization/resourcePath
        var recurrence = await _recurrenceRepository.GetByIdAsync(
            recurrenceId,
            organization,
            resourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException(
                $"Recurrence with ID '{recurrenceId}' not found.");

        // Delete cascade: exceptions, overrides, then recurrence
        await _exceptionRepository.DeleteByRecurrenceIdAsync(
            recurrenceId,
            organization,
            resourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false);

        await _overrideRepository.DeleteByRecurrenceIdAsync(
            recurrenceId,
            organization,
            resourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false);

        await _recurrenceRepository.DeleteAsync(
            recurrenceId,
            organization,
            resourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a standalone occurrence.
    /// </summary>
    private async Task DeleteStandaloneOccurrenceAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        await _occurrenceRepository.DeleteAsync(
            entry.OccurrenceId!.Value,
            entry.Organization,
            entry.ResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an exception to cancel a virtualized occurrence that has no override.
    /// </summary>
    private async Task CreateExceptionForVirtualizedOccurrenceAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        // Get the original time from Original.StartTime
        var originalTime = entry.Original?.StartTime
            ?? throw new InvalidOperationException(
                "Cannot create exception: Original start time is missing.");

        var recurrenceId = entry.RecurrenceId
            ?? throw new InvalidOperationException("Cannot create exception: RecurrenceId is missing.");

        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = entry.Organization,
            ResourcePath = entry.ResourcePath,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = originalTime
        };

        await _exceptionRepository.CreateAsync(
            exception,
            transactionContext,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an override and creates an exception at the original time.
    /// </summary>
    private async Task DeleteVirtualizedOccurrenceWithOverrideAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        // Get the original time from Original.StartTime
        var originalTime = entry.Original?.StartTime
            ?? throw new InvalidOperationException(
                "Cannot create exception: Original start time is missing.");

        var recurrenceId = entry.RecurrenceId
            ?? throw new InvalidOperationException("Cannot create exception: RecurrenceId is missing.");

        // Delete the override
        await _overrideRepository.DeleteAsync(
            entry.OverrideId!.Value,
            entry.Organization,
            entry.ResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false);

        // Create exception at the original time (not the overridden time)
        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = entry.Organization,
            ResourcePath = entry.ResourcePath,
            RecurrenceId = recurrenceId,
            OriginalTimeUtc = originalTime
        };

        await _exceptionRepository.CreateAsync(
            exception,
            transactionContext,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RestoreAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Validate entry type - RestoreAsync is only valid for overridden virtualized occurrences
        if (entry.EntryType == CalendarEntryType.Standalone || entry.OccurrenceId.HasValue)
        {
            throw new InvalidOperationException(
                "Cannot restore a standalone occurrence. RestoreAsync is only valid for overridden virtualized occurrences.");
        }

        if (entry.EntryType == CalendarEntryType.Recurrence)
        {
            throw new InvalidOperationException(
                "Cannot restore a recurrence pattern. RestoreAsync is only valid for overridden virtualized occurrences.");
        }

        if (entry.EntryType != CalendarEntryType.Virtualized)
        {
            throw new InvalidOperationException(
                "Cannot determine entry type. Entry must have EntryType set to Virtualized for RestoreAsync.");
        }

        if (!entry.IsOverridden)
        {
            throw new InvalidOperationException(
                "Cannot restore a virtualized occurrence without an override. " +
                "RestoreAsync is only valid for occurrences that have been modified (IsOverridden must be true).");
        }

        // Delete the override to restore the occurrence to its original virtualized state
        await _overrideRepository.DeleteAsync(
            entry.OverrideId!.Value,
            entry.Organization,
            entry.ResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<CalendarEntry> GetRecurrencesAsync(
        string organization,
        string resourcePath,
        DateTime start,
        DateTime end,
        string[]? types = null,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Validate that start and end have a specified Kind (UTC or Local, not Unspecified)
        if (start.Kind == DateTimeKind.Unspecified)
        {
            throw new ArgumentException(
                "start must have a specified Kind (Utc or Local). DateTimeKind.Unspecified is not allowed.",
                nameof(start));
        }

        if (end.Kind == DateTimeKind.Unspecified)
        {
            throw new ArgumentException(
                "end must have a specified Kind (Utc or Local). DateTimeKind.Unspecified is not allowed.",
                nameof(end));
        }

        // Convert to UTC if needed (using system timezone for Local times)
        var startUtc = start.Kind == DateTimeKind.Utc ? start : start.ToUniversalTime();
        var endUtc = end.Kind == DateTimeKind.Utc ? end : end.ToUniversalTime();

        // Validate types filter
        Validator.ValidateTypesFilter(types);

        // Get recurrences from repository
        var recurrences = _recurrenceRepository.GetInRangeAsync(
            organization, resourcePath, startUtc, endUtc, types, transactionContext, cancellationToken);

        await foreach (var recurrence in recurrences.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return CreateRecurrenceEntry(recurrence);
        }
    }

    /// <summary>
    /// Converts a DateTime to UTC. If already UTC, returns as-is. If Local, converts using the specified timezone.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <param name="timeZone">The IANA timezone to use for conversion if the DateTime is Local.</param>
    /// <returns>The DateTime in UTC.</returns>
    private static DateTime ConvertToUtc(DateTime dateTime, string timeZone)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime;
        }

        var tz = DateTimeZoneProviders.Tzdb[timeZone];
        var localDateTime = LocalDateTime.FromDateTime(dateTime);
        var zonedDateTime = localDateTime.InZoneLeniently(tz);
        return zonedDateTime.ToDateTimeUtc();
    }

    /// <summary>
    /// Converts a UTC DateTime to local time in the specified timezone.
    /// </summary>
    /// <param name="utcDateTime">The UTC DateTime to convert.</param>
    /// <param name="timeZone">The IANA timezone for the local time.</param>
    /// <returns>The DateTime in local time (with DateTimeKind.Unspecified).</returns>
    private static DateTime ConvertToLocal(DateTime utcDateTime, string timeZone)
    {
        var tz = DateTimeZoneProviders.Tzdb[timeZone];
        var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc));
        var zonedDateTime = instant.InZone(tz);
        return zonedDateTime.LocalDateTime.ToDateTimeUnspecified();
    }
}
