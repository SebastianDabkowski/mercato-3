using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a feature flag for controlling platform features.
/// </summary>
public class FeatureFlag
{
    /// <summary>
    /// Gets or sets the unique identifier for the feature flag.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique key for the feature flag (e.g., "seller_user_management", "promo_code").
    /// This is used in code to check if a feature is enabled.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the feature flag.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the feature flag.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the feature is enabled by default (when no targeting rules match).
    /// </summary>
    public bool IsEnabledByDefault { get; set; }

    /// <summary>
    /// Gets or sets whether the feature flag is active and can be evaluated.
    /// Inactive flags are ignored during evaluation.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the environments where this flag is applicable (comma-separated: dev,test,stage,prod).
    /// If null or empty, the flag applies to all environments.
    /// </summary>
    [MaxLength(200)]
    public string? Environments { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the flag was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the flag was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this flag.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who created this flag (navigation property).
    /// </summary>
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last updated this flag.
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated this flag (navigation property).
    /// </summary>
    public User? UpdatedByUser { get; set; }

    /// <summary>
    /// Gets or sets the targeting rules for this feature flag.
    /// </summary>
    public List<FeatureFlagRule> Rules { get; set; } = new();
}
