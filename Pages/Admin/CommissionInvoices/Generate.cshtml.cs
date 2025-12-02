using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.CommissionInvoices;

[Authorize(Policy = "AdminOnly")]
public class GenerateModel : PageModel
{
    private readonly ICommissionInvoiceService _invoiceService;
    private readonly ILogger<GenerateModel> _logger;

    public GenerateModel(
        ICommissionInvoiceService invoiceService,
        ILogger<GenerateModel> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostGenerateMonthlyAsync(int year, int month)
    {
        try
        {
            if (month < 1 || month > 12)
            {
                TempData["ErrorMessage"] = "Invalid month. Please select a month between 1 and 12.";
                return Page();
            }

            var count = await _invoiceService.GenerateMonthlyInvoicesAsync(year, month);
            
            TempData["SuccessMessage"] = $"Successfully generated {count} commission invoice(s) for {new DateTime(year, month, 1):MMMM yyyy}.";
            return RedirectToPage("/Admin/CommissionInvoices/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly invoices for {Year}-{Month}", year, month);
            TempData["ErrorMessage"] = $"Error generating invoices: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostGenerateForStoreAsync(int storeId, DateTime periodStartDate, DateTime periodEndDate)
    {
        try
        {
            if (periodEndDate < periodStartDate)
            {
                TempData["ErrorMessage"] = "Period end date must be after the start date.";
                return Page();
            }

            var invoice = await _invoiceService.GenerateInvoiceAsync(storeId, periodStartDate, periodEndDate);
            
            if (invoice == null)
            {
                TempData["ErrorMessage"] = $"No commission transactions found for store {storeId} in the specified period.";
                return Page();
            }

            TempData["SuccessMessage"] = $"Successfully generated invoice {invoice.InvoiceNumber} for store {storeId}.";
            return RedirectToPage("/Admin/CommissionInvoices/Details", new { id = invoice.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for store {StoreId}", storeId);
            TempData["ErrorMessage"] = $"Error generating invoice: {ex.Message}";
            return Page();
        }
    }
}
