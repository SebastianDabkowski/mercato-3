using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a shipping method that buyers can select during checkout.
/// Each seller can offer multiple shipping methods with different costs and delivery times.
/// </summary>
public class ShippingMethod
{
    /// <summary>
    /// Gets or sets the unique identifier for the shipping method.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns this shipping method.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store that owns this shipping method (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the shipping method (e.g., "Standard Shipping", "Express Delivery").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the shipping method.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the estimated delivery time (e.g., "3-5 business days").
    /// </summary>
    [MaxLength(100)]
    public string? EstimatedDelivery { get; set; }

    /// <summary>
    /// Gets or sets the base shipping cost for this method.
    /// This is applied to the first item or up to the free shipping threshold.
    /// </summary>
    [Required]
    [Range(0, 999999.99)]
    public decimal BaseCost { get; set; }

    /// <summary>
    /// Gets or sets the additional cost per item after the first item.
    /// Used for incremental shipping cost calculation.
    /// </summary>
    [Range(0, 999999.99)]
    public decimal AdditionalItemCost { get; set; } = 0;

    /// <summary>
    /// Gets or sets the minimum order amount for free shipping.
    /// If the order subtotal meets or exceeds this amount, shipping is free.
    /// Null means no free shipping threshold.
    /// </summary>
    [Range(0, 999999.99)]
    public decimal? FreeShippingThreshold { get; set; }

    /// <summary>
    /// Gets or sets whether this shipping method is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the display order for this shipping method.
    /// Lower values are displayed first.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Gets or sets the date and time when the method was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the method was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
