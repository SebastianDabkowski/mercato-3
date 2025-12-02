namespace MercatoApp.Models;

/// <summary>
/// Represents the type of moderation action taken on a product photo.
/// </summary>
public enum PhotoModerationAction
{
    /// <summary>
    /// Photo was approved by admin.
    /// </summary>
    Approved,

    /// <summary>
    /// Photo was rejected by admin.
    /// </summary>
    Rejected,

    /// <summary>
    /// Photo was removed by admin.
    /// </summary>
    Removed,

    /// <summary>
    /// Photo was flagged for review.
    /// </summary>
    Flagged,

    /// <summary>
    /// Photo was unflagged (flag removed).
    /// </summary>
    Unflagged,

    /// <summary>
    /// Photo was auto-approved by system.
    /// </summary>
    AutoApproved,

    /// <summary>
    /// Photo was auto-flagged by automated rules.
    /// </summary>
    AutoFlagged
}
