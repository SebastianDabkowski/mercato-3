using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a seller's configuration for a specific shipping provider.
/// Each seller can enable and configure the providers they want to use.
/// </summary>
public class ShippingProviderConfig
{
    /// <summary>
    /// Gets or sets the unique identifier for this configuration.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns this configuration.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the shipping provider ID.
    /// </summary>
    public int ShippingProviderId { get; set; }

    /// <summary>
    /// Gets or sets the shipping provider (navigation property).
    /// </summary>
    public ShippingProvider ShippingProvider { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether this provider is enabled for the seller.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the seller's account number with this provider.
    /// </summary>
    [MaxLength(100)]
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the seller's API key for this provider (encrypted).
    /// </summary>
    [MaxLength(500)]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the seller's API secret for this provider (encrypted).
    /// </summary>
    [MaxLength(500)]
    public string? ApiSecret { get; set; }

    /// <summary>
    /// Gets or sets seller-specific configuration metadata (stored as JSON).
    /// Can contain pickup addresses, service preferences, or other seller-specific settings.
    /// </summary>
    public string? ConfigMetadata { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically create shipments when order is marked ready to ship.
    /// Defaults to false to give sellers control over when shipments are created.
    /// </summary>
    public bool AutoCreateShipments { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to send automatic tracking updates to buyers.
    /// </summary>
    public bool AutoSendTrackingUpdates { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the configuration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
