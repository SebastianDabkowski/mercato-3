using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace MercatoApp.Pages.Checkout;

public class PaymentModel : PageModel
{
    private readonly IAddressService _addressService;
    private readonly ICartService _cartService;
    private readonly IGuestCartService _guestCartService;
    private readonly IShippingMethodService _shippingMethodService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentModel> _logger;

    public PaymentModel(
        IAddressService addressService,
        ICartService cartService,
        IGuestCartService guestCartService,
        IShippingMethodService shippingMethodService,
        IPaymentService paymentService,
        ILogger<PaymentModel> logger)
    {
        _addressService = addressService;
        _cartService = cartService;
        _guestCartService = guestCartService;
        _shippingMethodService = shippingMethodService;
        _paymentService = paymentService;
        _logger = logger;
    }

    public Address? DeliveryAddress { get; set; }
    public Dictionary<Store, List<CartItem>> ItemsBySeller { get; set; } = new();
    public Dictionary<int, ShippingMethod> SelectedShippingMethodsBySeller { get; set; } = new();
    public List<PaymentMethod> AvailablePaymentMethods { get; set; } = new();
    public decimal ItemsSubtotal { get; set; }
    public decimal TotalShipping { get; set; }
    public decimal TotalAmount { get; set; }

    [BindProperty]
    public int SelectedPaymentMethodId { get; set; }

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

        // Get selected shipping methods from session
        var selectedMethodsJson = HttpContext.Session.GetString("CheckoutShippingMethods");
        if (string.IsNullOrEmpty(selectedMethodsJson))
        {
            TempData["ErrorMessage"] = "Please select shipping methods.";
            return RedirectToPage("/Checkout/Shipping");
        }

        var selectedMethods = JsonSerializer.Deserialize<Dictionary<int, int>>(selectedMethodsJson);
        if (selectedMethods == null)
        {
            TempData["ErrorMessage"] = "Invalid shipping method selection.";
            return RedirectToPage("/Checkout/Shipping");
        }

        // Load selected shipping methods and calculate costs
        foreach (var sellerGroup in ItemsBySeller)
        {
            var store = sellerGroup.Key;
            var items = sellerGroup.Value;

            if (selectedMethods.ContainsKey(store.Id))
            {
                var shippingMethod = await _shippingMethodService.GetShippingMethodByIdAsync(selectedMethods[store.Id]);
                if (shippingMethod != null)
                {
                    SelectedShippingMethodsBySeller[store.Id] = shippingMethod;
                    var cost = await _shippingMethodService.CalculateShippingCostAsync(shippingMethod.Id, items);
                    TotalShipping += cost;
                }
            }
        }

        // Calculate totals
        ItemsSubtotal = ItemsBySeller.SelectMany(s => s.Value).Sum(i => i.PriceAtAdd * i.Quantity);
        TotalAmount = ItemsSubtotal + TotalShipping;

        // Load available payment methods
        AvailablePaymentMethods = await _paymentService.GetOrCreateDefaultPaymentMethodsAsync();

        // Try to load previously selected payment method from session
        var selectedPaymentMethodId = HttpContext.Session.GetInt32("CheckoutPaymentMethodId");
        if (selectedPaymentMethodId.HasValue)
        {
            SelectedPaymentMethodId = selectedPaymentMethodId.Value;
        }
        else if (AvailablePaymentMethods.Any())
        {
            // Pre-select the first payment method
            SelectedPaymentMethodId = AvailablePaymentMethods.First().Id;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (SelectedPaymentMethodId == 0)
        {
            ModelState.AddModelError(nameof(SelectedPaymentMethodId), "Please select a payment method.");
            await OnGetAsync();
            return Page();
        }

        // Validate payment method exists
        var paymentMethod = await _paymentService.GetPaymentMethodByIdAsync(SelectedPaymentMethodId);
        if (paymentMethod == null)
        {
            ModelState.AddModelError(nameof(SelectedPaymentMethodId), "Invalid payment method.");
            await OnGetAsync();
            return Page();
        }

        // Store selected payment method in session
        HttpContext.Session.SetInt32("CheckoutPaymentMethodId", SelectedPaymentMethodId);

        // Redirect to review page
        return RedirectToPage("/Checkout/Review");
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
