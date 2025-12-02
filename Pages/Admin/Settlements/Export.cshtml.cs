using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Settlements;

/// <summary>
/// Page model for exporting settlements to CSV.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class ExportModel : PageModel
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<ExportModel> _logger;

    public ExportModel(
        ISettlementService settlementService,
        ILogger<ExportModel> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var settlement = await _settlementService.GetSettlementAsync(id);
            if (settlement == null)
            {
                return NotFound();
            }

            var csvBytes = await _settlementService.ExportSettlementToCsvAsync(id);
            var fileName = $"{settlement.SettlementNumber}_{DateTime.UtcNow:yyyyMMdd}.csv";

            return File(csvBytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting settlement {SettlementId}", id);
            TempData["ErrorMessage"] = "An error occurred while exporting the settlement.";
            return RedirectToPage("./Details", new { id });
        }
    }
}
