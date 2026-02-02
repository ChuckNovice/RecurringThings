namespace RecurringThings.PostgreSQL.Repositories;

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RecurringThings.Exceptions;
using RecurringThings.Filters;
using RecurringThings.PostgreSQL.Data;
using RecurringThings.PostgreSQL.Data.Entities;
using RecurringThings.Repository;

/// <summary>
/// PostgreSQL implementation of <see cref="IRecurringThingsRepository"/> using Entity Framework Core.
/// </summary>
internal sealed class PostgresRecurringThingsRepository : IRecurringThingsRepository
{
    private readonly IDbContextFactory<RecurringThingsDbContext> _contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresRecurringThingsRepository"/> class.
    /// </summary>
    /// <param name="contextFactory">The DbContext factory.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="contextFactory"/> is null.</exception>
    public PostgresRecurringThingsRepository(
        IDbContextFactory<RecurringThingsDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetAsync(
        string tenantId,
        EventFilter eventFilter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Events.Where(e => e.TenantId == tenantId);

        // Apply user filter based on mode.
        switch (eventFilter.UserMode)
        {
            case UserFilterMode.Specific:
                query = query.Where(e => e.UserId == eventFilter.UserId);
                break;
            case UserFilterMode.TenantWide:
                query = query.Where(e => e.UserId == null);
                break;
            case UserFilterMode.All:
                // No additional filter - return all users.
                break;
        }

        // Date overlap conditions.
        if (eventFilter.StartDateUtc.HasValue)
        {
            query = query.Where(e => e.EndDate == null || e.EndDate >= eventFilter.StartDateUtc.Value);
        }

        if (eventFilter.EndDateUtc.HasValue)
        {
            query = query.Where(e => e.StartDate == null || e.StartDate <= eventFilter.EndDateUtc.Value);
        }

        // ComponentType filter (if specified).
        if (eventFilter.ComponentType.HasValue)
        {
            query = query.Where(e => e.ComponentType == eventFilter.ComponentType.Value);
        }

        // Categories filter (case-insensitive - stored as lowercase).
        if (eventFilter.Categories is { Count: > 0 })
        {
            query = query.Where(e => e.Categories.Any(c => eventFilter.Categories.Contains(c.Value)));
        }

        // Use AsAsyncEnumerable for streaming.
        await foreach (var serializedData in query
            .Select(e => e.SerializedData)
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            yield return serializedData;
        }
    }

    /// <inheritdoc />
    public async Task CreateAsync(
        EventMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = new EventEntity
        {
            Uid = metadata.Uid,
            ComponentType = metadata.ComponentType,
            TenantId = metadata.TenantId,
            UserId = metadata.UserId,
            StartDate = metadata.StartDate,
            EndDate = metadata.EndDate,
            SerializedData = metadata.SerializedData,
            Categories = [.. metadata.Categories.Select(c => new CategoryEntity { Value = c })],
            Properties = [.. metadata.Properties
                .Select(p => new PropertyEntity
                {
                    Name = p.Key,
                    Value = p.Value
                })]
        };

        context.Events.Add(entity);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateUidException(metadata.Uid, ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(
        EventMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.Events
            .Include(e => e.Categories)
            .Include(e => e.Properties)
            .FirstOrDefaultAsync(e =>
                e.TenantId == metadata.TenantId &&
                e.UserId == metadata.UserId &&
                e.Uid == metadata.Uid, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        // Update scalar properties.
        entity.ComponentType = metadata.ComponentType;
        entity.StartDate = metadata.StartDate;
        entity.EndDate = metadata.EndDate;
        entity.SerializedData = metadata.SerializedData;

        // Replace categories.
        entity.Categories.Clear();
        foreach (var c in metadata.Categories)
        {
            entity.Categories.Add(new CategoryEntity { Value = c });
        }

        // Replace properties.
        entity.Properties.Clear();
        foreach (var p in metadata.Properties)
        {
            entity.Properties.Add(new PropertyEntity { Name = p.Key, Value = p.Value });
        }

        return await context.SaveChangesAsync(cancellationToken) > 0;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string uid,
        string tenantId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var deleted = await context.Events
            .Where(e => e.TenantId == tenantId && e.UserId == userId && e.Uid == uid)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

}
