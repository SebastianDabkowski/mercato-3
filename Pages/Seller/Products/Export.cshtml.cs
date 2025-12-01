using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller.Products;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class ExportModel : PageModel
{
    private readonly IProductExportService _exportService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<ExportModel> _logger;

    public ExportModel(
        IProductExportService exportService,
        IStoreProfileService storeProfileService,
        ILogger<ExportModel> logger)
    {
        _exportService = exportService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public List<int> SelectedProductIds { get; set; } = new();

    [BindProperty]
    public string ExportFormat { get; set; } = "csv";

    [BindProperty]
    public string ExportScope { get; set; } = "all";

    public List<string> ErrorMessages { get; set; } = new();
    public bool HasSelectedProducts => SelectedProductIds.Count > 0;

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

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
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

        try
        {
            // Determine which products to export
            List<int>? productIdsToExport = null;
            if (ExportScope == "selected" && SelectedProductIds.Count > 0)
            {
                productIdsToExport = SelectedProductIds;
            }

            // Generate the export file based on format
            ProductExportResult result;
            if (ExportFormat == "excel")
            {
                result = await _exportService.ExportToExcelAsync(store.Id, productIdsToExport);
            }
            else
            {
                result = await _exportService.ExportToCsvAsync(store.Id, productIdsToExport);
            }

            if (!result.Success || result.FileData == null || result.FileName == null || result.ContentType == null)
            {
                ErrorMessages.AddRange(result.Errors);
                return Page();
            }

            _logger.LogInformation("User {UserId} exported products for store {StoreId} in {Format} format",
                userId.Value, store.Id, ExportFormat);

            // Return the file for download
            return File(result.FileData, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting products for user {UserId}", userId);
            ErrorMessages.Add($"An error occurred while exporting: {ex.Message}");
            return Page();
        }
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
