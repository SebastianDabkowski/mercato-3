using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a status update event for a shipment.
/// Tracks the history of status changes as the shipment moves through the delivery process.
/// </summary>
public class ShipmentStatusUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier for this status update.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the shipment ID that this update belongs to.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the shipment (navigation property).
    /// </summary>
    public Shipment Shipment { get; set; } = null!;

    /// <summary>
    /// Gets or sets the previous status (before this update).
    /// </summary>
    public ShipmentStatus PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new status (after this update).
    /// </summary>
    public ShipmentStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the location where the status change occurred.
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets a description or notes about this status change.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the status change (from provider).
    /// </summary>
    public DateTime StatusChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether this update was received via webhook (true) or polling (false).
    /// </summary>
    public bool ReceivedViaWebhook { get; set; } = false;

    /// <summary>
    /// Gets or sets the raw data from the provider (stored as JSON).
    /// </summary>
    public string? RawData { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this update was recorded in our system.
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
