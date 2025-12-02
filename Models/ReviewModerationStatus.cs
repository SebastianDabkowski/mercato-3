namespace MercatoApp.Models;

/// <summary>
/// Represents the moderation status of a review.
/// </summary>
public enum ReviewModerationStatus
{
    /// <summary>
    /// Review is pending moderation review.
    /// </summary>
    PendingReview,

    /// <summary>
    /// Review has been approved and is visible publicly.
    /// </summary>
    Approved,

    /// <summary>
    /// Review has been rejected and is hidden from public view.
    /// </summary>
    Rejected,

    /// <summary>
    /// Review has been flagged for admin attention but is still visible.
    /// </summary>
    Flagged
}
