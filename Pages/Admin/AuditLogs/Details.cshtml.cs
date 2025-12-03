using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.AuditLogs;

/// <summary>
/// Page model for displaying audit log entry details.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class DetailsModel : PageModel
{
    private readonly IAdminAuditLogService _auditLogService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IAdminAuditLogService auditLogService,
        ILogger<DetailsModel> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the audit log entry.
    /// </summary>
    public AdminAuditLog? AuditLog { get; set; }

    /// <summary>
    /// Handles GET request to display audit log details.
    /// </summary>
    /// <param name="id">The audit log entry ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            AuditLog = await _auditLogService.GetAuditLogByIdAsync(id);

            if (AuditLog == null)
            {
                return NotFound();
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audit log entry {Id}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the audit log entry.";
            return RedirectToPage("/Admin/AuditLogs/Index");
        }
    }
}
