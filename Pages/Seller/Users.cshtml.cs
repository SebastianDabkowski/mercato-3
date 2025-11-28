using System.Security.Claims;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class UsersModel : PageModel
{
    private readonly IInternalUserManagementService _internalUserService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly IFeatureFlagService _featureFlagService;

    public UsersModel(
        IInternalUserManagementService internalUserService,
        IStoreProfileService storeProfileService,
        IFeatureFlagService featureFlagService)
    {
        _internalUserService = internalUserService;
        _storeProfileService = storeProfileService;
        _featureFlagService = featureFlagService;
    }

    public User? StoreOwner { get; set; }
    public List<StoreInternalUser> TeamMembers { get; set; } = [];
    public List<StoreUserInvitation> PendingInvitations { get; set; } = [];
    public Store? Store { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

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

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        // Check if user is the store owner
        if (!await _internalUserService.IsStoreOwnerAsync(Store.Id, userId.Value))
        {
            ErrorMessage = "Only store owners can manage team members.";
            return RedirectToPage("StoreSettings");
        }

        // Get store owner information
        StoreOwner = Store.User;

        // Get all team members (excluding the original store owner)
        var allUsers = await _internalUserService.GetStoreUsersAsync(Store.Id);
        TeamMembers = allUsers.Where(u => u.UserId != Store.UserId).ToList();

        // Get pending invitations
        PendingInvitations = await _internalUserService.GetPendingInvitationsAsync(Store.Id);

        return Page();
    }

    public async Task<IActionResult> OnPostChangeRoleAsync(int userId, StoreRole newRole)
    {
        if (!_featureFlagService.IsSellerUserManagementEnabled)
        {
            return RedirectToPage("StoreSettings");
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(currentUserId.Value);
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        var result = await _internalUserService.ChangeUserRoleAsync(Store.Id, userId, newRole, currentUserId.Value);

        if (result.IsSuccess)
        {
            SuccessMessage = "User role updated successfully.";
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to update user role.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateUserAsync(int userId)
    {
        if (!_featureFlagService.IsSellerUserManagementEnabled)
        {
            return RedirectToPage("StoreSettings");
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(currentUserId.Value);
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        var result = await _internalUserService.DeactivateUserAsync(Store.Id, userId, currentUserId.Value);

        if (result.IsSuccess)
        {
            SuccessMessage = "User deactivated successfully.";
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to deactivate user.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReactivateUserAsync(int userId)
    {
        if (!_featureFlagService.IsSellerUserManagementEnabled)
        {
            return RedirectToPage("StoreSettings");
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(currentUserId.Value);
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        var result = await _internalUserService.ReactivateUserAsync(Store.Id, userId, currentUserId.Value);

        if (result.IsSuccess)
        {
            SuccessMessage = "User reactivated successfully.";
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to reactivate user.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeInvitationAsync(int invitationId)
    {
        if (!_featureFlagService.IsSellerUserManagementEnabled)
        {
            return RedirectToPage("StoreSettings");
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var result = await _internalUserService.RevokeInvitationAsync(invitationId, currentUserId.Value);

        if (result.IsSuccess)
        {
            SuccessMessage = "Invitation revoked successfully.";
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to revoke invitation.";
        }

        return RedirectToPage();
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
