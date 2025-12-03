using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a buyer's rating for a seller (store) based on a completed order.
/// Seller ratings contribute to the store's overall reputation score.
/// </summary>
public class SellerRating
{
    /// <summary>
    /// Gets or sets the unique identifier for the seller rating.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID being rated.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store being rated (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID of the buyer submitting the rating.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the user who submitted the rating (navigation property).
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the seller sub-order ID that this rating is associated with.
    /// This ensures the buyer actually purchased from the seller and the order is completed.
    /// One rating per sub-order to prevent duplicate ratings.
    /// </summary>
    public int SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order (navigation property).
    /// </summary>
    public SellerSubOrder SellerSubOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the rating (1-5 stars).
    /// </summary>
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>
    /// Gets or sets the optional review text/feedback for the seller.
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
    /// Gets or sets the date and time when the rating was submitted.
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
    public ICollection<SellerRatingFlag> Flags { get; set; } = new List<SellerRatingFlag>();

    /// <summary>
    /// Gets or sets the collection of moderation log entries for this review.
    /// </summary>
    public ICollection<SellerRatingModerationLog> ModerationLogs { get; set; } = new List<SellerRatingModerationLog>();
}
