using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Admin.CommissionInvoices;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly ICommissionInvoiceService _invoiceService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ICommissionInvoiceService invoiceService,
        ApplicationDbContext context,
        ILogger<IndexModel> logger)
    {
        _invoiceService = invoiceService;
        _context = context;
        _logger = logger;
    }

    public List<CommissionInvoice> Invoices { get; set; } = new();

    // Filter properties
    [BindProperty(SupportsGet = true)]
    public int? FilterYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FilterMonth { get; set; }

    [BindProperty(SupportsGet = true)]
    public CommissionInvoiceStatus? SelectedStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? StoreId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Set default year if not provided
        if (!FilterYear.HasValue)
        {
            FilterYear = DateTime.UtcNow.Year;
        }

        // Get all invoices
        var query = _context.CommissionInvoices
            .Include(i => i.Store)
            .Include(i => i.Items)
            .AsQueryable();

        // Apply filters
        if (FilterYear.HasValue && FilterMonth.HasValue)
        {
            var periodStart = new DateTime(FilterYear.Value, FilterMonth.Value, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);
            query = query.Where(i => i.PeriodStartDate >= periodStart && i.PeriodEndDate <= periodEnd);
        }
        else if (FilterYear.HasValue)
        {
            var yearStart = new DateTime(FilterYear.Value, 1, 1);
            var yearEnd = new DateTime(FilterYear.Value, 12, 31);
            query = query.Where(i => i.IssueDate >= yearStart && i.IssueDate <= yearEnd);
        }

        if (SelectedStatus.HasValue)
        {
            query = query.Where(i => i.Status == SelectedStatus.Value);
        }

        if (StoreId.HasValue)
        {
            query = query.Where(i => i.StoreId == StoreId.Value);
        }

        Invoices = await query
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();

        return Page();
    }
}
