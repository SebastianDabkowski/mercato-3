using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

/// <summary>
/// Page model for adding a new shipping address.
/// </summary>
[Authorize]
public class AddAddressModel : PageModel
{
    private readonly IAddressService _addressService;
    private readonly ILogger<AddAddressModel> _logger;

    public AddAddressModel(
        IAddressService addressService,
        ILogger<AddAddressModel> logger)
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

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
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
            var address = new Address
            {
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

            await _addressService.CreateAddressAsync(address);

            TempData["SuccessMessage"] = "Address added successfully.";
            _logger.LogInformation("User {UserId} added new address {AddressId}", userId, address.Id);

            return RedirectToPage("/Account/Addresses");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            _logger.LogWarning(ex, "Invalid operation when adding address for user {UserId}", userId);
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Failed to add address. Please try again.");
            _logger.LogError(ex, "Error adding address for user {UserId}", userId);
            return Page();
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
