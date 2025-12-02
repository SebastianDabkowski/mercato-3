namespace MercatoApp.Models;

/// <summary>
/// Represents the status of a payout transaction.
/// </summary>
public enum PayoutStatus
{
    /// <summary>
    /// Payout is scheduled for future processing.
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Payout is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Payout has been successfully completed and funds have been sent.
    /// </summary>
    Paid = 2,

    /// <summary>
    /// Payout failed and needs attention.
    /// </summary>
    Failed = 3
}
