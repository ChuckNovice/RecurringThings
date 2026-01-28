using BenchmarkDotNet.Running;
using RecurringThings.Benchmarks.Benchmarks;
using RecurringThings.Benchmarks.Infrastructure;

Console.WriteLine("=== RecurringThings Benchmarks ===");
Console.WriteLine();

// Check environment variables
var mongoAvailable = ProviderFactory.IsMongoAvailable();
var postgresAvailable = ProviderFactory.IsPostgresAvailable();

Console.WriteLine("Provider availability:");
Console.WriteLine($"  MongoDB:    {(mongoAvailable ? "Available" : "NOT AVAILABLE (set MONGODB_CONNECTION_STRING)")}");
Console.WriteLine($"  PostgreSQL: {(postgresAvailable ? "Available" : "NOT AVAILABLE (set POSTGRES_CONNECTION_STRING)")}");
Console.WriteLine();

if (!mongoAvailable && !postgresAvailable)
{
    Console.WriteLine("ERROR: No database providers available. Set at least one connection string.");
    return 1;
}

// Initialize providers BEFORE benchmarks (migrations/indexes don't count in benchmark time)
Console.WriteLine("=== Initializing Providers (migrations/indexes) ===");
if (mongoAvailable)
{
    await ProviderFactory.InitializeProviderAsync(BenchmarkProvider.MongoDB);
}

if (postgresAvailable)
{
    await ProviderFactory.InitializeProviderAsync(BenchmarkProvider.PostgreSQL);
}

Console.WriteLine();

// Clean up any leftover data from previous runs
Console.WriteLine("=== Cleaning up previous benchmark data ===");
if (mongoAvailable)
{
    Console.WriteLine("Cleaning MongoDB...");
    var mongoEngine = ProviderFactory.CreateEngine(BenchmarkProvider.MongoDB);
    await DataSeeder.CleanupAllAsync(mongoEngine);
}

if (postgresAvailable)
{
    Console.WriteLine("Cleaning PostgreSQL...");
    var pgEngine = ProviderFactory.CreateEngine(BenchmarkProvider.PostgreSQL);
    await DataSeeder.CleanupAllAsync(pgEngine);
}

Console.WriteLine();

Console.WriteLine("=== Starting Benchmarks ===");
Console.WriteLine("BenchmarkDotNet will now run. Progress will be shown for each benchmark.");
Console.WriteLine();

// Run benchmarks
if (args.Length == 0)
{
    BenchmarkRunner.Run<QueryBenchmarks>(new BenchmarkConfig());
    BenchmarkRunner.Run<CreateBenchmarks>(new BenchmarkConfig());
    BenchmarkRunner.Run<UpdateBenchmarks>(new BenchmarkConfig());
    BenchmarkRunner.Run<DeleteBenchmarks>(new BenchmarkConfig());
}
else
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new BenchmarkConfig());
}

// Final cleanup
Console.WriteLine();
Console.WriteLine("=== Final Cleanup ===");
if (mongoAvailable)
{
    Console.WriteLine("Cleaning MongoDB...");
    var mongoEngine = ProviderFactory.CreateEngine(BenchmarkProvider.MongoDB);
    await DataSeeder.CleanupAllAsync(mongoEngine);
}

if (postgresAvailable)
{
    Console.WriteLine("Cleaning PostgreSQL...");
    var pgEngine = ProviderFactory.CreateEngine(BenchmarkProvider.PostgreSQL);
    await DataSeeder.CleanupAllAsync(pgEngine);
}

Console.WriteLine();
Console.WriteLine("=== Benchmarks Complete ===");
Console.WriteLine("Results available in ./BenchmarkResults/");

return 0;
