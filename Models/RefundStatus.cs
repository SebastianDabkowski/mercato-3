namespace MercatoApp.Models;

/// <summary>
/// Represents the status of a refund transaction.
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// Refund has been requested but not yet processed.
    /// </summary>
    Requested,

    /// <summary>
    /// Refund is being processed by the payment provider.
    /// </summary>
    Processing,

    /// <summary>
    /// Refund has been successfully completed and funds returned to buyer.
    /// </summary>
    Completed,

    /// <summary>
    /// Refund processing failed.
    /// </summary>
    Failed
}
