using MercatoApp.Authorization;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.DataProcessing;

/// <summary>
/// Page model for exporting GDPR processing activities to Excel.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class ExportModel : PageModel
{
    private readonly IProcessingActivityService _processingActivityService;
    private readonly ILogger<ExportModel> _logger;

    public ExportModel(
        IProcessingActivityService processingActivityService,
        ILogger<ExportModel> logger)
    {
        _processingActivityService = processingActivityService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var excelData = await _processingActivityService.ExportToExcelAsync();
            var fileName = $"ProcessingRegistry_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            return File(excelData, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting processing activities");
            TempData["ErrorMessage"] = "An error occurred while exporting the processing registry.";
            return RedirectToPage("./Index");
        }
    }
}
