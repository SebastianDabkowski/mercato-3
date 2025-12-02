using MercatoApp.Authorization;
using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MercatoApp.Pages.Seller;

/// <summary>
/// Page model for the seller sales dashboard.
/// </summary>
[Authorize(Policy = PolicyNames.SellerOnly)]
public class DashboardModel : PageModel
{
    private readonly ISellerDashboardService _dashboardService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(
        ISellerDashboardService dashboardService,
        ApplicationDbContext context,
        ILogger<DashboardModel> logger)
    {
        _dashboardService = dashboardService;
        _context = context;
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
    /// Gets or sets the time granularity (day, week, month).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Granularity { get; set; } = "day";

    /// <summary>
    /// Gets or sets the selected product ID filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the selected category ID filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the dashboard metrics.
    /// </summary>
    public SellerDashboardMetrics? Metrics { get; set; }

    /// <summary>
    /// Gets or sets the current store.
    /// </summary>
    public Store? CurrentStore { get; set; }

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
    /// Gets or sets the list of products for the filter dropdown.
    /// </summary>
    public List<SelectListItem> ProductOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of categories for the filter dropdown.
    /// </summary>
    public List<SelectListItem> CategoryOptions { get; set; } = new();

    /// <summary>
    /// Handles GET request to display the dashboard.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Get current user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Get the seller's store
            CurrentStore = await _context.Stores
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (CurrentStore == null)
            {
                TempData["ErrorMessage"] = "Store not found. Please complete seller onboarding.";
                return RedirectToPage("/Index");
            }

            // Calculate date range based on preset or custom dates
            CalculateDateRange();

            // Parse granularity
            var timeGranularity = ParseGranularity(Granularity);

            // Get metrics for the selected period
            Metrics = await _dashboardService.GetMetricsAsync(
                CurrentStore.Id,
                StartDate,
                EndDate,
                timeGranularity,
                ProductId,
                CategoryId);

            // Load filter options
            await LoadFilterOptionsAsync(CurrentStore.Id);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading seller dashboard");
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

            case "last90days":
                StartDate = now.Date.AddDays(-89);
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

    /// <summary>
    /// Parses the granularity string to TimeGranularity enum.
    /// </summary>
    private TimeGranularity ParseGranularity(string granularity)
    {
        return granularity.ToLowerInvariant() switch
        {
            "day" => TimeGranularity.Day,
            "week" => TimeGranularity.Week,
            "month" => TimeGranularity.Month,
            _ => TimeGranularity.Day
        };
    }

    /// <summary>
    /// Loads filter options for products and categories.
    /// </summary>
    private async Task LoadFilterOptionsAsync(int storeId)
    {
        // Load products for this store
        var products = await _context.Products
            .Where(p => p.StoreId == storeId)
            .OrderBy(p => p.Title)
            .Select(p => new { p.Id, p.Title })
            .ToListAsync();

        ProductOptions = products
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Title
            })
            .ToList();

        ProductOptions.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "All Products"
        });

        // Load categories that have products from this store
        var categories = await _context.Categories
            .Where(c => _context.Products.Any(p => p.StoreId == storeId && p.CategoryId == c.Id))
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        CategoryOptions = categories
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToList();

        CategoryOptions.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "All Categories"
        });
    }
}
