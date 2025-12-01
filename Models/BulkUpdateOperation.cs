namespace MercatoApp.Models;

/// <summary>
/// Operation to perform for bulk update.
/// </summary>
public enum BulkUpdateOperation
{
    /// <summary>
    /// Set to a fixed value.
    /// </summary>
    SetValue,

    /// <summary>
    /// Increase by a fixed amount.
    /// </summary>
    IncreaseBy,

    /// <summary>
    /// Decrease by a fixed amount.
    /// </summary>
    DecreaseBy,

    /// <summary>
    /// Increase by a percentage.
    /// </summary>
    IncreaseByPercent,

    /// <summary>
    /// Decrease by a percentage.
    /// </summary>
    DecreaseByPercent
}
