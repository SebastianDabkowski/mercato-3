using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.CommissionRules;

/// <summary>
/// Page model for listing and managing commission rules.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly ICommissionRuleService _ruleService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ICommissionRuleService ruleService,
        ILogger<IndexModel> logger)
    {
        _ruleService = ruleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of all commission rules.
    /// </summary>
    public List<CommissionRule> Rules { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of future-dated rules.
    /// </summary>
    public List<CommissionRule> FutureRules { get; set; } = new();

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
            Rules = await _ruleService.GetAllRulesAsync(activeOnly: Filter == "active");
            
            // Get future-dated rules for informational display
            FutureRules = await _ruleService.GetFutureRulesAsync();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading commission rules");
            ErrorMessage = "An error occurred while loading commission rules.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var rule = await _ruleService.GetRuleByIdAsync(id);
            if (rule == null)
            {
                ErrorMessage = "Commission rule not found.";
                return RedirectToPage();
            }

            var success = await _ruleService.DeleteRuleAsync(id);
            if (success)
            {
                SuccessMessage = $"Commission rule '{rule.Name}' deleted successfully.";
            }
            else
            {
                ErrorMessage = "Failed to delete commission rule.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting commission rule {RuleId}", id);
            ErrorMessage = "An error occurred while deleting the commission rule.";
        }

        return RedirectToPage();
    }
}
