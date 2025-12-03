namespace MercatoApp.Authorization;

/// <summary>
/// Constants for authorization policy names.
/// </summary>
public static class PolicyNames
{
    /// <summary>
    /// Policy that requires the user to be a buyer.
    /// </summary>
    public const string BuyerOnly = "BuyerOnly";

    /// <summary>
    /// Policy that requires the user to be a seller.
    /// </summary>
    public const string SellerOnly = "SellerOnly";

    /// <summary>
    /// Policy that requires the user to be an admin.
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Policy that requires the user to be a support staff member.
    /// </summary>
    public const string SupportOnly = "SupportOnly";

    /// <summary>
    /// Policy that requires the user to be a compliance officer.
    /// </summary>
    public const string ComplianceOnly = "ComplianceOnly";

    /// <summary>
    /// Policy that requires the user to be either a buyer or seller.
    /// </summary>
    public const string BuyerOrSeller = "BuyerOrSeller";

    /// <summary>
    /// Policy that requires the user to be either a seller or admin.
    /// </summary>
    public const string SellerOrAdmin = "SellerOrAdmin";

    /// <summary>
    /// Policy that requires the user to be either admin, support, or compliance.
    /// </summary>
    public const string AdminOrSupportOrCompliance = "AdminOrSupportOrCompliance";
}
