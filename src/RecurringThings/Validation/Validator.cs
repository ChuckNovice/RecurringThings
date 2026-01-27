namespace RecurringThings.Validation;

using System;
using RecurringThings.Domain;

/// <summary>
/// Provides validation logic for RecurringThings entities and request models.
/// </summary>
/// <remarks>
/// Most entity validation is handled by FluentValidation validators in the Validators namespace.
/// This class contains relationship validation that doesn't fit the FluentValidation pattern.
/// </remarks>
public static class Validator
{
    /// <summary>
    /// Validates that an exception or override belongs to the same tenant scope as its parent recurrence.
    /// </summary>
    /// <param name="parentRecurrence">The parent recurrence.</param>
    /// <param name="childOrganization">The child entity's organization.</param>
    /// <param name="childResourcePath">The child entity's resource path.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the child entity has a different Organization or ResourcePath than the parent.
    /// </exception>
    public static void ValidateTenantScope(
        Recurrence parentRecurrence,
        string childOrganization,
        string childResourcePath)
    {
        ArgumentNullException.ThrowIfNull(parentRecurrence);

        if (!string.Equals(parentRecurrence.Organization, childOrganization, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Organization mismatch: exception/override Organization '{childOrganization}' " +
                $"must match parent recurrence Organization '{parentRecurrence.Organization}'.");
        }

        if (!string.Equals(parentRecurrence.ResourcePath, childResourcePath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ResourcePath mismatch: exception/override ResourcePath '{childResourcePath}' " +
                $"must match parent recurrence ResourcePath '{parentRecurrence.ResourcePath}'.");
        }
    }

    /// <summary>
    /// Validates that the types filter is not empty.
    /// </summary>
    /// <param name="types">The types array to validate.</param>
    /// <exception cref="ArgumentException">Thrown when types is an empty array.</exception>
    public static void ValidateTypesFilter(string[]? types)
    {
        if (types is { Length: 0 })
        {
            throw new ArgumentException("Types filter cannot be an empty array. Use null to include all types.", nameof(types));
        }
    }
}
