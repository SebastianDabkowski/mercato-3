using MercatoApp.Models;
using System.Text.Json;

namespace MercatoApp.Services;

/// <summary>
/// Mock shipping provider service for testing and development.
/// Simulates integration with shipping carriers like FedEx, UPS, etc.
/// In production, this would be replaced with real carrier API integrations.
/// </summary>
public class MockShippingProviderService : IShippingProviderService
{
    private readonly ILogger<MockShippingProviderService> _logger;
    private readonly string _providerId;

    public MockShippingProviderService(
        ILogger<MockShippingProviderService> logger,
        string providerId = "mock_standard")
    {
        _logger = logger;
        _providerId = providerId;
    }

    /// <inheritdoc />
    public string ProviderId => _providerId;

    /// <inheritdoc />
    public async Task<ShipmentCreationResult> CreateShipmentAsync(
        SellerSubOrder subOrder,
        ShippingProviderConfig providerConfig,
        Address shipFromAddress,
        Address shipToAddress)
    {
        await Task.Delay(200); // Simulate API call

        _logger.LogInformation(
            "Creating shipment for sub-order {SubOrderId} with provider {ProviderId}",
            subOrder.Id, ProviderId);

        // Validate addresses
        if (string.IsNullOrWhiteSpace(shipFromAddress.AddressLine1) || 
            string.IsNullOrWhiteSpace(shipFromAddress.City) ||
            string.IsNullOrWhiteSpace(shipFromAddress.PostalCode))
        {
            return new ShipmentCreationResult
            {
                Success = false,
                ErrorMessage = "Ship from address is incomplete. Please provide complete address information."
            };
        }

        if (string.IsNullOrWhiteSpace(shipToAddress.AddressLine1) || 
            string.IsNullOrWhiteSpace(shipToAddress.City) ||
            string.IsNullOrWhiteSpace(shipToAddress.PostalCode))
        {
            return new ShipmentCreationResult
            {
                Success = false,
                ErrorMessage = "Ship to address is incomplete. Please provide complete address information."
            };
        }

        // Generate mock tracking number based on provider
        var trackingNumber = GenerateTrackingNumber(ProviderId);
        var providerShipmentId = $"{ProviderId.ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..12]}";

        // Determine carrier service name based on provider
        var carrierService = ProviderId switch
        {
            "mock_express" => "Mock Express Overnight",
            "mock_standard" => "Mock Standard Ground",
            "fedex" => "FedEx Ground",
            "ups" => "UPS Ground",
            "usps" => "USPS Priority Mail",
            "dhl" => "DHL Express",
            _ => "Standard Shipping"
        };

        // Calculate estimated delivery (3-5 days for standard, 1-2 for express)
        var daysToDeliver = ProviderId.Contains("express", StringComparison.OrdinalIgnoreCase) ? 2 : 4;
        var estimatedDelivery = DateTime.UtcNow.AddDays(daysToDeliver);

        // Simulate shipping cost
        var shippingCost = ProviderId.Contains("express", StringComparison.OrdinalIgnoreCase) ? 15.99m : 8.99m;

        _logger.LogInformation(
            "Shipment created successfully. Tracking: {TrackingNumber}, Provider ID: {ProviderShipmentId}",
            trackingNumber, providerShipmentId);

        return new ShipmentCreationResult
        {
            Success = true,
            ProviderShipmentId = providerShipmentId,
            TrackingNumber = trackingNumber,
            CarrierService = carrierService,
            TrackingUrl = $"https://track.example.com/{trackingNumber}",
            LabelUrl = $"https://labels.example.com/{providerShipmentId}.pdf",
            ShippingCost = shippingCost,
            EstimatedDeliveryDate = estimatedDelivery,
            Metadata = new Dictionary<string, string>
            {
                { "provider", ProviderId },
                { "service_type", carrierService },
                { "created_at", DateTime.UtcNow.ToString("O") }
            }
        };
    }

