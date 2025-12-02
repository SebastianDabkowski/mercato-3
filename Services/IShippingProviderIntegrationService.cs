using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for the shipping provider integration orchestration service.
/// Manages the lifecycle of shipments and coordinates between different provider implementations.
/// </summary>
public interface IShippingProviderIntegrationService
{
    /// <summary>
    /// Creates a shipment for a sub-order using the configured shipping provider.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <param name="userId">The user ID creating the shipment (for audit).</param>
    /// <returns>The created shipment or null if creation failed.</returns>
    Task<Shipment?> CreateShipmentAsync(int subOrderId, int? userId = null);

    /// <summary>
    /// Gets the shipment for a sub-order.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <returns>The shipment or null if not found.</returns>
    Task<Shipment?> GetShipmentBySubOrderIdAsync(int subOrderId);

    /// <summary>
    /// Updates tracking information for a shipment by polling the provider.
    /// </summary>
    /// <param name="shipmentId">The shipment ID.</param>
    /// <returns>True if update was successful, false otherwise.</returns>
    Task<bool> UpdateShipmentTrackingAsync(int shipmentId);

    /// <summary>
    /// Processes a tracking status update and updates order status accordingly.
    /// </summary>
    /// <param name="shipmentId">The shipment ID.</param>
    /// <param name="newStatus">The new shipment status.</param>
    /// <param name="location">The location where the status change occurred.</param>
    /// <param name="description">Description of the status change.</param>
    /// <param name="statusChangedAt">Timestamp of the status change.</param>
    /// <param name="receivedViaWebhook">Whether this update came from a webhook.</param>
    /// <param name="rawData">Raw data from the provider.</param>
    /// <returns>True if processing was successful, false otherwise.</returns>
    Task<bool> ProcessTrackingUpdateAsync(
        int shipmentId,
        ShipmentStatus newStatus,
        string? location,
        string? description,
        DateTime statusChangedAt,
        bool receivedViaWebhook = false,
        string? rawData = null);

    /// <summary>
    /// Cancels a shipment that hasn't been picked up yet.
    /// </summary>
    /// <param name="shipmentId">The shipment ID.</param>
    /// <param name="userId">The user ID cancelling the shipment (for audit).</param>
    /// <returns>True if cancellation was successful, false otherwise.</returns>
    Task<bool> CancelShipmentAsync(int shipmentId, int? userId = null);

    /// <summary>
    /// Gets all enabled shipping providers for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>List of enabled shipping provider configurations.</returns>
    Task<List<ShippingProviderConfig>> GetEnabledProvidersForStoreAsync(int storeId);

    /// <summary>
    /// Validates a provider configuration.
    /// </summary>
    /// <param name="configId">The provider config ID.</param>
    /// <returns>Validation result.</returns>
    Task<ProviderValidationResult> ValidateProviderConfigAsync(int configId);

    /// <summary>
    /// Processes a webhook from a shipping provider.
    /// </summary>
    /// <param name="providerId">The provider ID (e.g., "fedex", "ups").</param>
    /// <param name="webhookData">The webhook payload.</param>
    /// <param name="headers">HTTP headers from the webhook request.</param>
    /// <returns>True if webhook was processed successfully, false otherwise.</returns>
    Task<bool> ProcessProviderWebhookAsync(
        string providerId,
        string webhookData,
        Dictionary<string, string> headers);
}
