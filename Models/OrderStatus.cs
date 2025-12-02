namespace MercatoApp.Models;

/// <summary>
/// Represents the status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order is pending payment.
    /// </summary>
    Pending,

    /// <summary>
    /// Order has been paid and is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Order has been shipped.
    /// </summary>
    Shipped,

    /// <summary>
    /// Order has been delivered.
    /// </summary>
    Delivered,

    /// <summary>
    /// Order has been cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Order has been refunded.
    /// </summary>
    Refunded
}
