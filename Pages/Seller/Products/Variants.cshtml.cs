using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller.Products;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class VariantsModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly IProductVariantService _variantService;

    public VariantsModel(
        IProductService productService,
        IStoreProfileService storeProfileService,
        IProductVariantService variantService)
    {
        _productService = productService;
        _storeProfileService = storeProfileService;
        _variantService = variantService;
    }

    public Store? Store { get; set; }
    public Product? Product { get; set; }
    public List<ProductVariantAttribute> VariantAttributes { get; set; } = new();
    public List<ProductVariant> Variants { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

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

        Product = await _productService.GetProductByIdAsync(id, Store.Id);
        if (Product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToPage("Index");
        }

        if (Product.HasVariants)
        {
            VariantAttributes = await _variantService.GetVariantAttributesAsync(id, Store.Id);
            Variants = await _variantService.GetVariantsAsync(id, Store.Id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostEnableVariantsAsync(int id, List<string> attributeNames, List<string> attributeValues)
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

        Product = await _productService.GetProductByIdAsync(id, Store.Id);
        if (Product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToPage("Index");
        }

        if (attributeNames.Count == 0 || attributeValues.Count == 0 || attributeNames.Count != attributeValues.Count)
        {
            TempData["ErrorMessage"] = "Invalid variant attribute data.";
            return RedirectToPage("Variants", new { id });
        }

        var attributes = new List<VariantAttributeData>();
        for (int i = 0; i < attributeNames.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(attributeNames[i]) && !string.IsNullOrWhiteSpace(attributeValues[i]))
            {
                var values = attributeValues[i]
                    .Split(',')
                    .Select(v => v.Trim())
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList();

                if (values.Count > 0)
                {
                    attributes.Add(new VariantAttributeData
                    {
                        Name = attributeNames[i].Trim(),
                        Values = values
                    });
                }
            }
        }

        var result = await _variantService.EnableVariantsAsync(id, Store.Id, attributes);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Variants enabled successfully. You can now generate variant combinations.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage("Variants", new { id });
    }

    public async Task<IActionResult> OnPostDisableVariantsAsync(int id)
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

        var result = await _variantService.DisableVariantsAsync(id, Store.Id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Variants disabled successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage("Variants", new { id });
    }

    public async Task<IActionResult> OnPostGenerateVariantsAsync(int id)
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

        var result = await _variantService.GenerateVariantCombinationsAsync(id, Store.Id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Variant combinations generated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage("Variants", new { id });
    }

    public async Task<IActionResult> OnPostUpdateVariantAsync(int id, int variantId, string? sku, int stock, decimal? priceOverride, bool isEnabled)
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

        var data = new UpdateVariantData
        {
            Sku = sku,
            Stock = stock,
            PriceOverride = priceOverride,
            IsEnabled = isEnabled
        };

        var result = await _variantService.UpdateVariantAsync(variantId, Store.Id, data);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Variant updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage("Variants", new { id });
    }

    public async Task<IActionResult> OnPostUpdateVariantStockAsync(int id, int variantId, int stock)
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

        // Get the existing variant
        var variant = await _variantService.GetVariantByIdAsync(variantId, Store.Id);
        if (variant == null)
        {
            TempData["ErrorMessage"] = "Variant not found.";
            return RedirectToPage("Variants", new { id });
        }

        var data = new UpdateVariantData
        {
            Sku = variant.Sku,
            Stock = stock,
            PriceOverride = variant.PriceOverride,
            IsEnabled = variant.IsEnabled
        };

        var result = await _variantService.UpdateVariantAsync(variantId, Store.Id, data);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Stock updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage("Variants", new { id });
    }

    public async Task<IActionResult> OnPostDeleteVariantAsync(int id, int variantId)
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

        var result = await _variantService.DeleteVariantAsync(variantId, Store.Id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Variant deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage("Variants", new { id });
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
