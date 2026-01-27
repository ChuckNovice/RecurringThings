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
/// PostgreSQL implementation of <see cref="IOccurrenceOverrideRepository"/>.
/// </summary>
public sealed class PostgresOccurrenceOverrideRepository : IOccurrenceOverrideRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresOccurrenceOverrideRepository"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    public PostgresOccurrenceOverrideRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <inheritdoc/>
    public async Task<OccurrenceOverride> CreateAsync(
        OccurrenceOverride @override,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@override);

        const string sql = """
            INSERT INTO occurrence_overrides (id, organization, resource_path, recurrence_id, original_time_utc, start_time, end_time, duration, original_duration, original_extensions, extensions)
            VALUES (@Id, @Organization, @ResourcePath, @RecurrenceId, @OriginalTimeUtc, @StartTime, @EndTime, @Duration, @OriginalDuration, @OriginalExtensions, @Extensions)
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        AddOverrideParameters(command, @override);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
        }

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
        const string sql = """
            SELECT id, organization, resource_path, recurrence_id, original_time_utc, start_time, end_time, duration, original_duration, original_extensions, extensions
            FROM occurrence_overrides
            WHERE id = @Id AND organization = @Organization AND resource_path = @ResourcePath
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Organization", organization);
        command.Parameters.AddWithValue("@ResourcePath", resourcePath);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        OccurrenceOverride? result = null;
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result = MapOverride(reader);
        }

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<OccurrenceOverride> GetByRecurrenceIdAsync(
        Guid recurrenceId,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, organization, resource_path, recurrence_id, original_time_utc, start_time, end_time, duration, original_duration, original_extensions, extensions
            FROM occurrence_overrides
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
            yield return MapOverride(reader);
        }

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
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

        const string sql = """
            SELECT id, organization, resource_path, recurrence_id, original_time_utc, start_time, end_time, duration, original_duration, original_extensions, extensions
            FROM occurrence_overrides
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
            yield return MapOverride(reader);
        }

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
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

        // Override is relevant if:
        // 1. Its originalTimeUtc falls within the range (the virtualized occurrence it replaces would have been shown)
        // 2. OR its actual StartTime/EndTime overlaps with the range (the override itself should be shown)
        const string sql = """
            SELECT id, organization, resource_path, recurrence_id, original_time_utc, start_time, end_time, duration, original_duration, original_extensions, extensions
            FROM occurrence_overrides
            WHERE recurrence_id = ANY(@RecurrenceIds)
              AND organization = @Organization
              AND resource_path = @ResourcePath
              AND (
                  (original_time_utc >= @StartUtc AND original_time_utc <= @EndUtc)
                  OR (start_time <= @EndUtc AND end_time >= @StartUtc)
              )
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@RecurrenceIds", recurrenceIdList.ToArray());
        command.Parameters.AddWithValue("@Organization", organization);
        command.Parameters.AddWithValue("@ResourcePath", resourcePath);
        command.Parameters.AddWithValue("@StartUtc", startUtc);
        command.Parameters.AddWithValue("@EndUtc", endUtc);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return MapOverride(reader);
        }

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<OccurrenceOverride> UpdateAsync(
        OccurrenceOverride @override,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@override);

        const string sql = """
            UPDATE occurrence_overrides
            SET start_time = @StartTime, end_time = @EndTime, duration = @Duration, extensions = @Extensions
            WHERE id = @Id AND organization = @Organization AND resource_path = @ResourcePath
            """;

        await using var command = await CreateCommandAsync(sql, transactionContext, cancellationToken)
            .ConfigureAwait(false);

        command.Parameters.AddWithValue("@Id", @override.Id);
        command.Parameters.AddWithValue("@Organization", @override.Organization);
        command.Parameters.AddWithValue("@ResourcePath", @override.ResourcePath);
        command.Parameters.AddWithValue("@StartTime", @override.StartTime);
        command.Parameters.AddWithValue("@EndTime", @override.EndTime);
        command.Parameters.AddWithValue("@Duration", @override.Duration);
        command.Parameters.Add(new NpgsqlParameter("@Extensions", NpgsqlDbType.Jsonb)
        {
            Value = @override.Extensions is null
                ? DBNull.Value
                : JsonSerializer.Serialize(@override.Extensions)
        });

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        if (transactionContext is null)
        {
            await command.Connection!.DisposeAsync().ConfigureAwait(false);
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
        const string sql = """
            DELETE FROM occurrence_overrides
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
            DELETE FROM occurrence_overrides
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

    private static void AddOverrideParameters(NpgsqlCommand command, OccurrenceOverride @override)
    {
        command.Parameters.AddWithValue("@Id", @override.Id);
        command.Parameters.AddWithValue("@Organization", @override.Organization);
        command.Parameters.AddWithValue("@ResourcePath", @override.ResourcePath);
        command.Parameters.AddWithValue("@RecurrenceId", @override.RecurrenceId);
        command.Parameters.AddWithValue("@OriginalTimeUtc", @override.OriginalTimeUtc);
        command.Parameters.AddWithValue("@StartTime", @override.StartTime);
        command.Parameters.AddWithValue("@EndTime", @override.EndTime);
        command.Parameters.AddWithValue("@Duration", @override.Duration);
        command.Parameters.AddWithValue("@OriginalDuration", @override.OriginalDuration);
        command.Parameters.Add(new NpgsqlParameter("@OriginalExtensions", NpgsqlDbType.Jsonb)
        {
            Value = @override.OriginalExtensions is null
                ? DBNull.Value
                : JsonSerializer.Serialize(@override.OriginalExtensions)
        });
        command.Parameters.Add(new NpgsqlParameter("@Extensions", NpgsqlDbType.Jsonb)
        {
            Value = @override.Extensions is null
                ? DBNull.Value
                : JsonSerializer.Serialize(@override.Extensions)
        });
    }

    private static OccurrenceOverride MapOverride(NpgsqlDataReader reader)
    {
        var originalExtensionsJson = reader.IsDBNull(9) ? null : reader.GetString(9);
        var originalExtensions = originalExtensionsJson is null
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string>>(originalExtensionsJson);

        var extensionsJson = reader.IsDBNull(10) ? null : reader.GetString(10);
        var extensions = extensionsJson is null
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string>>(extensionsJson);

        var @override = new OccurrenceOverride
        {
            Id = reader.GetGuid(0),
            Organization = reader.GetString(1),
            ResourcePath = reader.GetString(2),
            RecurrenceId = reader.GetGuid(3),
            OriginalTimeUtc = DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
            OriginalDuration = reader.GetTimeSpan(8),
            OriginalExtensions = originalExtensions,
            Extensions = extensions
        };

        @override.Initialize(
            DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc),
            reader.GetTimeSpan(7));

        return @override;
    }
}
