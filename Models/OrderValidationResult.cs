namespace MercatoApp.Models;

/// <summary>
/// Represents the result of order validation before placing an order.
/// </summary>
public class OrderValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the list of stock validation issues.
    /// </summary>
    public List<StockValidationIssue> StockIssues { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of price validation issues.
    /// </summary>
    public List<PriceValidationIssue> PriceIssues { get; set; } = new();

    /// <summary>
    /// Gets or sets a general error message if validation fails for other reasons.
    /// </summary>
    public string? GeneralError { get; set; }
}

/// <summary>
/// Represents a stock availability issue for a cart item.
/// </summary>
public class StockValidationIssue
{
    /// <summary>
    /// Gets or sets the cart item ID.
    /// </summary>
    public int CartItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product variant ID (null for simple products).
    /// </summary>
    public int? ProductVariantId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the variant description (e.g., "Size: L, Color: Red").
    /// </summary>
    public string? VariantDescription { get; set; }

    /// <summary>
    /// Gets or sets the requested quantity.
    /// </summary>
    public int RequestedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the available stock quantity.
    /// </summary>
    public int AvailableStock { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Represents a price change issue for a cart item.
/// </summary>
public class PriceValidationIssue
{
    /// <summary>
    /// Gets or sets the cart item ID.
    /// </summary>
    public int CartItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product variant ID (null for simple products).
    /// </summary>
    public int? ProductVariantId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the variant description (e.g., "Size: L, Color: Red").
    /// </summary>
    public string? VariantDescription { get; set; }

    /// <summary>
    /// Gets or sets the price when the item was added to the cart.
    /// </summary>
    public decimal PriceInCart { get; set; }

    /// <summary>
    /// Gets or sets the current product price.
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
