namespace RecurringThings.PostgreSQL;

using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using RecurringThings.Repository;

/// <summary>
/// PostgreSQL implementation of <see cref="ITransactionContext"/> that wraps an <see cref="NpgsqlTransaction"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="CreateAsync"/> to start a new transaction.
/// </para>
/// <para>
/// The transaction will be automatically rolled back if <see cref="DisposeAsync"/> is called
/// before <see cref="CommitAsync"/> or <see cref="RollbackAsync"/>.
/// </para>
/// </remarks>
public sealed class PostgresTransactionContext : ITransactionContext
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private bool _isDisposed;
    private bool _isCommittedOrRolledBack;

    private PostgresTransactionContext(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    /// <summary>
    /// Gets the underlying PostgreSQL connection.
    /// </summary>
    /// <remarks>
    /// Use this to execute commands within the transaction.
    /// </remarks>
    public NpgsqlConnection Connection => _connection;

    /// <summary>
    /// Gets the underlying PostgreSQL transaction.
    /// </summary>
    /// <remarks>
    /// Use this to pass the transaction to commands for transaction support.
    /// </remarks>
    public NpgsqlTransaction Transaction => _transaction;

    /// <summary>
    /// Creates a new PostgreSQL transaction context with an active transaction.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A new transaction context with an active transaction.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public static async Task<PostgresTransactionContext> CreateAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        return new PostgresTransactionContext(connection, transaction);
    }

    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposedOrCompleted();

        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        _isCommittedOrRolledBack = true;
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposedOrCompleted();

        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        _isCommittedOrRolledBack = true;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        // If transaction is still active, roll it back
        if (!_isCommittedOrRolledBack)
        {
            try
            {
                await _transaction.RollbackAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        await _transaction.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
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
