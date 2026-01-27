namespace RecurringThings.Validation;

using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using NodaTime;

/// <summary>
/// Extension methods for FluentValidation rule builders.
/// </summary>
internal static class FluentValidationExtensions
{
    /// <summary>
    /// Validates that a DateTime has Kind == Utc.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>Rule builder options for chaining.</returns>
    public static IRuleBuilderOptions<T, DateTime> MustBeUtc<T>(
        this IRuleBuilder<T, DateTime> ruleBuilder)
    {
        return ruleBuilder
            .Must(dt => dt.Kind == DateTimeKind.Utc)
            .WithMessage("{PropertyName} must be in UTC (Kind must be DateTimeKind.Utc). Actual Kind: {PropertyValue}.");
    }

    /// <summary>
    /// Validates that a string is a valid IANA time zone identifier.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>Rule builder options for chaining.</returns>
    public static IRuleBuilderOptions<T, string> MustBeValidIanaTimeZone<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(timeZone => DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZone) is not null)
            .WithMessage("TimeZone '{PropertyValue}' is not a valid IANA time zone identifier.");
    }

    /// <summary>
    /// Validates that a Duration (TimeSpan) is positive.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>Rule builder options for chaining.</returns>
    public static IRuleBuilderOptions<T, TimeSpan> MustBePositive<T>(
        this IRuleBuilder<T, TimeSpan> ruleBuilder)
    {
        return ruleBuilder
            .Must(duration => duration > TimeSpan.Zero)
            .WithMessage("Duration must be positive.");
    }

    /// <summary>
    /// Validates an extensions dictionary ensuring keys are 1-100 chars and values are 0-1024 chars, all non-null.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <returns>Rule builder options for chaining.</returns>
    public static IRuleBuilderOptions<T, Dictionary<string, string>?> ValidExtensions<T>(
        this IRuleBuilder<T, Dictionary<string, string>?> ruleBuilder)
    {
        return ruleBuilder
            .Must(ValidateExtensionsDictionary)
            .WithMessage(GetExtensionsErrorMessage);
    }

    private static bool ValidateExtensionsDictionary(Dictionary<string, string>? extensions)
    {
        if (extensions is null)
        {
            return true;
        }

        foreach (var (key, value) in extensions)
        {
            if (key is null)
            {
                return false;
            }

            if (key.Length < ValidationConstants.MinExtensionKeyLength)
            {
                return false;
            }

            if (key.Length > ValidationConstants.MaxExtensionKeyLength)
            {
                return false;
            }

            if (value is null)
            {
                return false;
            }

            if (value.Length > ValidationConstants.MaxExtensionValueLength)
            {
                return false;
            }
        }

        return true;
    }

    private static string GetExtensionsErrorMessage<T>(T instance, Dictionary<string, string>? extensions)
    {
        if (extensions is null)
        {
            return string.Empty;
        }

        foreach (var (key, value) in extensions)
        {
            if (key is null)
            {
                return "Extension keys cannot be null.";
            }

            if (key.Length < ValidationConstants.MinExtensionKeyLength)
            {
                return $"Extension keys must be at least {ValidationConstants.MinExtensionKeyLength} character(s). Found empty key.";
            }

            if (key.Length > ValidationConstants.MaxExtensionKeyLength)
            {
                return $"Extension keys must not exceed {ValidationConstants.MaxExtensionKeyLength} characters. Key '{key}' has length {key.Length}.";
            }

            if (value is null)
            {
                return $"Extension values cannot be null. Key: '{key}'.";
            }

            if (value.Length > ValidationConstants.MaxExtensionValueLength)
            {
                return $"Extension values must not exceed {ValidationConstants.MaxExtensionValueLength} characters. Key '{key}' has value length {value.Length}.";
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if the validation result is not valid.
    /// Uses the first validation error to construct the exception.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void ThrowIfInvalid(this ValidationResult result)
    {
        if (!result.IsValid)
        {
            var error = result.Errors.First();
            throw new ArgumentException(error.ErrorMessage, error.PropertyName);
        }
    }
}
