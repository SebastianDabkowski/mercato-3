using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an audit log entry for a review moderation action.
/// </summary>
public class ReviewModerationLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the log entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the review ID that was moderated.
    /// </summary>
    public int ProductReviewId { get; set; }

    /// <summary>
    /// Gets or sets the review that was moderated (navigation property).
    /// </summary>
    public ProductReview ProductReview { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of moderation action taken.
    /// </summary>
    [Required]
    public ReviewModerationAction Action { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who took the action (null for automated actions).
    /// </summary>
    public int? ModeratedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the admin who took the action (navigation property).
    /// </summary>
    public User? ModeratedByUser { get; set; }

    /// <summary>
    /// Gets or sets the reason or notes for the moderation action.
    /// </summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the previous status before the action (for audit trail).
    /// </summary>
    public ReviewModerationStatus? PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new status after the action (for audit trail).
    /// </summary>
    public ReviewModerationStatus? NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the action was taken.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether this action was automated by the system.
    /// </summary>
    public bool IsAutomated { get; set; }
}
