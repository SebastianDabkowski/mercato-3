namespace MercatoApp.Models;

/// <summary>
/// Represents an external integration configuration.
/// </summary>
public class Integration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the integration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the integration type.
    /// </summary>
    public IntegrationType Type { get; set; }

    /// <summary>
    /// Gets or sets the integration provider (e.g., "Stripe", "FedEx").
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the environment mode.
    /// </summary>
    public IntegrationEnvironment Environment { get; set; }

    /// <summary>
    /// Gets or sets the current status.
    /// </summary>
    public IntegrationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the API endpoint URL.
    /// </summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the encrypted API key or secret.
    /// Should never be displayed in full to users.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the merchant ID or account ID.
    /// </summary>
    public string? MerchantId { get; set; }

    /// <summary>
    /// Gets or sets the webhook/callback URL for receiving notifications.
    /// </summary>
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Gets or sets additional configuration as JSON.
    /// </summary>
    public string? AdditionalConfig { get; set; }

    /// <summary>
    /// Gets or sets whether the integration is enabled.
    /// When disabled, calls to this integration are gracefully blocked.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the last health check timestamp.
    /// </summary>
    public DateTime? LastHealthCheckAt { get; set; }

    /// <summary>
    /// Gets or sets the last health check status message.
    /// </summary>
    public string? LastHealthCheckStatus { get; set; }

    /// <summary>
    /// Gets or sets whether the last health check was successful.
    /// </summary>
    public bool? LastHealthCheckSuccess { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created this integration.
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the created by user navigation property.
    /// </summary>
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated this integration.
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the updated by user navigation property.
    /// </summary>
    public User? UpdatedByUser { get; set; }
}
