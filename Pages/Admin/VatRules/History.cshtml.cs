using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.VatRules;

/// <summary>
/// Page model for viewing VAT rule history and audit trail.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class HistoryModel : PageModel
{
    private readonly IVatRuleService _vatRuleService;
    private readonly ILogger<HistoryModel> _logger;

    public HistoryModel(
        IVatRuleService vatRuleService,
        ILogger<HistoryModel> logger)
    {
        _vatRuleService = vatRuleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of VAT rules in the audit history.
    /// </summary>
    public List<VatRule> Rules { get; set; } = new();

    /// <summary>
    /// Gets or sets the filter for specific rule ID.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? RuleId { get; set; }

    /// <summary>
    /// Gets or sets the start date filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Rules = await _vatRuleService.GetAuditHistoryAsync(
                ruleId: RuleId,
                fromDate: FromDate,
                toDate: ToDate);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading VAT rule history");
            ErrorMessage = "An error occurred while loading the history.";
            return Page();
        }
    }
}
