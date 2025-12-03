using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MercatoApp.Pages.Admin.SystemAuditLogs;

/// <summary>
/// Page model for displaying system-wide audit logs with filtering capabilities.
/// Shows comprehensive audit logs for all critical actions including login, role changes, payouts, refunds, etc.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IAuditLogService auditLogService,
        ILogger<IndexModel> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the paginated list of audit logs.
    /// </summary>
    public PaginatedList<AuditLog> AuditLogs { get; set; } = new();

    /// <summary>
    /// Gets or sets the filter criteria.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public AuditLogFilter Filter { get; set; } = new();

    /// <summary>
    /// Gets or sets the available entity types for filtering.
    /// </summary>
    public List<SelectListItem> EntityTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the available action types for filtering.
    /// </summary>
    public List<SelectListItem> ActionTypes { get; set; } = [];

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

            // Get available entity types for filter dropdown
            var entityTypes = await _auditLogService.GetEntityTypesAsync();
            EntityTypes = new List<SelectListItem> { new SelectListItem("All Entity Types", "") };
            EntityTypes.AddRange(entityTypes.Select(e => new SelectListItem(e, e)));

            // Get available action types for filter dropdown
            ActionTypes = new List<SelectListItem> { new SelectListItem("All Actions", "") };
            ActionTypes.AddRange(Enum.GetValues<AuditActionType>()
                .Select(a => new SelectListItem(a.ToString(), ((int)a).ToString())));

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading system audit logs");
            TempData["ErrorMessage"] = "An error occurred while loading the audit logs.";
            return RedirectToPage("/Admin/Dashboard");
        }
    }
}
