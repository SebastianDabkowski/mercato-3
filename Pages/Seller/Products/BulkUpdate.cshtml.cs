using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller.Products;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class BulkUpdateModel : PageModel
{
    private readonly IBulkProductUpdateService _bulkUpdateService;
    private readonly IStoreProfileService _storeProfileService;

    public BulkUpdateModel(
        IBulkProductUpdateService bulkUpdateService,
        IStoreProfileService storeProfileService)
    {
        _bulkUpdateService = bulkUpdateService;
        _storeProfileService = storeProfileService;
    }

    [BindProperty]
    public List<int> SelectedProductIds { get; set; } = new();

    [BindProperty]
    public BulkUpdateType UpdateType { get; set; }

    [BindProperty]
    public BulkUpdateOperation Operation { get; set; }

    [BindProperty]
    public decimal Value { get; set; }

    public List<BulkUpdatePreviewItem> PreviewItems { get; set; } = new();

    public List<string> ErrorMessages { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(List<int> productIds)
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

        SelectedProductIds = productIds;
        return Page();
    }

    public async Task<IActionResult> OnPostPreviewAsync()
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

        if (SelectedProductIds.Count == 0)
        {
            ErrorMessages.Add("No products selected.");
            return Page();
        }

        var request = new BulkUpdateRequest
        {
            ProductIds = SelectedProductIds,
            UpdateType = UpdateType,
            Operation = Operation,
            Value = Value
        };

        PreviewItems = await _bulkUpdateService.PreviewBulkUpdateAsync(store.Id, request);
        return Page();
    }

    public async Task<IActionResult> OnPostExecuteAsync()
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

        if (SelectedProductIds.Count == 0)
        {
            ErrorMessages.Add("No products selected.");
            return Page();
        }

        var request = new BulkUpdateRequest
        {
            ProductIds = SelectedProductIds,
            UpdateType = UpdateType,
            Operation = Operation,
            Value = Value
        };

        var result = await _bulkUpdateService.ExecuteBulkUpdateAsync(store.Id, request, userId.Value);

        if (result.GeneralErrors.Count > 0)
        {
            ErrorMessages.AddRange(result.GeneralErrors);
            return Page();
        }

        if (result.Errors.Count > 0)
        {
            foreach (var error in result.Errors)
            {
                ErrorMessages.Add($"{error.ProductTitle}: {error.ErrorMessage}");
            }
        }

        if (result.Success)
        {
            var updateTypeName = UpdateType == BulkUpdateType.Price ? "price" : "stock";
            SuccessMessage = $"Successfully updated {updateTypeName} for {result.SuccessCount} product(s).";
            
            if (result.FailureCount > 0)
            {
                SuccessMessage += $" {result.FailureCount} product(s) could not be updated.";
            }

            return RedirectToPage("Index");
        }

        return Page();
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
