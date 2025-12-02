using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for managing seller ratings.
/// </summary>
public interface ISellerRatingService
{
    /// <summary>
    /// Submits a new seller rating for a delivered sub-order.
    /// </summary>
    /// <param name="userId">The ID of the user submitting the rating.</param>
    /// <param name="sellerSubOrderId">The ID of the seller sub-order being rated.</param>
    /// <param name="rating">The rating (1-5 stars).</param>
    /// <returns>The created seller rating.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the user is not authorized to rate the seller,
    /// the sub-order is not delivered, or the user has already rated this sub-order.
    /// </exception>
    Task<SellerRating> SubmitRatingAsync(int userId, int sellerSubOrderId, int rating);

    /// <summary>
    /// Gets the average rating for a seller (store).
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The average rating, or null if no ratings exist.</returns>
    Task<decimal?> GetAverageRatingAsync(int storeId);

    /// <summary>
    /// Gets the total count of ratings for a seller (store).
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The total count of ratings.</returns>
    Task<int> GetRatingCountAsync(int storeId);

    /// <summary>
    /// Checks if a user has already rated a specific seller sub-order.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="sellerSubOrderId">The seller sub-order ID.</param>
    /// <returns>True if the user has already rated this sub-order.</returns>
    Task<bool> HasUserRatedSubOrderAsync(int userId, int sellerSubOrderId);
}
