using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.FeatureFlags;

/// <summary>
/// Page model for editing a feature flag.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly IFeatureFlagManagementService _flagService;
    private readonly IAdminAuditLogService _auditLogService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IFeatureFlagManagementService flagService,
        IAdminAuditLogService auditLogService,
        ILogger<EditModel> logger)
    {
        _flagService = flagService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [BindProperty]
    public FeatureFlag FeatureFlag { get; set; } = new();

    [BindProperty]
    public List<RuleInput> Rules { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var flag = await _flagService.GetFlagByIdAsync(id);
        if (flag == null)
        {
            return NotFound();
        }

        FeatureFlag = flag;
        
        // Convert rules to input model
        Rules = flag.Rules.Select(r => new RuleInput
        {
            Priority = r.Priority,
            RuleType = r.RuleType.ToString(),
            RuleValue = r.RuleValue ?? string.Empty,
            IsEnabled = r.IsEnabled,
            Description = r.Description ?? string.Empty
        }).ToList();

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

            // Convert input rules to domain model
            FeatureFlag.Rules = Rules.Select((r, index) => new FeatureFlagRule
            {
                Priority = index + 1, // Auto-assign priority based on order
                RuleType = Enum.Parse<FeatureFlagRuleType>(r.RuleType),
                RuleValue = string.IsNullOrWhiteSpace(r.RuleValue) ? null : r.RuleValue.Trim(),
                IsEnabled = r.IsEnabled,
                Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim()
            }).ToList();

            var updatedFlag = await _flagService.UpdateFlagAsync(FeatureFlag, adminUserId, ipAddress, userAgent);

            // Log to admin audit log
            await _auditLogService.LogActionAsync(
                adminUserId,
                "UpdateFeatureFlag",
                "FeatureFlag",
                updatedFlag.Id,
                $"Feature Flag: {updatedFlag.Name}",
                null,
                $"Updated feature flag '{updatedFlag.Key}'"
            );

            SuccessMessage = $"Feature flag '{updatedFlag.Name}' updated successfully.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feature flag {Id}", FeatureFlag.Id);
            ErrorMessage = $"Error updating feature flag: {ex.Message}";
            return Page();
        }
    }

    public class RuleInput
    {
        public int Priority { get; set; }
        public string RuleType { get; set; } = string.Empty;
        public string RuleValue { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
