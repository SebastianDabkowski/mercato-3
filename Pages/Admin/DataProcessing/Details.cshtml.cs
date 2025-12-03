using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.DataProcessing;

/// <summary>
/// Page model for viewing GDPR processing activity details.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class DetailsModel : PageModel
{
    private readonly IProcessingActivityService _processingActivityService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IProcessingActivityService processingActivityService,
        ILogger<DetailsModel> logger)
    {
        _processingActivityService = processingActivityService;
        _logger = logger;
    }

    public ProcessingActivity? ProcessingActivity { get; set; }
    public List<ProcessingActivityHistory> History { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            ProcessingActivity = await _processingActivityService.GetByIdAsync(id);
            if (ProcessingActivity == null)
            {
                TempData["ErrorMessage"] = "Processing activity not found.";
                return RedirectToPage("./Index");
            }

            History = await _processingActivityService.GetHistoryAsync(id);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading processing activity {Id}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the processing activity.";
            return RedirectToPage("./Index");
        }
    }
}
