using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.DataProcessing;

/// <summary>
/// Page model for creating a new GDPR processing activity.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class CreateModel : PageModel
{
    private readonly IProcessingActivityService _processingActivityService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IProcessingActivityService processingActivityService,
        ILogger<CreateModel> logger)
    {
        _processingActivityService = processingActivityService;
        _logger = logger;
    }

    [BindProperty]
    public ProcessingActivity ProcessingActivity { get; set; } = new();

    public IActionResult OnGet()
    {
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
            await _processingActivityService.CreateAsync(ProcessingActivity, userId);

            TempData["SuccessMessage"] = $"Processing activity '{ProcessingActivity.Name}' created successfully.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating processing activity");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the processing activity.");
            return Page();
        }
    }
}
