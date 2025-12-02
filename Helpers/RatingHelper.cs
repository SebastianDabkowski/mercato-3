namespace MercatoApp.Helpers;

/// <summary>
/// Helper class for rating display utilities.
/// </summary>
public static class RatingHelper
{
    /// <summary>
    /// Determines the star icon class to display based on position and rating value.
    /// </summary>
    /// <param name="position">The position of the star (1-5).</param>
    /// <param name="rating">The rating value.</param>
    /// <returns>The Bootstrap icon class to use for the star.</returns>
    public static string GetStarIconClass(int position, decimal rating)
    {
        if (position <= Math.Floor(rating))
        {
            return "bi-star-fill";
        }
        else if (position == Math.Ceiling(rating) && rating % 1 >= 0.5M)
        {
            return "bi-star-half";
        }
        else
        {
            return "bi-star";
        }
    }

    /// <summary>
    /// Formats a rating value for display.
    /// </summary>
    /// <param name="rating">The rating value.</param>
    /// <returns>The formatted rating string.</returns>
    public static string FormatRating(decimal rating)
    {
        return rating.ToString("0.0");
    }

    /// <summary>
    /// Gets the plural form for "rating" based on count.
    /// </summary>
    /// <param name="count">The rating count.</param>
    /// <returns>"rating" or "ratings" based on count.</returns>
    public static string GetRatingLabel(int count)
    {
        return count == 1 ? "rating" : "ratings";
    }
}
