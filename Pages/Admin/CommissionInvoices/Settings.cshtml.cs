using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.CommissionInvoices;

[Authorize(Policy = "AdminOnly")]
public class SettingsModel : PageModel
{
    private readonly ICommissionInvoiceService _invoiceService;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(
        ICommissionInvoiceService invoiceService,
        ILogger<SettingsModel> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    [BindProperty]
    public CommissionInvoiceConfig Config { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        Config = await _invoiceService.GetOrCreateConfigAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _invoiceService.UpdateConfigAsync(Config);
            TempData["SuccessMessage"] = "Settings saved successfully.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving commission invoice settings");
            TempData["ErrorMessage"] = $"Error saving settings: {ex.Message}";
            return Page();
        }
    }
}
