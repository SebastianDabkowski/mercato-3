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
    /// Gets or sets the date and time when the review was submitted.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the review was approved (if applicable).
    /// </summary>
    public DateTime? ApprovedAt { get; set; }
}
