namespace RecurringThings.MongoDB.Repositories;

using System;
using System.Collections.Generic;
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
/// MongoDB implementation of <see cref="IOccurrenceRepository"/>.
/// </summary>
internal sealed class MongoOccurrenceRepository : IOccurrenceRepository
{
    private readonly IMongoCollection<RecurringThingDocument> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoOccurrenceRepository"/> class.
    /// </summary>
    /// <param name="database">The MongoDB database.</param>
    /// <param name="collectionName">The collection name. Defaults to "recurring_things".</param>
    public MongoOccurrenceRepository(IMongoDatabase database, string collectionName = "recurring_things")
    {
        ArgumentNullException.ThrowIfNull(database);
        _collection = database.GetCollection<RecurringThingDocument>(collectionName);
    }

    /// <inheritdoc/>
    public async Task<Occurrence> CreateAsync(
        Occurrence occurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        var document = DocumentMapper.FromOccurrence(occurrence);
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
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Id, id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Occurrence));

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

        return document is null ? null : DocumentMapper.ToOccurrence(document);
    }

    /// <inheritdoc/>
    public async Task<Occurrence> UpdateAsync(
        Occurrence occurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        var document = DocumentMapper.FromOccurrence(occurrence);
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Id, occurrence.Id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, occurrence.Organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, occurrence.ResourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Occurrence));

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
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Id, id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Occurrence));

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
    public async IAsyncEnumerable<Occurrence> GetInRangeAsync(
        string organization,
        string resourcePath,
        DateTime startUtc,
        DateTime endUtc,
        string[]? types,
        ITransactionContext? transactionContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<RecurringThingDocument>.Filter;
        var filters = new List<FilterDefinition<RecurringThingDocument>>
        {
            filterBuilder.Eq(d => d.Organization, organization),
            filterBuilder.Eq(d => d.ResourcePath, resourcePath),
            filterBuilder.Eq(d => d.DocumentType, DocumentTypes.Occurrence),
            // StartTime <= endUtc AND EndTime >= startUtc
            filterBuilder.Lte(d => d.StartTime, endUtc),
            filterBuilder.Gte(d => d.EndTime, startUtc)
        };

        if (types is { Length: > 0 })
        {
            filters.Add(filterBuilder.In(d => d.Type, types));
        }

        var filter = filterBuilder.And(filters);
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
                yield return DocumentMapper.ToOccurrence(document);
            }
        }
    }

    private static IClientSessionHandle? GetSession(ITransactionContext? transactionContext)
    {
        return transactionContext is IMongoTransactionContext mongoContext
            ? mongoContext.Session
            : null;
    }
}
