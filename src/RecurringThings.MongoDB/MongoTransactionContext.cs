namespace RecurringThings.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;
using RecurringThings.Repository;

/// <summary>
/// MongoDB implementation of <see cref="ITransactionContext"/> that wraps an <see cref="IClientSessionHandle"/>.
/// </summary>
/// <remarks>
/// <para>
/// MongoDB transactions require a replica set or sharded cluster.
/// Use <see cref="CreateAsync"/> to start a new transaction.
/// </para>
/// <para>
/// The transaction will be automatically aborted if <see cref="DisposeAsync"/> is called
/// before <see cref="CommitAsync"/> or <see cref="RollbackAsync"/>.
/// </para>
/// </remarks>
public sealed class MongoTransactionContext : ITransactionContext
{
    private readonly IClientSessionHandle _session;
    private bool _isDisposed;
    private bool _isCommittedOrRolledBack;

    private MongoTransactionContext(IClientSessionHandle session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>
    /// Gets the underlying MongoDB client session.
    /// </summary>
    /// <remarks>
    /// Use this to pass the session to MongoDB operations for transaction support.
    /// </remarks>
    public IClientSessionHandle Session => _session;

    /// <summary>
    /// Creates a new MongoDB transaction context with an active transaction.
    /// </summary>
    /// <param name="client">The MongoDB client.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A new transaction context with an active transaction.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> is null.</exception>
    public static async Task<MongoTransactionContext> CreateAsync(
        IMongoClient client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        var session = await client.StartSessionAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        session.StartTransaction();

        return new MongoTransactionContext(session);
    }

    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposedOrCompleted();

        await _session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        _isCommittedOrRolledBack = true;
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposedOrCompleted();

        await _session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
        _isCommittedOrRolledBack = true;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        // If transaction is still active, abort it
        if (!_isCommittedOrRolledBack && _session.IsInTransaction)
        {
            try
            {
                await _session.AbortTransactionAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        _session.Dispose();
        _isDisposed = true;
    }

    private void ThrowIfDisposedOrCompleted()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_isCommittedOrRolledBack)
        {
            throw new InvalidOperationException("Transaction has already been committed or rolled back.");
        }
    }
}
