using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Returns;

/// <summary>
/// Page model for admin SLA dashboard displaying metrics and statistics.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class SLADashboardModel : PageModel
{
    private readonly ISLAService _slaService;
    private readonly ILogger<SLADashboardModel> _logger;

    public SLADashboardModel(
        ISLAService slaService,
        ILogger<SLADashboardModel> logger)
    {
        _slaService = slaService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the platform-wide SLA statistics.
    /// </summary>
    public SLAStatistics PlatformStatistics { get; set; } = new SLAStatistics();

    /// <summary>
    /// Gets or sets the per-seller SLA statistics.
    /// </summary>
    public List<SellerSLAStatistics> SellerStatistics { get; set; } = new();

    /// <summary>
    /// Gets or sets the start date for the reporting period.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the reporting period.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Handles GET request to display SLA dashboard.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        // Default to last 30 days if no dates provided
        var toDate = ToDate ?? DateTime.UtcNow;
        var fromDate = FromDate ?? toDate.AddDays(-30);

        // Ensure fromDate is not after toDate
        if (fromDate > toDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        FromDate = fromDate;
        ToDate = toDate;

        // Get platform-wide statistics
        PlatformStatistics = await _slaService.GetPlatformSLAStatisticsAsync(fromDate, toDate);

        // Get per-seller statistics
        SellerStatistics = await _slaService.GetAllSellerSLAStatisticsAsync(fromDate, toDate);

        return Page();
    }
}
