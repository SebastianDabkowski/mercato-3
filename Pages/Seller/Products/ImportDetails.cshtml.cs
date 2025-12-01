using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace MercatoApp.Pages.Seller.Products;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class ImportDetailsModel : PageModel
{
    private readonly IProductImportService _importService;
    private readonly IStoreProfileService _storeProfileService;

    public ImportDetailsModel(
        IProductImportService importService,
        IStoreProfileService storeProfileService)
    {
        _importService = importService;
        _storeProfileService = storeProfileService;
    }

    public ProductImportJob? Job { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
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

        Job = await _importService.GetImportJobAsync(id, store.Id);

        if (Job == null)
        {
            return Page();
        }

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
            return RedirectToPage(new { id });
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
}
