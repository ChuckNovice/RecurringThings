namespace RecurringThings.MongoDB.Indexing;

using global::MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurringThings.MongoDB.Configuration;
using RecurringThings.MongoDB.Documents;

/// <summary>
/// Manages MongoDB index creation for RecurringThings collection.
/// </summary>
/// <remarks>
/// Index creation is idempotent - running multiple times has no effect if indexes already exist.
/// MongoDB handles concurrent index creation attempts gracefully.
/// </remarks>
public sealed class IndexManager
{
    private readonly IMongoDatabase _database;
    private readonly IOptions<MongoDbOptions> _options;
    private volatile bool _indexesEnsured;
    private readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexManager"/> class.
    /// </summary>
    /// <param name="database">The MongoDB database.</param>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="database"/> or <paramref name="options"/> is null.</exception>
    public IndexManager(
        [FromKeyedServices("RecurringThings_MongoDatabase")] IMongoDatabase database,
        IOptions<MongoDbOptions> options)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(options);

        _database = database;
        _options = options;
    }

    /// <summary>
    /// Ensures all required indexes exist on the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is designed to be called on first use. It uses a double-checked locking pattern
    /// to ensure indexes are only created once per application lifetime.
    /// </para>
    /// <para>
    /// If indexes already exist, this method completes quickly without modifying them.
    /// </para>
    /// </remarks>
    public void EnsureIndexes()
    {
        if (!_options.Value.CreateIndexesOnStartup)
            return;

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

            var collection = _database.GetCollection<EventDocument>(_options.Value.CollectionName);
            var indexKeysDefinition = Builders<EventDocument>.IndexKeys;

            var indexes = new List<CreateIndexModel<EventDocument>>
            {
                // Primary index for user-specific queries (UserMode.Specific and UserMode.TenantWide)
                // Supports: TenantId + UserId + date range filtering
                new(indexKeysDefinition
                    .Ascending(d => d.TenantId)
                    .Ascending(d => d.UserId)
                    .Ascending(d => d.EndDate),
                    new CreateIndexOptions { Name = "ix_tenant_user_enddate" }),

                // Tenant-wide index for admin/reporting queries (UserMode.All)
                // Supports: TenantId + date range filtering without UserId
                new(indexKeysDefinition
                    .Ascending(d => d.TenantId)
                    .Ascending(d => d.EndDate),
                    new CreateIndexOptions { Name = "ix_tenant_enddate" }),

                // Categories multikey index for category filtering
                // Supports: TenantId + InCategories() filter
                new(indexKeysDefinition
                    .Ascending(d => d.TenantId)
                    .Ascending(d => d.Categories),
                    new CreateIndexOptions { Name = "ix_tenant_categories" })
            };

            collection.Indexes.CreateMany(indexes);

            _indexesEnsured = true;
        }
    }
}
