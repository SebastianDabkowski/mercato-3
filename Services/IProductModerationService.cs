using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for managing product moderation.
/// </summary>
public interface IProductModerationService
{
    /// <summary>
    /// Gets products filtered by moderation status and optional category.
    /// </summary>
    /// <param name="status">The moderation status to filter by (null for all).</param>
    /// <param name="categoryId">Optional category ID to filter by.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of products per page.</param>
    /// <returns>List of products matching the criteria.</returns>
    Task<List<Product>> GetProductsByModerationStatusAsync(
        ProductModerationStatus? status = null,
        int? categoryId = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets the total count of products with a specific moderation status.
    /// </summary>
    /// <param name="status">The moderation status to filter by (null for all).</param>
    /// <returns>The count of products with the specified status.</returns>
    Task<int> GetProductCountByModerationStatusAsync(ProductModerationStatus? status = null);

    /// <summary>
    /// Gets a product by its ID with store and category details.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>The product, or null if not found.</returns>
    Task<Product?> GetProductByIdAsync(int productId);

    /// <summary>
    /// Approves a product and updates its moderation status.
    /// </summary>
    /// <param name="productId">The product ID to approve.</param>
    /// <param name="adminUserId">The admin user ID approving the product.</param>
    /// <param name="reason">Optional reason for approval.</param>
    /// <returns>The approved product.</returns>
    Task<Product> ApproveProductAsync(int productId, int adminUserId, string? reason = null);

    /// <summary>
    /// Rejects a product and updates its moderation status.
    /// </summary>
    /// <param name="productId">The product ID to reject.</param>
    /// <param name="adminUserId">The admin user ID rejecting the product.</param>
    /// <param name="reason">Reason for rejection.</param>
    /// <returns>The rejected product.</returns>
    Task<Product> RejectProductAsync(int productId, int adminUserId, string reason);

    /// <summary>
    /// Performs bulk approve operation on multiple products.
    /// </summary>
    /// <param name="productIds">List of product IDs to approve.</param>
    /// <param name="adminUserId">The admin user ID performing the bulk operation.</param>
    /// <param name="reason">Optional reason for bulk approval.</param>
    /// <returns>Count of successfully approved products.</returns>
    Task<int> BulkApproveProductsAsync(List<int> productIds, int adminUserId, string? reason = null);

    /// <summary>
    /// Performs bulk reject operation on multiple products.
    /// </summary>
    /// <param name="productIds">List of product IDs to reject.</param>
    /// <param name="adminUserId">The admin user ID performing the bulk operation.</param>
    /// <param name="reason">Reason for bulk rejection.</param>
    /// <returns>Count of successfully rejected products.</returns>
    Task<int> BulkRejectProductsAsync(List<int> productIds, int adminUserId, string reason);

    /// <summary>
    /// Gets the moderation history for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>List of moderation log entries for the product.</returns>
    Task<List<ProductModerationLog>> GetProductModerationHistoryAsync(int productId);

    /// <summary>
    /// Gets statistics about product moderation.
    /// </summary>
    /// <returns>Dictionary with moderation statistics.</returns>
    Task<Dictionary<string, int>> GetModerationStatsAsync();
}
