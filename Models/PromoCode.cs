using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a promotional code for discounts.
/// </summary>
public class PromoCode
{
    /// <summary>
    /// Gets or sets the unique identifier for the promo code.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the promo code string (e.g., "SAVE20").
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Code { get; set; } = null!;

    /// <summary>
    /// Gets or sets the scope of the promo code (Platform or Seller).
    /// </summary>
    public PromoCodeScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the store ID for seller-specific promo codes (null for platform codes).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store (navigation property).
    /// </summary>
    public Store? Store { get; set; }

    /// <summary>
    /// Gets or sets the discount type (Percentage or FixedAmount).
    /// </summary>
    public PromoCodeDiscountType DiscountType { get; set; }

    /// <summary>
    /// Gets or sets the discount value.
    /// For Percentage: value between 0-100 (e.g., 20 for 20% off).
    /// For FixedAmount: the amount to discount (e.g., 5.00 for $5 off).
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Gets or sets the minimum order subtotal required to use this promo code.
    /// </summary>
    public decimal? MinimumOrderSubtotal { get; set; }

    /// <summary>
    /// Gets or sets the maximum discount amount (useful for percentage discounts).
    /// </summary>
    public decimal? MaximumDiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the start date for when the promo code becomes valid.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the expiration date for the promo code.
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of times this promo code can be used across all users.
    /// </summary>
    public int? MaximumUsageCount { get; set; }

    /// <summary>
    /// Gets or sets the current usage count.
    /// </summary>
    public int CurrentUsageCount { get; set; }

    /// <summary>
    /// Gets or sets whether the promo code is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the promo code was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
