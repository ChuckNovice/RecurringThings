namespace RecurringThings.MongoDB.Repositories;

using System.Runtime.CompilerServices;
using global::MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurringThings.Exceptions;
using RecurringThings.Filters;
using RecurringThings.MongoDB.Configuration;
using RecurringThings.MongoDB.Documents;
using RecurringThings.Repository;

/// <summary>
/// MongoDB implementation of <see cref="IRecurringThingsRepository"/>.
/// </summary>
internal sealed class MongoRecurringThingsRepository : IRecurringThingsRepository
{
    private readonly IMongoCollection<EventDocument> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoRecurringThingsRepository"/> class.
    /// </summary>
    /// <param name="database">The MongoDB database.</param>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public MongoRecurringThingsRepository(
        [FromKeyedServices("RecurringThings_MongoDatabase")] IMongoDatabase database,
        IOptions<MongoDbOptions> options)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(options);

        _collection = database.GetCollection<EventDocument>(options.Value.CollectionName);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetAsync(
        string tenantId,
        EventFilter eventFilter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // CRITICAL: TenantId must be first filter for sharding.
        var filterBuilder = Builders<EventDocument>.Filter;
        var filter = filterBuilder.Eq(d => d.TenantId, tenantId);

        // Apply user filter based on mode.
        switch (eventFilter.UserMode)
        {
            case UserFilterMode.Specific:
                filter &= filterBuilder.Eq(d => d.UserId, eventFilter.UserId);
                break;
            case UserFilterMode.TenantWide:
                filter &= filterBuilder.Eq(d => d.UserId, null);
                break;
            case UserFilterMode.All:
                // No additional filter - return all users.
                break;
        }

        // Date overlap: event overlaps if event.End >= query.Start AND event.Start <= query.End.
        if (eventFilter.StartDateUtc.HasValue)
        {
            // event.EndDate is null (infinite) OR event.EndDate >= startDateUtc.
            filter &= filterBuilder.Or(
                filterBuilder.Eq(d => d.EndDate, null),
                filterBuilder.Gte(d => d.EndDate, eventFilter.StartDateUtc.Value));
        }

        if (eventFilter.EndDateUtc.HasValue)
        {
            // event.StartDate is null OR event.StartDate <= endDateUtc.
            filter &= filterBuilder.Or(
                filterBuilder.Eq(d => d.StartDate, null),
                filterBuilder.Lte(d => d.StartDate, eventFilter.EndDateUtc.Value));
        }

        // ComponentType filter (if specified).
        if (eventFilter.ComponentType.HasValue)
        {
            filter &= filterBuilder.Eq(d => d.ComponentType, eventFilter.ComponentType.Value);
        }

        // Categories filter (case-insensitive, match any).
        if (eventFilter.Categories is { Count: > 0 })
        {
            filter &= filterBuilder.AnyIn(d => d.Categories, eventFilter.Categories);
        }

        // Use async cursor for streaming.
        using var cursor = await _collection
            .Find(filter)
            .Project(d => d.SerializedData)
            .ToCursorAsync(cancellationToken);

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var serializedData in cursor.Current)
            {
                yield return serializedData;
            }
        }
    }

    /// <inheritdoc />
    public async Task CreateAsync(
        EventMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        var document = new EventDocument
        {
            Id = metadata.Uid,
            ComponentType = metadata.ComponentType,
            TenantId = metadata.TenantId,
            UserId = metadata.UserId,
            StartDate = metadata.StartDate,
            EndDate = metadata.EndDate,
            Categories = [.. metadata.Categories],
            Properties = [.. metadata.Properties
                .Select(p => new PropertyDocument
                {
                    Name = p.Key,
                    Value = p.Value
                })],
            SerializedData = metadata.SerializedData
        };

        try
        {
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateUidException(metadata.Uid, ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(
        EventMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        var document = new EventDocument
        {
            Id = metadata.Uid,
            ComponentType = metadata.ComponentType,
            TenantId = metadata.TenantId,
            UserId = metadata.UserId,
            StartDate = metadata.StartDate,
            EndDate = metadata.EndDate,
            Categories = [.. metadata.Categories],
            Properties = [.. metadata.Properties
                .Select(p => new PropertyDocument
                {
                    Name = p.Key,
                    Value = p.Value
                })],
            SerializedData = metadata.SerializedData
        };

        // CRITICAL: TenantId must be first filter for sharding.
        var filterBuilder = Builders<EventDocument>.Filter;
        var filter = filterBuilder.Eq(d => d.TenantId, metadata.TenantId)
            & filterBuilder.Eq(d => d.UserId, metadata.UserId)
            & filterBuilder.Eq(d => d.Id, metadata.Uid);

        var result = await _collection.ReplaceOneAsync(filter, document, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string uid,
        string tenantId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        // CRITICAL: TenantId must be first filter for sharding.
        var filterBuilder = Builders<EventDocument>.Filter;
        var filter = filterBuilder.Eq(d => d.TenantId, tenantId)
            & filterBuilder.Eq(d => d.UserId, userId)
            & filterBuilder.Eq(d => d.Id, uid);

        var result = await _collection.DeleteOneAsync(filter, cancellationToken);
        return result.DeletedCount > 0;
    }

}
