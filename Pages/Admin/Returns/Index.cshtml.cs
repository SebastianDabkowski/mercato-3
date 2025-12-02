using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Admin.Returns;

/// <summary>
/// Page model for admin view of all return requests across all stores.
/// </summary>
[Authorize(Policy = "AdminOnly")]
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
    /// Gets or sets the list of all return requests.
    /// </summary>
    public List<ReturnRequest> ReturnRequests { get; set; } = new();

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public ReturnStatus? StatusFilter { get; set; }

    /// <summary>
    /// Gets or sets the type filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public ReturnRequestType? TypeFilter { get; set; }

    /// <summary>
    /// Gets or sets the store filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? StoreFilter { get; set; }

    /// <summary>
    /// Gets or sets the search query for case number, buyer name, or seller name.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets available stores for filtering.
    /// </summary>
    public List<Store> AvailableStores { get; set; } = new();

    /// <summary>
    /// Handles GET request to display all return requests.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        // Get all stores for filter dropdown
        AvailableStores = await _context.Stores
            .OrderBy(s => s.StoreName)
            .ToListAsync();

        // Get all return requests with related data
        var query = _context.ReturnRequests
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.Store)
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.ParentOrder)
            .Include(rr => rr.Buyer)
            .Include(rr => rr.Items)
                .ThenInclude(ri => ri.OrderItem)
            .Include(rr => rr.Messages)
            .AsQueryable();

        // Apply filters
        if (StatusFilter.HasValue)
        {
            query = query.Where(rr => rr.Status == StatusFilter.Value);
        }

        if (TypeFilter.HasValue)
        {
            query = query.Where(rr => rr.RequestType == TypeFilter.Value);
        }

        if (StoreFilter.HasValue)
        {
            query = query.Where(rr => rr.SubOrder.StoreId == StoreFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            // Apply search filter - case insensitive comparison
            query = query.Where(rr =>
                EF.Functions.Like(rr.ReturnNumber, $"%{SearchQuery}%") ||
                EF.Functions.Like(rr.Buyer.FirstName, $"%{SearchQuery}%") ||
                EF.Functions.Like(rr.Buyer.LastName, $"%{SearchQuery}%") ||
                EF.Functions.Like(rr.Buyer.Email, $"%{SearchQuery}%") ||
                EF.Functions.Like(rr.SubOrder.Store.StoreName, $"%{SearchQuery}%"));
        }

        ReturnRequests = await query
            .OrderByDescending(rr => rr.RequestedAt)
            .ToListAsync();

        return Page();
    }
}
