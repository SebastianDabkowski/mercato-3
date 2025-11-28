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
    /// Product is inactive/paused, not visible in public catalog.
    /// </summary>
    Inactive,

    /// <summary>
    /// Product has been archived.
    /// </summary>
    Archived
}
