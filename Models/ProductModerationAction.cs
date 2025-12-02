namespace MercatoApp.Models;

/// <summary>
/// Represents the type of moderation action taken on a product.
/// </summary>
public enum ProductModerationAction
{
    /// <summary>
    /// Product was submitted for moderation.
    /// </summary>
    Submitted,

    /// <summary>
    /// Product was approved by admin.
    /// </summary>
    Approved,

    /// <summary>
    /// Product was rejected by admin.
    /// </summary>
    Rejected,

    /// <summary>
    /// Product was flagged for review.
    /// </summary>
    Flagged,

    /// <summary>
    /// Product moderation was reset (e.g., after edits).
    /// </summary>
    Reset
}
