namespace MercatoApp.Models;

/// <summary>
/// Represents the status of a payment transaction.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending authorization.
    /// </summary>
    Pending,

    /// <summary>
    /// Payment has been authorized but not yet captured.
    /// </summary>
    Authorized,

    /// <summary>
    /// Payment has been captured/completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Payment failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Payment was cancelled by the user.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Payment has been refunded.
    /// </summary>
    Refunded
}
