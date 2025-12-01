using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller.Products;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class IndexModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly IProductImageService _productImageService;

    public IndexModel(
        IProductService productService,
        IStoreProfileService storeProfileService,
        IProductImageService productImageService)
    {
        _productService = productService;
        _storeProfileService = storeProfileService;
        _productImageService = productImageService;
    }

    public Store? Store { get; set; }
    public List<Product> Products { get; set; } = new();
    public Dictionary<int, ProductImage?> ProductMainImages { get; set; } = new();

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

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        Products = await _productService.GetProductsByStoreIdAsync(Store.Id);

        // Load main images for all products in a single query
        var productIds = Products.Select(p => p.Id);
        ProductMainImages = await _productImageService.GetMainImagesAsync(productIds);

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

    public static string GetStatusBadgeClass(ProductStatus status)
    {
        return status switch
        {
            ProductStatus.Draft => "bg-secondary",
            ProductStatus.Active => "bg-success",
            ProductStatus.Suspended => "bg-warning",
            ProductStatus.Archived => "bg-dark",
            _ => "bg-secondary"
        };
    }
}
