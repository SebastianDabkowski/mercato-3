using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Admin.Integrations;

/// <summary>
/// Page model for listing and managing integrations.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IIntegrationService integrationService,
        ILogger<IndexModel> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of all integrations.
    /// </summary>
    public List<Integration> Integrations { get; set; } = new();

    /// <summary>
    /// Gets or sets the filter for integration type.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public IntegrationType? TypeFilter { get; set; }

    /// <summary>
    /// Gets or sets the filter for enabled/disabled integrations.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Filter { get; set; } = "all";

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to display the integrations list.
    /// </summary>
    /// <returns>Page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Get all integrations or only enabled ones based on filter
            Integrations = await _integrationService.GetAllIntegrationsAsync(
                typeFilter: TypeFilter,
                enabledOnly: Filter == "enabled");

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading integrations");
            ErrorMessage = "An error occurred while loading integrations.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST request to enable an integration.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <returns>Redirect to index page.</returns>
    public async Task<IActionResult> OnPostEnableAsync(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _integrationService.EnableIntegrationAsync(id, userId);
            if (success)
            {
                SuccessMessage = "Integration enabled successfully.";
            }
            else
            {
                ErrorMessage = "Integration not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling integration {Id}", id);
            ErrorMessage = $"Error enabling integration: {ex.Message}";
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Handles POST request to disable an integration.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <returns>Redirect to index page.</returns>
    public async Task<IActionResult> OnPostDisableAsync(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _integrationService.DisableIntegrationAsync(id, userId);
            if (success)
            {
                SuccessMessage = "Integration disabled successfully.";
            }
            else
            {
                ErrorMessage = "Integration not found.";
            }
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling integration {Id}", id);
            ErrorMessage = $"Error disabling integration: {ex.Message}";
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Handles POST request to delete an integration.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <returns>Redirect to index page.</returns>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var integration = await _integrationService.GetIntegrationByIdAsync(id);
            if (integration == null)
            {
                ErrorMessage = "Integration not found.";
                return RedirectToPage();
            }

            var success = await _integrationService.DeleteIntegrationAsync(id);
            if (success)
            {
                SuccessMessage = $"Integration {integration.Name} deleted successfully.";
            }
            else
            {
                ErrorMessage = "Failed to delete integration.";
            }
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting integration {Id}", id);
            ErrorMessage = $"Error deleting integration: {ex.Message}";
        }

        return RedirectToPage();
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new InvalidOperationException("User ID not found in claims.");
        }
        return userId;
    }
}
