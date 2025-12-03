using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a commission rule that can be applied based on effective dates and applicability criteria.
/// Supports versioning and audit trail for financial and legal compliance.
/// </summary>
public class CommissionRule
{
    /// <summary>
    /// Gets or sets the unique identifier for the commission rule.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name/description of the rule.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the commission rate as a percentage (e.g., 10.5 for 10.5%).
    /// Applied to the item subtotal before shipping.
    /// </summary>
    [Required]
    [Range(0, 100)]
    public decimal CommissionPercentage { get; set; }

    /// <summary>
    /// Gets or sets the fixed commission amount per transaction.
    /// Applied in addition to the percentage-based commission.
    /// </summary>
    [Range(0, 999999.99)]
    public decimal FixedCommissionAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the applicability type of the rule.
    /// Values: "Global", "Category", "Seller", "SellerTier"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ApplicabilityType { get; set; } = CommissionRuleApplicability.Global;

    /// <summary>
    /// Gets or sets the category ID if this rule is category-specific.
    /// Null for non-category rules.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category (navigation property).
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// Gets or sets the store ID if this rule is seller-specific.
    /// Null for non-seller-specific rules.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store (navigation property).
    /// </summary>
    public Store? Store { get; set; }

    /// <summary>
    /// Gets or sets the seller tier if this rule applies to a specific seller tier.
    /// Values: "Bronze", "Silver", "Gold", "Platinum"
    /// Null for non-tier-specific rules.
    /// </summary>
    [MaxLength(50)]
    public string? SellerTier { get; set; }

    /// <summary>
    /// Gets or sets the effective start date for this rule.
    /// The rule applies to transactions on or after this date.
    /// </summary>
    [Required]
    public DateTime EffectiveStartDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Gets or sets the effective end date for this rule.
    /// Null means the rule has no end date (active indefinitely).
    /// The rule applies to transactions before this date.
    /// </summary>
    public DateTime? EffectiveEndDate { get; set; }

    /// <summary>
    /// Gets or sets the priority of the rule for conflict resolution.
    /// Higher values have higher priority. Default is 0.
    /// When multiple rules could apply, the one with highest priority wins.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this rule is currently active.
    /// Inactive rules are not considered during commission calculation.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the user ID of the admin who created this rule.
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who created this rule (navigation property).
    /// </summary>
    public User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the rule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID of the admin who last updated this rule.
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated this rule (navigation property).
    /// </summary>
    public User? UpdatedByUser { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the rule was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets notes/comments about this rule.
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Commission rule applicability types.
/// </summary>
public static class CommissionRuleApplicability
{
    /// <summary>
    /// Global rule applies to all transactions unless overridden.
    /// </summary>
    public const string Global = "Global";

    /// <summary>
    /// Rule applies to a specific product category.
    /// </summary>
    public const string Category = "Category";

    /// <summary>
    /// Rule applies to a specific seller/store.
    /// </summary>
    public const string Seller = "Seller";

    /// <summary>
    /// Rule applies to all sellers in a specific tier.
    /// </summary>
    public const string SellerTier = "SellerTier";
}

/// <summary>
/// Seller tiers for tiered commission structures.
/// </summary>
public static class SellerTiers
{
    public const string Bronze = "Bronze";
    public const string Silver = "Silver";
    public const string Gold = "Gold";
    public const string Platinum = "Platinum";
}
