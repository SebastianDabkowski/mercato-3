using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MercatoApp.Pages.Checkout;

public class AddressModel : PageModel
{
    private readonly IAddressService _addressService;
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly IGuestCartService _guestCartService;
    private readonly ILogger<AddressModel> _logger;

    public AddressModel(
        IAddressService addressService,
        IOrderService orderService,
        ICartService cartService,
        IGuestCartService guestCartService,
        ILogger<AddressModel> logger)
    {
        _addressService = addressService;
        _orderService = orderService;
        _cartService = cartService;
        _guestCartService = guestCartService;
        _logger = logger;
    }

    public List<Address> SavedAddresses { get; set; } = new();
    public bool IsAuthenticated { get; set; }
    public bool IsNewAddress { get; set; }

    [BindProperty]
    public int? SelectedAddressId { get; set; }

    [BindProperty]
    public AddressInput NewAddress { get; set; } = new();

    public class AddressInput
    {
        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(200)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
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
        public string CountryCode { get; set; } = string.Empty;

        [Display(Name = "Save this address to my profile")]
        public bool SaveToProfile { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Check if cart is empty
        var (userId, sessionId) = GetUserOrSessionId();
        var cartItems = await _cartService.GetCartItemsBySellerAsync(userId, sessionId);
        
        if (!cartItems.Any())
        {
            TempData["ErrorMessage"] = "Your cart is empty. Please add items before checking out.";
            return RedirectToPage("/Cart");
        }

        IsAuthenticated = User.Identity?.IsAuthenticated == true;

        // Load saved addresses for authenticated users
        if (IsAuthenticated && userId.HasValue)
        {
            SavedAddresses = await _addressService.GetUserAddressesAsync(userId.Value);
            
            // Pre-select the default address
            if (SavedAddresses.Any(a => a.IsDefault))
            {
                SelectedAddressId = SavedAddresses.First(a => a.IsDefault).Id;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        var (userId, sessionId) = GetUserOrSessionId();
        IsAuthenticated = User.Identity?.IsAuthenticated == true;

        // Load saved addresses for authenticated users
        if (IsAuthenticated && userId.HasValue)
        {
            SavedAddresses = await _addressService.GetUserAddressesAsync(userId.Value);
        }

        if (action == "useExisting")
        {
            if (!SelectedAddressId.HasValue)
            {
                ModelState.AddModelError(nameof(SelectedAddressId), "Please select an address.");
                return Page();
            }

            // Validate the selected address
            var address = await _addressService.GetAddressByIdAsync(SelectedAddressId.Value);
            if (address == null)
            {
                ModelState.AddModelError(nameof(SelectedAddressId), "Selected address not found.");
                return Page();
            }

            // Validate shipping for this address
            var (isValid, errorMessage) = await _orderService.ValidateShippingForCartAsync(userId, sessionId, address.CountryCode);
            if (!isValid)
            {
                TempData["ErrorMessage"] = errorMessage;
                return Page();
            }

            // Store the selected address in session and proceed to review
            HttpContext.Session.SetInt32("CheckoutAddressId", SelectedAddressId.Value);
            return RedirectToPage("/Checkout/Review");
        }
        else if (action == "addNew")
        {
            if (!ModelState.IsValid)
            {
                IsNewAddress = true;
                return Page();
            }

            try
            {
                // Validate shipping for the new address
                var (isValid, errorMessage) = await _orderService.ValidateShippingForCartAsync(userId, sessionId, NewAddress.CountryCode);
                if (!isValid)
                {
                    ModelState.AddModelError(string.Empty, errorMessage ?? "Shipping validation failed.");
                    IsNewAddress = true;
                    return Page();
                }

                // Create the new address
                var address = new Address
                {
                    UserId = (IsAuthenticated && NewAddress.SaveToProfile) ? userId : null,
                    FullName = NewAddress.FullName,
                    PhoneNumber = NewAddress.PhoneNumber,
                    AddressLine1 = NewAddress.AddressLine1,
                    AddressLine2 = NewAddress.AddressLine2,
                    City = NewAddress.City,
                    StateProvince = NewAddress.StateProvince,
                    PostalCode = NewAddress.PostalCode,
                    CountryCode = NewAddress.CountryCode.ToUpperInvariant(),
                    IsDefault = false
                };

                var createdAddress = await _addressService.CreateAddressAsync(address);

                // Store the address in session and proceed to review
                HttpContext.Session.SetInt32("CheckoutAddressId", createdAddress.Id);
                return RedirectToPage("/Checkout/Review");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                IsNewAddress = true;
                return Page();
            }
        }

        return Page();
    }

    private (int? userId, string? sessionId) GetUserOrSessionId()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return (userId, null);
            }
        }

        var guestCartId = _guestCartService.GetOrCreateGuestCartId();
        return (null, guestCartId);
    }
}
