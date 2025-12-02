using MercatoApp.Authorization;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Admin.Orders;

/// <summary>
/// Page model for the admin orders index page.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ApplicationDbContext context,
        ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of orders.
    /// </summary>
    public List<Order> Orders { get; set; } = new();

    /// <summary>
    /// Gets or sets the filter status.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    /// <summary>
    /// Gets or sets the filter order number.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? FilterOrderNumber { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; } = 20;

    /// <summary>
    /// Gets or sets the total count of orders.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

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
    /// Handles GET request to display orders.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Start with base query
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.DeliveryAddress)
                .Include(o => o.SubOrders)
                    .ThenInclude(so => so.Store)
                .AsQueryable();

            // Apply status filter if provided
            if (!string.IsNullOrEmpty(FilterStatus) && Enum.TryParse<OrderStatus>(FilterStatus, out var status))
            {
                query = query.Where(o => o.Status == status);
            }

            // Apply order number filter if provided
            if (!string.IsNullOrEmpty(FilterOrderNumber))
            {
                query = query.Where(o => o.OrderNumber.Contains(FilterOrderNumber));
            }

            // Get total count for pagination
            TotalCount = await query.CountAsync();

            // Get paginated results, ordered by most recent first
            Orders = await query
                .OrderByDescending(o => o.OrderedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading orders for admin");
            ErrorMessage = "An error occurred while loading orders.";
            return Page();
        }
    }
}
