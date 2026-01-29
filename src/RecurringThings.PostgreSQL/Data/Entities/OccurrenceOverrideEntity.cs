namespace RecurringThings.PostgreSQL.Data.Entities;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// EF Core entity for the occurrence_overrides table.
/// </summary>
[Table("occurrence_overrides")]
internal sealed class OccurrenceOverrideEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("organization")]
    public string Organization { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hierarchical resource scope.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("resource_path")]
    public string ResourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user-defined type inherited from the parent recurrence.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent recurrence identifier.
    /// </summary>
    [Column("recurrence_id")]
    public Guid RecurrenceId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the original occurrence being replaced.
    /// </summary>
    [Column("original_time_utc")]
    public DateTime OriginalTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the new UTC start time.
    /// </summary>
    [Column("start_time")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the new UTC end time.
    /// </summary>
    [Column("end_time")]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the new duration.
    /// </summary>
    [Column("duration")]
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the original duration from the parent recurrence.
    /// </summary>
    [Column("original_duration")]
    public TimeSpan OriginalDuration { get; set; }

    /// <summary>
    /// Gets or sets the original extensions from the parent recurrence.
    /// </summary>
    [Column("original_extensions", TypeName = "jsonb")]
    public Dictionary<string, string>? OriginalExtensions { get; set; }

    /// <summary>
    /// Gets or sets the user-defined key-value metadata.
    /// </summary>
    [Column("extensions", TypeName = "jsonb")]
    public Dictionary<string, string>? Extensions { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the parent recurrence.
    /// </summary>
    [ForeignKey(nameof(RecurrenceId))]
    public RecurrenceEntity? Recurrence { get; set; }
}
