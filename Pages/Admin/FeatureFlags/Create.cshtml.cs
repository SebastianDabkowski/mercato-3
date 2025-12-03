using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.FeatureFlags;

/// <summary>
/// Page model for creating a new feature flag.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class CreateModel : PageModel
{
    private readonly IFeatureFlagManagementService _flagService;
    private readonly IAdminAuditLogService _auditLogService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IFeatureFlagManagementService flagService,
        IAdminAuditLogService auditLogService,
        ILogger<CreateModel> logger)
    {
        _flagService = flagService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [BindProperty]
    public FeatureFlag FeatureFlag { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        // Initialize with default values
        FeatureFlag.IsActive = true;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")!.Value);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // Check if key already exists
            var existing = await _flagService.GetFlagByKeyAsync(FeatureFlag.Key);
            if (existing != null)
            {
                ModelState.AddModelError("FeatureFlag.Key", "A feature flag with this key already exists.");
                return Page();
            }

            var createdFlag = await _flagService.CreateFlagAsync(FeatureFlag, adminUserId, ipAddress, userAgent);

            // Log to admin audit log
            await _auditLogService.LogActionAsync(
                adminUserId,
                "CreateFeatureFlag",
                "FeatureFlag",
                createdFlag.Id,
                $"Feature Flag: {createdFlag.Name}",
                null,
                $"Created new feature flag '{createdFlag.Key}'"
            );

            SuccessMessage = $"Feature flag '{createdFlag.Name}' created successfully.";
            return RedirectToPage("./Edit", new { id = createdFlag.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feature flag");
            ErrorMessage = $"Error creating feature flag: {ex.Message}";
            return Page();
        }
    }
}
