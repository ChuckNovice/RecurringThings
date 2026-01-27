namespace RecurringThings.MongoDB.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;
using RecurringThings.Domain;
using RecurringThings.MongoDB.Documents;
using RecurringThings.Repository;
using Transactional.Abstractions;
using Transactional.MongoDB;

/// <summary>
/// MongoDB implementation of <see cref="IOccurrenceOverrideRepository"/>.
/// </summary>
public sealed class MongoOccurrenceOverrideRepository : IOccurrenceOverrideRepository
{
    private readonly IMongoCollection<RecurringThingDocument> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoOccurrenceOverrideRepository"/> class.
    /// </summary>
    /// <param name="database">The MongoDB database.</param>
    /// <param name="collectionName">The collection name. Defaults to "recurring_things".</param>
    public MongoOccurrenceOverrideRepository(IMongoDatabase database, string collectionName = "recurring_things")
    {
        ArgumentNullException.ThrowIfNull(database);
        _collection = database.GetCollection<RecurringThingDocument>(collectionName);
    }

    /// <inheritdoc/>
    public async Task<OccurrenceOverride> CreateAsync(
        OccurrenceOverride @override,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@override);

        var document = DocumentMapper.FromOccurrenceOverride(@override);
        var session = GetSession(transactionContext);

        if (session is not null)
        {
            await _collection.InsertOneAsync(session, document, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
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
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Id, id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Override));

        var session = GetSession(transactionContext);

        RecurringThingDocument? document;
        if (session is not null)
        {
            document = await _collection.Find(session, filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            document = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return document is null ? null : DocumentMapper.ToOccurrenceOverride(document);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<OccurrenceOverride> GetByRecurrenceIdAsync(
        Guid recurrenceId,
        string organization,
        string resourcePath,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.RecurrenceId, recurrenceId),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Override));

        var session = GetSession(transactionContext);

        IAsyncCursor<RecurringThingDocument> cursor;
        if (session is not null)
        {
            cursor = await _collection.FindAsync(session, filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var document in cursor.Current)
            {
                yield return DocumentMapper.ToOccurrenceOverride(document);
            }
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

        // Cast to nullable Guid for filter compatibility
        var nullableIds = recurrenceIdList.Select(id => (Guid?)id).ToList();

        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.In(d => d.RecurrenceId, nullableIds),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Override));

        var session = GetSession(transactionContext);

        IAsyncCursor<RecurringThingDocument> cursor;
        if (session is not null)
        {
            cursor = await _collection.FindAsync(session, filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var document in cursor.Current)
            {
                yield return DocumentMapper.ToOccurrenceOverride(document);
            }
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

        // Cast to nullable Guid for filter compatibility
        var nullableIds = recurrenceIdList.Select(id => (Guid?)id).ToList();

        var filterBuilder = Builders<RecurringThingDocument>.Filter;

        // Override is relevant if:
        // 1. Its originalTimeUtc falls within the range (the virtualized occurrence it replaces would have been shown)
        // 2. OR its actual StartTime/EndTime overlaps with the range (the override itself should be shown)
        var rangeFilter = filterBuilder.Or(
            // Original time in range
            filterBuilder.And(
                filterBuilder.Gte(d => d.OriginalTimeUtc, startUtc),
                filterBuilder.Lte(d => d.OriginalTimeUtc, endUtc)),
            // Override time range overlaps query range
            filterBuilder.And(
                filterBuilder.Lte(d => d.StartTime, endUtc),
                filterBuilder.Gte(d => d.EndTime, startUtc)));

        var filter = filterBuilder.And(
            filterBuilder.In(d => d.RecurrenceId, nullableIds),
            filterBuilder.Eq(d => d.Organization, organization),
            filterBuilder.Eq(d => d.ResourcePath, resourcePath),
            filterBuilder.Eq(d => d.DocumentType, DocumentTypes.Override),
            rangeFilter);

        var session = GetSession(transactionContext);

        IAsyncCursor<RecurringThingDocument> cursor;
        if (session is not null)
        {
            cursor = await _collection.FindAsync(session, filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var document in cursor.Current)
            {
                yield return DocumentMapper.ToOccurrenceOverride(document);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<OccurrenceOverride> UpdateAsync(
        OccurrenceOverride @override,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@override);

        var document = DocumentMapper.FromOccurrenceOverride(@override);
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Id, @override.Id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, @override.Organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, @override.ResourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Override));

        var session = GetSession(transactionContext);

        if (session is not null)
        {
            await _collection.ReplaceOneAsync(session, filter, document, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await _collection.ReplaceOneAsync(filter, document, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
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
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Id, id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Override));

        var session = GetSession(transactionContext);

        if (session is not null)
        {
            await _collection.DeleteOneAsync(session, filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await _collection.DeleteOneAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
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
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.RecurrenceId, recurrenceId),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Override));

        var session = GetSession(transactionContext);

        if (session is not null)
        {
            await _collection.DeleteManyAsync(session, filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await _collection.DeleteManyAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static IClientSessionHandle? GetSession(ITransactionContext? transactionContext)
    {
        return transactionContext is IMongoTransactionContext mongoContext
            ? mongoContext.Session
            : null;
    }
}
