namespace MercatoApp.Models;

/// <summary>
/// Represents filter criteria for product lists.
/// </summary>
public class ProductFilter
{
    /// <summary>
    /// Gets or sets the list of category IDs to filter by.
    /// </summary>
    public List<int>? CategoryIds { get; set; }

    /// <summary>
    /// Gets or sets the minimum price filter.
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Gets or sets the maximum price filter.
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Gets or sets the list of product conditions to filter by.
    /// </summary>
    public List<ProductCondition>? Conditions { get; set; }

    /// <summary>
    /// Gets or sets the list of store IDs to filter by (sellers).
    /// </summary>
    public List<int>? StoreIds { get; set; }

    /// <summary>
    /// Gets a value indicating whether any filters are active.
    /// </summary>
    public bool HasActiveFilters =>
        (CategoryIds != null && CategoryIds.Count > 0) ||
        MinPrice.HasValue ||
        MaxPrice.HasValue ||
        (Conditions != null && Conditions.Count > 0) ||
        (StoreIds != null && StoreIds.Count > 0);
}
