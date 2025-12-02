namespace MercatoApp.Models;

/// <summary>
/// Represents the type of moderation action taken on a review.
/// </summary>
public enum ReviewModerationAction
{
    /// <summary>
    /// Review was approved by admin.
    /// </summary>
    Approved,

    /// <summary>
    /// Review was rejected by admin.
    /// </summary>
    Rejected,

    /// <summary>
    /// Review was flagged for review.
    /// </summary>
    Flagged,

    /// <summary>
    /// Review was unflagged (flag removed).
    /// </summary>
    Unflagged,

    /// <summary>
    /// Review visibility was edited.
    /// </summary>
    VisibilityEdited,

    /// <summary>
    /// Review was auto-approved by system.
    /// </summary>
    AutoApproved,

    /// <summary>
    /// Review was auto-flagged by automated rules.
    /// </summary>
    AutoFlagged
}
