using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.FeatureFlags;

/// <summary>
/// Page model for viewing feature flag change history.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class HistoryModel : PageModel
{
    private readonly IFeatureFlagManagementService _flagService;
    private readonly ILogger<HistoryModel> _logger;

    public HistoryModel(
        IFeatureFlagManagementService flagService,
        ILogger<HistoryModel> logger)
    {
        _flagService = flagService;
        _logger = logger;
    }

    public FeatureFlag FeatureFlag { get; set; } = null!;
    public List<FeatureFlagHistory> History { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var flag = await _flagService.GetFlagByIdAsync(id);
        if (flag == null)
        {
            return NotFound();
        }

        FeatureFlag = flag;
        History = await _flagService.GetFlagHistoryAsync(id);

        return Page();
    }
}
