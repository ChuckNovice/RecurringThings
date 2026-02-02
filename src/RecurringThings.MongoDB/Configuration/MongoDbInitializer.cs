namespace RecurringThings.MongoDB.Configuration;

using System.Threading;
using global::MongoDB.Bson.Serialization.Conventions;

/// <summary>
/// Initializes MongoDB conventions when the assembly is loaded.
/// </summary>
/// <remarks>
/// This ensures that conventions are registered before any serialization occurs,
/// regardless of whether <see cref="MongoDbBuilderExtensions.UseMongoDb"/> is called.
/// </remarks>
internal static class MongoDbInitializer
{
    private static bool _initialized;
    private static readonly Lock _lock = new();

    /// <summary>
    /// Registers MongoDB conventions for RecurringThings documents.
    /// </summary>
    /// <remarks>
    /// This method is idempotent and can be called multiple times safely.
    /// It registers the following conventions:
    /// <list type="bullet">
    /// <item><description>CamelCaseElementNameConvention: Converts property names to camelCase</description></item>
    /// <item><description>IgnoreIfNullConvention: Excludes null properties from serialization</description></item>
    /// </list>
    /// </remarks>
    internal static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            var conventionPack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreIfNullConvention(true),
            };

            ConventionRegistry.Register(
                "RecurringThingsConventions",
                conventionPack,
                t => t.Namespace?.StartsWith("RecurringThings.MongoDB") == true);

            _initialized = true;
        }
    }
}
