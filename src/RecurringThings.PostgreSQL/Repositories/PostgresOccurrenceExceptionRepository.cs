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
/// PostgreSQL implementation of <see cref="IOccurrenceExceptionRepository"/> using Entity Framework Core.
/// </summary>
internal sealed class PostgresOccurrenceExceptionRepository : IOccurrenceExceptionRepository
{
    private readonly IDbContextFactory<RecurringThingsDbContext> _contextFactory;
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresOccurrenceExceptionRepository"/> class.
    /// </summary>
    /// <param name="contextFactory">The DbContext factory.</param>
    /// <param name="connectionString">The PostgreSQL connection string for transaction context scenarios.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="contextFactory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public PostgresOccurrenceExceptionRepository(
        IDbContextFactory<RecurringThingsDbContext> contextFactory,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _contextFactory = contextFactory;
        _connectionString = connectionString;
    }

    /// <inheritdoc/>
    public async Task<OccurrenceException> CreateAsync(
        OccurrenceException exception,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var entity = EntityMapper.ToEntity(exception);
        context.OccurrenceExceptions.Add(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return exception;
    }

    /// <inheritdoc/>
    public async Task<OccurrenceException?> GetByIdAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var entity = await context.OccurrenceExceptions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == id &&
                     e.Organization == organization &&
                     e.ResourcePath == resourcePath,
                cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : EntityMapper.ToDomain(entity);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<OccurrenceException> GetByRecurrenceIdAsync(
        Guid recurrenceId,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(transactionContext, cancellationToken)
            .ConfigureAwait(false);

        var query = context.OccurrenceExceptions
            .AsNoTracking()
            .Where(e =>
                e.RecurrenceId == recurrenceId &&
                e.Organization == organization &&
                e.ResourcePath == resourcePath);

        await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return EntityMapper.ToDomain(entity);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<OccurrenceException> GetByRecurrenceIdsAsync(
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

        var query = context.OccurrenceExceptions
            .AsNoTracking()
            .Where(e =>
                recurrenceIdList.Contains(e.RecurrenceId) &&
                e.Organization == organization &&
                e.ResourcePath == resourcePath);

        await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return EntityMapper.ToDomain(entity);
        }
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

        var entity = await context.OccurrenceExceptions
            .FirstOrDefaultAsync(
                e => e.Id == id &&
                     e.Organization == organization &&
                     e.ResourcePath == resourcePath,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            context.OccurrenceExceptions.Remove(entity);
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

        var entities = await context.OccurrenceExceptions
            .Where(e =>
                e.RecurrenceId == recurrenceId &&
                e.Organization == organization &&
                e.ResourcePath == resourcePath)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entities.Count > 0)
        {
            context.OccurrenceExceptions.RemoveRange(entities);
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
