using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.CommissionInvoices;

[Authorize(Policy = "AdminOnly")]
public class DetailsModel : PageModel
{
    private readonly ICommissionInvoiceService _invoiceService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        ICommissionInvoiceService invoiceService,
        ILogger<DetailsModel> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public CommissionInvoice? Invoice { get; set; }
    public CommissionInvoiceConfig? Config { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Invoice = await _invoiceService.GetInvoiceAsync(id);
        
        if (Invoice == null)
        {
            TempData["ErrorMessage"] = "Invoice not found.";
            return RedirectToPage("/Admin/CommissionInvoices/Index");
        }

        Config = await _invoiceService.GetOrCreateConfigAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostIssueAsync(int id)
    {
        var success = await _invoiceService.IssueInvoiceAsync(id);
        
        if (success)
        {
            TempData["SuccessMessage"] = "Invoice issued successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to issue invoice. It may not be in Draft status.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostMarkAsPaidAsync(int id)
    {
        var success = await _invoiceService.MarkInvoiceAsPaidAsync(id);
        
        if (success)
        {
            TempData["SuccessMessage"] = "Invoice marked as paid successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to mark invoice as paid. It may not be in Issued status.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var success = await _invoiceService.CancelInvoiceAsync(id);
        
        if (success)
        {
            TempData["SuccessMessage"] = "Invoice cancelled successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to cancel invoice. It may already be paid.";
        }

        return RedirectToPage(new { id });
    }
}
