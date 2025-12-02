using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a buyer's review and rating for a product after delivery.
/// Reviews are tied to specific order items to ensure buyers can only review products they've purchased.
/// </summary>
public class ProductReview
{
    /// <summary>
    /// Gets or sets the unique identifier for the review.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID being reviewed.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product being reviewed (navigation property).
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID of the reviewer.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the user who wrote the review (navigation property).
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the order item ID that this review is associated with.
    /// This ensures the buyer actually purchased the product.
    /// </summary>
    public int OrderItemId { get; set; }

    /// <summary>
    /// Gets or sets the order item (navigation property).
    /// </summary>
    public OrderItem OrderItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the rating (1-5 stars).
    /// </summary>
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>
    /// Gets or sets the review text/feedback.
    /// </summary>
    [MaxLength(2000)]
    public string? ReviewText { get; set; }

    /// <summary>
    /// Gets or sets whether the review is approved for public display.
    /// Reviews may require moderation before becoming visible.
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// Gets or sets the moderation status of the review.
    /// </summary>
    public ReviewModerationStatus ModerationStatus { get; set; } = ReviewModerationStatus.PendingReview;

    /// <summary>
    /// Gets or sets the date and time when the review was submitted.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the review was approved (if applicable).
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who last moderated this review.
    /// </summary>
    public int? ModeratedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the admin who last moderated this review (navigation property).
    /// </summary>
    public User? ModeratedByUser { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the review was last moderated.
    /// </summary>
    public DateTime? ModeratedAt { get; set; }

    /// <summary>
    /// Gets or sets the collection of flags raised on this review.
    /// </summary>
    public ICollection<ReviewFlag> Flags { get; set; } = new List<ReviewFlag>();

    /// <summary>
    /// Gets or sets the collection of moderation log entries for this review.
    /// </summary>
    public ICollection<ReviewModerationLog> ModerationLogs { get; set; } = new List<ReviewModerationLog>();
}
