using MercatoApp.Authorization;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Reports;

/// <summary>
/// Page model for admin commission summaries.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class CommissionSummariesModel : PageModel
{
    private readonly IAdminReportService _reportService;
    private readonly ILogger<CommissionSummariesModel> _logger;

    public CommissionSummariesModel(
        IAdminReportService reportService,
        ILogger<CommissionSummariesModel> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of commission summaries.
    /// </summary>
    public List<CommissionSummaryData> Summaries { get; set; } = new();

    /// <summary>
    /// Gets or sets the from date filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime FromDate { get; set; } = DateTime.UtcNow.AddMonths(-1).Date;

    /// <summary>
    /// Gets or sets the to date filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime ToDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Gets or sets the selected seller ID for drill-down view.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? SelectedStoreId { get; set; }

    /// <summary>
    /// Gets or sets the order details for the selected seller.
    /// </summary>
    public List<CommissionOrderDetail> OrderDetails { get; set; } = new();

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the success message to display.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets the total GMV for all sellers.
    /// </summary>
    public decimal TotalGMV => Summaries.Sum(s => s.TotalGMV);

    /// <summary>
    /// Gets the total commission for all sellers.
    /// </summary>
    public decimal TotalCommission => Summaries.Sum(s => s.TotalCommission);

    /// <summary>
    /// Gets the total net payout for all sellers.
    /// </summary>
    public decimal TotalNetPayout => Summaries.Sum(s => s.TotalNetPayout);

    /// <summary>
    /// Gets the total order count for all sellers.
    /// </summary>
    public int TotalOrderCount => Summaries.Sum(s => s.OrderCount);

    /// <summary>
    /// Handles GET request to display the commission summary.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Validate date range
            if (FromDate > ToDate)
            {
                ErrorMessage = "From Date cannot be after To Date.";
                return Page();
            }

            // Get commission summary data
            Summaries = await _reportService.GetCommissionSummaryAsync(FromDate, ToDate);

            // If a seller is selected, get order details
            if (SelectedStoreId.HasValue)
            {
                OrderDetails = await _reportService.GetCommissionOrderDetailsAsync(
                    SelectedStoreId.Value, 
                    FromDate, 
                    ToDate);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading commission summaries");
            ErrorMessage = "An error occurred while loading the commission summaries.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST request to export the commission summary to CSV.
    /// </summary>
    public async Task<IActionResult> OnPostExportAsync()
    {
        try
        {
            // Validate date range
            if (FromDate > ToDate)
            {
                ErrorMessage = "From Date cannot be after To Date.";
                return RedirectToPage(new { FromDate, ToDate });
            }

            var result = await _reportService.ExportCommissionSummaryToCsvAsync(FromDate, ToDate);

            if (!result.Success)
            {
                ErrorMessage = string.Join(", ", result.Errors);
                return RedirectToPage(new { FromDate, ToDate });
            }

            return File(result.FileData!, result.ContentType!, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting commission summary");
            ErrorMessage = "An error occurred while exporting the commission summary.";
            return RedirectToPage(new { FromDate, ToDate });
        }
    }
}
