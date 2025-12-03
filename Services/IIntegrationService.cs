using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing external integrations.
/// </summary>
public interface IIntegrationService
{
    /// <summary>
    /// Gets all integrations.
    /// </summary>
    /// <param name="typeFilter">Optional filter by integration type.</param>
    /// <param name="enabledOnly">If true, returns only enabled integrations.</param>
    /// <returns>List of integrations.</returns>
    Task<List<Integration>> GetAllIntegrationsAsync(IntegrationType? typeFilter = null, bool enabledOnly = false);

    /// <summary>
    /// Gets an integration by ID.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <returns>The integration or null if not found.</returns>
    Task<Integration?> GetIntegrationByIdAsync(int id);

    /// <summary>
    /// Creates a new integration.
    /// </summary>
    /// <param name="integration">Integration to create.</param>
    /// <param name="userId">ID of the user creating the integration.</param>
    /// <returns>The created integration.</returns>
    Task<Integration> CreateIntegrationAsync(Integration integration, int userId);

    /// <summary>
    /// Updates an existing integration.
    /// </summary>
    /// <param name="integration">Integration with updated values.</param>
    /// <param name="userId">ID of the user updating the integration.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> UpdateIntegrationAsync(Integration integration, int userId);

    /// <summary>
    /// Deletes an integration.
    /// </summary>
    /// <param name="id">Integration ID to delete.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> DeleteIntegrationAsync(int id);

    /// <summary>
    /// Enables an integration.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <param name="userId">ID of the user enabling the integration.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> EnableIntegrationAsync(int id, int userId);

    /// <summary>
    /// Disables an integration.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <param name="userId">ID of the user disabling the integration.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> DisableIntegrationAsync(int id, int userId);

    /// <summary>
    /// Performs a health check on an integration.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <returns>Health check result with status and message.</returns>
    Task<HealthCheckResult> PerformHealthCheckAsync(int id);

    /// <summary>
    /// Masks an API key for display purposes.
    /// Shows only the last 4 characters.
    /// </summary>
    /// <param name="apiKey">The API key to mask.</param>
    /// <returns>Masked API key.</returns>
    string MaskApiKey(string? apiKey);
}

/// <summary>
/// Result of an integration health check.
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Gets or sets whether the health check was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the check.
    /// </summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// Gets or sets additional details about the check.
    /// </summary>
    public string? Details { get; set; }
}
