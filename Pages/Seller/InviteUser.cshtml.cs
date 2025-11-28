using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class InviteUserModel : PageModel
{
    private readonly IInternalUserManagementService _internalUserService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly IFeatureFlagService _featureFlagService;

    public InviteUserModel(
        IInternalUserManagementService internalUserService,
        IStoreProfileService storeProfileService,
        IFeatureFlagService featureFlagService)
    {
        _internalUserService = internalUserService;
        _storeProfileService = storeProfileService;
        _featureFlagService = featureFlagService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(256, ErrorMessage = "Email must be 256 characters or less.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a role.")]
        [Display(Name = "Role")]
        public StoreRole Role { get; set; } = StoreRole.ReadOnly;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Check if feature is enabled
        if (!_featureFlagService.IsSellerUserManagementEnabled)
        {
            return RedirectToPage("StoreSettings");
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        // Check if user is the store owner
        if (!await _internalUserService.IsStoreOwnerAsync(store.Id, userId.Value))
        {
            TempData["ErrorMessage"] = "Only store owners can invite team members.";
            return RedirectToPage("Users");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Check if feature is enabled
        if (!_featureFlagService.IsSellerUserManagementEnabled)
        {
            return RedirectToPage("StoreSettings");
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        // Check if user is the store owner
        if (!await _internalUserService.IsStoreOwnerAsync(store.Id, userId.Value))
        {
            TempData["ErrorMessage"] = "Only store owners can invite team members.";
            return RedirectToPage("Users");
        }

        // Prevent assigning StoreOwner role via invitation
        if (Input.Role == StoreRole.StoreOwner)
        {
            ModelState.AddModelError("Input.Role", "Cannot assign Store Owner role via invitation.");
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var inviteData = new InviteUserData
        {
            Email = Input.Email,
            Role = Input.Role
        };

        var result = await _internalUserService.InviteUserAsync(store.Id, userId.Value, inviteData);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = $"Invitation sent to {Input.Email}.";
            return RedirectToPage("Users");
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to send invitation.");
        return Page();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
