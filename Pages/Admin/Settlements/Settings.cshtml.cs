using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Settlements;

/// <summary>
/// Page model for settlement settings configuration.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class SettingsModel : PageModel
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(
        ISettlementService settlementService,
        ILogger<SettingsModel> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    public SettlementConfig Config { get; set; } = null!;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Config = await _settlementService.GetOrCreateSettlementConfigAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        int generationDayOfMonth,
        bool autoGenerateEnabled,
        int gracePeriodDays,
        bool useCalendarMonth)
    {
        try
        {
            var config = await _settlementService.GetOrCreateSettlementConfigAsync();
            config.GenerationDayOfMonth = generationDayOfMonth;
            config.AutoGenerateEnabled = autoGenerateEnabled;
            config.GracePeriodDays = gracePeriodDays;
            config.UseCalendarMonth = useCalendarMonth;

            await _settlementService.UpdateSettlementConfigAsync(config);

            SuccessMessage = "Settlement settings updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settlement settings");
            ErrorMessage = "An error occurred while updating the settings.";
        }

        return RedirectToPage();
    }
}
