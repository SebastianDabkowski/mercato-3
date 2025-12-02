using MercatoApp.Data;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = "SellerOnly")]
public class InvoiceDownloadModel : PageModel
{
    private readonly ICommissionInvoiceService _invoiceService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceDownloadModel> _logger;

    public InvoiceDownloadModel(
        ICommissionInvoiceService invoiceService,
        ApplicationDbContext context,
        ILogger<InvoiceDownloadModel> logger)
    {
        _invoiceService = invoiceService;
        _context = context;
        _logger = logger;
    }

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

        // Get the invoice to verify ownership
        var invoice = await _invoiceService.GetInvoiceAsync(id);

        if (invoice == null || invoice.StoreId != currentStore.Id)
        {
            TempData["ErrorMessage"] = "Invoice not found or access denied.";
            return RedirectToPage("/Seller/Invoices");
        }

        // Generate PDF
        var pdfBytes = await _invoiceService.GenerateInvoicePdfAsync(id);

        // Return as downloadable file
        // Note: For HTML content, we're returning it as HTML for now
        // In production, use a proper PDF library
        return File(pdfBytes, "text/html", $"{invoice.InvoiceNumber}.html");
    }
}
