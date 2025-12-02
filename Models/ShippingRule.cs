using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a shipping rule configured by a seller for their store.
/// Each seller can configure their own shipping costs and rules.
/// </summary>
public class ShippingRule
{
    /// <summary>
    /// Gets or sets the unique identifier for the shipping rule.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns this shipping rule.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store that owns this shipping rule (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the shipping rule (e.g., "Standard Shipping", "Express Delivery").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base shipping cost for this rule.
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
    /// Gets or sets whether this shipping rule is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the rule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the rule was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
