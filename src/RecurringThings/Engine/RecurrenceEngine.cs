namespace RecurringThings.Engine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NodaTime;
using RecurringThings.Domain;
using RecurringThings.Models;
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
public sealed class RecurrenceEngine : IRecurrenceEngine
{
    private readonly IRecurrenceRepository _recurrenceRepository;
    private readonly IOccurrenceRepository _occurrenceRepository;
    private readonly IOccurrenceExceptionRepository _exceptionRepository;
    private readonly IOccurrenceOverrideRepository _overrideRepository;
    private readonly IValidator<RecurrenceCreate> _recurrenceCreateValidator;
    private readonly IValidator<OccurrenceCreate> _occurrenceCreateValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurrenceEngine"/> class.
    /// </summary>
    /// <param name="recurrenceRepository">The recurrence repository.</param>
    /// <param name="occurrenceRepository">The occurrence repository.</param>
    /// <param name="exceptionRepository">The exception repository.</param>
    /// <param name="overrideRepository">The override repository.</param>
    /// <param name="recurrenceCreateValidator">The validator for recurrence create requests.</param>
    /// <param name="occurrenceCreateValidator">The validator for occurrence create requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public RecurrenceEngine(
        IRecurrenceRepository recurrenceRepository,
        IOccurrenceRepository occurrenceRepository,
        IOccurrenceExceptionRepository exceptionRepository,
        IOccurrenceOverrideRepository overrideRepository,
        IValidator<RecurrenceCreate> recurrenceCreateValidator,
        IValidator<OccurrenceCreate> occurrenceCreateValidator)
    {
        ArgumentNullException.ThrowIfNull(recurrenceRepository);
        ArgumentNullException.ThrowIfNull(occurrenceRepository);
        ArgumentNullException.ThrowIfNull(exceptionRepository);
        ArgumentNullException.ThrowIfNull(overrideRepository);
        ArgumentNullException.ThrowIfNull(recurrenceCreateValidator);
        ArgumentNullException.ThrowIfNull(occurrenceCreateValidator);

        _recurrenceRepository = recurrenceRepository;
        _occurrenceRepository = occurrenceRepository;
        _exceptionRepository = exceptionRepository;
        _overrideRepository = overrideRepository;
        _recurrenceCreateValidator = recurrenceCreateValidator;
        _occurrenceCreateValidator = occurrenceCreateValidator;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<CalendarEntry> GetAsync(
        string organization,
        string resourcePath,
        DateTime start,
        DateTime end,
        string[]? types,
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
    public async Task<Recurrence> CreateRecurrenceAsync(
        RecurrenceCreate request,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        // Validate the request
        var validationResult = await _recurrenceCreateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        validationResult.ThrowIfInvalid();

        // Convert input time to UTC if it's local
        var startTimeUtc = ConvertToUtc(request.StartTime, request.TimeZone);

        // Extract RecurrenceEndTime from RRule UNTIL clause
        var recurrenceEndTimeUtc = ExtractUntilFromRRule(request.RRule);

        // Create the recurrence entity
        var recurrence = new Recurrence
        {
            Id = Guid.NewGuid(),
            Organization = request.Organization,
            ResourcePath = request.ResourcePath,
            Type = request.Type,
            StartTime = startTimeUtc,
            Duration = request.Duration,
            RecurrenceEndTime = recurrenceEndTimeUtc,
            RRule = request.RRule,
            TimeZone = request.TimeZone,
            Extensions = request.Extensions
        };

        // Persist via repository
        return await _recurrenceRepository.CreateAsync(
            recurrence,
            transactionContext,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Domain.Occurrence> CreateOccurrenceAsync(
        OccurrenceCreate request,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        // Validate the request
        var validationResult = await _occurrenceCreateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        validationResult.ThrowIfInvalid();

        // Convert input time to UTC if it's local
        var startTimeUtc = ConvertToUtc(request.StartTime, request.TimeZone);

        // Create the occurrence entity
        var occurrence = new Domain.Occurrence
        {
            Id = Guid.NewGuid(),
            Organization = request.Organization,
            ResourcePath = request.ResourcePath,
            Type = request.Type,
            TimeZone = request.TimeZone,
            Extensions = request.Extensions
        };

        // Initialize with StartTime and Duration (auto-computes EndTime)
        occurrence.Initialize(startTimeUtc, request.Duration);

        // Persist via repository
        return await _occurrenceRepository.CreateAsync(
            occurrence,
            transactionContext,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates virtualized occurrence times from a recurrence pattern.
    /// </summary>
    private static IEnumerable<DateTime> GenerateOccurrences(Recurrence recurrence, DateTime startUtc, DateTime endUtc)
    {
        // Get the IANA timezone
        var timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(recurrence.TimeZone);
        if (timeZone is null)
        {
            yield break;
        }

        // Convert query range to local time
        var startInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc));
        var endInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(endUtc, DateTimeKind.Utc));

        var startLocal = startInstant.InZone(timeZone).LocalDateTime;
        var endLocal = endInstant.InZone(timeZone).LocalDateTime;

        // Convert recurrence start time to local
        var recurrenceStartInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(recurrence.StartTime, DateTimeKind.Utc));
        var recurrenceStartLocal = recurrenceStartInstant.InZone(timeZone).LocalDateTime;

        // Create a calendar event with the RRule
        var calendar = new Calendar();
        var calendarEvent = new CalendarEvent
        {
            DtStart = new CalDateTime(
                recurrenceStartLocal.Year,
                recurrenceStartLocal.Month,
                recurrenceStartLocal.Day,
                recurrenceStartLocal.Hour,
                recurrenceStartLocal.Minute,
                recurrenceStartLocal.Second,
                recurrence.TimeZone)
        };

        // Parse and add the RRule
        var rrule = new RecurrencePattern(recurrence.RRule);
        calendarEvent.RecurrenceRules.Add(rrule);

        calendar.Events.Add(calendarEvent);

        // Get occurrences in the local time range
        // In Ical.Net 5.x, use GetOccurrences(start) and filter with TakeWhileBefore(end)
        var searchStart = new CalDateTime(startLocal.Year, startLocal.Month, startLocal.Day, 0, 0, 0, recurrence.TimeZone);
        var searchEnd = new CalDateTime(endLocal.Year, endLocal.Month, endLocal.Day, 23, 59, 59, recurrence.TimeZone);

        var icalOccurrences = calendarEvent.GetOccurrences(searchStart).TakeWhileBefore(searchEnd);

        foreach (var icalOccurrence in icalOccurrences)
        {
            var occurrenceDateTime = icalOccurrence.Period.StartTime;

            // Convert back to UTC using NodaTime for correct DST handling
            var localDateTime = new LocalDateTime(
                occurrenceDateTime.Year,
                occurrenceDateTime.Month,
                occurrenceDateTime.Day,
                occurrenceDateTime.Hour,
                occurrenceDateTime.Minute,
                occurrenceDateTime.Second);

            // Handle ambiguous times during DST fall-back
            var zonedDateTime = localDateTime.InZoneLeniently(timeZone);
            var utcDateTime = zonedDateTime.ToDateTimeUtc();

            // Filter to exact query range
            if (utcDateTime >= startUtc && utcDateTime <= endUtc)
            {
                // Also check against RecurrenceEndTime
                if (utcDateTime <= recurrence.RecurrenceEndTime)
                {
                    yield return utcDateTime;
                }
            }
        }
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
            RecurrenceOccurrenceDetails = new RecurrenceOccurrenceDetails
            {
                RecurrenceId = recurrence.Id,
                Original = new OccurrenceOriginal
                {
                    StartTime = occurrenceTimeUtc,
                    Duration = recurrence.Duration,
                    Extensions = recurrence.Extensions
                }
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
            RecurrenceOccurrenceDetails = new RecurrenceOccurrenceDetails
            {
                RecurrenceId = recurrence.Id,
                Original = new OccurrenceOriginal
                {
                    StartTime = @override.OriginalTimeUtc,
                    Duration = @override.OriginalDuration,
                    Extensions = @override.OriginalExtensions
                }
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
            OccurrenceId = occurrence.Id
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
    public async Task<CalendarEntry> UpdateAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Determine entry type and delegate to appropriate handler
        if (entry.OccurrenceId.HasValue)
        {
            // Standalone occurrence
            return await UpdateStandaloneOccurrenceAsync(entry, transactionContext, cancellationToken).ConfigureAwait(false);
        }

        if (entry.RecurrenceOccurrenceDetails is not null)
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

        if (entry.RecurrenceId.HasValue)
        {
            // Recurrence pattern
            return await UpdateRecurrenceAsync(entry, transactionContext, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            "Cannot determine entry type. Entry must have RecurrenceId, OccurrenceId, or RecurrenceOccurrenceDetails set.");
    }

    /// <summary>
    /// Updates a recurrence pattern. Only Duration and Extensions are mutable.
    /// </summary>
    private async Task<CalendarEntry> UpdateRecurrenceAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        var recurrence = await _recurrenceRepository.GetByIdAsync(
            entry.RecurrenceId!.Value,
            entry.Organization,
            entry.ResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException(
                $"Recurrence with ID '{entry.RecurrenceId}' not found.");

        // Validate immutable fields
        ValidateImmutableFields(entry, recurrence);

        // Apply mutable changes
        recurrence.Duration = entry.Duration;
        recurrence.Extensions = entry.Extensions;

        var updated = await _recurrenceRepository.UpdateAsync(
            recurrence,
            transactionContext,
            cancellationToken).ConfigureAwait(false);

        return new CalendarEntry
        {
            Organization = updated.Organization,
            ResourcePath = updated.ResourcePath,
            Type = updated.Type,
            StartTime = updated.StartTime,
            EndTime = updated.StartTime + updated.Duration,
            Duration = updated.Duration,
            TimeZone = updated.TimeZone,
            Extensions = updated.Extensions,
            RecurrenceId = updated.Id,
            RecurrenceDetails = new RecurrenceDetails
            {
                RRule = updated.RRule
            }
        };
    }

    /// <summary>
    /// Validates that immutable fields on a recurrence have not been changed.
    /// </summary>
    private static void ValidateImmutableFields(CalendarEntry entry, Recurrence existing)
    {
        if (entry.Organization != existing.Organization)
        {
            throw new InvalidOperationException(
                "Cannot modify Organization. This field is immutable after creation.");
        }

        if (entry.ResourcePath != existing.ResourcePath)
        {
            throw new InvalidOperationException(
                "Cannot modify ResourcePath. This field is immutable after creation.");
        }

        if (entry.Type != existing.Type)
        {
            throw new InvalidOperationException(
                "Cannot modify Type. This field is immutable after creation.");
        }

        if (entry.TimeZone != existing.TimeZone)
        {
            throw new InvalidOperationException(
                "Cannot modify TimeZone. This field is immutable after creation.");
        }

        if (entry.StartTime != existing.StartTime)
        {
            throw new InvalidOperationException(
                "Cannot modify StartTime on a recurrence. This field is immutable after creation.");
        }

        // Only validate RRule if RecurrenceDetails is provided
        if (entry.RecurrenceDetails is not null)
        {
            if (entry.RecurrenceDetails.RRule != existing.RRule)
            {
                throw new InvalidOperationException(
                    "Cannot modify RRule. This field is immutable after creation.");
            }
        }
    }

    /// <summary>
    /// Updates a standalone occurrence. StartTime, Duration, and Extensions are mutable.
    /// </summary>
    private async Task<CalendarEntry> UpdateStandaloneOccurrenceAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        var occurrence = await _occurrenceRepository.GetByIdAsync(
            entry.OccurrenceId!.Value,
            entry.Organization,
            entry.ResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException(
                $"Occurrence with ID '{entry.OccurrenceId}' not found.");

        // Validate immutable fields
        ValidateImmutableOccurrenceFields(entry, occurrence);

        // Apply mutable changes - setters auto-compute EndTime
        occurrence.StartTime = entry.StartTime;
        occurrence.Duration = entry.Duration;
        occurrence.Extensions = entry.Extensions;

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

        if (entry.ResourcePath != existing.ResourcePath)
        {
            throw new InvalidOperationException(
                "Cannot modify ResourcePath. This field is immutable after creation.");
        }

        if (entry.Type != existing.Type)
        {
            throw new InvalidOperationException(
                "Cannot modify Type. This field is immutable after creation.");
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
        var recurrenceId = entry.RecurrenceOccurrenceDetails!.RecurrenceId;

        var recurrence = await _recurrenceRepository.GetByIdAsync(
            recurrenceId,
            entry.Organization,
            entry.ResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException(
                $"Parent recurrence with ID '{recurrenceId}' not found.");

        // Validate immutable fields against the parent recurrence
        ValidateImmutableVirtualizedFields(entry, recurrence);

        // Get the original time from RecurrenceOccurrenceDetails.Original
        // This is populated by CreateVirtualizedEntry when the entry is first queried
        var originalTime = entry.RecurrenceOccurrenceDetails.Original?.StartTime
            ?? throw new InvalidOperationException(
                "Cannot create override: Original start time is missing from RecurrenceOccurrenceDetails.");

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
        var @override = await _overrideRepository.GetByIdAsync(
            entry.OverrideId!.Value,
            entry.Organization,
            entry.ResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException(
                $"Override with ID '{entry.OverrideId}' not found.");

        // Get parent recurrence for immutability validation
        var recurrence = await _recurrenceRepository.GetByIdAsync(
            @override.RecurrenceId,
            entry.Organization,
            entry.ResourcePath,
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
    public async Task DeleteAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Determine entry type and delegate to appropriate handler
        if (entry.OccurrenceId.HasValue)
        {
            // Standalone occurrence - direct delete
            await DeleteStandaloneOccurrenceAsync(entry, transactionContext, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (entry.RecurrenceOccurrenceDetails is not null)
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

        if (entry.RecurrenceId.HasValue)
        {
            // Recurrence pattern - cascade delete
            await DeleteRecurrenceAsync(entry, transactionContext, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException(
            "Cannot determine entry type. Entry must have RecurrenceId, OccurrenceId, or RecurrenceOccurrenceDetails set.");
    }

    /// <summary>
    /// Deletes a recurrence pattern with cascade delete behavior.
    /// </summary>
    private async Task DeleteRecurrenceAsync(
        CalendarEntry entry,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        // Repository handles cascade delete (PostgreSQL via ON DELETE CASCADE, MongoDB via explicit transaction)
        await _recurrenceRepository.DeleteAsync(
            entry.RecurrenceId!.Value,
            entry.Organization,
            entry.ResourcePath,
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
        // Get the original time from RecurrenceOccurrenceDetails.Original
        var originalTime = entry.RecurrenceOccurrenceDetails!.Original?.StartTime
            ?? throw new InvalidOperationException(
                "Cannot create exception: Original start time is missing from RecurrenceOccurrenceDetails.");

        var exception = new OccurrenceException
        {
            Id = Guid.NewGuid(),
            Organization = entry.Organization,
            ResourcePath = entry.ResourcePath,
            RecurrenceId = entry.RecurrenceOccurrenceDetails.RecurrenceId,
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
        // Get the original time from RecurrenceOccurrenceDetails.Original
        var originalTime = entry.RecurrenceOccurrenceDetails!.Original?.StartTime
            ?? throw new InvalidOperationException(
                "Cannot create exception: Original start time is missing from RecurrenceOccurrenceDetails.");

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
            RecurrenceId = entry.RecurrenceOccurrenceDetails.RecurrenceId,
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
        if (entry.OccurrenceId.HasValue)
        {
            throw new InvalidOperationException(
                "Cannot restore a standalone occurrence. RestoreAsync is only valid for overridden virtualized occurrences.");
        }

        if (entry.RecurrenceId.HasValue && entry.RecurrenceOccurrenceDetails is null)
        {
            throw new InvalidOperationException(
                "Cannot restore a recurrence pattern. RestoreAsync is only valid for overridden virtualized occurrences.");
        }

        if (entry.RecurrenceOccurrenceDetails is null)
        {
            throw new InvalidOperationException(
                "Cannot determine entry type. Entry must have RecurrenceOccurrenceDetails set for RestoreAsync.");
        }

        if (!entry.OverrideId.HasValue)
        {
            throw new InvalidOperationException(
                "Cannot restore a virtualized occurrence without an override. " +
                "RestoreAsync is only valid for occurrences that have been modified (OverrideId must be set).");
        }

        // Delete the override to restore the occurrence to its original virtualized state
        await _overrideRepository.DeleteAsync(
            entry.OverrideId.Value,
            entry.Organization,
            entry.ResourcePath,
            transactionContext,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Extracts the UNTIL value from an RRule string and returns it as a UTC DateTime.
    /// </summary>
    /// <param name="rrule">The RRule string containing an UNTIL clause.</param>
    /// <returns>The UNTIL value as a UTC DateTime.</returns>
    /// <exception cref="ArgumentException">Thrown when the RRule does not contain a valid UNTIL clause.</exception>
    private static DateTime ExtractUntilFromRRule(string rrule)
    {
        var pattern = new RecurrencePattern(rrule);

        if (pattern.Until is null)
        {
            throw new ArgumentException("RRule must contain UNTIL clause.", nameof(rrule));
        }

        // Ical.Net parses UNTIL as UTC when the Z suffix is present
        return DateTime.SpecifyKind(pattern.Until.Value, DateTimeKind.Utc);
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
