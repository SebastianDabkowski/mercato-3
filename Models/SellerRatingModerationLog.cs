using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an audit log entry for moderation actions on seller ratings.
/// </summary>
public class SellerRatingModerationLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the log entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the seller rating ID this log entry is for.
    /// </summary>
    public int SellerRatingId { get; set; }

    /// <summary>
    /// Gets or sets the seller rating this log entry is for (navigation property).
    /// </summary>
    public SellerRating SellerRating { get; set; } = null!;

    /// <summary>
    /// Gets or sets the moderation action that was taken.
    /// </summary>
    [Required]
    public ReviewModerationAction Action { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who performed the moderation action.
    /// </summary>
    public int? ModeratedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the admin who performed the moderation action (navigation property).
    /// </summary>
    public User? ModeratedByUser { get; set; }

    /// <summary>
    /// Gets or sets the reason for the moderation action.
    /// </summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the previous moderation status before this action.
    /// </summary>
    public ReviewModerationStatus? PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new moderation status after this action.
    /// </summary>
    public ReviewModerationStatus? NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the moderation action was performed.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
