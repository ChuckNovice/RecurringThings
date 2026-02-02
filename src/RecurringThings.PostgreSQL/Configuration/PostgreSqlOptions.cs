namespace RecurringThings.PostgreSQL.Configuration;

using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

/// <summary>
/// Configuration options for PostgreSQL persistence.
/// </summary>
public sealed class PostgreSqlOptions
{
    /// <summary>
    /// Gets or sets the PostgreSQL connection string.
    /// </summary>
    /// <remarks>
    /// Example: "Host=localhost;Database=myapp;Username=user;Password=pass"
    /// </remarks>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to run migrations on startup.
    /// </summary>
    /// <remarks>
    /// When true, the database schema will be automatically created/updated
    /// when the application starts. Defaults to true.
    /// </remarks>
    public bool RunMigrationsOnStartup { get; set; } = true;

    /// <summary>
    /// Optional callback to configure DbContext options for performance tuning, logging, etc.
    /// </summary>
    public Action<NpgsqlDbContextOptionsBuilder>? ConfigureNpgsql { get; set; }

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when required options are missing.</exception>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new ArgumentException("PostgreSQL connection string is required.", nameof(ConnectionString));
        }
    }
}
