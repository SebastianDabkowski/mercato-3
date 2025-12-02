namespace MercatoApp.Models;

/// <summary>
/// Represents available sort options for product reviews.
/// </summary>
public enum ReviewSortOption
{
    /// <summary>
    /// Sort by newest reviews first (creation date descending).
    /// </summary>
    Newest,

    /// <summary>
    /// Sort by highest rating first (rating descending, then newest).
    /// </summary>
    HighestRating,

    /// <summary>
    /// Sort by lowest rating first (rating ascending, then newest).
    /// </summary>
    LowestRating
}
