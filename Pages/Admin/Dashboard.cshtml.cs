using MercatoApp.Authorization;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin;

/// <summary>
/// Page model for the admin marketplace performance dashboard.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class DashboardModel : PageModel
{
    private readonly IAdminDashboardService _dashboardService;
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(
        IAdminDashboardService dashboardService,
        ILogger<DashboardModel> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the date range preset.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string DateRange { get; set; } = "last7days";

    /// <summary>
    /// Gets or sets the custom start date.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? CustomStartDate { get; set; }

    /// <summary>
    /// Gets or sets the custom end date.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? CustomEndDate { get; set; }

    /// <summary>
    /// Gets or sets the dashboard metrics.
    /// </summary>
    public DashboardMetrics? Metrics { get; set; }

    /// <summary>
    /// Gets or sets the start date for the current view.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the current view.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET request to display the dashboard.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Calculate date range based on preset or custom dates
            CalculateDateRange();

            // Get metrics for the selected period
            Metrics = await _dashboardService.GetMetricsAsync(StartDate, EndDate);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            ErrorMessage = "An error occurred while loading the dashboard.";
            return Page();
        }
    }

    /// <summary>
    /// Calculates the start and end dates based on the selected date range preset or custom dates.
    /// </summary>
    private void CalculateDateRange()
    {
        var now = DateTime.UtcNow;

        switch (DateRange.ToLowerInvariant())
        {
            case "today":
                StartDate = now.Date;
                EndDate = now.Date;
                break;

            case "last7days":
                StartDate = now.Date.AddDays(-6);
                EndDate = now.Date;
                break;

            case "last30days":
                StartDate = now.Date.AddDays(-29);
                EndDate = now.Date;
                break;

            case "custom":
                if (CustomStartDate.HasValue && CustomEndDate.HasValue)
                {
                    StartDate = CustomStartDate.Value.Date;
                    EndDate = CustomEndDate.Value.Date;

                    // Ensure start date is not after end date
                    if (StartDate > EndDate)
                    {
                        (StartDate, EndDate) = (EndDate, StartDate);
                    }
                }
                else
                {
                    // Default to last 7 days if custom dates are invalid
                    StartDate = now.Date.AddDays(-6);
                    EndDate = now.Date;
                    DateRange = "last7days";
                }
                break;

            default:
                // Default to last 7 days for unknown presets
                StartDate = now.Date.AddDays(-6);
                EndDate = now.Date;
                DateRange = "last7days";
                break;
        }
    }
}
