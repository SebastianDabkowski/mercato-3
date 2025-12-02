using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller.ShippingMethods;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class DeleteModel : PageModel
{
    private readonly IShippingMethodService _shippingMethodService;
    private readonly IStoreProfileService _storeProfileService;

    public DeleteModel(
        IShippingMethodService shippingMethodService,
        IStoreProfileService storeProfileService)
    {
        _shippingMethodService = shippingMethodService;
        _storeProfileService = storeProfileService;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public Store? Store { get; set; }
    public ShippingMethod? ShippingMethod { get; set; }

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

        ShippingMethod = await _shippingMethodService.GetShippingMethodByIdAsync(Id);
        if (ShippingMethod == null || ShippingMethod.StoreId != Store.Id)
        {
            TempData["ErrorMessage"] = "Shipping method not found.";
            return RedirectToPage("Index");
        }

        // Don't show delete page for already inactive methods
        if (!ShippingMethod.IsActive)
        {
            TempData["ErrorMessage"] = "This shipping method is already inactive.";
            return RedirectToPage("Index");
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

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        ShippingMethod = await _shippingMethodService.GetShippingMethodByIdAsync(Id);
        if (ShippingMethod == null || ShippingMethod.StoreId != Store.Id)
        {
            TempData["ErrorMessage"] = "Shipping method not found.";
            return RedirectToPage("Index");
        }

        var success = await _shippingMethodService.DeleteShippingMethodAsync(Id);

        if (!success)
        {
            TempData["ErrorMessage"] = "Failed to disable shipping method.";
            return RedirectToPage("Index");
        }

        TempData["SuccessMessage"] = "Shipping method disabled successfully. It is no longer available at checkout.";
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
}
