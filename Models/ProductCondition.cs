namespace MercatoApp.Models;

/// <summary>
/// Represents the condition of a product.
/// </summary>
public enum ProductCondition
{
    /// <summary>
    /// Brand new product, never used.
    /// </summary>
    New,

    /// <summary>
    /// Used product in good condition.
    /// </summary>
    Used,

    /// <summary>
    /// Refurbished or reconditioned product.
    /// </summary>
    Refurbished,

    /// <summary>
    /// Product with visible wear and tear but still functional.
    /// </summary>
    LikeNew,

    /// <summary>
    /// Product sold for parts or not working.
    /// </summary>
    ForParts
}
