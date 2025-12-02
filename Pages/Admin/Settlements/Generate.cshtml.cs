using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Admin.Settlements;

/// <summary>
/// Page model for generating new settlements.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class GenerateModel : PageModel
{
    private readonly ISettlementService _settlementService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GenerateModel> _logger;

    public GenerateModel(
        ISettlementService settlementService,
        ApplicationDbContext context,
        ILogger<GenerateModel> logger)
    {
        _settlementService = settlementService;
        _context = context;
        _logger = logger;
    }

    public List<Store> Stores { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Stores = await _context.Stores
            .Where(s => s.Status == StoreStatus.Active || s.Status == StoreStatus.LimitedActive)
            .OrderBy(s => s.StoreName)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string periodType,
        int? year,
        int? month,
        int? storeId,
        DateTime? startDate,
        DateTime? endDate)
    {
        try
        {
            if (periodType == "month")
            {
                if (!year.HasValue || !month.HasValue)
                {
                    ErrorMessage = "Year and month are required for monthly generation.";
                    return RedirectToPage();
                }

                var count = await _settlementService.GenerateMonthlySettlementsAsync(year.Value, month.Value);
                TempData["SuccessMessage"] = $"Generated {count} settlement(s) for {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month.Value)} {year}.";
                return RedirectToPage("./Index", new { year = year.Value, month = month.Value });
            }
            else if (periodType == "custom")
            {
                if (!storeId.HasValue || !startDate.HasValue || !endDate.HasValue)
                {
                    ErrorMessage = "Store, start date, and end date are required for custom generation.";
                    return RedirectToPage();
                }

                var result = await _settlementService.GenerateSettlementAsync(
                    storeId.Value,
                    startDate.Value.ToUniversalTime(),
                    endDate.Value.ToUniversalTime());

                if (result.Success && result.Settlement != null)
                {
                    TempData["SuccessMessage"] = $"Settlement {result.Settlement.SettlementNumber} generated successfully.";
                    return RedirectToPage("./Details", new { id = result.Settlement.Id });
                }
                else
                {
                    ErrorMessage = string.Join(", ", result.Errors);
                    return RedirectToPage();
                }
            }
            else
            {
                ErrorMessage = "Invalid period type.";
                return RedirectToPage();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating settlement");
            ErrorMessage = "An error occurred while generating the settlement.";
            return RedirectToPage();
        }
    }
}
