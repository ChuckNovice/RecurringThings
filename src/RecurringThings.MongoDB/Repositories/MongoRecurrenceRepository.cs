namespace RecurringThings.MongoDB.Repositories;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;
using RecurringThings.Domain;
using RecurringThings.MongoDB.Configuration;
using RecurringThings.MongoDB.Documents;
using RecurringThings.Repository;
using Transactional.Abstractions;
using Transactional.MongoDB;

/// <summary>
/// MongoDB implementation of <see cref="IRecurrenceRepository"/>.
/// </summary>
internal sealed class MongoRecurrenceRepository : IRecurrenceRepository
{
    private readonly IMongoCollection<RecurringThingDocument> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoRecurrenceRepository"/> class.
    /// </summary>
    /// <param name="database">The MongoDB database.</param>
    /// <param name="collectionName">The collection name. Defaults to "recurring_things".</param>
    public MongoRecurrenceRepository(IMongoDatabase database, string collectionName = "recurring_things")
    {
        ArgumentNullException.ThrowIfNull(database);
        MongoDbInitializer.EnsureInitialized();
        _collection = database.GetCollection<RecurringThingDocument>(collectionName);
    }

    /// <inheritdoc/>
    public async Task<Recurrence> CreateAsync(
        Recurrence recurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recurrence);

        var document = DocumentMapper.FromRecurrence(recurrence);
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
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Id, id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Recurrence));

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

        return document is null ? null : DocumentMapper.ToRecurrence(document);
    }

    /// <inheritdoc/>
    public async Task<Recurrence> UpdateAsync(
        Recurrence recurrence,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recurrence);

        var document = DocumentMapper.FromRecurrence(recurrence);
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Id, recurrence.Id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, recurrence.Organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, recurrence.ResourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Recurrence));

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
        var session = GetSession(transactionContext);

        // Delete the recurrence
        var recurrenceFilter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Id, id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Recurrence));

        // Delete related exceptions and overrides (cascade delete)
        var relatedFilter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.RecurrenceId, id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.In(d => d.DocumentType,
                [DocumentTypes.Exception, DocumentTypes.Override]));

        if (session is not null)
        {
            await _collection.DeleteManyAsync(session, relatedFilter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await _collection.DeleteOneAsync(session, recurrenceFilter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await _collection.DeleteManyAsync(relatedFilter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await _collection.DeleteOneAsync(recurrenceFilter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
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
        var filterBuilder = Builders<RecurringThingDocument>.Filter;
        var filters = new List<FilterDefinition<RecurringThingDocument>>
        {
            filterBuilder.Eq(d => d.Organization, organization),
            filterBuilder.Eq(d => d.ResourcePath, resourcePath),
            filterBuilder.Eq(d => d.DocumentType, DocumentTypes.Recurrence),
            // StartTime <= endUtc AND RecurrenceEndTime >= startUtc
            filterBuilder.Lte(d => d.StartTime, endUtc),
            filterBuilder.Gte(d => d.RecurrenceEndTime, startUtc)
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
                yield return DocumentMapper.ToRecurrence(document);
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
