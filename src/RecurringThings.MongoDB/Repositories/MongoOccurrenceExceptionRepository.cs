namespace RecurringThings.MongoDB.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
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
/// MongoDB implementation of <see cref="IOccurrenceExceptionRepository"/>.
/// </summary>
internal sealed class MongoOccurrenceExceptionRepository : IOccurrenceExceptionRepository
{
    private readonly IMongoCollection<RecurringThingDocument> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoOccurrenceExceptionRepository"/> class.
    /// </summary>
    /// <param name="database">The MongoDB database.</param>
    /// <param name="collectionName">The collection name. Defaults to "recurring_things".</param>
    public MongoOccurrenceExceptionRepository(IMongoDatabase database, string collectionName = "recurring_things")
    {
        ArgumentNullException.ThrowIfNull(database);
        MongoDbInitializer.EnsureInitialized();
        _collection = database.GetCollection<RecurringThingDocument>(collectionName);
    }

    /// <inheritdoc/>
    public async Task<OccurrenceException> CreateAsync(
        OccurrenceException exception,
        ITransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var document = DocumentMapper.FromOccurrenceException(exception);
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
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Id, id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Exception));

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

        return document is null ? null : DocumentMapper.ToOccurrenceException(document);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<OccurrenceException> GetByRecurrenceIdAsync(
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
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Exception));

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
                yield return DocumentMapper.ToOccurrenceException(document);
            }
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

        // Cast to nullable Guid for filter compatibility
        var nullableIds = recurrenceIdList.Select(id => (Guid?)id).ToList();

        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.In(d => d.RecurrenceId, nullableIds),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Exception));

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
                yield return DocumentMapper.ToOccurrenceException(document);
            }
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
        var filter = Builders<RecurringThingDocument>.Filter.And(
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Id, id),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.Organization, organization),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.ResourcePath, resourcePath),
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Exception));

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
            Builders<RecurringThingDocument>.Filter.Eq(d => d.DocumentType, DocumentTypes.Exception));

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
