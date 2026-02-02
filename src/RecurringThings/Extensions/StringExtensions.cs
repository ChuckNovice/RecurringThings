namespace RecurringThings.Extensions;

/// <summary>
/// Extension methods for string collections.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Determines whether the collection contains duplicate strings.
    /// </summary>
    /// <param name="source">The source collection to check.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <returns><c>true</c> if a duplicate is found; otherwise, <c>false</c>.</returns>
    public static bool HasDuplicate(this IEnumerable<string> source, StringComparer comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(comparer);

        var seen = new HashSet<string>(comparer);

        foreach (var item in source)
        {
            if (!seen.Add(item))
            {
                return true;
            }
        }

        return false;
    }
}
