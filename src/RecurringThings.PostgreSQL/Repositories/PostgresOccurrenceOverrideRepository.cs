namespace RecurringThings.PostgreSQL.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RecurringThings.Domain;
using RecurringThings.PostgreSQL.Data;
using RecurringThings.PostgreSQL.Data.Entities;
using RecurringThings.Repository;
using Transactional.Abstractions;
using Transactional.PostgreSQL;

/// <summary>
/// PostgreSQL implementation of <see cref="IOccurrenceOverrideRepository"/> using Entity Framework Core.
/// </summary>
internal sealed class PostgresOccurrenceOverrideRepository : IOccurrenceOverrideRepository
{
    private readonly IDbContextFactory<RecurringThingsDbContext> _contextFactory;
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresOccurrenceOverrideRepository"/> class.
    /// </summary>
    /// <param name="contextFactory">The DbContext factory.</param>
    /// <param name="connectionString">The PostgreSQL connection string for transaction context scenarios.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="contextFactory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public PostgresOccurrenceOverrideRepository(
        IDbContextFactory<RecurringThingsDbContext> contextFactory,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _contextFactory = contextFactory;
        _connectionString = connectionString;
    }

    /// <inheritdoc/>
    public async Task<OccurrenceOverride> CreateAsync(
        OccurrenceOverride @override,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@override);

        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var entity = EntityMapper.ToEntity(@override);
        context.OccurrenceOverrides.Add(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return @override;
    }

    /// <inheritdoc/>
    public async Task<OccurrenceOverride?> GetByIdAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var entity = await context.OccurrenceOverrides
            .AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.Id == id &&
                     o.Organization == organization &&
                     o.ResourcePath == resourcePath,
                cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : EntityMapper.ToDomain(entity);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<OccurrenceOverride> GetByRecurrenceIdAsync(
        Guid recurrenceId,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var query = context.OccurrenceOverrides
            .AsNoTracking()
            .Where(o =>
                o.RecurrenceId == recurrenceId &&
                o.Organization == organization &&
                o.ResourcePath == resourcePath);

        await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return EntityMapper.ToDomain(entity);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<OccurrenceOverride> GetByRecurrenceIdsAsync(
        string organization,
        string resourcePath,
        IEnumerable<Guid> recurrenceIds,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var recurrenceIdList = recurrenceIds as IList<Guid> ?? [.. recurrenceIds];

        if (recurrenceIdList.Count == 0)
        {
            yield break;
        }

        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var query = context.OccurrenceOverrides
            .AsNoTracking()
            .Where(o =>
                recurrenceIdList.Contains(o.RecurrenceId) &&
                o.Organization == organization &&
                o.ResourcePath == resourcePath);

        await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return EntityMapper.ToDomain(entity);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<OccurrenceOverride> GetInRangeAsync(
        string organization,
        string resourcePath,
        IEnumerable<Guid> recurrenceIds,
        DateTime startUtc,
        DateTime endUtc,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var recurrenceIdList = recurrenceIds as IList<Guid> ?? [.. recurrenceIds];

        if (recurrenceIdList.Count == 0)
        {
            yield break;
        }

        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        // Override is relevant if:
        // 1. Its originalTimeUtc falls within the range (the virtualized occurrence it replaces would have been shown)
        // 2. OR its actual StartTime/EndTime overlaps with the range (the override itself should be shown)
        var query = context.OccurrenceOverrides
            .AsNoTracking()
            .Where(o =>
                recurrenceIdList.Contains(o.RecurrenceId) &&
                o.Organization == organization &&
                o.ResourcePath == resourcePath &&
                ((o.OriginalTimeUtc >= startUtc && o.OriginalTimeUtc <= endUtc) ||
                 (o.StartTime <= endUtc && o.EndTime >= startUtc)));

        await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return EntityMapper.ToDomain(entity);
        }
    }

    /// <inheritdoc/>
    public async Task<OccurrenceOverride> UpdateAsync(
        OccurrenceOverride @override,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@override);

        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var entity = await context.OccurrenceOverrides
            .FirstOrDefaultAsync(
                o => o.Id == @override.Id &&
                     o.Organization == @override.Organization &&
                     o.ResourcePath == @override.ResourcePath,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            entity.StartTime = @override.StartTime;
            entity.EndTime = @override.EndTime;
            entity.Duration = @override.Duration;
            entity.Extensions = @override.Extensions;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return @override;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var entity = await context.OccurrenceOverrides
            .FirstOrDefaultAsync(
                o => o.Id == id &&
                     o.Organization == organization &&
                     o.ResourcePath == resourcePath,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            context.OccurrenceOverrides.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteByRecurrenceIdAsync(
        Guid recurrenceId,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var entities = await context.OccurrenceOverrides
            .Where(o =>
                o.RecurrenceId == recurrenceId &&
                o.Organization == organization &&
                o.ResourcePath == resourcePath)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entities.Count > 0)
        {
            context.OccurrenceOverrides.RemoveRange(entities);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<RecurringThingsDbContext> CreateContextAsync(
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        if (transactionContext is IPostgresTransactionContext pgContext)
        {
            // Create context using the transaction's connection
            var options = new DbContextOptionsBuilder<RecurringThingsDbContext>()
                .UseNpgsql(pgContext.Transaction.Connection!)
                .Options;
            var context = new RecurringThingsDbContext(options);
            await context.Database.UseTransactionAsync(pgContext.Transaction, cancellationToken)
                .ConfigureAwait(false);
            return context;
        }

        // Use factory for normal operation (pooled DbContext)
        return await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
    }
}
