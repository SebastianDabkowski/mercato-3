using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a targeting rule for a feature flag.
/// Rules are evaluated in priority order to determine if a feature should be enabled for a specific request.
/// </summary>
public class FeatureFlagRule
{
    /// <summary>
    /// Gets or sets the unique identifier for the rule.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the feature flag this rule belongs to.
    /// </summary>
    public int FeatureFlagId { get; set; }

    /// <summary>
    /// Gets or sets the feature flag (navigation property).
    /// </summary>
    public FeatureFlag FeatureFlag { get; set; } = null!;

    /// <summary>
    /// Gets or sets the priority of this rule (lower number = higher priority).
    /// Rules are evaluated in ascending order of priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the type of targeting rule.
    /// </summary>
    public FeatureFlagRuleType RuleType { get; set; }

    /// <summary>
    /// Gets or sets the value to match against (depends on RuleType).
    /// For UserRole: role name (e.g., "Admin", "Seller", "Buyer")
    /// For UserId: comma-separated user IDs
    /// For StoreId: comma-separated store IDs
    /// For PercentageRollout: percentage value (0-100)
    /// For Environment: environment name (e.g., "dev", "prod")
    /// </summary>
    [MaxLength(1000)]
    public string? RuleValue { get; set; }

    /// <summary>
    /// Gets or sets whether the feature should be enabled when this rule matches.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a description of this rule.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the rule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the type of feature flag targeting rule.
/// </summary>
public enum FeatureFlagRuleType
{
    /// <summary>
    /// Target users by their role (Admin, Seller, Buyer).
    /// </summary>
    UserRole,

    /// <summary>
    /// Target specific users by ID.
    /// </summary>
    UserId,

    /// <summary>
    /// Target specific stores by ID.
    /// </summary>
    StoreId,

    /// <summary>
    /// Percentage-based rollout (0-100).
    /// Uses consistent hashing to ensure the same user always gets the same result.
    /// </summary>
    PercentageRollout,

    /// <summary>
    /// Target by environment (dev, test, stage, prod).
    /// </summary>
    Environment
}
