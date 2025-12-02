using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a shipping provider available on the platform.
/// Platform owners configure which providers are available for sellers to use.
/// </summary>
public class ShippingProvider
{
    /// <summary>
    /// Gets or sets the unique identifier for the shipping provider.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the provider identifier (e.g., "fedex", "ups", "usps", "dhl").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the shipping provider.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the shipping provider.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this provider is currently active and available for sellers.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this provider supports automatic shipment creation via API.
    /// </summary>
    public bool SupportsAutomation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this provider supports tracking updates via webhooks.
    /// </summary>
    public bool SupportsWebhooks { get; set; } = false;

    /// <summary>
    /// Gets or sets the webhook URL for receiving status updates from this provider.
    /// </summary>
    [MaxLength(500)]
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Gets or sets the API endpoint URL for this provider (if applicable).
    /// </summary>
    [MaxLength(500)]
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// Gets or sets configuration metadata for the provider (stored as JSON).
    /// Can contain API keys, authentication details, or other provider-specific settings.
    /// </summary>
    public string? ConfigMetadata { get; set; }

    /// <summary>
    /// Gets or sets the display order for this provider.
    /// Lower values are displayed first.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Gets or sets the date and time when the provider was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the provider was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
