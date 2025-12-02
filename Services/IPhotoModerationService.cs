using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for managing product photo moderation.
/// </summary>
public interface IPhotoModerationService
{
    /// <summary>
    /// Gets product photos filtered by moderation status and optional product/seller filters.
    /// </summary>
    /// <param name="status">The moderation status to filter by (null for all).</param>
    /// <param name="productId">Optional product ID to filter by.</param>
    /// <param name="storeId">Optional store ID to filter by.</param>
    /// <param name="flaggedOnly">If true, only show flagged photos.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of photos per page.</param>
    /// <returns>List of product images matching the criteria.</returns>
    Task<List<ProductImage>> GetPhotosByModerationStatusAsync(
        PhotoModerationStatus? status = null,
        int? productId = null,
        int? storeId = null,
        bool flaggedOnly = false,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets the total count of photos with a specific moderation status.
    /// </summary>
    /// <param name="status">The moderation status to filter by (null for all).</param>
    /// <param name="flaggedOnly">If true, only count flagged photos.</param>
    /// <returns>The count of photos with the specified status.</returns>
    Task<int> GetPhotoCountByModerationStatusAsync(PhotoModerationStatus? status = null, bool flaggedOnly = false);

    /// <summary>
    /// Gets a product image by its ID with full details including product and store.
    /// </summary>
    /// <param name="imageId">The product image ID.</param>
    /// <returns>The product image, or null if not found.</returns>
    Task<ProductImage?> GetPhotoByIdAsync(int imageId);

    /// <summary>
    /// Approves a photo and updates its moderation status.
    /// </summary>
    /// <param name="imageId">The product image ID to approve.</param>
    /// <param name="adminUserId">The admin user ID approving the photo.</param>
    /// <param name="reason">Optional reason for approval.</param>
    /// <returns>The approved product image.</returns>
    Task<ProductImage> ApprovePhotoAsync(int imageId, int adminUserId, string? reason = null);

    /// <summary>
    /// Removes a photo and updates its moderation status.
    /// </summary>
    /// <param name="imageId">The product image ID to remove.</param>
    /// <param name="adminUserId">The admin user ID removing the photo.</param>
    /// <param name="reason">Reason for removal.</param>
    /// <returns>The removed product image.</returns>
    Task<ProductImage> RemovePhotoAsync(int imageId, int adminUserId, string reason);

    /// <summary>
    /// Flags a photo for moderation review.
    /// </summary>
    /// <param name="imageId">The product image ID to flag.</param>
    /// <param name="userId">The user ID flagging the photo (null for automated flags).</param>
    /// <param name="reason">Reason for flagging.</param>
    /// <param name="isAutomated">Whether this flag is automated.</param>
    /// <returns>The created photo flag.</returns>
    Task<PhotoFlag> FlagPhotoAsync(int imageId, int? userId, string reason, bool isAutomated = false);

    /// <summary>
    /// Gets all flags for a specific photo.
    /// </summary>
    /// <param name="imageId">The product image ID.</param>
    /// <returns>List of flags for the photo.</returns>
    Task<List<PhotoFlag>> GetPhotoFlagsAsync(int imageId);

    /// <summary>
    /// Gets the moderation history for a specific photo.
    /// </summary>
    /// <param name="imageId">The product image ID.</param>
    /// <returns>List of moderation log entries for the photo.</returns>
    Task<List<PhotoModerationLog>> GetPhotoModerationHistoryAsync(int imageId);

    /// <summary>
    /// Gets statistics about photo moderation.
    /// </summary>
    /// <returns>Dictionary with moderation statistics.</returns>
    Task<Dictionary<string, int>> GetModerationStatsAsync();

    /// <summary>
    /// Performs bulk approve operation on multiple photos.
    /// </summary>
    /// <param name="imageIds">List of product image IDs to approve.</param>
    /// <param name="adminUserId">The admin user ID performing the bulk operation.</param>
    /// <param name="reason">Optional reason for bulk approval.</param>
    /// <returns>Count of successfully approved photos.</returns>
    Task<int> BulkApprovePhotosAsync(List<int> imageIds, int adminUserId, string? reason = null);
}
