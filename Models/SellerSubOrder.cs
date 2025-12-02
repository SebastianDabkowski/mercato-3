using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a seller-specific portion of a parent order.
/// When a buyer's order contains items from multiple sellers, each seller gets their own sub-order.
/// </summary>
public class SellerSubOrder
{
    /// <summary>
    /// Gets or sets the unique identifier for the sub-order.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the parent order ID.
    /// </summary>
    public int ParentOrderId { get; set; }

    /// <summary>
    /// Gets or sets the parent order (navigation property).
    /// </summary>
    public Order ParentOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the store ID (seller) that this sub-order belongs to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sub-order number (human-readable).
    /// Format: {ParentOrderNumber}-{SellerSequence} (e.g., "ORD-20241202-12345-1")
    /// </summary>
    [Required]
    [MaxLength(60)]
    public string SubOrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order status specific to this seller's portion.
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>
    /// Gets or sets the subtotal for items in this sub-order.
    /// </summary>
    [Required]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Gets or sets the shipping cost for this seller's items.
    /// </summary>
    [Required]
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Gets or sets the total amount for this sub-order (subtotal + shipping).
    /// </summary>
    [Required]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the shipping method ID selected for this sub-order.
    /// </summary>
    public int? ShippingMethodId { get; set; }

    /// <summary>
    /// Gets or sets the shipping method (navigation property).
    /// </summary>
    public ShippingMethod? ShippingMethod { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the sub-order was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the sub-order was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the order items belonging to this sub-order (navigation property).
    /// Note: Items are linked via StoreId matching
    /// </summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
