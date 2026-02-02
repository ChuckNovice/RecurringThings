namespace RecurringThings.MongoDB.Repositories;

using global::MongoDB.Driver;
using System;
using Transactional.Abstractions;
using Transactional.MongoDB;

/// <summary>
/// Helper utilities for MongoDB repository operations.
/// </summary>
internal static class Helper
{
    /// <summary>
    /// Extracts the MongoDB client session from a transaction context.
    /// </summary>
    /// <param name="transactionContext">The transaction context, or null for non-transactional operations.</param>
    /// <returns>
    /// The <see cref="IClientSessionHandle"/> if a valid MongoDB transaction context is provided;
    /// otherwise, <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="transactionContext"/> is not null and is not an <see cref="IMongoTransactionContext"/>.
    /// </exception>
    internal static IClientSessionHandle? GetSession(ITransactionContext? transactionContext)
    {
        if (transactionContext == null)
            return null;

        if (transactionContext is not IMongoTransactionContext mongoContext)
            throw new ArgumentException($"The transaction type is invalid. " +
                $"Expected {nameof(IMongoTransactionContext)}, got {transactionContext.GetType().Name}.", nameof(transactionContext));

        return mongoContext.Session;
    }
}
