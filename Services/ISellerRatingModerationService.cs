using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for managing seller rating moderation and flagging.
/// </summary>
public interface ISellerRatingModerationService
{
    /// <summary>
    /// Flags a seller rating for moderation with the specified reason.
    /// </summary>
    /// <param name="ratingId">The seller rating ID to flag.</param>
    /// <param name="reason">The reason for flagging.</param>
    /// <param name="details">Additional details about the flag.</param>
    /// <param name="flaggedByUserId">The user ID who flagged the rating (null for automated flags).</param>
    /// <param name="isAutomated">Whether this is an automated flag.</param>
    /// <returns>The created seller rating flag.</returns>
    Task<SellerRatingFlag> FlagRatingAsync(int ratingId, ReviewFlagReason reason, string? details, int? flaggedByUserId, bool isAutomated = false);

    /// <summary>
    /// Approves a seller rating and makes it publicly visible.
    /// </summary>
    /// <param name="ratingId">The seller rating ID to approve.</param>
    /// <param name="adminUserId">The admin user ID approving the rating.</param>
    /// <param name="reason">Optional reason for approval.</param>
    /// <returns>The approved seller rating.</returns>
    Task<SellerRating> ApproveRatingAsync(int ratingId, int adminUserId, string? reason = null);

    /// <summary>
    /// Rejects a seller rating and removes it from public view.
    /// </summary>
    /// <param name="ratingId">The seller rating ID to reject.</param>
    /// <param name="adminUserId">The admin user ID rejecting the rating.</param>
    /// <param name="reason">Reason for rejection.</param>
    /// <returns>The rejected seller rating.</returns>
    Task<SellerRating> RejectRatingAsync(int ratingId, int adminUserId, string reason);

    /// <summary>
    /// Toggles the visibility of a seller rating without changing its moderation status.
    /// </summary>
    /// <param name="ratingId">The seller rating ID.</param>
    /// <param name="adminUserId">The admin user ID making the change.</param>
    /// <param name="isVisible">Whether the rating should be visible.</param>
    /// <param name="reason">Reason for the visibility change.</param>
    /// <returns>The updated seller rating.</returns>
    Task<SellerRating> ToggleRatingVisibilityAsync(int ratingId, int adminUserId, bool isVisible, string? reason = null);

    /// <summary>
    /// Resolves a flag on a seller rating.
    /// </summary>
    /// <param name="flagId">The flag ID to resolve.</param>
    /// <param name="adminUserId">The admin user ID resolving the flag.</param>
    /// <returns>The resolved flag.</returns>
    Task<SellerRatingFlag> ResolveFlagAsync(int flagId, int adminUserId);

    /// <summary>
    /// Gets all flagged seller ratings pending moderation.
    /// </summary>
    /// <param name="includeResolved">Whether to include resolved flags.</param>
    /// <returns>List of flagged seller ratings.</returns>
    Task<List<SellerRatingFlag>> GetFlaggedRatingsAsync(bool includeResolved = false);

    /// <summary>
    /// Gets all seller ratings with a specific moderation status.
    /// </summary>
    /// <param name="status">The moderation status to filter by.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of ratings per page.</param>
    /// <returns>List of seller ratings with the specified status.</returns>
    Task<List<SellerRating>> GetRatingsByStatusAsync(ReviewModerationStatus status, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets the total count of seller ratings with a specific moderation status.
    /// </summary>
    /// <param name="status">The moderation status to filter by.</param>
    /// <returns>The count of seller ratings with the specified status.</returns>
    Task<int> GetRatingCountByStatusAsync(ReviewModerationStatus status);

    /// <summary>
    /// Gets the moderation history for a specific seller rating.
    /// </summary>
    /// <param name="ratingId">The seller rating ID.</param>
    /// <returns>List of moderation log entries for the seller rating.</returns>
    Task<List<SellerRatingModerationLog>> GetRatingModerationHistoryAsync(int ratingId);

    /// <summary>
    /// Checks a seller rating for automated flagging based on content rules.
    /// </summary>
    /// <param name="ratingId">The seller rating ID to check.</param>
    /// <returns>True if the rating was auto-flagged, false otherwise.</returns>
    Task<bool> AutoCheckRatingAsync(int ratingId);

    /// <summary>
    /// Gets statistics about seller rating moderation.
    /// </summary>
    /// <returns>Dictionary with moderation statistics.</returns>
    Task<Dictionary<string, int>> GetModerationStatsAsync();

    /// <summary>
    /// Gets a seller rating by its ID.
    /// </summary>
    /// <param name="ratingId">The seller rating ID.</param>
    /// <returns>The seller rating, or null if not found.</returns>
    Task<SellerRating?> GetRatingByIdAsync(int ratingId);

    /// <summary>
    /// Gets all flags for a specific seller rating.
    /// </summary>
    /// <param name="ratingId">The seller rating ID.</param>
    /// <param name="includeResolved">Whether to include resolved flags.</param>
    /// <returns>List of flags for the seller rating.</returns>
    Task<List<SellerRatingFlag>> GetFlagsByRatingIdAsync(int ratingId, bool includeResolved = false);
}
