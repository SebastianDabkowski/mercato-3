using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a VAT (Value-Added Tax) rule that can be applied based on effective dates, country, and category.
/// Supports versioning and audit trail for financial and legal compliance.
/// </summary>
public class VatRule
{
    /// <summary>
    /// Gets or sets the unique identifier for the VAT rule.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name/description of the rule.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the VAT/tax rate as a percentage (e.g., 20.0 for 20%).
    /// </summary>
    [Required]
    [Range(0, 100)]
    public decimal TaxPercentage { get; set; }

    /// <summary>
    /// Gets or sets the country code (ISO 3166-1 alpha-2) this rule applies to.
    /// Examples: "US", "GB", "DE", "FR", "PL"
    /// </summary>
    [Required]
    [MaxLength(2)]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the region/state code (optional, for country subdivisions like US states).
    /// Examples: "CA", "NY", "TX" for US states
    /// </summary>
    [MaxLength(10)]
    public string? RegionCode { get; set; }

    /// <summary>
    /// Gets or sets the applicability type of the rule.
    /// Values: "Global", "Category"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ApplicabilityType { get; set; } = VatRuleApplicability.Global;

    /// <summary>
    /// Gets or sets the category ID if this rule is category-specific.
    /// Null for global rules that apply to all categories in the country/region.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category (navigation property).
    /// </summary>
    public Category? Category { get; set; }

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
    /// Inactive rules are not considered during tax calculation.
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
/// VAT rule applicability types.
/// </summary>
public static class VatRuleApplicability
{
    /// <summary>
    /// Global rule applies to all products in the country/region unless overridden.
    /// </summary>
    public const string Global = "Global";

    /// <summary>
    /// Rule applies to a specific product category.
    /// </summary>
    public const string Category = "Category";
}
