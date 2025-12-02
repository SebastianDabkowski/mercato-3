using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MercatoApp.Models;

/// <summary>
/// Represents an analytics event for tracking user behavior and business metrics.
/// Events are stored for Phase 2 advanced analytics, funnels, and cohort analysis.
/// All data is collected in compliance with privacy and cookie/consent policies.
/// </summary>
public class AnalyticsEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for the analytics event.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the type of analytics event.
    /// </summary>
    public AnalyticsEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with this event.
    /// Null for anonymous/guest users.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the User.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the session identifier for anonymous users.
    /// Used to track behavior across a browsing session without requiring login.
    /// </summary>
    [MaxLength(256)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the product ID related to this event (if applicable).
    /// Used for product view, add to cart, product click events.
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the Product.
    /// </summary>
    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    /// <summary>
    /// Gets or sets the product variant ID related to this event (if applicable).
    /// </summary>
    public int? ProductVariantId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the ProductVariant.
    /// </summary>
    [ForeignKey(nameof(ProductVariantId))]
    public ProductVariant? ProductVariant { get; set; }

    /// <summary>
    /// Gets or sets the category ID related to this event (if applicable).
    /// Used for category view events and product context.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the Category.
    /// </summary>
    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    /// <summary>
    /// Gets or sets the store/seller ID related to this event (if applicable).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the Store.
    /// </summary>
    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    /// <summary>
    /// Gets or sets the order ID related to this event (if applicable).
    /// Used for order completion and checkout events.
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the Order.
    /// </summary>
    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    /// <summary>
    /// Gets or sets the search query text (for search events).
    /// </summary>
    [MaxLength(500)]
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the promo code used (for promo code events).
    /// </summary>
    [MaxLength(50)]
    public string? PromoCode { get; set; }

    /// <summary>
    /// Gets or sets the quantity involved in the event (for cart events).
    /// </summary>
    public int? Quantity { get; set; }

    /// <summary>
    /// Gets or sets the monetary value associated with the event (if applicable).
    /// For order completion, this is the order total.
    /// For add to cart, this is the item value.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Value { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON for flexible event properties.
    /// Can store custom properties specific to certain event types.
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the referrer URL or source of the event.
    /// </summary>
    [MaxLength(500)]
    public string? Referrer { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// Useful for device type analytics.
    /// </summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the IP address (anonymized for privacy).
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
