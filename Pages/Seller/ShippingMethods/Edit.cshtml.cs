using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller.ShippingMethods;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class EditModel : PageModel
{
    private readonly IShippingMethodService _shippingMethodService;
    private readonly IStoreProfileService _storeProfileService;

    public EditModel(
        IShippingMethodService shippingMethodService,
        IStoreProfileService storeProfileService)
    {
        _shippingMethodService = shippingMethodService;
        _storeProfileService = storeProfileService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public Store? Store { get; set; }
    public ShippingMethod? ShippingMethod { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name must be 100 characters or less.")]
        [Display(Name = "Method Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description must be 500 characters or less.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [MaxLength(100, ErrorMessage = "Estimated delivery must be 100 characters or less.")]
        [Display(Name = "Estimated Delivery")]
        public string? EstimatedDelivery { get; set; }

        [Required(ErrorMessage = "Base cost is required.")]
        [Range(0, 999999.99, ErrorMessage = "Base cost must be between 0 and 999,999.99.")]
        [Display(Name = "Base Cost")]
        public decimal BaseCost { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Additional item cost must be between 0 and 999,999.99.")]
        [Display(Name = "Additional Item Cost")]
        public decimal AdditionalItemCost { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Free shipping threshold must be between 0 and 999,999.99.")]
        [Display(Name = "Free Shipping Threshold")]
        public decimal? FreeShippingThreshold { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }
    }

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

        // Populate the form with existing data
        Input.Name = ShippingMethod.Name;
        Input.Description = ShippingMethod.Description;
        Input.EstimatedDelivery = ShippingMethod.EstimatedDelivery;
        Input.BaseCost = ShippingMethod.BaseCost;
        Input.AdditionalItemCost = ShippingMethod.AdditionalItemCost;
        Input.FreeShippingThreshold = ShippingMethod.FreeShippingThreshold;
        Input.IsActive = ShippingMethod.IsActive;

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

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Update the shipping method
        ShippingMethod.Name = Input.Name;
        ShippingMethod.Description = Input.Description;
        ShippingMethod.EstimatedDelivery = Input.EstimatedDelivery;
        ShippingMethod.BaseCost = Input.BaseCost;
        ShippingMethod.AdditionalItemCost = Input.AdditionalItemCost;
        ShippingMethod.FreeShippingThreshold = Input.FreeShippingThreshold;
        ShippingMethod.IsActive = Input.IsActive;

        var success = await _shippingMethodService.UpdateShippingMethodAsync(ShippingMethod);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, "Failed to update shipping method.");
            return Page();
        }

        TempData["SuccessMessage"] = "Shipping method updated successfully.";
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
