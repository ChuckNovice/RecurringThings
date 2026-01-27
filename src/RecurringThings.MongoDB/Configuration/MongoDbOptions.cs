namespace RecurringThings.MongoDB.Configuration;

/// <summary>
/// Configuration options for MongoDB persistence.
/// </summary>
public sealed class MongoDbOptions
{
    /// <summary>
    /// Gets or sets the MongoDB connection string.
    /// </summary>
    /// <remarks>
    /// Example: "mongodb://localhost:27017" or "mongodb+srv://user:pass@cluster.mongodb.net"
    /// </remarks>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection name for storing recurring things.
    /// </summary>
    /// <remarks>
    /// Defaults to "recurring_things". All document types (recurrences, occurrences,
    /// exceptions, and overrides) are stored in this single collection with a discriminator field.
    /// </remarks>
    public string CollectionName { get; set; } = "recurring_things";

    /// <summary>
    /// Gets or sets a value indicating whether to create indexes on startup.
    /// </summary>
    /// <remarks>
    /// When true, compound indexes will be created on the collection when the application starts.
    /// This is idempotent - running multiple times has no effect if indexes already exist.
    /// Defaults to true.
    /// </remarks>
    public bool CreateIndexesOnStartup { get; set; } = true;

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="System.ArgumentException">Thrown when required options are missing.</exception>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new System.ArgumentException("MongoDB connection string is required.", nameof(ConnectionString));
        }

        if (string.IsNullOrWhiteSpace(DatabaseName))
        {
            throw new System.ArgumentException("MongoDB database name is required.", nameof(DatabaseName));
        }
    }
}
