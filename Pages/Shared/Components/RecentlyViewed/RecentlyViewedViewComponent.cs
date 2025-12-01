using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace MercatoApp.Pages.Shared.Components.RecentlyViewed;

/// <summary>
/// View component for displaying recently viewed products.
/// </summary>
public class RecentlyViewedViewComponent : ViewComponent
{
    private readonly IRecentlyViewedService _recentlyViewedService;

    public RecentlyViewedViewComponent(IRecentlyViewedService recentlyViewedService)
    {
        _recentlyViewedService = recentlyViewedService;
    }

    /// <summary>
    /// Invokes the view component.
    /// </summary>
    /// <param name="maxItems">Maximum number of items to display.</param>
    /// <param name="currentProductId">Optional current product ID to exclude from list.</param>
    public async Task<IViewComponentResult> InvokeAsync(int maxItems = 10, int? currentProductId = null)
    {
        var products = await _recentlyViewedService.GetRecentlyViewedProductsAsync(maxItems + 1);
        
        // Exclude current product if specified
        if (currentProductId.HasValue)
        {
            products = products.Where(p => p.Id != currentProductId.Value).Take(maxItems).ToList();
        }
        else
        {
            products = products.Take(maxItems).ToList();
        }

        return View(products);
    }
}
