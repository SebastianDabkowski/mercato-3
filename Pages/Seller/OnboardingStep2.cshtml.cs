using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class OnboardingStep2Model : PageModel
{
    private readonly ISellerOnboardingService _onboardingService;

    public OnboardingStep2Model(ISellerOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Business type is required.")]
        [Display(Name = "Business Type")]
        public string BusinessType { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Business registration number must be 50 characters or less.")]
        [Display(Name = "Business Registration Number")]
        public string? BusinessRegistrationNumber { get; set; }

        [MaxLength(50, ErrorMessage = "Tax ID must be 50 characters or less.")]
        [Display(Name = "Tax ID / VAT Number")]
        public string? TaxId { get; set; }
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

        // Load existing draft data
        var draft = await _onboardingService.GetOrCreateDraftAsync(userId.Value);
        
        // Ensure Step 1 is completed
        if (draft.LastCompletedStep < 1)
        {
            return RedirectToPage("OnboardingStep1");
        }

        Input.BusinessType = draft.BusinessType ?? string.Empty;
        Input.BusinessRegistrationNumber = draft.BusinessRegistrationNumber;
        Input.TaxId = draft.TaxId;

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

        // Custom validation: Business registration number required for Business type
        if (Input.BusinessType == "Business" && string.IsNullOrWhiteSpace(Input.BusinessRegistrationNumber))
        {
            ModelState.AddModelError("Input.BusinessRegistrationNumber", "Business registration number is required for business accounts.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var data = new VerificationData
        {
            BusinessType = Input.BusinessType,
            BusinessRegistrationNumber = Input.BusinessRegistrationNumber,
            TaxId = Input.TaxId
        };

        var result = await _onboardingService.SaveVerificationDataAsync(userId.Value, data);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        return RedirectToPage("OnboardingStep3");
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
