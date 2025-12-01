namespace MercatoApp.Models;

/// <summary>
/// Represents available sort options for product listings.
/// </summary>
public enum ProductSortOption
{
    /// <summary>
    /// Sort by relevance (for search results - title matches prioritized).
    /// </summary>
    Relevance,

    /// <summary>
    /// Sort by newest products first (creation date descending).
    /// </summary>
    Newest,

    /// <summary>
    /// Sort by price ascending (low to high).
    /// </summary>
    PriceAscending,

    /// <summary>
    /// Sort by price descending (high to low).
    /// </summary>
    PriceDescending
}
