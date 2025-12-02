namespace MercatoApp.Models;

/// <summary>
/// Represents the type of settlement adjustment.
/// </summary>
public enum SettlementAdjustmentType
{
    /// <summary>
    /// Prior period adjustment from previous month.
    /// </summary>
    PriorPeriodAdjustment = 0,

    /// <summary>
    /// Manual credit adjustment.
    /// </summary>
    Credit = 1,

    /// <summary>
    /// Manual debit adjustment.
    /// </summary>
    Debit = 2,

    /// <summary>
    /// Fee charged to the seller.
    /// </summary>
    Fee = 3,

    /// <summary>
    /// Refund processing adjustment.
    /// </summary>
    RefundAdjustment = 4,

    /// <summary>
    /// Other miscellaneous adjustment.
    /// </summary>
    Other = 5
}
