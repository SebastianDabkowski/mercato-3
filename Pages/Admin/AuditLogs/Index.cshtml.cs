using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MercatoApp.Pages.Admin.AuditLogs;

/// <summary>
/// Page model for displaying admin audit logs with filtering capabilities.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly IAdminAuditLogService _auditLogService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IAdminAuditLogService auditLogService,
        ILogger<IndexModel> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the paginated list of audit logs.
    /// </summary>
    public PaginatedList<AdminAuditLog> AuditLogs { get; set; } = new();

    /// <summary>
    /// Gets or sets the filter criteria.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public AdminAuditLogFilter Filter { get; set; } = new();

    /// <summary>
    /// Gets or sets the available entity types for filtering.
    /// </summary>
    public List<SelectListItem> EntityTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the available actions for filtering.
    /// </summary>
    public List<SelectListItem> Actions { get; set; } = [];

    /// <summary>
    /// Handles GET request to display audit logs.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Get audit logs based on filter
            AuditLogs = await _auditLogService.GetAuditLogsAsync(Filter);

            // Get available entity types and actions for filter dropdowns
            var entityTypes = await _auditLogService.GetEntityTypesAsync();
            EntityTypes = new List<SelectListItem> { new SelectListItem("All Entity Types", "") };
            EntityTypes.AddRange(entityTypes.Select(e => new SelectListItem(e, e)));

            var actions = await _auditLogService.GetActionsAsync();
            Actions = new List<SelectListItem> { new SelectListItem("All Actions", "") };
            Actions.AddRange(actions.Select(a => new SelectListItem(a, a)));

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audit logs");
            TempData["ErrorMessage"] = "An error occurred while loading the audit logs.";
            return RedirectToPage("/Admin/Dashboard");
        }
    }
}
