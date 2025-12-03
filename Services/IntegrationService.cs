using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing external integrations.
/// </summary>
public class IntegrationService : IIntegrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IntegrationService> _logger;
    private readonly IDataEncryptionService _encryptionService;

    public IntegrationService(
        ApplicationDbContext context,
        ILogger<IntegrationService> logger,
        IDataEncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    /// <inheritdoc />
    public async Task<List<Integration>> GetAllIntegrationsAsync(IntegrationType? typeFilter = null, bool enabledOnly = false)
    {
        var query = _context.Integrations
            .Include(i => i.CreatedByUser)
            .Include(i => i.UpdatedByUser)
            .AsQueryable();

        if (typeFilter.HasValue)
        {
            query = query.Where(i => i.Type == typeFilter.Value);
        }

        if (enabledOnly)
        {
            query = query.Where(i => i.IsEnabled);
        }

        return await query
            .OrderBy(i => i.Type)
            .ThenBy(i => i.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Integration?> GetIntegrationByIdAsync(int id)
    {
        return await _context.Integrations
            .Include(i => i.CreatedByUser)
            .Include(i => i.UpdatedByUser)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    /// <inheritdoc />
    public async Task<Integration> CreateIntegrationAsync(Integration integration, int userId)
    {
        // Encrypt API key before storing
        if (!string.IsNullOrWhiteSpace(integration.ApiKey))
        {
            integration.ApiKey = _encryptionService.Encrypt(integration.ApiKey);
        }

        integration.CreatedAt = DateTime.UtcNow;
        integration.CreatedByUserId = userId;
        integration.UpdatedAt = DateTime.UtcNow;
        integration.UpdatedByUserId = userId;

        _context.Integrations.Add(integration);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Integration created: {IntegrationName} (Type: {Type}, Environment: {Environment}) by user {UserId}",
            integration.Name, integration.Type, integration.Environment, userId);

        return integration;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateIntegrationAsync(Integration integration, int userId)
    {
        var existing = await _context.Integrations.FindAsync(integration.Id);
        if (existing == null)
        {
            return false;
        }

        existing.Name = integration.Name;
        existing.Type = integration.Type;
        existing.Provider = integration.Provider;
        existing.Environment = integration.Environment;
        existing.Status = integration.Status;
        existing.ApiEndpoint = integration.ApiEndpoint;
        
        // Only update API key if a new one is provided, and encrypt it
        if (!string.IsNullOrWhiteSpace(integration.ApiKey))
        {
            existing.ApiKey = _encryptionService.Encrypt(integration.ApiKey);
        }

        existing.MerchantId = integration.MerchantId;
        existing.CallbackUrl = integration.CallbackUrl;
        existing.AdditionalConfig = integration.AdditionalConfig;
        existing.IsEnabled = integration.IsEnabled;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Integration updated: {IntegrationName} (ID: {Id}) by user {UserId}",
            integration.Name, integration.Id, userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteIntegrationAsync(int id)
    {
        var integration = await _context.Integrations.FindAsync(id);
        if (integration == null)
        {
            return false;
        }

        _context.Integrations.Remove(integration);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Integration deleted: {IntegrationName} (ID: {Id})",
            integration.Name, integration.Id);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> EnableIntegrationAsync(int id, int userId)
    {
        var integration = await _context.Integrations.FindAsync(id);
        if (integration == null)
        {
            return false;
        }

        integration.IsEnabled = true;
        integration.Status = IntegrationStatus.Active;
        integration.UpdatedAt = DateTime.UtcNow;
        integration.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Integration enabled: {IntegrationName} (ID: {Id}) by user {UserId}",
            integration.Name, integration.Id, userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DisableIntegrationAsync(int id, int userId)
    {
        var integration = await _context.Integrations.FindAsync(id);
        if (integration == null)
        {
            return false;
        }

        integration.IsEnabled = false;
        integration.Status = IntegrationStatus.Inactive;
        integration.UpdatedAt = DateTime.UtcNow;
        integration.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Integration disabled: {IntegrationName} (ID: {Id}) by user {UserId}",
            integration.Name, integration.Id, userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> PerformHealthCheckAsync(int id)
    {
        var integration = await _context.Integrations.FindAsync(id);
        if (integration == null)
        {
            return new HealthCheckResult
            {
                Success = false,
                Message = "Integration not found",
                CheckedAt = DateTime.UtcNow
            };
        }

        // Perform basic validation checks
        var result = new HealthCheckResult
        {
            CheckedAt = DateTime.UtcNow
        };

        var validationErrors = new List<string>();

        // Check required fields
        if (string.IsNullOrWhiteSpace(integration.ApiEndpoint))
        {
            validationErrors.Add("API endpoint is not configured");
        }

        if (string.IsNullOrWhiteSpace(integration.ApiKey))
        {
            validationErrors.Add("API key is not configured");
        }

        // Validate URL format if endpoint is provided
        if (!string.IsNullOrWhiteSpace(integration.ApiEndpoint))
        {
            if (!Uri.TryCreate(integration.ApiEndpoint, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                validationErrors.Add("API endpoint is not a valid URL");
            }
        }

        // Validate callback URL if provided
        if (!string.IsNullOrWhiteSpace(integration.CallbackUrl))
        {
            if (!Uri.TryCreate(integration.CallbackUrl, UriKind.Absolute, out var callbackUri) ||
                (callbackUri.Scheme != Uri.UriSchemeHttp && callbackUri.Scheme != Uri.UriSchemeHttps))
            {
                validationErrors.Add("Callback URL is not a valid URL");
            }
        }

        if (validationErrors.Any())
        {
            result.Success = false;
            result.Message = "Configuration validation failed";
            result.Details = string.Join("; ", validationErrors);
        }
        else
        {
            result.Success = true;
            result.Message = "Configuration validation passed";
            result.Details = $"Integration {integration.Name} is properly configured. Note: This is a configuration check only. Actual connectivity tests would require provider-specific implementation.";
        }

        // Update integration with health check results
        integration.LastHealthCheckAt = result.CheckedAt;
        integration.LastHealthCheckSuccess = result.Success;
        integration.LastHealthCheckStatus = result.Success ? result.Message : $"{result.Message}: {result.Details}";

        // Update status based on health check
        if (!result.Success && integration.IsEnabled)
        {
            integration.Status = IntegrationStatus.Error;
        }
        else if (result.Success && integration.IsEnabled)
        {
            integration.Status = IntegrationStatus.Active;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Health check performed for integration {IntegrationName} (ID: {Id}): {Success}",
            integration.Name, integration.Id, result.Success);

        return result;
    }

    /// <inheritdoc />
    public string MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return string.Empty;
        }

        if (apiKey.Length <= 4)
        {
            return new string('*', apiKey.Length);
        }

        var lastFour = apiKey.Substring(apiKey.Length - 4);
        var masked = new string('*', apiKey.Length - 4);
        return masked + lastFour;
    }
}
