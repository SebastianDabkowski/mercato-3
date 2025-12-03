using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an audit log entry for admin actions.
/// </summary>
public class AdminAuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit log entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who performed the action.
    /// </summary>
    public int AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the admin user (navigation property).
    /// </summary>
    public User AdminUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the target user ID (the user being acted upon).
    /// Optional - only set for user-related actions.
    /// </summary>
    public int? TargetUserId { get; set; }

    /// <summary>
    /// Gets or sets the target user (navigation property).
    /// </summary>
    public User? TargetUser { get; set; }

    /// <summary>
    /// Gets or sets the type of entity being acted upon (e.g., "User", "Product", "Review", "Order", "Setting").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the entity being acted upon.
    /// For example, ProductId, ReviewId, OrderId, etc.
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Gets or sets a string representation of the entity for display purposes.
    /// For example, "Product: iPhone 15" or "User: john@example.com".
    /// </summary>
    [MaxLength(500)]
    public string? EntityDisplayName { get; set; }

    /// <summary>
    /// Gets or sets the action performed (e.g., "BlockUser", "UnblockUser", "ApproveProduct", "UpdateSetting").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for the action.
    /// </summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the action was performed.
    /// </summary>
    public DateTime ActionTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata about the action (e.g., previous status, new status).
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }
}
