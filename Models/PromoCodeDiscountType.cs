namespace MercatoApp.Models;

/// <summary>
/// Type of discount for a promo code.
/// </summary>
public enum PromoCodeDiscountType
{
    /// <summary>
    /// Percentage discount (e.g., 10% off).
    /// </summary>
    Percentage = 0,

    /// <summary>
    /// Fixed amount discount (e.g., $5 off).
    /// </summary>
    FixedAmount = 1
}
