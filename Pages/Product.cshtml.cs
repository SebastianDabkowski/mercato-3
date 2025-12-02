using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages;

public class ProductModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IProductVariantService _variantService;
    private readonly IRecentlyViewedService _recentlyViewedService;
    private readonly ICartService _cartService;
    private readonly IGuestCartService _guestCartService;

    public ProductModel(
        IProductService productService,
        IProductVariantService variantService,
        IRecentlyViewedService recentlyViewedService,
        ICartService cartService,
        IGuestCartService guestCartService)
    {
        _productService = productService;
        _variantService = variantService;
        _recentlyViewedService = recentlyViewedService;
        _cartService = cartService;
        _guestCartService = guestCartService;
    }

    public Product? Product { get; set; }
    public List<ProductVariantAttribute> VariantAttributes { get; set; } = new();
    public List<ProductVariant> Variants { get; set; } = new();
    public ProductVariant? SelectedVariant { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets a value indicating whether the product is publicly viewable (Active status).
    /// </summary>
    public bool IsProductAvailable => Product != null && Product.Status == ProductStatus.Active;

    /// <summary>
    /// Gets the message to display when product is not available.
    /// </summary>
    public string? UnavailableMessage { get; private set; }

    /// <summary>
    /// Gets the referrer URL for "Back to results" navigation.
    /// </summary>
    public string? ReferrerUrl { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there is a valid referrer URL.
    /// </summary>
    public bool HasReferrer => !string.IsNullOrEmpty(ReferrerUrl);

    public async Task<IActionResult> OnGetAsync(int id, int? variantId = null)
    {
        // Try to get the product - we want to handle the case where a product
        // exists but is not available (archived/inactive) vs doesn't exist at all
        Product = await _productService.GetProductByIdAsync(id);

        if (Product == null)
        {
            return NotFound();
        }

        // Capture referrer URL for "Back to results" navigation
        // Only use referrer if it's from the same origin (security consideration)
        var refererUri = Request.GetTypedHeaders().Referer;
        if (refererUri != null && 
            string.Equals(refererUri.Authority, Request.Host.Value, StringComparison.OrdinalIgnoreCase) &&
            (refererUri.AbsolutePath.StartsWith("/Search", StringComparison.OrdinalIgnoreCase) || 
             refererUri.AbsolutePath.StartsWith("/Category/", StringComparison.OrdinalIgnoreCase)))
        {
            ReferrerUrl = refererUri.ToString();
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
        else
        {
            // Track the product view only if the product is available
            _recentlyViewedService.TrackProductView(id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int id, int? variantId, int quantity = 1)
    {
        // Reload product data
        await OnGetAsync(id, variantId);

        if (Product == null || !IsProductAvailable)
        {
            ErrorMessage = "This product is not available.";
            return Page();
        }

        // Validate stock
        var hasStock = false;
        if (Product.HasVariants && variantId.HasValue && SelectedVariant != null)
        {
            hasStock = SelectedVariant.Stock > 0 && SelectedVariant.IsEnabled;
        }
        else if (!Product.HasVariants)
        {
            hasStock = Product.Stock > 0;
        }

        if (!hasStock)
        {
            ErrorMessage = "This product is out of stock.";
            return Page();
        }

        try
        {
            var (userId, sessionId) = GetUserOrSessionId();
            await _cartService.AddToCartAsync(userId, sessionId, id, variantId, quantity);
            SuccessMessage = "Item added to cart successfully!";
            return RedirectToPage("/Cart");
        }
        catch (Exception)
        {
            ErrorMessage = "Failed to add item to cart. Please try again.";
            return Page();
        }
    }

    private (int? userId, string? sessionId) GetUserOrSessionId()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return (userId, null);
            }
        }

        // Use persistent guest cart ID for anonymous users
        var guestCartId = _guestCartService.GetOrCreateGuestCartId();
        return (null, guestCartId);
    }
}