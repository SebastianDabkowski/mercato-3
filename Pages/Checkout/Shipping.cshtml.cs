using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace MercatoApp.Pages.Checkout;

public class ShippingModel : PageModel
{
    private readonly IAddressService _addressService;
    private readonly ICartService _cartService;
    private readonly IGuestCartService _guestCartService;
    private readonly IShippingMethodService _shippingMethodService;
    private readonly ILogger<ShippingModel> _logger;

    public ShippingModel(
        IAddressService addressService,
        ICartService cartService,
        IGuestCartService guestCartService,
        IShippingMethodService shippingMethodService,
        ILogger<ShippingModel> logger)
    {
        _addressService = addressService;
        _cartService = cartService;
        _guestCartService = guestCartService;
        _shippingMethodService = shippingMethodService;
        _logger = logger;
    }

    public Address? DeliveryAddress { get; set; }
    public Dictionary<Store, List<CartItem>> ItemsBySeller { get; set; } = new();
    public Dictionary<int, List<ShippingMethod>> ShippingMethodsBySeller { get; set; } = new();
    public Dictionary<int, decimal> ShippingCostsBySeller { get; set; } = new();

    [BindProperty]
    public Dictionary<int, int> SelectedShippingMethods { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var (userId, sessionId) = GetUserOrSessionId();

        // Check if cart is empty
        ItemsBySeller = await _cartService.GetCartItemsBySellerAsync(userId, sessionId);
        if (!ItemsBySeller.Any())
        {
            TempData["ErrorMessage"] = "Your cart is empty.";
            return RedirectToPage("/Cart");
        }

        // Get the selected address from session
        var addressId = HttpContext.Session.GetInt32("CheckoutAddressId");
        if (!addressId.HasValue)
        {
            TempData["ErrorMessage"] = "Please select a delivery address.";
            return RedirectToPage("/Checkout/Address");
        }

        DeliveryAddress = await _addressService.GetAddressByIdAsync(addressId.Value);
        if (DeliveryAddress == null)
        {
            TempData["ErrorMessage"] = "Delivery address not found.";
            return RedirectToPage("/Checkout/Address");
        }

        // Load shipping methods for each seller
        foreach (var sellerGroup in ItemsBySeller)
        {
            var store = sellerGroup.Key;
            var items = sellerGroup.Value;

            var shippingMethods = await _shippingMethodService.GetOrCreateDefaultShippingMethodsAsync(store.Id);
            ShippingMethodsBySeller[store.Id] = shippingMethods;

            // Calculate costs for each method
            ShippingCostsBySeller[store.Id] = 0;

            // Try to load previously selected shipping methods from session
            var selectedMethodsJson = HttpContext.Session.GetString("CheckoutShippingMethods");
            if (!string.IsNullOrEmpty(selectedMethodsJson))
            {
                var savedSelections = JsonSerializer.Deserialize<Dictionary<int, int>>(selectedMethodsJson);
                if (savedSelections != null && savedSelections.ContainsKey(store.Id))
                {
                    SelectedShippingMethods[store.Id] = savedSelections[store.Id];
                    var cost = await _shippingMethodService.CalculateShippingCostAsync(savedSelections[store.Id], items);
                    ShippingCostsBySeller[store.Id] = cost;
                }
                else
                {
                    // Pre-select the first (cheapest) shipping method
                    if (shippingMethods.Any())
                    {
                        SelectedShippingMethods[store.Id] = shippingMethods.First().Id;
                        var cost = await _shippingMethodService.CalculateShippingCostAsync(shippingMethods.First().Id, items);
                        ShippingCostsBySeller[store.Id] = cost;
                    }
                }
            }
            else
            {
                // Pre-select the first (cheapest) shipping method
                if (shippingMethods.Any())
                {
                    SelectedShippingMethods[store.Id] = shippingMethods.First().Id;
                    var cost = await _shippingMethodService.CalculateShippingCostAsync(shippingMethods.First().Id, items);
                    ShippingCostsBySeller[store.Id] = cost;
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var (userId, sessionId) = GetUserOrSessionId();

        // Validate that all sellers have a shipping method selected
        ItemsBySeller = await _cartService.GetCartItemsBySellerAsync(userId, sessionId);
        
        foreach (var sellerGroup in ItemsBySeller)
        {
            var storeId = sellerGroup.Key.Id;
            if (!SelectedShippingMethods.ContainsKey(storeId) || SelectedShippingMethods[storeId] == 0)
            {
                ModelState.AddModelError(string.Empty, $"Please select a shipping method for {sellerGroup.Key.StoreName}.");
            }
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        // Store selected shipping methods in session
        var selectedMethodsJson = JsonSerializer.Serialize(SelectedShippingMethods);
        HttpContext.Session.SetString("CheckoutShippingMethods", selectedMethodsJson);

        // Redirect to payment page
        return RedirectToPage("/Checkout/Payment");
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
