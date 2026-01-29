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
/// PostgreSQL implementation of <see cref="IOccurrenceRepository"/> using Entity Framework Core.
/// </summary>
internal sealed class PostgresOccurrenceRepository : IOccurrenceRepository
{
    private readonly IDbContextFactory<RecurringThingsDbContext> _contextFactory;
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresOccurrenceRepository"/> class.
    /// </summary>
    /// <param name="contextFactory">The DbContext factory.</param>
    /// <param name="connectionString">The PostgreSQL connection string for transaction context scenarios.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="contextFactory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public PostgresOccurrenceRepository(
        IDbContextFactory<RecurringThingsDbContext> contextFactory,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _contextFactory = contextFactory;
        _connectionString = connectionString;
    }

    /// <inheritdoc/>
    public async Task<Occurrence> CreateAsync(
        Occurrence occurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var entity = EntityMapper.ToEntity(occurrence);
        context.Occurrences.Add(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return occurrence;
    }

    /// <inheritdoc/>
    public async Task<Occurrence?> GetByIdAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var entity = await context.Occurrences
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
    public async Task<Occurrence?> GetAsync(
        string organization,
        Guid id,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var entity = await context.Occurrences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.Organization == organization && o.Id == id,
                cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : EntityMapper.ToDomain(entity);
    }

    /// <inheritdoc/>
    public async Task<Occurrence> UpdateAsync(
        Occurrence occurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var entity = await context.Occurrences
            .FirstOrDefaultAsync(
                o => o.Id == occurrence.Id &&
                     o.Organization == occurrence.Organization &&
                     o.ResourcePath == occurrence.ResourcePath,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            entity.StartTime = occurrence.StartTime;
            entity.EndTime = occurrence.EndTime;
            entity.Duration = occurrence.Duration;
            entity.Extensions = occurrence.Extensions;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return occurrence;
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

        var entity = await context.Occurrences
            .FirstOrDefaultAsync(
                o => o.Id == id &&
                     o.Organization == organization &&
                     o.ResourcePath == resourcePath,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            context.Occurrences.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Occurrence> GetInRangeAsync(
        string organization,
        string resourcePath,
        DateTime startUtc,
        DateTime endUtc,
        string[]? types,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var query = context.Occurrences
            .AsNoTracking()
            .Where(o =>
                o.Organization == organization &&
                o.ResourcePath == resourcePath &&
                o.StartTime <= endUtc &&
                o.EndTime >= startUtc);

        if (types is { Length: > 0 })
        {
            query = query.Where(o => types.Contains(o.Type));
        }

        await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return EntityMapper.ToDomain(entity);
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
