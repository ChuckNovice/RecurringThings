namespace RecurringThings.PostgreSQL.Data;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time DbContext factory for EF Core CLI tools (dotnet ef migrations).
/// </summary>
/// <remarks>
/// <para>
/// This factory is used by the EF Core CLI tools to create a DbContext instance
/// at design time for generating migrations.
/// </para>
/// <para>
/// Set the <c>POSTGRES_CONNECTION_STRING</c> environment variable before running
/// migration commands.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// # Set connection string
/// $env:POSTGRES_CONNECTION_STRING = "Host=localhost;Database=mydb;Username=user;Password=pass"
///
/// # Generate a migration
/// dotnet ef migrations add InitialCreate --project src/RecurringThings.PostgreSQL
///
/// # Update database
/// dotnet ef database update --project src/RecurringThings.PostgreSQL
/// </code>
/// </example>
internal sealed class RecurringThingsDbContextFactory : IDesignTimeDbContextFactory<RecurringThingsDbContext>
{
    /// <inheritdoc/>
    public RecurringThingsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "POSTGRES_CONNECTION_STRING environment variable is not set. " +
                "Set it before running EF Core CLI commands.");

        var optionsBuilder = new DbContextOptionsBuilder<RecurringThingsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new RecurringThingsDbContext(optionsBuilder.Options);
    }
}
