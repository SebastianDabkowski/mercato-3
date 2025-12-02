namespace MercatoApp.Models;

/// <summary>
/// Represents the status of an order or sub-order in the fulfillment lifecycle.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order is new and awaiting payment confirmation.
    /// </summary>
    New,

    /// <summary>
    /// Payment has been authorized and confirmed. Order is ready to be prepared.
    /// </summary>
    Paid,

    /// <summary>
    /// Seller is preparing the shipment.
    /// </summary>
    Preparing,

    /// <summary>
    /// Order has been shipped to the customer.
    /// </summary>
    Shipped,

    /// <summary>
    /// Order has been delivered to the customer.
    /// </summary>
    Delivered,

    /// <summary>
    /// Order has been cancelled (before shipment).
    /// </summary>
    Cancelled,

    /// <summary>
    /// Order has been refunded.
    /// </summary>
    Refunded
}
