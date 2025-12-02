namespace MercatoApp.Models;

/// <summary>
/// Represents the moderation status of a product.
/// </summary>
public enum ProductModerationStatus
{
    /// <summary>
    /// Product is pending moderation review.
    /// </summary>
    Pending,

    /// <summary>
    /// Product has been approved and can be made active.
    /// </summary>
    Approved,

    /// <summary>
    /// Product has been rejected and should not be visible to buyers.
    /// </summary>
    Rejected
}
