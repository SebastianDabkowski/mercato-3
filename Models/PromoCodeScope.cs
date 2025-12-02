namespace MercatoApp.Models;

/// <summary>
/// Scope of a promo code.
/// </summary>
public enum PromoCodeScope
{
    /// <summary>
    /// Promo code issued by Mercato platform, applies to any seller.
    /// </summary>
    Platform = 0,

    /// <summary>
    /// Promo code issued by a specific seller, applies only to that seller's products.
    /// </summary>
    Seller = 1
}
