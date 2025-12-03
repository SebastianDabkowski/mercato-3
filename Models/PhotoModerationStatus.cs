namespace MercatoApp.Models;

/// <summary>
/// Represents the moderation status of a product photo.
/// </summary>
public enum PhotoModerationStatus
{
    /// <summary>
    /// Photo is pending moderation review.
    /// </summary>
    PendingReview,

    /// <summary>
    /// Photo has been approved and is visible publicly.
    /// </summary>
    Approved,

    /// <summary>
    /// Photo has been rejected and is hidden from public view.
    /// </summary>
    Rejected,

    /// <summary>
    /// Photo has been flagged for admin attention but is still visible.
    /// </summary>
    Flagged
}
