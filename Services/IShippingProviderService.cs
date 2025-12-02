using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for shipping provider integration.
/// Handles communication with external shipping carrier APIs.
/// Each provider implementation (FedEx, UPS, etc.) implements this interface.
/// </summary>
public interface IShippingProviderService
{
    /// <summary>
    /// Gets the provider ID that this service handles (e.g., "fedex", "ups", "usps").
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Creates a shipment with the shipping provider.
    /// </summary>
    /// <param name="subOrder">The seller sub-order to create a shipment for.</param>
    /// <param name="providerConfig">The seller's configuration for this provider.</param>
    /// <param name="shipFromAddress">The address to ship from (seller's warehouse/store).</param>
    /// <param name="shipToAddress">The address to ship to (buyer's delivery address).</param>
    /// <returns>Result containing shipment details and tracking information.</returns>
    Task<ShipmentCreationResult> CreateShipmentAsync(
        SellerSubOrder subOrder,
        ShippingProviderConfig providerConfig,
        Address shipFromAddress,
        Address shipToAddress);

    /// <summary>
    /// Gets tracking information for an existing shipment.
    /// </summary>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <param name="providerConfig">The seller's configuration for this provider.</param>
    /// <returns>Tracking information including current status and history.</returns>
    Task<TrackingInfoResult> GetTrackingInfoAsync(
        string trackingNumber,
        ShippingProviderConfig providerConfig);

    /// <summary>
    /// Cancels a shipment that hasn't been picked up yet.
    /// </summary>
    /// <param name="providerShipmentId">The provider's shipment ID.</param>
    /// <param name="providerConfig">The seller's configuration for this provider.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<ShipmentCancellationResult> CancelShipmentAsync(
        string providerShipmentId,
        ShippingProviderConfig providerConfig);

    /// <summary>
    /// Validates provider configuration credentials.
    /// </summary>
    /// <param name="providerConfig">The configuration to validate.</param>
    /// <returns>Validation result.</returns>
    Task<ProviderValidationResult> ValidateConfigurationAsync(
        ShippingProviderConfig providerConfig);

    /// <summary>
    /// Processes a webhook notification from the provider.
    /// </summary>
    /// <param name="webhookData">The webhook payload data.</param>
    /// <param name="headers">HTTP headers from the webhook request.</param>
    /// <returns>Parsed tracking update information.</returns>
    Task<WebhookProcessingResult> ProcessWebhookAsync(
        string webhookData,
        Dictionary<string, string> headers);
}

/// <summary>
/// Result of shipment creation.
/// </summary>
public class ShipmentCreationResult
{
    /// <summary>
    /// Gets or sets whether the shipment was created successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the provider's shipment/label ID.
    /// </summary>
    public string? ProviderShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number assigned by the provider.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the carrier/service name.
    /// </summary>
    public string? CarrierService { get; set; }

    /// <summary>
    /// Gets or sets the tracking URL.
    /// </summary>
    public string? TrackingUrl { get; set; }

    /// <summary>
    /// Gets or sets the shipping label URL (if available).
    /// </summary>
    public string? LabelUrl { get; set; }

    /// <summary>
    /// Gets or sets the shipping cost charged by the provider.
    /// </summary>
    public decimal? ShippingCost { get; set; }

    /// <summary>
    /// Gets or sets the estimated delivery date.
    /// </summary>
    public DateTime? EstimatedDeliveryDate { get; set; }

    /// <summary>
    /// Gets or sets the error message if creation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata from the provider.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Result of tracking information query.
/// </summary>
public class TrackingInfoResult
{
    /// <summary>
    /// Gets or sets whether the tracking query was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the current status of the shipment.
    /// </summary>
    public ShipmentStatus? CurrentStatus { get; set; }

    /// <summary>
    /// Gets or sets the current location of the shipment.
    /// </summary>
    public string? CurrentLocation { get; set; }

    /// <summary>
    /// Gets or sets the estimated delivery date.
    /// </summary>
    public DateTime? EstimatedDeliveryDate { get; set; }

    /// <summary>
    /// Gets or sets the actual delivery date (if delivered).
    /// </summary>
    public DateTime? ActualDeliveryDate { get; set; }

    /// <summary>
    /// Gets or sets the tracking history events.
    /// </summary>
    public List<TrackingEvent>? TrackingHistory { get; set; }

    /// <summary>
    /// Gets or sets the error message if query failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents a single tracking event in the shipment history.
/// </summary>
public class TrackingEvent
{
    /// <summary>
    /// Gets or sets the status at this event.
    /// </summary>
    public ShipmentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the location where the event occurred.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the description of the event.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the event.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Result of shipment cancellation.
/// </summary>
public class ShipmentCancellationResult
{
    /// <summary>
    /// Gets or sets whether the cancellation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if cancellation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of provider configuration validation.
/// </summary>
public class ProviderValidationResult
{
    /// <summary>
    /// Gets or sets whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets validation error messages.
    /// </summary>
    public List<string>? ErrorMessages { get; set; }
}

/// <summary>
/// Result of webhook processing.
/// </summary>
public class WebhookProcessingResult
{
    /// <summary>
    /// Gets or sets whether the webhook was processed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the tracking number from the webhook.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the new status from the webhook.
    /// </summary>
    public ShipmentStatus? NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the location from the webhook.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the description from the webhook.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the timestamp from the webhook.
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
