using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for managing review moderation and flagging.
/// </summary>
public interface IReviewModerationService
{
    /// <summary>
    /// Flags a review for moderation with the specified reason.
    /// </summary>
    /// <param name="reviewId">The review ID to flag.</param>
    /// <param name="reason">The reason for flagging.</param>
    /// <param name="details">Additional details about the flag.</param>
    /// <param name="flaggedByUserId">The user ID who flagged the review (null for automated flags).</param>
    /// <param name="isAutomated">Whether this is an automated flag.</param>
    /// <returns>The created review flag.</returns>
    Task<ReviewFlag> FlagReviewAsync(int reviewId, ReviewFlagReason reason, string? details, int? flaggedByUserId, bool isAutomated = false);

    /// <summary>
    /// Approves a review and makes it publicly visible.
    /// </summary>
    /// <param name="reviewId">The review ID to approve.</param>
    /// <param name="adminUserId">The admin user ID approving the review.</param>
    /// <param name="reason">Optional reason for approval.</param>
    /// <returns>The approved review.</returns>
    Task<ProductReview> ApproveReviewAsync(int reviewId, int adminUserId, string? reason = null);

    /// <summary>
    /// Rejects a review and removes it from public view.
    /// </summary>
    /// <param name="reviewId">The review ID to reject.</param>
    /// <param name="adminUserId">The admin user ID rejecting the review.</param>
    /// <param name="reason">Reason for rejection.</param>
    /// <returns>The rejected review.</returns>
    Task<ProductReview> RejectReviewAsync(int reviewId, int adminUserId, string reason);

    /// <summary>
    /// Toggles the visibility of a review without changing its moderation status.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <param name="adminUserId">The admin user ID making the change.</param>
    /// <param name="isVisible">Whether the review should be visible.</param>
    /// <param name="reason">Reason for the visibility change.</param>
    /// <returns>The updated review.</returns>
    Task<ProductReview> ToggleReviewVisibilityAsync(int reviewId, int adminUserId, bool isVisible, string? reason = null);

    /// <summary>
    /// Resolves a flag on a review.
    /// </summary>
    /// <param name="flagId">The flag ID to resolve.</param>
    /// <param name="adminUserId">The admin user ID resolving the flag.</param>
    /// <returns>The resolved flag.</returns>
    Task<ReviewFlag> ResolveFlagAsync(int flagId, int adminUserId);

    /// <summary>
    /// Gets all flagged reviews pending moderation.
    /// </summary>
    /// <param name="includeResolved">Whether to include resolved flags.</param>
    /// <returns>List of flagged reviews.</returns>
    Task<List<ReviewFlag>> GetFlaggedReviewsAsync(bool includeResolved = false);

    /// <summary>
    /// Gets all reviews with a specific moderation status.
    /// </summary>
    /// <param name="status">The moderation status to filter by.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of reviews per page.</param>
    /// <returns>List of reviews with the specified status.</returns>
    Task<List<ProductReview>> GetReviewsByStatusAsync(ReviewModerationStatus status, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets the total count of reviews with a specific moderation status.
    /// </summary>
    /// <param name="status">The moderation status to filter by.</param>
    /// <returns>The count of reviews with the specified status.</returns>
    Task<int> GetReviewCountByStatusAsync(ReviewModerationStatus status);

    /// <summary>
    /// Gets the moderation history for a specific review.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <returns>List of moderation log entries for the review.</returns>
    Task<List<ReviewModerationLog>> GetReviewModerationHistoryAsync(int reviewId);

    /// <summary>
    /// Checks a review for automated flagging based on content rules.
    /// </summary>
    /// <param name="reviewId">The review ID to check.</param>
    /// <returns>True if the review was auto-flagged, false otherwise.</returns>
    Task<bool> AutoCheckReviewAsync(int reviewId);

    /// <summary>
    /// Gets statistics about review moderation.
    /// </summary>
    /// <returns>Dictionary with moderation statistics.</returns>
    Task<Dictionary<string, int>> GetModerationStatsAsync();

    /// <summary>
    /// Gets a review by its ID.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <returns>The review, or null if not found.</returns>
    Task<ProductReview?> GetReviewByIdAsync(int reviewId);

    /// <summary>
    /// Gets all flags for a specific review.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <param name="includeResolved">Whether to include resolved flags.</param>
    /// <returns>List of flags for the review.</returns>
    Task<List<ReviewFlag>> GetFlagsByReviewIdAsync(int reviewId, bool includeResolved = false);
}
