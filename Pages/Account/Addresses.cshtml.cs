using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

/// <summary>
/// Page model for managing buyer shipping addresses.
/// </summary>
[Authorize]
public class AddressesModel : PageModel
{
    private readonly IAddressService _addressService;
    private readonly ILogger<AddressesModel> _logger;

    public AddressesModel(
        IAddressService addressService,
        ILogger<AddressesModel> logger)
    {
        _addressService = addressService;
        _logger = logger;
    }

    public List<Address> Addresses { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Addresses = await _addressService.GetUserAddressesAsync(userId.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostSetDefaultAsync(int id)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            await _addressService.SetDefaultAddressAsync(userId.Value, id);
            SuccessMessage = "Default address updated successfully.";
            _logger.LogInformation("User {UserId} set address {AddressId} as default", userId, id);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to set default address. Please try again.";
            _logger.LogError(ex, "Error setting default address {AddressId} for user {UserId}", id, userId);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            // Verify the address belongs to the user before deleting
            var address = await _addressService.GetAddressByIdAsync(id);
            if (address == null || address.UserId != userId)
            {
                ErrorMessage = "Address not found.";
                return RedirectToPage();
            }

            await _addressService.DeleteAddressAsync(id);
            SuccessMessage = "Address deleted successfully.";
            _logger.LogInformation("User {UserId} deleted address {AddressId}", userId, id);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Cannot delete address {AddressId} for user {UserId}", id, userId);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to delete address. Please try again.";
            _logger.LogError(ex, "Error deleting address {AddressId} for user {UserId}", id, userId);
        }

        return RedirectToPage();
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
