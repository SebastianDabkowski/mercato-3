using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.FeatureFlags;

/// <summary>
/// Page model for listing and managing feature flags.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IFeatureFlagManagementService _flagService;
    private readonly IAdminAuditLogService _auditLogService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IFeatureFlagManagementService flagService,
        IAdminAuditLogService auditLogService,
        ILogger<IndexModel> logger)
    {
        _flagService = flagService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of feature flags.
    /// </summary>
    public List<FeatureFlag> FeatureFlags { get; set; } = new();

    /// <summary>
    /// Gets or sets the filter for active/inactive flags.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Filter { get; set; } = "all";

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Get all flags
            FeatureFlags = await _flagService.GetAllFlagsAsync(activeOnly: false);

            // Apply filter
            if (Filter == "active")
            {
                FeatureFlags = FeatureFlags.Where(f => f.IsActive).ToList();
            }
            else if (Filter == "inactive")
            {
                FeatureFlags = FeatureFlags.Where(f => !f.IsActive).ToList();
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading feature flags");
            ErrorMessage = "An error occurred while loading feature flags.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, bool isEnabled)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var flag = await _flagService.ToggleFlagAsync(id, isEnabled, adminUserId, ipAddress, userAgent);
            if (flag != null)
            {
                SuccessMessage = $"Feature flag '{flag.Name}' has been {(isEnabled ? "enabled" : "disabled")}.";

                // Log to admin audit log
                await _auditLogService.LogActionAsync(
                    adminUserId,
                    isEnabled ? "EnableFeatureFlag" : "DisableFeatureFlag",
                    "FeatureFlag",
                    flag.Id,
                    $"Feature Flag: {flag.Name}",
                    null,
                    $"Toggled feature flag '{flag.Key}' to {(isEnabled ? "enabled" : "disabled")}"
                );
            }
            else
            {
                ErrorMessage = "Feature flag not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling feature flag {Id}", id);
            ErrorMessage = $"Error toggling feature flag: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var flag = await _flagService.GetFlagByIdAsync(id);
            if (flag == null)
            {
                ErrorMessage = "Feature flag not found.";
                return RedirectToPage();
            }

            var flagName = flag.Name;
            var flagKey = flag.Key;

            var success = await _flagService.DeleteFlagAsync(id, adminUserId, ipAddress, userAgent);
            if (success)
            {
                SuccessMessage = $"Feature flag '{flagName}' deleted successfully.";

                // Log to admin audit log
                await _auditLogService.LogActionAsync(
                    adminUserId,
                    "DeleteFeatureFlag",
                    "FeatureFlag",
                    id,
                    $"Feature Flag: {flagName}",
                    null,
                    $"Deleted feature flag '{flagKey}'"
                );
            }
            else
            {
                ErrorMessage = "Failed to delete feature flag.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feature flag {Id}", id);
            ErrorMessage = $"Error deleting feature flag: {ex.Message}";
        }

        return RedirectToPage();
    }
}
