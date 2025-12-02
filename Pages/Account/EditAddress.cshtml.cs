using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

/// <summary>
/// Page model for editing an existing shipping address.
/// </summary>
[Authorize]
public class EditAddressModel : PageModel
{
    private readonly IAddressService _addressService;
    private readonly ILogger<EditAddressModel> _logger;

    public EditAddressModel(
        IAddressService addressService,
        ILogger<EditAddressModel> logger)
    {
        _addressService = addressService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(200)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [MaxLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address line 1 is required")]
        [MaxLength(200)]
        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "Address Line 2")]
        public string? AddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required")]
        [MaxLength(100)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "State/Province")]
        public string? StateProvince { get; set; }

        [Required(ErrorMessage = "Postal code is required")]
        [MaxLength(20)]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        [MaxLength(2)]
        [Display(Name = "Country")]
        public string CountryCode { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Delivery Instructions")]
        public string? DeliveryInstructions { get; set; }

        [Display(Name = "Set as default address")]
        public bool IsDefault { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var address = await _addressService.GetAddressByIdAsync(id);
        if (address == null || address.UserId != userId)
        {
            TempData["ErrorMessage"] = "Address not found.";
            return RedirectToPage("/Account/Addresses");
        }

        // Populate the form with existing data
        Input = new InputModel
        {
            FullName = address.FullName,
            PhoneNumber = address.PhoneNumber,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            StateProvince = address.StateProvince,
            PostalCode = address.PostalCode,
            CountryCode = address.CountryCode,
            DeliveryInstructions = address.DeliveryInstructions,
            IsDefault = address.IsDefault
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = GetUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            var existingAddress = await _addressService.GetAddressByIdAsync(id);
            if (existingAddress == null || existingAddress.UserId != userId)
            {
                TempData["ErrorMessage"] = "Address not found.";
                return RedirectToPage("/Account/Addresses");
            }

            // Update the address
            var address = new Address
            {
                Id = id,
                UserId = userId.Value,
                FullName = Input.FullName,
                PhoneNumber = Input.PhoneNumber,
                AddressLine1 = Input.AddressLine1,
                AddressLine2 = Input.AddressLine2,
                City = Input.City,
                StateProvince = Input.StateProvince,
                PostalCode = Input.PostalCode,
                CountryCode = Input.CountryCode,
                DeliveryInstructions = Input.DeliveryInstructions,
                IsDefault = Input.IsDefault
            };

            await _addressService.UpdateAddressAsync(address);

            TempData["SuccessMessage"] = "Address updated successfully.";
            _logger.LogInformation("User {UserId} updated address {AddressId}", userId, id);

            return RedirectToPage("/Account/Addresses");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            _logger.LogWarning(ex, "Invalid operation when updating address {AddressId} for user {UserId}", id, userId);
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Failed to update address. Please try again.");
            _logger.LogError(ex, "Error updating address {AddressId} for user {UserId}", id, userId);
            return Page();
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