    /// <inheritdoc />
    public async Task<TrackingInfoResult> GetTrackingInfoAsync(
        string trackingNumber,
        ShippingProviderConfig providerConfig)
    {
        await Task.Delay(150); // Simulate API call

        _logger.LogInformation(
            "Getting tracking info for {TrackingNumber} with provider {ProviderId}",
            trackingNumber, ProviderId);

        // Simulate different tracking scenarios based on tracking number pattern
        var status = DetermineCurrentStatus(trackingNumber);
        var trackingHistory = GenerateTrackingHistory(trackingNumber, status);

        var estimatedDelivery = DateTime.UtcNow.AddDays(3);
        DateTime? actualDelivery = status == ShipmentStatus.Delivered 
            ? DateTime.UtcNow.AddDays(-1) 
            : null;

        return new TrackingInfoResult
        {
            Success = true,
            CurrentStatus = status,
            CurrentLocation = GetCurrentLocation(status),
            EstimatedDeliveryDate = estimatedDelivery,
            ActualDeliveryDate = actualDelivery,
            TrackingHistory = trackingHistory
        };
    }

    /// <inheritdoc />
    public async Task<ShipmentCancellationResult> CancelShipmentAsync(
        string providerShipmentId,
        ShippingProviderConfig providerConfig)
    {
        await Task.Delay(100); // Simulate API call

        _logger.LogInformation(
            "Cancelling shipment {ProviderShipmentId} with provider {ProviderId}",
            providerShipmentId, ProviderId);

        // Simulate cancellation success
        return new ShipmentCancellationResult
        {
            Success = true
        };
    }

    /// <inheritdoc />
    public async Task<ProviderValidationResult> ValidateConfigurationAsync(
        ShippingProviderConfig providerConfig)
    {
        await Task.Delay(50); // Simulate API call

        _logger.LogInformation(
            "Validating configuration for provider {ProviderId}",
            ProviderId);

        var errors = new List<string>();

        // For mock provider, we don't require real credentials
        // In production, this would validate API keys with the actual provider

        if (string.IsNullOrWhiteSpace(providerConfig.AccountNumber))
        {
            errors.Add("Account number is required");
        }

        return new ProviderValidationResult
        {
            IsValid = errors.Count == 0,
            ErrorMessages = errors.Count > 0 ? errors : null
        };
    }

