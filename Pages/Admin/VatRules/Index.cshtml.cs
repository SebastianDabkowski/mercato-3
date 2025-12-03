using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.VatRules;

/// <summary>
/// Page model for listing and managing VAT rules.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IVatRuleService _vatRuleService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IVatRuleService vatRuleService,
        ILogger<IndexModel> logger)
    {
        _vatRuleService = vatRuleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of all VAT rules.
    /// </summary>
    public List<VatRule> Rules { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of future-dated rules.
    /// </summary>
    public List<VatRule> FutureRules { get; set; } = new();

    /// <summary>
    /// Gets or sets the filter for active/inactive rules.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Filter { get; set; } = "all";

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Get all rules or only active ones based on filter
            Rules = await _vatRuleService.GetAllRulesAsync(activeOnly: Filter == "active");
            
            // Get future-dated rules for informational display
            FutureRules = await _vatRuleService.GetFutureRulesAsync();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading VAT rules");
            ErrorMessage = "An error occurred while loading VAT rules.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var rule = await _vatRuleService.GetRuleByIdAsync(id);
            if (rule == null)
            {
                ErrorMessage = "VAT rule not found.";
                return RedirectToPage();
            }

            var success = await _vatRuleService.DeleteRuleAsync(id);
            if (success)
            {
                SuccessMessage = $"VAT rule '{rule.Name}' deleted successfully.";
            }
            else
            {
                ErrorMessage = "Failed to delete VAT rule.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting VAT rule {RuleId}", id);
            ErrorMessage = "An error occurred while deleting the VAT rule.";
        }

        return RedirectToPage();
    }
}
