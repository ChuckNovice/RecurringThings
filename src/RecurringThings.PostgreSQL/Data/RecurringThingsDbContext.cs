namespace RecurringThings.PostgreSQL.Data;

using Microsoft.EntityFrameworkCore;
using RecurringThings.PostgreSQL.Data.Entities;

/// <summary>
/// EF Core DbContext for RecurringThings PostgreSQL persistence.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RecurringThingsDbContext"/> class.
/// </remarks>
/// <param name="options">The DbContext options.</param>
internal sealed class RecurringThingsDbContext(DbContextOptions<RecurringThingsDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the events DbSet.
    /// </summary>
    public DbSet<EventEntity> Events { get; set; } = null!;

    /// <summary>
    /// Gets or sets the categories DbSet.
    /// </summary>
    public DbSet<CategoryEntity> Categories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the properties DbSet.
    /// </summary>
    public DbSet<PropertyEntity> Properties { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EventEntity>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.Uid).IsRequired();
            entity.HasIndex(e => e.Uid).IsUnique();
            entity.Property(e => e.SerializedData).IsRequired();

            entity.HasMany(e => e.Categories)
                .WithOne(c => c.Event)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Properties)
                .WithOne(p => p.Event)
                .HasForeignKey(p => p.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.ToTable("event_categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.Value).IsRequired();
        });

        modelBuilder.Entity<PropertyEntity>(entity =>
        {
            entity.ToTable("event_properties");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Value).IsRequired();
        });
    }
}
