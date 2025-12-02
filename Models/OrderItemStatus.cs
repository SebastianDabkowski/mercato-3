namespace MercatoApp.Models;

/// <summary>
/// Represents the fulfillment status of an individual order item.
/// Used for partial fulfillment tracking within sub-orders.
/// </summary>
public enum OrderItemStatus
{
    /// <summary>
    /// Item is new and awaiting processing.
    /// </summary>
    New,

    /// <summary>
    /// Item is being prepared for shipment.
    /// </summary>
    Preparing,

    /// <summary>
    /// Item has been shipped to the customer.
    /// </summary>
    Shipped,

    /// <summary>
    /// Item has been cancelled (before shipment).
    /// </summary>
    Cancelled
}
