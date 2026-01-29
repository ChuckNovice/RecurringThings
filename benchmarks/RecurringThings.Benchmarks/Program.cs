using BenchmarkDotNet.Running;
using RecurringThings.Benchmarks.Benchmarks;
using RecurringThings.Benchmarks.Infrastructure;

Console.WriteLine("=== RecurringThings Read Performance Benchmarks ===");
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

// Show benchmark configuration
Console.WriteLine("Benchmark configuration:");
Console.WriteLine($"  Data volumes: [{string.Join(", ", BenchmarkOptions.DataVolumes)}]");
Console.WriteLine($"  Concurrent requests: [{string.Join(", ", BenchmarkOptions.ConcurrentRequests)}]");
Console.WriteLine($"  Query range: {BenchmarkOptions.QueryStartUtc:yyyy-MM-dd} to {BenchmarkOptions.QueryEndUtc:yyyy-MM-dd}");
Console.WriteLine();

// Show database naming
Console.WriteLine("Database naming (persistent per volume):");
foreach (var volume in BenchmarkOptions.DataVolumes)
{
    Console.WriteLine($"  Volume {volume}: rt_bench_v{volume}");
}
Console.WriteLine();

Console.WriteLine("=== Starting Benchmarks ===");
Console.WriteLine("BenchmarkDotNet will measure response time for concurrent read queries.");
Console.WriteLine("Data is seeded once per (provider, volume) and reused across runs.");
Console.WriteLine();

// Run the single read performance benchmark
// Config is applied via [Config(typeof(BenchmarkConfig))] attribute on the benchmark class
BenchmarkRunner.Run<ReadPerformanceBenchmark>();

// Generate charts from CSV results
var csvPath = "./BenchmarkResults/results/RecurringThings.Benchmarks.Benchmarks.ReadPerformanceBenchmark-report.csv";
if (File.Exists(csvPath))
{
    Console.WriteLine();
    Console.WriteLine("=== Generating Charts ===");
    ChartGenerator.GenerateCharts(csvPath, "./BenchmarkResults/charts");
}

Console.WriteLine();
Console.WriteLine("=== Benchmarks Complete ===");
Console.WriteLine("Results available in ./BenchmarkResults/");
Console.WriteLine("Charts available in ./BenchmarkResults/charts/<theme>/");
Console.WriteLine();
Console.WriteLine("Note: Benchmark databases persist for reuse. To clean up manually:");
foreach (var volume in BenchmarkOptions.DataVolumes)
{
    Console.WriteLine($"  - MongoDB: db.dropDatabase() on rt_bench_v{volume}");
    Console.WriteLine($"  - PostgreSQL: DROP DATABASE rt_bench_v{volume}");
}

return 0;
