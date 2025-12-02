using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller.ShippingMethods;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class IndexModel : PageModel
{
    private readonly IShippingMethodService _shippingMethodService;
    private readonly IStoreProfileService _storeProfileService;

    public IndexModel(
        IShippingMethodService shippingMethodService,
        IStoreProfileService storeProfileService)
    {
        _shippingMethodService = shippingMethodService;
        _storeProfileService = storeProfileService;
    }

    public Store? Store { get; set; }
    public List<ShippingMethod> ShippingMethods { get; set; } = new();

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

        ShippingMethods = await _shippingMethodService.GetAllShippingMethodsAsync(Store.Id);

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
