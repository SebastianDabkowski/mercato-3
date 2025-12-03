using System.Text;
using System.Text.RegularExpressions;

namespace MercatoApp.Helpers;

/// <summary>
/// Utility class for generating URL-friendly slugs from strings.
/// </summary>
public static class SlugGenerator
{
    /// <summary>
    /// Generates a URL-friendly slug from the given text.
    /// </summary>
    /// <param name="text">The text to convert to a slug.</param>
    /// <returns>A URL-friendly slug.</returns>
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Convert to lowercase
        var slug = text.ToLowerInvariant();

        // Remove accents and diacritics
        slug = RemoveDiacritics(slug);

        // Replace spaces and underscores with hyphens
        slug = Regex.Replace(slug, @"[\s_]+", "-");

        // Remove invalid characters (keep only alphanumeric and hyphens)
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Remove consecutive hyphens
        slug = Regex.Replace(slug, @"-{2,}", "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        // Limit length to 150 characters
        if (slug.Length > 150)
        {
            slug = slug.Substring(0, 150).TrimEnd('-');
        }

        return slug;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
