namespace RecurringThings.PostgreSQL.Repositories;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using RecurringThings.Domain;
using RecurringThings.Repository;
using Transactional.Abstractions;
using Transactional.PostgreSQL;

/// <summary>
/// PostgreSQL implementation of <see cref="IOccurrenceRepository"/>.
/// </summary>
public sealed class PostgresOccurrenceRepository : IOccurrenceRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresOccurrenceRepository"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public PostgresOccurrenceRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <inheritdoc/>
    public async Task<Occurrence> CreateAsync(
        Occurrence occurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        const string sql = """
            INSERT INTO occurrences (id, organization, resource_path, type, start_time, end_time, duration, time_zone, extensions)
            VALUES (@Id, @Organization, @ResourcePath, @Type, @StartTime, @EndTime, @Duration, @TimeZone, @Extensions)
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        AddOccurrenceParameters(command, occurrence);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
        }

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
        const string sql = """
            SELECT id, organization, resource_path, type, start_time, end_time, duration, time_zone, extensions
            FROM occurrences
            WHERE id = @Id AND organization = @Organization AND resource_path = @ResourcePath
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Organization", organization);
        command.Parameters.AddWithValue("@ResourcePath", resourcePath);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        Occurrence? result = null;
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result = MapOccurrence(reader);
        }

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<Occurrence> UpdateAsync(
        Occurrence occurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        const string sql = """
            UPDATE occurrences
            SET start_time = @StartTime, end_time = @EndTime, duration = @Duration, extensions = @Extensions
            WHERE id = @Id AND organization = @Organization AND resource_path = @ResourcePath
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@Id", occurrence.Id);
        command.Parameters.AddWithValue("@Organization", occurrence.Organization);
        command.Parameters.AddWithValue("@ResourcePath", occurrence.ResourcePath);
        command.Parameters.AddWithValue("@StartTime", occurrence.StartTime);
        command.Parameters.AddWithValue("@EndTime", occurrence.EndTime);
        command.Parameters.AddWithValue("@Duration", occurrence.Duration);
        command.Parameters.Add(new NpgsqlParameter("@Extensions", NpgsqlDbType.Jsonb)
        {
            Value = occurrence.Extensions is null
                ? DBNull.Value
                : JsonSerializer.Serialize(occurrence.Extensions)
        });

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
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
        const string sql = """
            DELETE FROM occurrences
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
    public async IAsyncEnumerable<Occurrence> GetInRangeAsync(
        string organization,
        string resourcePath,
        DateTime startUtc,
        DateTime endUtc,
        string[]? types,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT id, organization, resource_path, type, start_time, end_time, duration, time_zone, extensions
            FROM occurrences
            WHERE organization = @Organization
              AND resource_path = @ResourcePath
              AND start_time <= @EndUtc
              AND end_time >= @StartUtc
            """;

        if (types is { Length: > 0 })
        {
            sql += " AND type = ANY(@Types)";
        }

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@Organization", organization);
        command.Parameters.AddWithValue("@ResourcePath", resourcePath);
        command.Parameters.AddWithValue("@StartUtc", startUtc);
        command.Parameters.AddWithValue("@EndUtc", endUtc);

        if (types is { Length: > 0 })
        {
            command.Parameters.AddWithValue("@Types", types);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return MapOccurrence(reader);
        }

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
        if (transactionContext is IPostgresTransactionContext pgContext)
        {
            return new NpgsqlCommand(sql, pgContext.Transaction.Connection, pgContext.Transaction);
        }

        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return new NpgsqlCommand(sql, connection);
    }

    private static void AddOccurrenceParameters(NpgsqlCommand command, Occurrence occurrence)
    {
        command.Parameters.AddWithValue("@Id", occurrence.Id);
        command.Parameters.AddWithValue("@Organization", occurrence.Organization);
        command.Parameters.AddWithValue("@ResourcePath", occurrence.ResourcePath);
        command.Parameters.AddWithValue("@Type", occurrence.Type);
        command.Parameters.AddWithValue("@StartTime", occurrence.StartTime);
        command.Parameters.AddWithValue("@EndTime", occurrence.EndTime);
        command.Parameters.AddWithValue("@Duration", occurrence.Duration);
        command.Parameters.AddWithValue("@TimeZone", occurrence.TimeZone);
        command.Parameters.Add(new NpgsqlParameter("@Extensions", NpgsqlDbType.Jsonb)
        {
            Value = occurrence.Extensions is null
                ? DBNull.Value
                : JsonSerializer.Serialize(occurrence.Extensions)
        });
    }

    private static Occurrence MapOccurrence(NpgsqlDataReader reader)
    {
        var extensionsJson = reader.IsDBNull(8) ? null : reader.GetString(8);
        var extensions = extensionsJson is null
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string>>(extensionsJson);

        var occurrence = new Occurrence
        {
            Id = reader.GetGuid(0),
            Organization = reader.GetString(1),
            ResourcePath = reader.GetString(2),
            Type = reader.GetString(3),
            TimeZone = reader.GetString(7),
            Extensions = extensions
        };

        occurrence.Initialize(
            DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
            reader.GetTimeSpan(6));

        return occurrence;
    }
}
