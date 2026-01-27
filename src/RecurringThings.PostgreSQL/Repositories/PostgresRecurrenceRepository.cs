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

/// <summary>
/// PostgreSQL implementation of <see cref="IRecurrenceRepository"/>.
/// </summary>
public sealed class PostgresRecurrenceRepository : IRecurrenceRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresRecurrenceRepository"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public PostgresRecurrenceRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <inheritdoc/>
    public async Task<Recurrence> CreateAsync(
        Recurrence recurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recurrence);

        const string sql = """
            INSERT INTO recurrences (id, organization, resource_path, type, start_time, duration, recurrence_end_time, r_rule, time_zone, extensions)
            VALUES (@Id, @Organization, @ResourcePath, @Type, @StartTime, @Duration, @RecurrenceEndTime, @RRule, @TimeZone, @Extensions)
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        AddRecurrenceParameters(command, recurrence);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
        }

        return recurrence;
    }

    /// <inheritdoc/>
    public async Task<Recurrence?> GetByIdAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, organization, resource_path, type, start_time, duration, recurrence_end_time, r_rule, time_zone, extensions
            FROM recurrences
            WHERE id = @Id AND organization = @Organization AND resource_path = @ResourcePath
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Organization", organization);
        command.Parameters.AddWithValue("@ResourcePath", resourcePath);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        Recurrence? result = null;
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result = MapRecurrence(reader);
        }

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<Recurrence> UpdateAsync(
        Recurrence recurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recurrence);

        const string sql = """
            UPDATE recurrences
            SET duration = @Duration, extensions = @Extensions
            WHERE id = @Id AND organization = @Organization AND resource_path = @ResourcePath
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@Id", recurrence.Id);
        command.Parameters.AddWithValue("@Organization", recurrence.Organization);
        command.Parameters.AddWithValue("@ResourcePath", recurrence.ResourcePath);
        command.Parameters.AddWithValue("@Duration", recurrence.Duration);
        command.Parameters.Add(new NpgsqlParameter("@Extensions", NpgsqlDbType.Jsonb)
        {
            Value = recurrence.Extensions is null
                ? DBNull.Value
                : JsonSerializer.Serialize(recurrence.Extensions)
        });

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
        }

        return recurrence;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        Guid id,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        // PostgreSQL uses ON DELETE CASCADE, so deleting the recurrence will automatically delete
        // related exceptions and overrides
        const string sql = """
            DELETE FROM recurrences
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
    public async IAsyncEnumerable<Recurrence> GetInRangeAsync(
        string organization,
        string resourcePath,
        DateTime startUtc,
        DateTime endUtc,
        string[]? types,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT id, organization, resource_path, type, start_time, duration, recurrence_end_time, r_rule, time_zone, extensions
            FROM recurrences
            WHERE organization = @Organization
              AND resource_path = @ResourcePath
              AND start_time <= @EndUtc
              AND recurrence_end_time >= @StartUtc
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
            yield return MapRecurrence(reader);
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
        if (transactionContext is PostgresTransactionContext pgContext)
        {
            return new NpgsqlCommand(sql, pgContext.Connection, pgContext.Transaction);
        }

        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return new NpgsqlCommand(sql, connection);
    }

    private static void AddRecurrenceParameters(NpgsqlCommand command, Recurrence recurrence)
    {
        command.Parameters.AddWithValue("@Id", recurrence.Id);
        command.Parameters.AddWithValue("@Organization", recurrence.Organization);
        command.Parameters.AddWithValue("@ResourcePath", recurrence.ResourcePath);
        command.Parameters.AddWithValue("@Type", recurrence.Type);
        command.Parameters.AddWithValue("@StartTime", recurrence.StartTime);
        command.Parameters.AddWithValue("@Duration", recurrence.Duration);
        command.Parameters.AddWithValue("@RecurrenceEndTime", recurrence.RecurrenceEndTime);
        command.Parameters.AddWithValue("@RRule", recurrence.RRule);
        command.Parameters.AddWithValue("@TimeZone", recurrence.TimeZone);
        command.Parameters.Add(new NpgsqlParameter("@Extensions", NpgsqlDbType.Jsonb)
        {
            Value = recurrence.Extensions is null
                ? DBNull.Value
                : JsonSerializer.Serialize(recurrence.Extensions)
        });
    }

    private static Recurrence MapRecurrence(NpgsqlDataReader reader)
    {
        var extensionsJson = reader.IsDBNull(9) ? null : reader.GetString(9);
        var extensions = extensionsJson is null
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string>>(extensionsJson);

        return new Recurrence
        {
            Id = reader.GetGuid(0),
            Organization = reader.GetString(1),
            ResourcePath = reader.GetString(2),
            Type = reader.GetString(3),
            StartTime = DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
            Duration = reader.GetTimeSpan(5),
            RecurrenceEndTime = DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc),
            RRule = reader.GetString(7),
            TimeZone = reader.GetString(8),
            Extensions = extensions
        };
    }
}
