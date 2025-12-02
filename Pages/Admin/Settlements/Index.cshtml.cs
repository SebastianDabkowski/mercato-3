using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Settlements;

/// <summary>
/// Page model for viewing all settlements.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ISettlementService settlementService,
        ILogger<IndexModel> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    public List<Settlement> Settlements { get; set; } = new();
    public int FilterYear { get; set; }
    public int? FilterMonth { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? year, int? month)
    {
        FilterYear = year ?? DateTime.UtcNow.Year;
        FilterMonth = month;

        try
        {
            // Get all settlements (across all stores)
            var allStores = await _settlementService.GetSettlementsAsync(0, includeSuperseded: false);

            // Filter by year and month
            Settlements = allStores
                .Where(s => s.PeriodStartDate.Year == FilterYear)
                .Where(s => !FilterMonth.HasValue || s.PeriodStartDate.Month == FilterMonth.Value)
                .OrderByDescending(s => s.PeriodStartDate)
                .ThenBy(s => s.Store.StoreName)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settlements");
            ErrorMessage = "An error occurred while loading settlements.";
        }

        return Page();
    }
}
