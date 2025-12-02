using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing shipping provider integrations.
/// Orchestrates shipment creation, tracking updates, and provider coordination.
/// </summary>
public class ShippingProviderIntegrationService : IShippingProviderIntegrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShippingProviderIntegrationService> _logger;
    private readonly IOrderStatusService _orderStatusService;
    private readonly IEmailService _emailService;
    // Note: _labelService is available for future use (e.g., cleanup operations, label management)
    // Currently labels are stored directly in shipment records, but this service provides
    // additional label management capabilities if needed.
    private readonly IShippingLabelService _labelService;
    private readonly Dictionary<string, IShippingProviderService> _providers;

    public ShippingProviderIntegrationService(
        ApplicationDbContext context,
        ILogger<ShippingProviderIntegrationService> logger,
        IOrderStatusService orderStatusService,
        IEmailService emailService,
        IShippingLabelService labelService,
        IEnumerable<IShippingProviderService> providers)
    {
        _context = context;
        _logger = logger;
        _orderStatusService = orderStatusService;
        _emailService = emailService;
        _labelService = labelService;
        
        // Build a dictionary of providers by their ProviderId
        _providers = providers.ToDictionary(p => p.ProviderId, p => p);
    }

    /// <inheritdoc />
    public async Task<Shipment?> CreateShipmentAsync(int subOrderId, int? userId = null)
    {
        // Load sub-order with all necessary related data
        var subOrder = await _context.SellerSubOrders
            .Include(so => so.Store)
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.DeliveryAddress)
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null)
        {
            _logger.LogWarning("Sub-order {SubOrderId} not found", subOrderId);
            return null;
        }

        // Check if shipment already exists
        var existingShipment = await _context.Set<Shipment>()
            .FirstOrDefaultAsync(s => s.SellerSubOrderId == subOrderId);

        if (existingShipment != null)
        {
            _logger.LogWarning("Shipment already exists for sub-order {SubOrderId}", subOrderId);
            return existingShipment;
        }

        // Find enabled provider configuration for the store
        var providerConfig = await _context.Set<ShippingProviderConfig>()
            .Include(pc => pc.ShippingProvider)
            .Where(pc => pc.StoreId == subOrder.StoreId && pc.IsEnabled)
            .OrderBy(pc => pc.ShippingProvider.DisplayOrder)
            .FirstOrDefaultAsync();

        if (providerConfig == null)
        {
            _logger.LogWarning("No enabled shipping provider found for store {StoreId}", subOrder.StoreId);
            return null;
        }

        // Get the provider service
        if (!_providers.TryGetValue(providerConfig.ShippingProvider.ProviderId, out var providerService))
        {
            _logger.LogError(
                "Provider service not found for {ProviderId}",
                providerConfig.ShippingProvider.ProviderId);
            return null;
        }

        // Get ship-from address (store's address)
        // TODO: Retrieve actual warehouse/pickup address from store configuration or provider config metadata
        var shipFromAddress = new Address
        {
            AddressLine1 = "123 Seller Street", // Placeholder - should come from config
            City = "Seller City",
            StateProvince = "ST",
            PostalCode = "12345",
            CountryCode = "US",
            FullName = subOrder.Store.StoreName,
            PhoneNumber = subOrder.Store.PhoneNumber ?? "555-0000"
        };

        // Get ship-to address (buyer's delivery address)
        var shipToAddress = subOrder.ParentOrder.DeliveryAddress;

        // Create shipment via provider API
        var result = await providerService.CreateShipmentAsync(
            subOrder,
            providerConfig,
            shipFromAddress,
            shipToAddress);

        if (!result.Success)
        {
            _logger.LogError(
                "Failed to create shipment for sub-order {SubOrderId}: {ErrorMessage}",
                subOrderId, result.ErrorMessage);
            return null;
        }

        // Save shipment to database
        var shipment = new Shipment
        {
            SellerSubOrderId = subOrderId,
            ShippingProviderConfigId = providerConfig.Id,
            ProviderShipmentId = result.ProviderShipmentId!,
            TrackingNumber = result.TrackingNumber!,
            CarrierService = result.CarrierService,
            TrackingUrl = result.TrackingUrl,
            Status = ShipmentStatus.Created,
            LabelUrl = result.LabelUrl,
            LabelData = result.LabelData,
            LabelFormat = result.LabelFormat,
            LabelContentType = result.LabelContentType,
            ShippingCost = result.ShippingCost,
            EstimatedDeliveryDate = result.EstimatedDeliveryDate,
            Metadata = result.Metadata != null 
                ? System.Text.Json.JsonSerializer.Serialize(result.Metadata) 
                : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<Shipment>().Add(shipment);

        // Add initial status update
        var statusUpdate = new ShipmentStatusUpdate
        {
            Shipment = shipment,
            PreviousStatus = ShipmentStatus.Created,
            NewStatus = ShipmentStatus.Created,
            Description = "Shipment created via provider API",
            StatusChangedAt = DateTime.UtcNow,
            ReceivedViaWebhook = false,
            RecordedAt = DateTime.UtcNow
        };

        _context.Set<ShipmentStatusUpdate>().Add(statusUpdate);

        // Update sub-order with tracking information
        subOrder.TrackingNumber = result.TrackingNumber;
        subOrder.CarrierName = result.CarrierService ?? providerConfig.ShippingProvider.Name;
        subOrder.TrackingUrl = result.TrackingUrl;
        subOrder.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Shipment created successfully for sub-order {SubOrderId}, Tracking: {TrackingNumber}, Label: {HasLabel}",
            subOrderId, result.TrackingNumber, result.LabelData != null ? "Yes" : "No");

        return shipment;
    }

    /// <inheritdoc />
    public async Task<Shipment?> GetShipmentBySubOrderIdAsync(int subOrderId)
    {
        return await _context.Set<Shipment>()
            .Include(s => s.ShippingProviderConfig)
                .ThenInclude(pc => pc.ShippingProvider)
            .Include(s => s.SellerSubOrder)
            .Include(s => s.StatusUpdates)
            .FirstOrDefaultAsync(s => s.SellerSubOrderId == subOrderId);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateShipmentTrackingAsync(int shipmentId)
    {
        var shipment = await _context.Set<Shipment>()
            .Include(s => s.ShippingProviderConfig)
                .ThenInclude(pc => pc.ShippingProvider)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found", shipmentId);
            return false;
        }

        // Get the provider service
        if (!_providers.TryGetValue(shipment.ShippingProviderConfig.ShippingProvider.ProviderId, 
            out var providerService))
        {
            _logger.LogError(
                "Provider service not found for {ProviderId}",
                shipment.ShippingProviderConfig.ShippingProvider.ProviderId);
            return false;
        }

        // Get tracking info from provider
        var trackingInfo = await providerService.GetTrackingInfoAsync(
            shipment.TrackingNumber,
            shipment.ShippingProviderConfig);

        if (!trackingInfo.Success)
        {
            _logger.LogError(
                "Failed to get tracking info for shipment {ShipmentId}: {ErrorMessage}",
                shipmentId, trackingInfo.ErrorMessage);
            return false;
        }

        // Update shipment if status changed
        if (trackingInfo.CurrentStatus.HasValue && 
            trackingInfo.CurrentStatus.Value != shipment.Status)
        {
            await ProcessTrackingUpdateAsync(
                shipmentId,
                trackingInfo.CurrentStatus.Value,
                trackingInfo.CurrentLocation,
                $"Status updated via polling",
                DateTime.UtcNow,
                receivedViaWebhook: false);
        }

        // Update estimated/actual delivery dates
        if (trackingInfo.EstimatedDeliveryDate.HasValue)
        {
            shipment.EstimatedDeliveryDate = trackingInfo.EstimatedDeliveryDate.Value;
        }

        if (trackingInfo.ActualDeliveryDate.HasValue)
        {
            shipment.ActualDeliveryDate = trackingInfo.ActualDeliveryDate.Value;
        }

        shipment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessTrackingUpdateAsync(
        int shipmentId,
        ShipmentStatus newStatus,
        string? location,
        string? description,
        DateTime statusChangedAt,
        bool receivedViaWebhook = false,
        string? rawData = null)
    {
        var shipment = await _context.Set<Shipment>()
            .Include(s => s.SellerSubOrder)
            .Include(s => s.ShippingProviderConfig)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found", shipmentId);
            return false;
        }

        var previousStatus = shipment.Status;

        // Don't process if status hasn't changed
        if (previousStatus == newStatus)
        {
            return true;
        }

        // Update shipment status
        shipment.Status = newStatus;
        shipment.UpdatedAt = DateTime.UtcNow;

        if (newStatus == ShipmentStatus.Delivered && !shipment.ActualDeliveryDate.HasValue)
        {
            shipment.ActualDeliveryDate = statusChangedAt;
        }

        // Create status update record
        var statusUpdate = new ShipmentStatusUpdate
        {
            ShipmentId = shipmentId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Location = location,
            Description = description,
            StatusChangedAt = statusChangedAt,
            ReceivedViaWebhook = receivedViaWebhook,
            RawData = rawData,
            RecordedAt = DateTime.UtcNow
        };

        _context.Set<ShipmentStatusUpdate>().Add(statusUpdate);

        // Update order status based on shipment status
        await UpdateOrderStatusFromShipmentAsync(shipment.SellerSubOrder, newStatus);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Shipment {ShipmentId} status updated from {PreviousStatus} to {NewStatus}",
            shipmentId, previousStatus, newStatus);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CancelShipmentAsync(int shipmentId, int? userId = null)
    {
        var shipment = await _context.Set<Shipment>()
            .Include(s => s.ShippingProviderConfig)
                .ThenInclude(pc => pc.ShippingProvider)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found", shipmentId);
            return false;
        }

        // Only allow cancellation if shipment hasn't been picked up
        if (shipment.Status != ShipmentStatus.Created)
        {
            _logger.LogWarning(
                "Cannot cancel shipment {ShipmentId} with status {Status}",
                shipmentId, shipment.Status);
            return false;
        }

        // Get the provider service
        if (!_providers.TryGetValue(shipment.ShippingProviderConfig.ShippingProvider.ProviderId, 
            out var providerService))
        {
            _logger.LogError(
                "Provider service not found for {ProviderId}",
                shipment.ShippingProviderConfig.ShippingProvider.ProviderId);
            return false;
        }

        // Cancel with provider
        var result = await providerService.CancelShipmentAsync(
            shipment.ProviderShipmentId,
            shipment.ShippingProviderConfig);

        if (!result.Success)
        {
            _logger.LogError(
                "Failed to cancel shipment {ShipmentId}: {ErrorMessage}",
                shipmentId, result.ErrorMessage);
            return false;
        }

        // Track previous status before updating
        var previousStatus = shipment.Status;

        // Update shipment status
        shipment.Status = ShipmentStatus.Cancelled;
        shipment.UpdatedAt = DateTime.UtcNow;

        // Create status update
        var statusUpdate = new ShipmentStatusUpdate
        {
            ShipmentId = shipmentId,
            PreviousStatus = previousStatus,
            NewStatus = ShipmentStatus.Cancelled,
            Description = "Shipment cancelled",
            StatusChangedAt = DateTime.UtcNow,
            ReceivedViaWebhook = false,
            RecordedAt = DateTime.UtcNow
        };

        _context.Set<ShipmentStatusUpdate>().Add(statusUpdate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Shipment {ShipmentId} cancelled successfully", shipmentId);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<ShippingProviderConfig>> GetEnabledProvidersForStoreAsync(int storeId)
    {
        return await _context.Set<ShippingProviderConfig>()
            .Include(pc => pc.ShippingProvider)
            .Where(pc => pc.StoreId == storeId && pc.IsEnabled && pc.ShippingProvider.IsActive)
            .OrderBy(pc => pc.ShippingProvider.DisplayOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProviderValidationResult> ValidateProviderConfigAsync(int configId)
    {
        var config = await _context.Set<ShippingProviderConfig>()
            .Include(pc => pc.ShippingProvider)
            .FirstOrDefaultAsync(pc => pc.Id == configId);

        if (config == null)
        {
            return new ProviderValidationResult
            {
                IsValid = false,
                ErrorMessages = new List<string> { "Configuration not found" }
            };
        }

        // Get the provider service
        if (!_providers.TryGetValue(config.ShippingProvider.ProviderId, out var providerService))
        {
            return new ProviderValidationResult
            {
                IsValid = false,
                ErrorMessages = new List<string> { "Provider service not available" }
            };
        }

        return await providerService.ValidateConfigurationAsync(config);
    }

    /// <inheritdoc />
    public async Task<bool> ProcessProviderWebhookAsync(
        string providerId,
        string webhookData,
        Dictionary<string, string> headers)
    {
        _logger.LogInformation("Processing webhook from provider {ProviderId}", providerId);

        // Get the provider service
        if (!_providers.TryGetValue(providerId, out var providerService))
        {
            _logger.LogError("Provider service not found for {ProviderId}", providerId);
            return false;
        }

        // Process the webhook
        var result = await providerService.ProcessWebhookAsync(webhookData, headers);

        if (!result.Success)
        {
            _logger.LogError(
                "Failed to process webhook from {ProviderId}: {ErrorMessage}",
                providerId, result.ErrorMessage);
            return false;
        }

        if (string.IsNullOrEmpty(result.TrackingNumber) || !result.NewStatus.HasValue)
        {
            _logger.LogWarning("Webhook missing tracking number or status");
            return false;
        }

        // Find shipment by tracking number
        var shipment = await _context.Set<Shipment>()
            .Include(s => s.SellerSubOrder)
            .FirstOrDefaultAsync(s => s.TrackingNumber == result.TrackingNumber);

        if (shipment == null)
        {
            _logger.LogWarning(
                "Shipment not found for tracking number {TrackingNumber}",
                result.TrackingNumber);
            return false;
        }

        // Process the status update
        await ProcessTrackingUpdateAsync(
            shipment.Id,
            result.NewStatus.Value,
            result.Location,
            result.Description,
            result.Timestamp ?? DateTime.UtcNow,
            receivedViaWebhook: true,
            rawData: webhookData);

        return true;
    }

    private async Task UpdateOrderStatusFromShipmentAsync(
        SellerSubOrder subOrder,
        ShipmentStatus shipmentStatus)
    {
        // Map shipment status to order status and update accordingly
        switch (shipmentStatus)
        {
            case ShipmentStatus.InTransit:
            case ShipmentStatus.OutForDelivery:
                if (subOrder.Status != OrderStatus.Shipped)
                {
                    var updateResult = await _orderStatusService.UpdateSubOrderToShippedAsync(
                        subOrder.Id,
                        trackingNumber: subOrder.TrackingNumber,
                        carrierName: subOrder.CarrierName,
                        trackingUrl: subOrder.TrackingUrl,
                        userId: null);

                    if (!updateResult.Success)
                    {
                        _logger.LogWarning(
                            "Failed to update sub-order {SubOrderId} to Shipped: {ErrorMessage}",
                            subOrder.Id, updateResult.ErrorMessage);
                    }
                }
                break;

            case ShipmentStatus.Delivered:
                if (subOrder.Status != OrderStatus.Delivered)
                {
                    var updateResult = await _orderStatusService.UpdateSubOrderToDeliveredAsync(
                        subOrder.Id,
                        userId: null);

                    if (!updateResult.Success)
                    {
                        _logger.LogWarning(
                            "Failed to update sub-order {SubOrderId} to Delivered: {ErrorMessage}",
                            subOrder.Id, updateResult.ErrorMessage);
                    }
                }
                break;
        }
    }
}
