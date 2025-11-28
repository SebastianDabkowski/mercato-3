using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class OnboardingStep1Model : PageModel
{
    private readonly ISellerOnboardingService _onboardingService;

    public OnboardingStep1Model(ISellerOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Store name is required.")]
        [MaxLength(100, ErrorMessage = "Store name must be 100 characters or less.")]
        [Display(Name = "Store Name")]
        public string StoreName { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Description must be 1000 characters or less.")]
        [Display(Name = "Store Description")]
        public string? Description { get; set; }

        [MaxLength(100, ErrorMessage = "Category must be 100 characters or less.")]
        [Display(Name = "Store Category")]
        public string? Category { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        // Check if user already has a store
        if (await _onboardingService.HasExistingStoreAsync(userId.Value))
        {
            return RedirectToPage("OnboardingComplete");
        }

        // Load existing draft data if available
        var draft = await _onboardingService.GetOrCreateDraftAsync(userId.Value);
        Input.StoreName = draft.StoreName ?? string.Empty;
        Input.Description = draft.Description;
        Input.Category = draft.Category;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        // Check if user already has a store
        if (await _onboardingService.HasExistingStoreAsync(userId.Value))
        {
            return RedirectToPage("OnboardingComplete");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var data = new StoreProfileData
        {
            StoreName = Input.StoreName,
            Description = Input.Description,
            Category = Input.Category
        };

        var result = await _onboardingService.SaveStoreProfileAsync(userId.Value, data);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        return RedirectToPage("OnboardingStep2");
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
