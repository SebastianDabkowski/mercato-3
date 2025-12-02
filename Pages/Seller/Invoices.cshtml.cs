using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = "SellerOnly")]
public class InvoicesModel : PageModel
{
    private readonly ICommissionInvoiceService _invoiceService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoicesModel> _logger;

    public InvoicesModel(
        ICommissionInvoiceService invoiceService,
        ApplicationDbContext context,
        ILogger<InvoicesModel> logger)
    {
        _invoiceService = invoiceService;
        _context = context;
        _logger = logger;
    }

    public List<CommissionInvoice> Invoices { get; set; } = new();
    public Store? CurrentStore { get; set; }

    // Filter properties
    [BindProperty(SupportsGet = true)]
    public CommissionInvoiceStatus? SelectedStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
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
            TempData["ErrorMessage"] = "Store not found.";
            return RedirectToPage("/Index");
        }

        // Get all invoices for the store
        Invoices = await _invoiceService.GetInvoicesAsync(CurrentStore.Id, includeSuperseded: false);

        // Apply filters
        if (SelectedStatus.HasValue)
        {
            Invoices = Invoices.Where(i => i.Status == SelectedStatus.Value).ToList();
        }

        if (FromDate.HasValue)
        {
            Invoices = Invoices.Where(i => i.IssueDate >= FromDate.Value).ToList();
        }

        if (ToDate.HasValue)
        {
            Invoices = Invoices.Where(i => i.IssueDate <= ToDate.Value).ToList();
        }

        return Page();
    }
}
