namespace RecurringThings.MongoDB.Documents;

using System;
using System.Collections.Generic;
using global::MongoDB.Bson;
using global::MongoDB.Bson.Serialization.Attributes;
using RecurringThings.Domain;

/// <summary>
/// MongoDB document model for storing recurrences, occurrences, exceptions, and overrides in a single collection.
/// </summary>
/// <remarks>
/// <para>
/// Uses a discriminator field to distinguish between entity types:
/// - "recurrence": A recurring event pattern
/// - "occurrence": A standalone (non-recurring) event
/// - "exception": A cancelled occurrence from a recurrence
/// - "override": A modified occurrence from a recurrence
/// </para>
/// <para>
/// Fields are mapped to camelCase for MongoDB conventions.
/// </para>
/// </remarks>
public sealed class RecurringThingDocument
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the document type discriminator.
    /// </summary>
    /// <remarks>
    /// Valid values: "recurrence", "occurrence", "exception", "override"
    /// </remarks>
    [BsonElement("documentType")]
    public required string DocumentType { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier for multi-tenant isolation.
    /// </summary>
    [BsonElement("organization")]
    public required string Organization { get; set; }

    /// <summary>
    /// Gets or sets the hierarchical resource scope.
    /// </summary>
    [BsonElement("resourcePath")]
    public required string ResourcePath { get; set; }

    /// <summary>
    /// Gets or sets the user-defined type.
    /// </summary>
    /// <remarks>
    /// Present on recurrences and occurrences. Null for exceptions and overrides.
    /// </remarks>
    [BsonElement("type")]
    [BsonIgnoreIfNull]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the UTC start time.
    /// </summary>
    /// <remarks>
    /// Present on recurrences, occurrences, and overrides. Null for exceptions.
    /// </remarks>
    [BsonElement("startTime")]
    [BsonIgnoreIfNull]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the UTC end time.
    /// </summary>
    /// <remarks>
    /// Present on occurrences and overrides. Null for recurrences and exceptions.
    /// Computed as StartTime + Duration.
    /// </remarks>
    [BsonElement("endTime")]
    [BsonIgnoreIfNull]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    /// <remarks>
    /// Stored as milliseconds for efficient MongoDB operations.
    /// Present on recurrences, occurrences, and overrides. Null for exceptions.
    /// </remarks>
    [BsonElement("durationMs")]
    [BsonIgnoreIfNull]
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the IANA time zone identifier.
    /// </summary>
    /// <remarks>
    /// Present on recurrences and occurrences. Null for exceptions and overrides.
    /// </remarks>
    [BsonElement("timeZone")]
    [BsonIgnoreIfNull]
    public string? TimeZone { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the recurrence series ends.
    /// </summary>
    /// <remarks>
    /// Present only on recurrences.
    /// </remarks>
    [BsonElement("recurrenceEndTime")]
    [BsonIgnoreIfNull]
    public DateTime? RecurrenceEndTime { get; set; }

    /// <summary>
    /// Gets or sets the RFC 5545 recurrence rule.
    /// </summary>
    /// <remarks>
    /// Present only on recurrences.
    /// </remarks>
    [BsonElement("rrule")]
    [BsonIgnoreIfNull]
    public string? RRule { get; set; }

    /// <summary>
    /// Gets or sets the parent recurrence identifier.
    /// </summary>
    /// <remarks>
    /// Present on exceptions and overrides.
    /// </remarks>
    [BsonElement("recurrenceId")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public Guid? RecurrenceId { get; set; }

    /// <summary>
    /// Gets or sets the original UTC timestamp of the occurrence being modified or cancelled.
    /// </summary>
    /// <remarks>
    /// Present on exceptions and overrides.
    /// </remarks>
    [BsonElement("originalTimeUtc")]
    [BsonIgnoreIfNull]
    public DateTime? OriginalTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the original duration in milliseconds.
    /// </summary>
    /// <remarks>
    /// Denormalized from the parent recurrence at creation time.
    /// Present only on overrides.
    /// </remarks>
    [BsonElement("originalDurationMs")]
    [BsonIgnoreIfNull]
    public long? OriginalDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the original extensions from the parent recurrence.
    /// </summary>
    /// <remarks>
    /// Denormalized from the parent recurrence at creation time.
    /// Present only on overrides.
    /// </remarks>
    [BsonElement("originalExtensions")]
    [BsonIgnoreIfNull]
    public Dictionary<string, string>? OriginalExtensions { get; set; }

    /// <summary>
    /// Gets or sets the user-defined key-value metadata.
    /// </summary>
    [BsonElement("extensions")]
    [BsonIgnoreIfNull]
    public Dictionary<string, string>? Extensions { get; set; }
}

/// <summary>
/// Document type discriminator values for <see cref="RecurringThingDocument"/>.
/// </summary>
public static class DocumentTypes
{
    /// <summary>
    /// Indicates the document is a recurrence pattern.
    /// </summary>
    public const string Recurrence = "recurrence";

    /// <summary>
    /// Indicates the document is a standalone occurrence.
    /// </summary>
    public const string Occurrence = "occurrence";

    /// <summary>
    /// Indicates the document is an occurrence exception (cancellation).
    /// </summary>
    public const string Exception = "exception";

    /// <summary>
    /// Indicates the document is an occurrence override (modification).
    /// </summary>
    public const string Override = "override";
}

/// <summary>
/// Provides mapping methods between domain entities and MongoDB documents.
/// </summary>
internal static class DocumentMapper
{
    /// <summary>
    /// Converts a <see cref="RecurringThingDocument"/> to a <see cref="Recurrence"/>.
    /// </summary>
    /// <param name="document">The document to convert.</param>
    /// <returns>The mapped recurrence entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the document is not a recurrence.</exception>
    public static Recurrence ToRecurrence(RecurringThingDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.DocumentType != DocumentTypes.Recurrence)
        {
            throw new InvalidOperationException(
                $"Cannot convert document of type '{document.DocumentType}' to Recurrence.");
        }

        return new Recurrence
        {
            Id = document.Id,
            Organization = document.Organization,
            ResourcePath = document.ResourcePath,
            Type = document.Type!,
            StartTime = document.StartTime!.Value,
            Duration = TimeSpan.FromMilliseconds(document.DurationMs!.Value),
            RecurrenceEndTime = document.RecurrenceEndTime!.Value,
            RRule = document.RRule!,
            TimeZone = document.TimeZone!,
            Extensions = document.Extensions
        };
    }

    /// <summary>
    /// Converts a <see cref="RecurringThingDocument"/> to an <see cref="Occurrence"/>.
    /// </summary>
    /// <param name="document">The document to convert.</param>
    /// <returns>The mapped occurrence entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the document is not an occurrence.</exception>
    public static Occurrence ToOccurrence(RecurringThingDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.DocumentType != DocumentTypes.Occurrence)
        {
            throw new InvalidOperationException(
                $"Cannot convert document of type '{document.DocumentType}' to Occurrence.");
        }

        var occurrence = new Occurrence
        {
            Id = document.Id,
            Organization = document.Organization,
            ResourcePath = document.ResourcePath,
            Type = document.Type!,
            TimeZone = document.TimeZone!,
            Extensions = document.Extensions
        };

        occurrence.Initialize(
            document.StartTime!.Value,
            TimeSpan.FromMilliseconds(document.DurationMs!.Value));

        return occurrence;
    }

    /// <summary>
    /// Converts a <see cref="RecurringThingDocument"/> to an <see cref="OccurrenceException"/>.
    /// </summary>
    /// <param name="document">The document to convert.</param>
    /// <returns>The mapped occurrence exception entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the document is not an exception.</exception>
    public static OccurrenceException ToOccurrenceException(RecurringThingDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.DocumentType != DocumentTypes.Exception)
        {
            throw new InvalidOperationException(
                $"Cannot convert document of type '{document.DocumentType}' to OccurrenceException.");
        }

        return new OccurrenceException
        {
            Id = document.Id,
            Organization = document.Organization,
            ResourcePath = document.ResourcePath,
            RecurrenceId = document.RecurrenceId!.Value,
            OriginalTimeUtc = document.OriginalTimeUtc!.Value,
            Extensions = document.Extensions
        };
    }

    /// <summary>
    /// Converts a <see cref="RecurringThingDocument"/> to an <see cref="OccurrenceOverride"/>.
    /// </summary>
    /// <param name="document">The document to convert.</param>
    /// <returns>The mapped occurrence override entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the document is not an override.</exception>
    public static OccurrenceOverride ToOccurrenceOverride(RecurringThingDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.DocumentType != DocumentTypes.Override)
        {
            throw new InvalidOperationException(
                $"Cannot convert document of type '{document.DocumentType}' to OccurrenceOverride.");
        }

        var @override = new OccurrenceOverride
        {
            Id = document.Id,
            Organization = document.Organization,
            ResourcePath = document.ResourcePath,
            RecurrenceId = document.RecurrenceId!.Value,
            OriginalTimeUtc = document.OriginalTimeUtc!.Value,
            OriginalDuration = TimeSpan.FromMilliseconds(document.OriginalDurationMs!.Value),
            OriginalExtensions = document.OriginalExtensions,
            Extensions = document.Extensions
        };

        @override.Initialize(
            document.StartTime!.Value,
            TimeSpan.FromMilliseconds(document.DurationMs!.Value));

        return @override;
    }

    /// <summary>
    /// Converts a <see cref="Recurrence"/> to a <see cref="RecurringThingDocument"/>.
    /// </summary>
    /// <param name="recurrence">The recurrence to convert.</param>
    /// <returns>The mapped document.</returns>
    public static RecurringThingDocument FromRecurrence(Recurrence recurrence)
    {
        ArgumentNullException.ThrowIfNull(recurrence);

        return new RecurringThingDocument
        {
            Id = recurrence.Id,
            DocumentType = DocumentTypes.Recurrence,
            Organization = recurrence.Organization,
            ResourcePath = recurrence.ResourcePath,
            Type = recurrence.Type,
            StartTime = recurrence.StartTime,
            DurationMs = (long)recurrence.Duration.TotalMilliseconds,
            RecurrenceEndTime = recurrence.RecurrenceEndTime,
            RRule = recurrence.RRule,
            TimeZone = recurrence.TimeZone,
            Extensions = recurrence.Extensions
            // EndTime is NOT set for recurrences
        };
    }

    /// <summary>
    /// Converts an <see cref="Occurrence"/> to a <see cref="RecurringThingDocument"/>.
    /// </summary>
    /// <param name="occurrence">The occurrence to convert.</param>
    /// <returns>The mapped document.</returns>
    public static RecurringThingDocument FromOccurrence(Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        return new RecurringThingDocument
        {
            Id = occurrence.Id,
            DocumentType = DocumentTypes.Occurrence,
            Organization = occurrence.Organization,
            ResourcePath = occurrence.ResourcePath,
            Type = occurrence.Type,
            StartTime = occurrence.StartTime,
            EndTime = occurrence.EndTime,
            DurationMs = (long)occurrence.Duration.TotalMilliseconds,
            TimeZone = occurrence.TimeZone,
            Extensions = occurrence.Extensions
        };
    }

    /// <summary>
    /// Converts an <see cref="OccurrenceException"/> to a <see cref="RecurringThingDocument"/>.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <returns>The mapped document.</returns>
    public static RecurringThingDocument FromOccurrenceException(OccurrenceException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new RecurringThingDocument
        {
            Id = exception.Id,
            DocumentType = DocumentTypes.Exception,
            Organization = exception.Organization,
            ResourcePath = exception.ResourcePath,
            RecurrenceId = exception.RecurrenceId,
            OriginalTimeUtc = exception.OriginalTimeUtc,
            Extensions = exception.Extensions
        };
    }

    /// <summary>
    /// Converts an <see cref="OccurrenceOverride"/> to a <see cref="RecurringThingDocument"/>.
    /// </summary>
    /// <param name="override">The override to convert.</param>
    /// <returns>The mapped document.</returns>
    public static RecurringThingDocument FromOccurrenceOverride(OccurrenceOverride @override)
    {
        ArgumentNullException.ThrowIfNull(@override);

        return new RecurringThingDocument
        {
            Id = @override.Id,
            DocumentType = DocumentTypes.Override,
            Organization = @override.Organization,
            ResourcePath = @override.ResourcePath,
            RecurrenceId = @override.RecurrenceId,
            OriginalTimeUtc = @override.OriginalTimeUtc,
            StartTime = @override.StartTime,
            EndTime = @override.EndTime,
            DurationMs = (long)@override.Duration.TotalMilliseconds,
            OriginalDurationMs = (long)@override.OriginalDuration.TotalMilliseconds,
            OriginalExtensions = @override.OriginalExtensions,
            Extensions = @override.Extensions
        };
    }
}
