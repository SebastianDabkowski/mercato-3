using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a flag raised on a product review for moderation.
/// </summary>
public class ReviewFlag
{
    /// <summary>
    /// Gets or sets the unique identifier for the flag.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the review ID being flagged.
    /// </summary>
    public int ProductReviewId { get; set; }

    /// <summary>
    /// Gets or sets the review being flagged (navigation property).
    /// </summary>
    public ProductReview ProductReview { get; set; } = null!;

    /// <summary>
    /// Gets or sets the reason for flagging the review.
    /// </summary>
    [Required]
    public ReviewFlagReason Reason { get; set; }

    /// <summary>
    /// Gets or sets additional details about the flag.
    /// </summary>
    [MaxLength(1000)]
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the user ID who flagged the review (null for automated flags).
    /// </summary>
    public int? FlaggedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who flagged the review (navigation property).
    /// </summary>
    public User? FlaggedByUser { get; set; }

    /// <summary>
    /// Gets or sets whether this flag was created automatically by the system.
    /// </summary>
    public bool IsAutomated { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the flag was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the flag is still active (not resolved).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the flag was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who resolved the flag.
    /// </summary>
    public int? ResolvedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the admin who resolved the flag (navigation property).
    /// </summary>
    public User? ResolvedByUser { get; set; }
}
