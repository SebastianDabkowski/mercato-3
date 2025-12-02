using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller.ShippingMethods;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class CreateModel : PageModel
{
    private readonly IShippingMethodService _shippingMethodService;
    private readonly IStoreProfileService _storeProfileService;

    public CreateModel(
        IShippingMethodService shippingMethodService,
        IStoreProfileService storeProfileService)
    {
        _shippingMethodService = shippingMethodService;
        _storeProfileService = storeProfileService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Store? Store { get; set; }

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
        public bool IsActive { get; set; } = true;
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

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var shippingMethod = new ShippingMethod
            {
                StoreId = Store.Id,
                Name = Input.Name,
                Description = Input.Description,
                EstimatedDelivery = Input.EstimatedDelivery,
                BaseCost = Input.BaseCost,
                AdditionalItemCost = Input.AdditionalItemCost,
                FreeShippingThreshold = Input.FreeShippingThreshold,
                IsActive = Input.IsActive,
                DisplayOrder = 0
            };

            await _shippingMethodService.CreateShippingMethodAsync(shippingMethod);

            TempData["SuccessMessage"] = "Shipping method created successfully.";
            return RedirectToPage("Index");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while creating the shipping method. Please try again.");
            return Page();
        }
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
