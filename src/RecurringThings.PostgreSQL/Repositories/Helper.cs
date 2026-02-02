namespace RecurringThings.PostgreSQL.Repositories;

using Microsoft.EntityFrameworkCore;
using RecurringThings.PostgreSQL.Data;
using Transactional.Abstractions;
using Transactional.PostgreSQL;

/// <summary>
/// Helper methods for creating DbContext instances with optional transaction support.
/// </summary>
internal static class Helper
{
    /// <summary>
    /// Creates a <see cref="RecurringThingsDbContext"/> instance, optionally enrolled in an existing transaction.
    /// </summary>
    /// <param name="contextFactory">The DbContext factory for creating pooled context instances.</param>
    /// <param name="transactionContext">
    /// Optional transaction context. If provided and is a <see cref="IPostgresTransactionContext"/>,
    /// the created context will be enrolled in the existing PostgreSQL transaction.
    /// If <c>null</c>, a new context is created using the factory.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="RecurringThingsDbContext"/> instance. If <paramref name="transactionContext"/> is provided,
    /// the context uses the transaction's connection and is enrolled in the transaction.
    /// Otherwise, a pooled context from the factory is returned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// When a PostgreSQL transaction context is provided, this method creates a context directly using
    /// the transaction's connection rather than the factory. This ensures all operations within the
    /// transaction use the same connection and transaction scope.
    /// </para>
    /// </remarks>
    internal static async Task<RecurringThingsDbContext> CreateContextAsync(
       this IDbContextFactory<RecurringThingsDbContext> contextFactory,
       ITransactionContext? transactionContext,
       CancellationToken cancellationToken = default)
    {
        var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        if (transactionContext is not null)
        {
            if (transactionContext is not IPostgresTransactionContext pgContext)
            {
                throw new ArgumentException(
                    $"The transaction type is invalid. " +
                    $"Expected {nameof(IPostgresTransactionContext)}, got {transactionContext.GetType().Name}.",
                    nameof(transactionContext));
            }

            context.Database.SetDbConnection(pgContext.Transaction.Connection!);
            await context.Database.UseTransactionAsync(pgContext.Transaction, cancellationToken)
                .ConfigureAwait(false);
        }

        return context;
    }
}