namespace MercatoApp.Models;

/// <summary>
/// Represents an error that occurred during bulk update for a specific product.
/// </summary>
public class ProductBulkUpdateError
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current value before the update.
    /// </summary>
    public decimal? CurrentValue { get; set; }

    /// <summary>
    /// Gets or sets the attempted new value.
    /// </summary>
    public decimal? AttemptedValue { get; set; }
}
