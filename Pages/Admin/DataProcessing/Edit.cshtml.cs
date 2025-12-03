using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.DataProcessing;

/// <summary>
/// Page model for editing a GDPR processing activity.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class EditModel : PageModel
{
    private readonly IProcessingActivityService _processingActivityService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IProcessingActivityService processingActivityService,
        ILogger<EditModel> logger)
    {
        _processingActivityService = processingActivityService;
        _logger = logger;
    }

    [BindProperty]
    public ProcessingActivity ProcessingActivity { get; set; } = new();

    [BindProperty]
    public string? ChangeNotes { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var activity = await _processingActivityService.GetByIdAsync(id);
        if (activity == null)
        {
            TempData["ErrorMessage"] = "Processing activity not found.";
            return RedirectToPage("./Index");
        }

        ProcessingActivity = activity;
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
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var success = await _processingActivityService.UpdateAsync(
                ProcessingActivity, userId, ChangeNotes);

            if (success)
            {
                TempData["SuccessMessage"] = $"Processing activity '{ProcessingActivity.Name}' updated successfully.";
                return RedirectToPage("./Index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Processing activity not found.");
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating processing activity {Id}", ProcessingActivity.Id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the processing activity.");
            return Page();
        }
    }
}
