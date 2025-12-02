using MercatoApp.Authorization;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Reports;

/// <summary>
/// Page model for user analytics report.
/// Provides aggregated, anonymized user registration and activity metrics.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class UserAnalyticsModel : PageModel
{
    private readonly IUserAnalyticsService _analyticsService;
    private readonly ILogger<UserAnalyticsModel> _logger;

    public UserAnalyticsModel(
        IUserAnalyticsService analyticsService,
        ILogger<UserAnalyticsModel> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the date range selector (preset or custom).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string DateRange { get; set; } = "last30days";

    /// <summary>
    /// Gets or sets the custom start date (used when DateRange is "custom").
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? CustomStartDate { get; set; }

    /// <summary>
    /// Gets or sets the custom end date (used when DateRange is "custom").
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? CustomEndDate { get; set; }

    /// <summary>
    /// Gets the calculated start date for the report.
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// Gets the calculated end date for the report.
    /// </summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// Gets or sets the aggregated user analytics metrics.
    /// </summary>
    public UserAnalyticsMetrics? Metrics { get; set; }

    /// <summary>
    /// Gets or sets the daily registration data for charting.
    /// </summary>
    public List<DailyRegistrationData> RegistrationData { get; set; } = new();

    /// <summary>
    /// Gets or sets the daily activity data for charting.
    /// </summary>
    public List<DailyActivityData> ActivityData { get; set; } = new();

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET request to display the user analytics report.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Calculate date range based on selection
            CalculateDateRange();

            // Get aggregated metrics
            Metrics = await _analyticsService.GetUserAnalyticsAsync(StartDate, EndDate);

            // Get daily data for charts
            RegistrationData = await _analyticsService.GetDailyRegistrationDataAsync(StartDate, EndDate);
            ActivityData = await _analyticsService.GetDailyActivityDataAsync(StartDate, EndDate);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user analytics report");
            ErrorMessage = "An error occurred while loading the analytics report.";
            return Page();
        }
    }

    /// <summary>
    /// Calculates the start and end dates based on the selected date range.
    /// </summary>
    private void CalculateDateRange()
    {
        var now = DateTime.UtcNow.Date;

        switch (DateRange)
        {
            case "today":
                StartDate = now;
                EndDate = now;
                break;

            case "last7days":
                StartDate = now.AddDays(-6);
                EndDate = now;
                break;

            case "last30days":
                StartDate = now.AddDays(-29);
                EndDate = now;
                break;

            case "custom":
                if (CustomStartDate.HasValue && CustomEndDate.HasValue)
                {
                    StartDate = CustomStartDate.Value.Date;
                    EndDate = CustomEndDate.Value.Date;

                    // Validate that start is not after end
                    if (StartDate > EndDate)
                    {
                        // Swap them
                        var temp = StartDate;
                        StartDate = EndDate;
                        EndDate = temp;
                    }

                    // Ensure dates are not in the future
                    if (EndDate > now)
                    {
                        EndDate = now;
                    }
                    if (StartDate > now)
                    {
                        StartDate = now;
                    }
                }
                else
                {
                    // Default to last 30 days if custom dates are not provided
                    StartDate = now.AddDays(-29);
                    EndDate = now;
                }
                break;

            default:
                // Default to last 30 days
                StartDate = now.AddDays(-29);
                EndDate = now;
                break;
        }
    }
}
