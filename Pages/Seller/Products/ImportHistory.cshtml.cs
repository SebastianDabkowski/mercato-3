using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace MercatoApp.Pages.Seller.Products;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class ImportHistoryModel : PageModel
{
    private readonly IProductImportService _importService;
    private readonly IStoreProfileService _storeProfileService;

    public ImportHistoryModel(
        IProductImportService importService,
        IStoreProfileService storeProfileService)
    {
        _importService = importService;
        _storeProfileService = storeProfileService;
    }

    public List<ProductImportJob> ImportJobs { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (store == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        ImportJobs = await _importService.GetImportJobsAsync(store.Id);

        return Page();
    }

    public async Task<IActionResult> OnGetDownloadErrorsAsync(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (store == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        var errorReport = await _importService.GenerateErrorReportAsync(id, store.Id);

        if (string.IsNullOrEmpty(errorReport))
        {
            TempData["ErrorMessage"] = "Error report not found.";
            return RedirectToPage();
        }

        var bytes = Encoding.UTF8.GetBytes(errorReport);
        return File(bytes, "text/csv", $"import-errors-{id}.csv");
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    public static string GetStatusBadgeClass(ProductImportJobStatus status)
    {
        return status switch
        {
            ProductImportJobStatus.Pending => "bg-secondary",
            ProductImportJobStatus.Processing => "bg-primary",
            ProductImportJobStatus.Completed => "bg-success",
            ProductImportJobStatus.CompletedWithErrors => "bg-warning",
            ProductImportJobStatus.Failed => "bg-danger",
            ProductImportJobStatus.Cancelled => "bg-dark",
            _ => "bg-secondary"
        };
    }
}
