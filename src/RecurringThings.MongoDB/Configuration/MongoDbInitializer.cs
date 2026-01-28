namespace RecurringThings.MongoDB.Configuration;

using System;
using global::MongoDB.Bson.Serialization.Conventions;

/// <summary>
/// Initializes MongoDB conventions when the assembly is loaded.
/// </summary>
/// <remarks>
/// This ensures that conventions are registered before any serialization occurs,
/// regardless of whether <see cref="RecurringThingsBuilderExtensions.UseMongoDb"/> is called.
/// </remarks>
internal static class MongoDbInitializer
{
    private static bool _initialized;
    private static readonly object _lock = new();

    /// <summary>
    /// Registers MongoDB conventions for RecurringThings documents.
    /// </summary>
    /// <remarks>
    /// This method is idempotent and can be called multiple times safely.
    /// It registers the following conventions:
    /// <list type="bullet">
    /// <item><description>NamedIdMemberConvention: Maps "Id" property to MongoDB "_id" field</description></item>
    /// <item><description>CamelCaseElementNameConvention: Converts property names to camelCase</description></item>
    /// <item><description>IgnoreIfNullConvention: Excludes null properties from serialization</description></item>
    /// <item><description>GuidStringRepresentationConvention: Serializes Guid properties as strings</description></item>
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
                new NamedIdMemberConvention("Id"),
                new CamelCaseElementNameConvention(),
                new IgnoreIfNullConvention(true),
                new GuidStringRepresentationConvention()
            };

            ConventionRegistry.Register(
                "RecurringThingsConventions",
                conventionPack,
                t => t.FullName?.StartsWith("RecurringThings", StringComparison.Ordinal) == true);

            _initialized = true;
        }
    }

}