    /// <inheritdoc />
    public async Task<WebhookProcessingResult> ProcessWebhookAsync(
        string webhookData,
        Dictionary<string, string> headers)
    {
        await Task.Delay(50); // Simulate processing

        _logger.LogInformation(
            "Processing webhook for provider {ProviderId}",
            ProviderId);

        try
        {
            // Parse the webhook data (simplified JSON parsing)
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(webhookData);
            
            if (data == null)
            {
                return new WebhookProcessingResult
                {
                    Success = false,
                    ErrorMessage = "Invalid webhook payload"
                };
            }

            // Extract tracking information from webhook
            var trackingNumber = data.ContainsKey("tracking_number") 
                ? data["tracking_number"].GetString() 
                : null;

            var statusStr = data.ContainsKey("status") 
                ? data["status"].GetString() 
                : null;

            var location = data.ContainsKey("location") 
                ? data["location"].GetString() 
                : null;

            var description = data.ContainsKey("description") 
                ? data["description"].GetString() 
                : null;

            if (string.IsNullOrEmpty(trackingNumber) || string.IsNullOrEmpty(statusStr))
            {
                return new WebhookProcessingResult
                {
                    Success = false,
                    ErrorMessage = "Webhook missing required fields"
                };
            }

            var newStatus = MapStatusFromProvider(statusStr);

            return new WebhookProcessingResult
            {
                Success = true,
                TrackingNumber = trackingNumber,
                NewStatus = newStatus,
                Location = location,
                Description = description,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return new WebhookProcessingResult
            {
                Success = false,
                ErrorMessage = $"Error processing webhook: {ex.Message}"
            };
        }
    }

    private string GenerateTrackingNumber(string providerId)
    {
        // Generate realistic-looking tracking numbers for different providers
        return providerId switch
        {
            "mock_express" => $"1Z{Random.Shared.Next(100000, 999999)}{Random.Shared.Next(10000000, 99999999)}",
            "fedex" => $"{Random.Shared.NextInt64(100000000000, 999999999999)}",
            "ups" => $"1Z{Random.Shared.Next(100000, 999999)}{Random.Shared.Next(10000000, 99999999)}",
            "usps" => $"9400{Random.Shared.NextInt64(1000000000, 9999999999)}",
            "dhl" => $"{Random.Shared.NextInt64(1000000000, 9999999999)}",
            _ => $"MOCK{Random.Shared.Next(100000000, 999999999)}"
        };
    }

    private ShipmentStatus DetermineCurrentStatus(string trackingNumber)
    {
        // Simulate different statuses based on the last digit of tracking number
        var lastChar = trackingNumber[trackingNumber.Length - 1];
        if (!char.IsDigit(lastChar))
        {
            return ShipmentStatus.InTransit;
        }
        
        var lastDigit = lastChar - '0';
        
        return lastDigit switch
        {
            0 or 1 => ShipmentStatus.Created,
            2 or 3 or 4 => ShipmentStatus.InTransit,
            5 or 6 => ShipmentStatus.OutForDelivery,
            7 or 8 or 9 => ShipmentStatus.Delivered,
            _ => ShipmentStatus.InTransit
        };
    }

    private List<TrackingEvent> GenerateTrackingHistory(string trackingNumber, ShipmentStatus currentStatus)
    {
        var history = new List<TrackingEvent>
        {
            new TrackingEvent
            {
                Status = ShipmentStatus.Created,
                Location = "Origin Facility",
                Description = "Shipment information received",
                Timestamp = DateTime.UtcNow.AddDays(-3)
            }
        };

        if ((int)currentStatus >= (int)ShipmentStatus.InTransit)
        {
            history.Add(new TrackingEvent
            {
                Status = ShipmentStatus.InTransit,
                Location = "Distribution Center",
                Description = "In transit to destination",
                Timestamp = DateTime.UtcNow.AddDays(-2)
            });
        }

        if ((int)currentStatus >= (int)ShipmentStatus.OutForDelivery)
        {
            history.Add(new TrackingEvent
            {
                Status = ShipmentStatus.OutForDelivery,
                Location = "Local Facility",
                Description = "Out for delivery",
                Timestamp = DateTime.UtcNow.AddHours(-4)
            });
        }

        if (currentStatus == ShipmentStatus.Delivered)
        {
            history.Add(new TrackingEvent
            {
                Status = ShipmentStatus.Delivered,
                Location = "Delivery Address",
                Description = "Delivered",
                Timestamp = DateTime.UtcNow.AddHours(-1)
            });
        }

        return history;
    }

    private string GetCurrentLocation(ShipmentStatus status)
    {
        return status switch
        {
            ShipmentStatus.Created => "Origin Facility",
            ShipmentStatus.InTransit => "Distribution Center - En Route",
            ShipmentStatus.OutForDelivery => "Local Facility - Out for Delivery",
            ShipmentStatus.Delivered => "Delivered",
            _ => "Unknown"
        };
    }

    private ShipmentStatus MapStatusFromProvider(string providerStatus)
    {
        // Map provider-specific status strings to our ShipmentStatus enum
        return providerStatus.ToLowerInvariant() switch
        {
            "created" or "label_created" or "pending_pickup" => ShipmentStatus.Created,
            "in_transit" or "transit" => ShipmentStatus.InTransit,
            "out_for_delivery" => ShipmentStatus.OutForDelivery,
            "delivered" => ShipmentStatus.Delivered,
            "failed_delivery" or "delivery_failed" => ShipmentStatus.FailedDelivery,
            "returning" or "return_to_sender" => ShipmentStatus.Returning,
            "returned" => ShipmentStatus.Returned,
            "lost" => ShipmentStatus.Lost,
            "cancelled" => ShipmentStatus.Cancelled,
            "exception" => ShipmentStatus.Exception,
            _ => ShipmentStatus.InTransit
        };
    }
}
