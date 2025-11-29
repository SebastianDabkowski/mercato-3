namespace MercatoApp.Models;

/// <summary>
/// Represents the workflow status of a product.
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// Product is in draft state, not visible in public catalog.
    /// </summary>
    Draft,

    /// <summary>
    /// Product is active and visible in the public catalog.
    /// </summary>
    Active,

    /// <summary>
    /// Product is suspended, no longer available for new orders but may remain
    /// visible where business rules allow (e.g., in order history).
    /// </summary>
    Suspended,

    /// <summary>
    /// Product has been archived, completely removed from public listings
    /// and normal seller editing flows, but remains available for reporting and audit.
    /// </summary>
    Archived
}
