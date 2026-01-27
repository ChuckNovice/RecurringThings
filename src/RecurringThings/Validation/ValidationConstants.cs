namespace RecurringThings.Validation;

/// <summary>
/// Constants used for validation throughout the RecurringThings library.
/// </summary>
internal static class ValidationConstants
{
    /// <summary>
    /// Maximum length for Organization field.
    /// </summary>
    public const int MaxOrganizationLength = 100;

    /// <summary>
    /// Maximum length for ResourcePath field.
    /// </summary>
    public const int MaxResourcePathLength = 100;

    /// <summary>
    /// Minimum length for Type field.
    /// </summary>
    public const int MinTypeLength = 1;

    /// <summary>
    /// Maximum length for Type field.
    /// </summary>
    public const int MaxTypeLength = 100;

    /// <summary>
    /// Minimum length for RRule field.
    /// </summary>
    public const int MinRRuleLength = 1;

    /// <summary>
    /// Maximum length for RRule field.
    /// </summary>
    public const int MaxRRuleLength = 2000;

    /// <summary>
    /// Minimum length for TimeZone field.
    /// </summary>
    public const int MinTimeZoneLength = 1;

    /// <summary>
    /// Maximum length for TimeZone field.
    /// </summary>
    public const int MaxTimeZoneLength = 100;

    /// <summary>
    /// Minimum length for extension keys.
    /// </summary>
    public const int MinExtensionKeyLength = 1;

    /// <summary>
    /// Maximum length for extension keys.
    /// </summary>
    public const int MaxExtensionKeyLength = 100;

    /// <summary>
    /// Maximum length for extension values.
    /// </summary>
    public const int MaxExtensionValueLength = 1024;
}
