namespace MercatoApp.Models;

/// <summary>
/// Represents the status of a shipment in the shipping provider system.
/// </summary>
public enum ShipmentStatus
{
    /// <summary>
    /// Shipment created but not yet picked up or in transit.
    /// </summary>
    Created = 0,

    /// <summary>
    /// Shipment has been picked up and is in transit.
    /// </summary>
    InTransit = 1,

    /// <summary>
    /// Shipment is out for delivery.
    /// </summary>
    OutForDelivery = 2,

    /// <summary>
    /// Shipment has been successfully delivered.
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// Delivery attempt failed (e.g., customer not home).
    /// </summary>
    FailedDelivery = 4,

    /// <summary>
    /// Shipment is being returned to sender.
    /// </summary>
    Returning = 5,

    /// <summary>
    /// Shipment has been returned to sender.
    /// </summary>
    Returned = 6,

    /// <summary>
    /// Shipment is lost or cannot be located.
    /// </summary>
    Lost = 7,

    /// <summary>
    /// Shipment was cancelled before pickup.
    /// </summary>
    Cancelled = 8,

    /// <summary>
    /// Shipment encountered an exception requiring attention.
    /// </summary>
    Exception = 9
}
