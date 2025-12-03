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
    Seller,

    /// <summary>
    /// An admin account that can manage the platform.
    /// </summary>
    Admin,

    /// <summary>
    /// A support account that can assist users and manage support tickets.
    /// </summary>
    Support,

    /// <summary>
    /// A compliance account that can review reports and audit logs.
    /// </summary>
    Compliance
}
