namespace MercatoApp.Helpers;

/// <summary>
/// Extension methods for string operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Parses a comma-separated string into a list of trimmed, non-empty values.
    /// </summary>
    /// <param name="value">The comma-separated string to parse.</param>
    /// <returns>A list of parsed values, or an empty list if the input is null or empty.</returns>
    public static List<string> ParseCommaSeparated(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    /// <summary>
    /// Gets the first value from a comma-separated string.
    /// </summary>
    /// <param name="value">The comma-separated string.</param>
    /// <returns>The first value, or null if the input is null or empty.</returns>
    public static string? GetFirstCommaSeparatedValue(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }
}
