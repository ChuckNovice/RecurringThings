namespace RecurringThings.PostgreSQL.Data.Entities;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// EF Core entity for the occurrences table.
/// </summary>
[Table("occurrences")]
internal sealed class OccurrenceEntity
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
    /// Gets or sets the UTC end time.
    /// </summary>
    [Column("end_time")]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    [Column("duration")]
    public TimeSpan Duration { get; set; }

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
}
