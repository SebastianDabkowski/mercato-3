using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a shipment created via a shipping provider integration.
/// Links an order/sub-order to provider-generated tracking information.
/// </summary>
public class Shipment
{
    /// <summary>
    /// Gets or sets the unique identifier for the shipment.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order ID that this shipment belongs to.
    /// </summary>
    public int SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order (navigation property).
    /// </summary>
    public SellerSubOrder SellerSubOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the shipping provider config ID used to create this shipment.
    /// </summary>
    public int ShippingProviderConfigId { get; set; }

    /// <summary>
    /// Gets or sets the shipping provider config (navigation property).
    /// </summary>
    public ShippingProviderConfig ShippingProviderConfig { get; set; } = null!;

    /// <summary>
    /// Gets or sets the provider's shipment/label ID.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ProviderShipmentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tracking number assigned by the provider.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the carrier/service name (e.g., "FedEx Ground", "USPS Priority Mail").
    /// </summary>
    [MaxLength(100)]
    public string? CarrierService { get; set; }

    /// <summary>
    /// Gets or sets the tracking URL provided by the carrier.
    /// </summary>
    [MaxLength(500)]
    public string? TrackingUrl { get; set; }

    /// <summary>
    /// Gets or sets the current status of the shipment.
    /// </summary>
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;

    /// <summary>
    /// Gets or sets the shipping label URL (if available).
    /// </summary>
    [MaxLength(500)]
    public string? LabelUrl { get; set; }

    /// <summary>
    /// Gets or sets the shipping label data stored as binary (PDF or image format).
    /// </summary>
    public byte[]? LabelData { get; set; }

    /// <summary>
    /// Gets or sets the format of the label (e.g., "PDF", "PNG", "ZPL").
    /// </summary>
    [MaxLength(20)]
    public string? LabelFormat { get; set; }

    /// <summary>
    /// Gets or sets the MIME content type of the label data (e.g., "application/pdf", "image/png").
    /// </summary>
    [MaxLength(100)]
    public string? LabelContentType { get; set; }

    /// <summary>
    /// Gets or sets the shipping cost charged by the provider.
    /// </summary>
    public decimal? ShippingCost { get; set; }

    /// <summary>
    /// Gets or sets the estimated delivery date provided by the carrier.
    /// </summary>
    public DateTime? EstimatedDeliveryDate { get; set; }

    /// <summary>
    /// Gets or sets the actual delivery date (when status becomes Delivered).
    /// </summary>
    public DateTime? ActualDeliveryDate { get; set; }

    /// <summary>
    /// Gets or sets additional metadata from the provider (stored as JSON).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the shipment was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the shipment was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the shipment status updates (navigation property).
    /// </summary>
    public ICollection<ShipmentStatusUpdate> StatusUpdates { get; set; } = new List<ShipmentStatusUpdate>();
}
