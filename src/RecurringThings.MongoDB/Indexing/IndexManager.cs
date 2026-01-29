namespace RecurringThings.MongoDB.Indexing;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;
using RecurringThings.MongoDB.Configuration;
using RecurringThings.MongoDB.Documents;

/// <summary>
/// Manages MongoDB index creation for RecurringThings collection.
/// </summary>
/// <remarks>
/// <para>
/// The IndexManager creates compound indexes optimized for the query patterns used by RecurringThings:
/// </para>
/// <list type="bullet">
/// <item>Query recurrences and occurrences by organization, resource path, type, and time range</item>
/// <item>Query exceptions and overrides by original time</item>
/// <item>Query overrides by new time range (for moved occurrences)</item>
/// <item>Cascade delete related documents by recurrence ID</item>
/// </list>
/// <para>
/// Index creation is idempotent - running multiple times has no effect if indexes already exist.
/// MongoDB handles concurrent index creation attempts gracefully.
/// </para>
/// </remarks>
public sealed class IndexManager
{
    private readonly IMongoCollection<RecurringThingDocument> _collection;
    private static volatile bool _indexesEnsured;
    private static readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexManager"/> class.
    /// </summary>
    /// <param name="database">The MongoDB database.</param>
    /// <param name="collectionName">The collection name.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="database"/> is null.</exception>
    public IndexManager(IMongoDatabase database, string collectionName = "recurring_things")
    {
        ArgumentNullException.ThrowIfNull(database);
        MongoDbInitializer.EnsureInitialized();
        _collection = database.GetCollection<RecurringThingDocument>(collectionName);
    }

    /// <summary>
    /// Ensures all required indexes exist on the collection.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method is designed to be called on first use. It uses a double-checked locking pattern
    /// to ensure indexes are only created once per application lifetime.
    /// </para>
    /// <para>
    /// If indexes already exist, this method completes quickly without modifying them.
    /// </para>
    /// </remarks>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        if (_indexesEnsured)
        {
            return;
        }

        lock (_lock)
        {
            if (_indexesEnsured)
            {
                return;
            }

            // Run synchronously within lock to ensure only one thread creates indexes
            EnsureIndexesInternalAsync(cancellationToken).GetAwaiter().GetResult();
            _indexesEnsured = true;
        }
    }

    /// <summary>
    /// Forces index creation regardless of whether they've been ensured before.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method is primarily intended for testing and administrative purposes.
    /// For normal application use, prefer <see cref="EnsureIndexesAsync"/>.
    /// </remarks>
    public Task ForceCreateIndexesAsync(CancellationToken cancellationToken = default)
    {
        return EnsureIndexesInternalAsync(cancellationToken);
    }

    private async Task EnsureIndexesInternalAsync(CancellationToken cancellationToken)
    {
        var indexModels = new List<CreateIndexModel<RecurringThingDocument>>
        {
            // Index 1: For recurrences, occurrences, and overrides by time range
            CreateIndexModel(
                "idx_time_range",
                Builders<RecurringThingDocument>.IndexKeys
                    .Hashed(d => d.Organization)
                    .Ascending(d => d.ResourcePath)
                    .Ascending(d => d.DocumentType)
                    .Ascending(d => d.StartTime)
                    .Ascending(d => d.EndTime)
                    .Ascending(d => d.Type)),

            // Index 2: For exceptions/overrides by original time
            CreateIndexModel(
                "idx_original_time",
                Builders<RecurringThingDocument>.IndexKeys
                    .Hashed(d => d.Organization)
                    .Ascending(d => d.ResourcePath)
                    .Ascending(d => d.DocumentType)
                    .Ascending(d => d.Type)
                    .Ascending(d => d.OriginalTimeUtc)),

            // Index 3: For cascade deletes by recurrence ID
            CreateIndexModel(
                "idx_cascade_delete",
                Builders<RecurringThingDocument>.IndexKeys
                    .Hashed(d => d.Organization)
                    .Ascending(d => d.RecurrenceId))
        };

        try
        {
            await _collection.Indexes.CreateManyAsync(indexModels, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MongoCommandException ex) when (ex.Code is 85 or 86)
        {
            // Code 85: IndexOptionsConflict - index with same name but different options
            // Code 86: IndexKeySpecsConflict - index with same key but different name
            // These are expected in concurrent creation scenarios or when indexes already exist
            // with slightly different definitions. MongoDB handles this gracefully.
        }
    }

    private static CreateIndexModel<RecurringThingDocument> CreateIndexModel(
        string name,
        IndexKeysDefinition<RecurringThingDocument> keys)
    {
        return new CreateIndexModel<RecurringThingDocument>(
            keys,
            new CreateIndexOptions
            {
                Name = name,
                Background = true
            });
    }

    /// <summary>
    /// Resets the index ensured flag, allowing indexes to be recreated.
    /// </summary>
    /// <remarks>
    /// This method is primarily intended for testing purposes.
    /// </remarks>
    internal static void ResetIndexFlag()
    {
        _indexesEnsured = false;
    }
}
