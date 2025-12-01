using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages;

public class ProductModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IProductVariantService _variantService;

    public ProductModel(
        IProductService productService,
        IProductVariantService variantService)
    {
        _productService = productService;
        _variantService = variantService;
    }

    public Product? Product { get; set; }
    public List<ProductVariantAttribute> VariantAttributes { get; set; } = new();
    public List<ProductVariant> Variants { get; set; } = new();
    public ProductVariant? SelectedVariant { get; set; }

    /// <summary>
    /// Gets a value indicating whether the product is publicly viewable (Active status).
    /// </summary>
    public bool IsProductAvailable => Product != null && Product.Status == ProductStatus.Active;

    /// <summary>
    /// Gets the message to display when product is not available.
    /// </summary>
    public string? UnavailableMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, int? variantId = null)
    {
        // Try to get the product - we want to handle the case where a product
        // exists but is not available (archived/inactive) vs doesn't exist at all
        Product = await _productService.GetProductByIdAsync(id);

        if (Product == null)
        {
            return NotFound();
        }

        // Load variant data if the product has variants
        if (Product.HasVariants)
        {
            VariantAttributes = await _variantService.GetVariantAttributesAsync(id, null);
            Variants = await _variantService.GetVariantsAsync(id, null);

            // If a specific variant is selected, load it
            if (variantId.HasValue)
            {
                SelectedVariant = await _variantService.GetVariantByIdAsync(variantId.Value, null);
            }
        }

        // Handle products that are not publicly viewable
        if (!IsProductAvailable)
        {
            UnavailableMessage = Product.Status switch
            {
                ProductStatus.Archived => "This product is no longer available.",
                ProductStatus.Suspended => "This product is currently unavailable.",
                ProductStatus.Draft => "This product is not yet available for viewing.",
                _ => "This product is currently unavailable."
            };
        }

        return Page();
    }
}
