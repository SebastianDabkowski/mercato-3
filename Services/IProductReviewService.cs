using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for managing product reviews.
/// </summary>
public interface IProductReviewService
{
    /// <summary>
    /// Submits a new product review for a delivered order item.
    /// </summary>
    /// <param name="userId">The ID of the user submitting the review.</param>
    /// <param name="orderItemId">The ID of the order item being reviewed.</param>
    /// <param name="rating">The rating (1-5 stars).</param>
    /// <param name="reviewText">Optional review text.</param>
    /// <returns>The created product review.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the user is not authorized to review the product,
    /// the order is not delivered, or the user has already reviewed this product.
    /// </exception>
    Task<ProductReview> SubmitReviewAsync(int userId, int orderItemId, int rating, string? reviewText);

    /// <summary>
    /// Gets all approved reviews for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A list of approved reviews for the product.</returns>
    Task<List<ProductReview>> GetApprovedReviewsForProductAsync(int productId);

    /// <summary>
    /// Gets paginated and sorted approved reviews for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="sortOption">The sort option to apply.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of reviews per page.</param>
    /// <returns>A list of approved reviews for the product.</returns>
    Task<List<ProductReview>> GetApprovedReviewsForProductAsync(int productId, ReviewSortOption sortOption, int page, int pageSize);

    /// <summary>
    /// Gets the total count of approved reviews for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>The total count of approved reviews.</returns>
    Task<int> GetApprovedReviewCountAsync(int productId);

    /// <summary>
    /// Gets the average rating for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>The average rating, or null if no reviews exist.</returns>
    Task<decimal?> GetAverageRatingAsync(int productId);

    /// <summary>
    /// Checks if a user has already reviewed a specific order item.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="orderItemId">The order item ID.</param>
    /// <returns>True if the user has already reviewed this order item.</returns>
    Task<bool> HasUserReviewedOrderItemAsync(int userId, int orderItemId);
}
