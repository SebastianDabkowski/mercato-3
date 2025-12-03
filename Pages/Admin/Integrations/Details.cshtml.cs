using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Integrations;

/// <summary>
/// Page model for viewing integration details and performing health checks.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class DetailsModel : PageModel
{
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IIntegrationService integrationService,
        ILogger<DetailsModel> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    public Integration Integration { get; set; } = null!;

    /// <summary>
    /// Gets the masked API key for display.
    /// </summary>
    public string MaskedApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the health check result after running a health check.
    /// </summary>
    public HealthCheckResult? HealthCheckResult { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET request to display integration details.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <returns>Page result or NotFound.</returns>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var integration = await _integrationService.GetIntegrationByIdAsync(id.Value);
        if (integration == null)
        {
            return NotFound();
        }
        
        Integration = integration;

        MaskedApiKey = _integrationService.MaskApiKey(Integration.ApiKey);

        return Page();
    }

    /// <summary>
    /// Handles POST request to perform a health check on the integration.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <returns>Page result with health check results.</returns>
    public async Task<IActionResult> OnPostHealthCheckAsync(int id)
    {
        try
        {
            var integration = await _integrationService.GetIntegrationByIdAsync(id);
            if (integration == null)
            {
                ErrorMessage = "Integration not found.";
                return RedirectToPage("Index");
            }
            
            Integration = integration;

            MaskedApiKey = _integrationService.MaskApiKey(Integration.ApiKey);

            HealthCheckResult = await _integrationService.PerformHealthCheckAsync(id);

            if (HealthCheckResult.Success)
            {
                SuccessMessage = "Health check completed successfully.";
            }
            else
            {
                ErrorMessage = $"Health check failed: {HealthCheckResult.Message}";
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check for integration {Id}", id);
            ErrorMessage = $"Error performing health check: {ex.Message}";
            
            var integration = await _integrationService.GetIntegrationByIdAsync(id);
            if (integration == null)
            {
                return RedirectToPage("Index");
            }
            
            Integration = integration;
            MaskedApiKey = _integrationService.MaskApiKey(Integration.ApiKey);
            return Page();
        }
    }
}
