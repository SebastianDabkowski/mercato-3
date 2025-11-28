namespace MercatoApp.Models;

/// <summary>
/// Represents the type of user account in the marketplace.
/// </summary>
public enum UserType
{
    /// <summary>
    /// A buyer account that can purchase products from sellers.
    /// </summary>
    Buyer,

    /// <summary>
    /// A seller account that can list and sell products.
    /// </summary>
    Seller
}
