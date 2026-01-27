namespace RecurringThings.PostgreSQL.Data;

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RecurringThings.PostgreSQL.Data.Entities;

/// <summary>
/// EF Core DbContext for RecurringThings PostgreSQL persistence.
/// </summary>
internal sealed class RecurringThingsDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringThingsDbContext"/> class.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    public RecurringThingsDbContext(DbContextOptions<RecurringThingsDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the recurrences DbSet.
    /// </summary>
    public DbSet<RecurrenceEntity> Recurrences => Set<RecurrenceEntity>();

    /// <summary>
    /// Gets or sets the occurrences DbSet.
    /// </summary>
    public DbSet<OccurrenceEntity> Occurrences => Set<OccurrenceEntity>();

    /// <summary>
    /// Gets or sets the occurrence exceptions DbSet.
    /// </summary>
    public DbSet<OccurrenceExceptionEntity> OccurrenceExceptions => Set<OccurrenceExceptionEntity>();

    /// <summary>
    /// Gets or sets the occurrence overrides DbSet.
    /// </summary>
    public DbSet<OccurrenceOverrideEntity> OccurrenceOverrides => Set<OccurrenceOverrideEntity>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRecurrenceEntity(modelBuilder);
        ConfigureOccurrenceEntity(modelBuilder);
        ConfigureOccurrenceExceptionEntity(modelBuilder);
        ConfigureOccurrenceOverrideEntity(modelBuilder);
    }

    private static void ConfigureRecurrenceEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RecurrenceEntity>();

        // Configure JSONB for extensions
        entity.Property(e => e.Extensions)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null));

        // Index for range queries
        entity.HasIndex(e => new { e.Organization, e.ResourcePath, e.Type, e.StartTime, e.RecurrenceEndTime })
            .HasDatabaseName("idx_recurrences_query");

        // Configure cascade delete for exceptions and overrides
        entity.HasMany(e => e.Exceptions)
            .WithOne(e => e.Recurrence)
            .HasForeignKey(e => e.RecurrenceId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.Overrides)
            .WithOne(e => e.Recurrence)
            .HasForeignKey(e => e.RecurrenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureOccurrenceEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OccurrenceEntity>();

        // Configure JSONB for extensions
        entity.Property(e => e.Extensions)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null));

        // Index for range queries
        entity.HasIndex(e => new { e.Organization, e.ResourcePath, e.Type, e.StartTime, e.EndTime })
            .HasDatabaseName("idx_occurrences_query");
    }

    private static void ConfigureOccurrenceExceptionEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OccurrenceExceptionEntity>();

        // Configure JSONB for extensions
        entity.Property(e => e.Extensions)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null));

        // Index for finding exceptions by recurrence
        entity.HasIndex(e => e.RecurrenceId)
            .HasDatabaseName("idx_exceptions_recurrence");

        // Index for range queries during virtualization
        entity.HasIndex(e => new { e.Organization, e.ResourcePath, e.OriginalTimeUtc })
            .HasDatabaseName("idx_exceptions_query");
    }

    private static void ConfigureOccurrenceOverrideEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OccurrenceOverrideEntity>();

        // Configure JSONB for extensions
        entity.Property(e => e.Extensions)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null));

        // Configure JSONB for original extensions
        entity.Property(e => e.OriginalExtensions)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null));

        // Index for finding overrides by recurrence
        entity.HasIndex(e => e.RecurrenceId)
            .HasDatabaseName("idx_overrides_recurrence");

        // Index for finding overrides by original time
        entity.HasIndex(e => new { e.Organization, e.ResourcePath, e.OriginalTimeUtc })
            .HasDatabaseName("idx_overrides_original");

        // Index for finding overrides by actual time range
        entity.HasIndex(e => new { e.Organization, e.ResourcePath, e.StartTime, e.EndTime })
            .HasDatabaseName("idx_overrides_start");
    }
}
