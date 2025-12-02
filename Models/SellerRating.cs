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
    /// Gets or sets the date and time when the rating was submitted.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
