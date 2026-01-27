namespace RecurringThings.PostgreSQL.Data.Entities;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// EF Core entity for the recurrences table.
/// </summary>
[Table("recurrences")]
internal sealed class RecurrenceEntity
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
    /// Gets or sets the user-defined type.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC start time.
    /// </summary>
    [Column("start_time")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    [Column("duration")]
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the UTC recurrence end time.
    /// </summary>
    [Column("recurrence_end_time")]
    public DateTime RecurrenceEndTime { get; set; }

    /// <summary>
    /// Gets or sets the RFC 5545 recurrence rule.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    [Column("r_rule")]
    public string RRule { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IANA time zone identifier.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("time_zone")]
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user-defined key-value metadata.
    /// </summary>
    [Column("extensions", TypeName = "jsonb")]
    public Dictionary<string, string>? Extensions { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for exceptions.
    /// </summary>
    public ICollection<OccurrenceExceptionEntity> Exceptions { get; set; } = [];

    /// <summary>
    /// Gets or sets the navigation property for overrides.
    /// </summary>
    public ICollection<OccurrenceOverrideEntity> Overrides { get; set; } = [];
}
