namespace RecurringThings.PostgreSQL.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using RecurringThings.Domain;
using RecurringThings.Repository;

/// <summary>
/// PostgreSQL implementation of <see cref="IOccurrenceExceptionRepository"/>.
/// </summary>
public sealed class PostgresOccurrenceExceptionRepository : IOccurrenceExceptionRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresOccurrenceExceptionRepository"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public PostgresOccurrenceExceptionRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <inheritdoc/>
    public async Task<OccurrenceException> CreateAsync(
        OccurrenceException exception,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        const string sql = """
            INSERT INTO occurrence_exceptions (id, organization, resource_path, recurrence_id, original_time_utc, extensions)
            VALUES (@Id, @Organization, @ResourcePath, @RecurrenceId, @OriginalTimeUtc, @Extensions)
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        AddExceptionParameters(command, exception);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
        }

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
        const string sql = """
            SELECT id, organization, resource_path, recurrence_id, original_time_utc, extensions
            FROM occurrence_exceptions
            WHERE id = @Id AND organization = @Organization AND resource_path = @ResourcePath
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Organization", organization);
        command.Parameters.AddWithValue("@ResourcePath", resourcePath);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        OccurrenceException? result = null;
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result = MapException(reader);
        }

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<OccurrenceException> GetByRecurrenceIdAsync(
        Guid recurrenceId,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, organization, resource_path, recurrence_id, original_time_utc, extensions
            FROM occurrence_exceptions
            WHERE recurrence_id = @RecurrenceId AND organization = @Organization AND resource_path = @ResourcePath
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@RecurrenceId", recurrenceId);
        command.Parameters.AddWithValue("@Organization", organization);
        command.Parameters.AddWithValue("@ResourcePath", resourcePath);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return MapException(reader);
        }

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
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

        const string sql = """
            SELECT id, organization, resource_path, recurrence_id, original_time_utc, extensions
            FROM occurrence_exceptions
            WHERE recurrence_id = ANY(@RecurrenceIds) AND organization = @Organization AND resource_path = @ResourcePath
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@RecurrenceIds", recurrenceIdList.ToArray());
        command.Parameters.AddWithValue("@Organization", organization);
        command.Parameters.AddWithValue("@ResourcePath", resourcePath);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return MapException(reader);
        }

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
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
        const string sql = """
            DELETE FROM occurrence_exceptions
            WHERE id = @Id AND organization = @Organization AND resource_path = @ResourcePath
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Organization", organization);
        command.Parameters.AddWithValue("@ResourcePath", resourcePath);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
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
        const string sql = """
            DELETE FROM occurrence_exceptions
            WHERE recurrence_id = @RecurrenceId AND organization = @Organization AND resource_path = @ResourcePath
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@RecurrenceId", recurrenceId);
        command.Parameters.AddWithValue("@Organization", organization);
        command.Parameters.AddWithValue("@ResourcePath", resourcePath);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<NpgsqlCommand> CreateCommandAsync(
        string sql,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        if (transactionContext is PostgresTransactionContext pgContext)
        {
            return new NpgsqlCommand(sql, pgContext.Connection, pgContext.Transaction);
        }

        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return new NpgsqlCommand(sql, connection);
    }

    private static void AddExceptionParameters(NpgsqlCommand command, OccurrenceException exception)
    {
        command.Parameters.AddWithValue("@Id", exception.Id);
        command.Parameters.AddWithValue("@Organization", exception.Organization);
        command.Parameters.AddWithValue("@ResourcePath", exception.ResourcePath);
        command.Parameters.AddWithValue("@RecurrenceId", exception.RecurrenceId);
        command.Parameters.AddWithValue("@OriginalTimeUtc", exception.OriginalTimeUtc);
        command.Parameters.Add(new NpgsqlParameter("@Extensions", NpgsqlDbType.Jsonb)
        {
            Value = exception.Extensions is null
                ? DBNull.Value
                : JsonSerializer.Serialize(exception.Extensions)
        });
    }

    private static OccurrenceException MapException(NpgsqlDataReader reader)
    {
        var extensionsJson = reader.IsDBNull(5) ? null : reader.GetString(5);
        var extensions = extensionsJson is null
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string>>(extensionsJson);

        return new OccurrenceException
        {
            Id = reader.GetGuid(0),
            Organization = reader.GetString(1),
            ResourcePath = reader.GetString(2),
            RecurrenceId = reader.GetGuid(3),
            OriginalTimeUtc = DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
            Extensions = extensions
        };
    }
}
