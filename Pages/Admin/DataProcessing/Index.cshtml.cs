using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.DataProcessing;

/// <summary>
/// Page model for listing GDPR processing activities.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly IProcessingActivityService _processingActivityService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IProcessingActivityService processingActivityService,
        ILogger<IndexModel> logger)
    {
        _processingActivityService = processingActivityService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of processing activities.
    /// </summary>
    public List<ProcessingActivity> ProcessingActivities { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to show only active processing activities.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool ActiveOnly { get; set; } = true;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            ProcessingActivities = await _processingActivityService.GetAllAsync(ActiveOnly);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading processing activities");
            ErrorMessage = "An error occurred while loading processing activities.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var activity = await _processingActivityService.GetByIdAsync(id);
            
            if (activity == null)
            {
                ErrorMessage = "Processing activity not found.";
                return RedirectToPage();
            }

            var success = await _processingActivityService.DeleteAsync(id, userId);
            if (success)
            {
                SuccessMessage = $"Processing activity '{activity.Name}' deactivated successfully.";
            }
            else
            {
                ErrorMessage = "Failed to deactivate processing activity.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting processing activity {Id}", id);
            ErrorMessage = "An error occurred while deactivating the processing activity.";
        }

        return RedirectToPage();
    }
}
