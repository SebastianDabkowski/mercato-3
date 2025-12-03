using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a history record of changes to feature flags for audit purposes.
/// </summary>
public class FeatureFlagHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the history entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the feature flag that was changed.
    /// </summary>
    public int FeatureFlagId { get; set; }

    /// <summary>
    /// Gets or sets the feature flag (navigation property).
    /// </summary>
    public FeatureFlag? FeatureFlag { get; set; }

    /// <summary>
    /// Gets or sets the type of change that was made.
    /// </summary>
    public FeatureFlagChangeType ChangeType { get; set; }

    /// <summary>
    /// Gets or sets the previous state of the flag (JSON serialized).
    /// </summary>
    [MaxLength(4000)]
    public string? PreviousState { get; set; }

    /// <summary>
    /// Gets or sets the new state of the flag (JSON serialized).
    /// </summary>
    [MaxLength(4000)]
    public string? NewState { get; set; }

    /// <summary>
    /// Gets or sets a description of the change.
    /// </summary>
    [MaxLength(1000)]
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who made the change.
    /// </summary>
    public int ChangedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who made the change (navigation property).
    /// </summary>
    public User ChangedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the change was made.
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the IP address from which the change was made.
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent from which the change was made.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
}

/// <summary>
/// Represents the type of change made to a feature flag.
/// </summary>
public enum FeatureFlagChangeType
{
    /// <summary>
    /// A new feature flag was created.
    /// </summary>
    Created,

    /// <summary>
    /// An existing feature flag was updated.
    /// </summary>
    Updated,

    /// <summary>
    /// A feature flag was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// A feature flag was toggled on or off.
    /// </summary>
    Toggled,

    /// <summary>
    /// Targeting rules were added or modified.
    /// </summary>
    RulesModified
}
