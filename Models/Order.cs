using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an order placed by a buyer.
/// Each order can contain items from multiple sellers (multi-vendor).
/// </summary>
public class Order
{
    /// <summary>
    /// Gets or sets the unique identifier for the order.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the order number (human-readable).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID (null for guest orders).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user (navigation property).
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the guest email for order tracking (used when UserId is null).
    /// </summary>
    [EmailAddress]
    [MaxLength(256)]
    public string? GuestEmail { get; set; }

    /// <summary>
    /// Gets or sets the delivery address ID.
    /// </summary>
    public int DeliveryAddressId { get; set; }

    /// <summary>
    /// Gets or sets the delivery address (navigation property).
    /// </summary>
    public Address DeliveryAddress { get; set; } = null!;

    /// <summary>
    /// Gets or sets the order status.
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>
    /// Gets or sets the subtotal (sum of all items before shipping).
    /// </summary>
    [Required]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Gets or sets the total shipping cost.
    /// </summary>
    [Required]
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Gets or sets the tax amount (if applicable).
    /// </summary>
    public decimal TaxAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the total amount (subtotal + shipping + tax).
    /// </summary>
    [Required]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the selected payment method ID.
    /// </summary>
    public int? PaymentMethodId { get; set; }

    /// <summary>
    /// Gets or sets the selected payment method (navigation property).
    /// </summary>
    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Gets or sets the payment status for this order.
    /// </summary>
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Gets or sets the date and time when the order was placed.
    /// </summary>
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the order was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the order items (navigation property).
    /// </summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    /// <summary>
    /// Gets or sets the shipping methods selected for each seller (navigation property).
    /// </summary>
    public ICollection<OrderShippingMethod> ShippingMethods { get; set; } = new List<OrderShippingMethod>();

    /// <summary>
    /// Gets or sets the payment transactions for this order (navigation property).
    /// </summary>
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    /// <summary>
    /// Gets or sets the seller sub-orders for this parent order (navigation property).
    /// Each sub-order represents a seller-specific portion of the order.
    /// </summary>
    public ICollection<SellerSubOrder> SubOrders { get; set; } = new List<SellerSubOrder>();
}
