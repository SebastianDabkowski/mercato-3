using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages;

public class StoreModel : PageModel
{
    private readonly IStoreProfileService _storeProfileService;
    private readonly IProductService _productService;

    public StoreModel(IStoreProfileService storeProfileService, IProductService productService)
    {
        _storeProfileService = storeProfileService;
        _productService = productService;
    }

    public Store? Store { get; set; }

    /// <summary>
    /// Gets the list of active products for this store.
    /// </summary>
    public List<Product> Products { get; set; } = new();

    /// <summary>
    /// Gets the products grouped by category.
    /// </summary>
    public Dictionary<string, List<Product>> ProductsByCategory { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether the store is publicly viewable (Active or LimitedActive).
    /// </summary>
    public bool IsStorePubliclyViewable => Store != null && 
        (Store.Status == StoreStatus.Active || Store.Status == StoreStatus.LimitedActive);

    /// <summary>
    /// Gets the message to display when store is not accessible.
    /// </summary>
    public string? UnavailableMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        Store = await _storeProfileService.GetStoreBySlugAsync(slug);

        if (Store == null)
        {
            return NotFound();
        }

        // Handle stores that are not publicly viewable
        if (!IsStorePubliclyViewable)
        {
            UnavailableMessage = Store.Status switch
            {
                StoreStatus.Suspended => "This store is currently unavailable.",
                StoreStatus.PendingVerification => "This store is not yet available for public viewing.",
                _ => "This store is currently unavailable."
            };
        }
        else
        {
            // Load active products for the store
            var allProducts = await _productService.GetActiveProductsByStoreIdAsync(Store.Id);
            Products = allProducts.Where(p => p.Status == ProductStatus.Active).ToList();
            
            // Group products by category
            ProductsByCategory = Products
                .GroupBy(p => p.Category)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        return Page();
    }
}
