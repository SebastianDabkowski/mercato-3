using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Settlements;

/// <summary>
/// Page model for viewing settlement details.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class DetailsModel : PageModel
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        ISettlementService settlementService,
        ILogger<DetailsModel> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    public Settlement Settlement { get; set; } = null!;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var settlement = await _settlementService.GetSettlementAsync(id);
        if (settlement == null)
        {
            return NotFound();
        }

        Settlement = settlement;
        return Page();
    }

    public async Task<IActionResult> OnPostFinalizeAsync(int settlementId)
    {
        try
        {
            var success = await _settlementService.FinalizeSettlementAsync(settlementId);
            if (success)
            {
                SuccessMessage = "Settlement finalized successfully.";
            }
            else
            {
                ErrorMessage = "Failed to finalize settlement.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing settlement {SettlementId}", settlementId);
            ErrorMessage = "An error occurred while finalizing the settlement.";
        }

        return RedirectToPage(new { id = settlementId });
    }

    public async Task<IActionResult> OnPostRegenerateAsync(int settlementId)
    {
        try
        {
            var result = await _settlementService.RegenerateSettlementAsync(settlementId);
            if (result.Success && result.Settlement != null)
            {
                SuccessMessage = $"Settlement regenerated successfully as version {result.Settlement.Version}.";
                return RedirectToPage(new { id = result.Settlement.Id });
            }
            else
            {
                ErrorMessage = string.Join(", ", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating settlement {SettlementId}", settlementId);
            ErrorMessage = "An error occurred while regenerating the settlement.";
        }

        return RedirectToPage(new { id = settlementId });
    }

    public async Task<IActionResult> OnPostAddAdjustmentAsync(
        int settlementId,
        int adjustmentType,
        decimal amount,
        string description)
    {
        try
        {
            await _settlementService.AddAdjustmentAsync(
                settlementId,
                (SettlementAdjustmentType)adjustmentType,
                amount,
                description);

            SuccessMessage = "Adjustment added successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding adjustment to settlement {SettlementId}", settlementId);
            ErrorMessage = "An error occurred while adding the adjustment.";
        }

        return RedirectToPage(new { id = settlementId });
    }
}
