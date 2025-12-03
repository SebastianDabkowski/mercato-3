using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an audit log entry for critical actions in the system.
/// Designed for tamper-evidence and compliance requirements.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit log entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID who performed the action.
    /// Null for system-initiated actions.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user who performed the action (navigation property).
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the type of action performed.
    /// </summary>
    public AuditActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the type of entity being acted upon (e.g., "User", "Order", "Payout", "Product").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the entity being acted upon.
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Gets or sets a string representation of the entity for display purposes.
    /// For example, "Order #12345" or "User: john@example.com".
    /// </summary>
    [MaxLength(500)]
    public string? EntityDisplayName { get; set; }

    /// <summary>
    /// Gets or sets the target user ID (for actions affecting another user).
    /// Optional - only set for user-related actions.
    /// </summary>
    public int? TargetUserId { get; set; }

    /// <summary>
    /// Gets or sets the target user (navigation property).
    /// </summary>
    public User? TargetUser { get; set; }

    /// <summary>
    /// Gets or sets whether the action was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the failure reason if the action was not successful.
    /// </summary>
    [MaxLength(1000)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets additional details about the action.
    /// </summary>
    [MaxLength(2000)]
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the previous state/value before the action (for change tracking).
    /// Stored as JSON for complex objects.
    /// </summary>
    [MaxLength(2000)]
    public string? PreviousValue { get; set; }

    /// <summary>
    /// Gets or sets the new state/value after the action (for change tracking).
    /// Stored as JSON for complex objects.
    /// </summary>
    [MaxLength(2000)]
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the action was performed.
    /// </summary>
    [MaxLength(46)] // IPv6 max length with all representations
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string from the request.
    /// </summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the action was performed.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a hash of the log entry for tamper detection.
    /// Calculated from concatenation of key fields.
    /// </summary>
    [MaxLength(64)]
    public string? EntryHash { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for grouping related audit entries.
    /// Useful for tracking multi-step operations.
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets whether this entry has been archived.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets the date when the entry was archived.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }
}
