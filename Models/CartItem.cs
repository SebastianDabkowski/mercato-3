using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an item in a shopping cart.
/// </summary>
public class CartItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the cart item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the cart ID that owns this item.
    /// </summary>
    public int CartId { get; set; }

    /// <summary>
    /// Gets or sets the cart that owns this item (navigation property).
    /// </summary>
    public Cart Cart { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product (navigation property).
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product variant ID (null for simple products).
    /// </summary>
    public int? ProductVariantId { get; set; }

    /// <summary>
    /// Gets or sets the product variant (navigation property).
    /// </summary>
    public ProductVariant? ProductVariant { get; set; }

    /// <summary>
    /// Gets or sets the quantity of this item in the cart.
    /// </summary>
    [Required]
    [Range(1, 999)]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Gets or sets the price at which the item was added to the cart.
    /// This captures the price at the time of adding to prevent price changes from affecting cart totals.
    /// </summary>
    [Required]
    public decimal PriceAtAdd { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the item was added to the cart.
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the item was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
