using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller.Products;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class DeleteModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;

    public DeleteModel(
        IProductService productService,
        IStoreProfileService storeProfileService)
    {
        _productService = productService;
        _storeProfileService = storeProfileService;
    }

    public Store? Store { get; set; }
    public Product? Product { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        // Get the product, ensuring it belongs to the seller's store
        Product = await _productService.GetProductByIdAsync(id, Store.Id);
        if (Product == null)
        {
            TempData["ErrorMessage"] = "Product not found or you do not have permission to delete it.";
            return RedirectToPage("Index");
        }

        // Cannot delete already archived products
        if (Product.Status == ProductStatus.Archived)
        {
            TempData["ErrorMessage"] = "This product is already archived.";
            return RedirectToPage("Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        // Archive the product (soft delete)
        var result = await _productService.ArchiveProductAsync(id, Store.Id, userId.Value);

        if (!result.Success)
        {
            ErrorMessage = result.Errors.FirstOrDefault() ?? "Failed to delete the product.";
            
            // Get the product again for display
            Product = await _productService.GetProductByIdAsync(id, Store.Id);
            if (Product == null)
            {
                return RedirectToPage("Index");
            }
            return Page();
        }

        TempData["SuccessMessage"] = "Product deleted successfully.";
        return RedirectToPage("Index");
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

    public static string GetStatusBadgeClass(ProductStatus status)
    {
        return status switch
        {
            ProductStatus.Draft => "bg-secondary",
            ProductStatus.Active => "bg-success",
            ProductStatus.Inactive => "bg-warning",
            ProductStatus.Archived => "bg-dark",
            _ => "bg-secondary"
        };
    }
}
