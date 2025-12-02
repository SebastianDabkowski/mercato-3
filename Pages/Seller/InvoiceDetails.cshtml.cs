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
public class InvoiceDetailsModel : PageModel
{
    private readonly ICommissionInvoiceService _invoiceService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceDetailsModel> _logger;

    public InvoiceDetailsModel(
        ICommissionInvoiceService invoiceService,
        ApplicationDbContext context,
        ILogger<InvoiceDetailsModel> logger)
    {
        _invoiceService = invoiceService;
        _context = context;
        _logger = logger;
    }

    public CommissionInvoice? Invoice { get; set; }
    public CommissionInvoiceConfig? Config { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Get the seller's store
        var currentStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (currentStore == null)
        {
            TempData["ErrorMessage"] = "Store not found.";
            return RedirectToPage("/Index");
        }

        // Get the invoice
        Invoice = await _invoiceService.GetInvoiceAsync(id);

        if (Invoice == null || Invoice.StoreId != currentStore.Id)
        {
            TempData["ErrorMessage"] = "Invoice not found or access denied.";
            return RedirectToPage("/Seller/Invoices");
        }

        // Get configuration
        Config = await _invoiceService.GetOrCreateConfigAsync();

        return Page();
    }
}
