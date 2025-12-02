using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an item in an order.
/// Captures the product details at the time of order.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the order item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the order ID that owns this item.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order that owns this item (navigation property).
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Gets or sets the seller sub-order ID that this item belongs to.
    /// </summary>
    public int? SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order (navigation property).
    /// </summary>
    public SellerSubOrder? SellerSubOrder { get; set; }

    /// <summary>
    /// Gets or sets the store ID (seller).
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

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
    /// Gets or sets the product title at the time of order.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the variant description (e.g., "Size: L, Color: Red").
    /// </summary>
    [MaxLength(500)]
    public string? VariantDescription { get; set; }

    /// <summary>
    /// Gets or sets the quantity ordered.
    /// </summary>
    [Required]
    [Range(1, 999)]
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price at the time of order.
    /// </summary>
    [Required]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the subtotal for this item (quantity * unit price).
    /// </summary>
    [Required]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Gets or sets the tax amount for this item.
    /// </summary>
    public decimal TaxAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the fulfillment status of this item.
    /// Enables partial fulfillment tracking within a sub-order.
    /// </summary>
    public OrderItemStatus Status { get; set; } = OrderItemStatus.New;

    /// <summary>
    /// Gets or sets the quantity that has been shipped.
    /// For partial fulfillment, this can be less than Quantity.
    /// </summary>
    [Range(0, 999)]
    public int QuantityShipped { get; set; } = 0;

    /// <summary>
    /// Gets or sets the quantity that has been cancelled.
    /// For partial cancellation, this can be less than Quantity.
    /// </summary>
    [Range(0, 999)]
    public int QuantityCancelled { get; set; } = 0;

    /// <summary>
    /// Gets or sets the amount that has been refunded for this item.
    /// Used when items are partially or fully cancelled.
    /// </summary>
    public decimal RefundedAmount { get; set; } = 0;
}
