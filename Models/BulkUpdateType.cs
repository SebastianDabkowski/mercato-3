namespace MercatoApp.Models;

/// <summary>
/// Type of bulk update operation.
/// </summary>
public enum BulkUpdateType
{
    /// <summary>
    /// Update product prices.
    /// </summary>
    Price,

    /// <summary>
    /// Update product stock levels.
    /// </summary>
    Stock
}
