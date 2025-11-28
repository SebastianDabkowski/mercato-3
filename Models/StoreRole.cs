namespace MercatoApp.Models;

/// <summary>
/// Represents the roles available for internal users within a store.
/// </summary>
public enum StoreRole
{
    /// <summary>
    /// Full access to all store functionality including user management.
    /// </summary>
    StoreOwner = 0,

    /// <summary>
    /// Can manage products, categories, and inventory.
    /// </summary>
    CatalogManager = 1,

    /// <summary>
    /// Can view and process orders.
    /// </summary>
    OrderManager = 2,

    /// <summary>
    /// Read-only access for reporting and accounting purposes.
    /// </summary>
    ReadOnly = 3
}

/// <summary>
/// Helper class for store role names and descriptions.
/// </summary>
public static class StoreRoleNames
{
    public const string StoreOwner = "StoreOwner";
    public const string CatalogManager = "CatalogManager";
    public const string OrderManager = "OrderManager";
    public const string ReadOnly = "ReadOnly";

    /// <summary>
    /// Gets a human-readable display name for the store role.
    /// </summary>
    public static string GetDisplayName(StoreRole role)
    {
        return role switch
        {
            StoreRole.StoreOwner => "Store Owner",
            StoreRole.CatalogManager => "Catalog Manager",
            StoreRole.OrderManager => "Order Manager",
            StoreRole.ReadOnly => "Read Only / Accounting",
            _ => role.ToString()
        };
    }

    /// <summary>
    /// Gets a description of the permissions for the store role.
    /// </summary>
    public static string GetDescription(StoreRole role)
    {
        return role switch
        {
            StoreRole.StoreOwner => "Full access to all store functionality including user management",
            StoreRole.CatalogManager => "Can manage products, categories, and inventory",
            StoreRole.OrderManager => "Can view and process orders",
            StoreRole.ReadOnly => "Read-only access for reporting and accounting purposes",
            _ => string.Empty
        };
    }
}
