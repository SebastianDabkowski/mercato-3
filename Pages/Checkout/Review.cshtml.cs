using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace MercatoApp.Pages.Checkout;

public class ReviewModel : PageModel
{
    private readonly IAddressService _addressService;
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly IShippingMethodService _shippingMethodService;
    private readonly IPaymentService _paymentService;
    private readonly IGuestCartService _guestCartService;
    private readonly ILogger<ReviewModel> _logger;

    public ReviewModel(
        IAddressService addressService,
        IOrderService orderService,
        ICartService cartService,
        IShippingMethodService shippingMethodService,
        IPaymentService paymentService,
        IGuestCartService guestCartService,
        ILogger<ReviewModel> logger)
    {
        _addressService = addressService;
        _orderService = orderService;
        _cartService = cartService;
        _shippingMethodService = shippingMethodService;
        _paymentService = paymentService;
        _guestCartService = guestCartService;
        _logger = logger;
    }

    public Address? DeliveryAddress { get; set; }
    public Dictionary<Store, List<CartItem>> ItemsBySeller { get; set; } = new();
    public Dictionary<int, ShippingMethod> SelectedShippingMethodsBySeller { get; set; } = new();
    public Dictionary<int, decimal> ShippingCostsBySeller { get; set; } = new();
    public PaymentMethod? SelectedPaymentMethod { get; set; }
    public decimal ItemsSubtotal { get; set; }
    public decimal TotalShipping { get; set; }
    public decimal TotalAmount { get; set; }
    public string? GuestEmail { get; set; }

    [BindProperty]
    public string? GuestEmailInput { get; set; }

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
        var selectedShippingMethodsJson = HttpContext.Session.GetString("CheckoutShippingMethods");
        if (string.IsNullOrEmpty(selectedShippingMethodsJson))
        {
            TempData["ErrorMessage"] = "Please select shipping methods.";
            return RedirectToPage("/Checkout/Shipping");
        }

        var selectedShippingMethods = JsonSerializer.Deserialize<Dictionary<int, int>>(selectedShippingMethodsJson);
        if (selectedShippingMethods == null)
        {
            TempData["ErrorMessage"] = "Invalid shipping method selection.";
            return RedirectToPage("/Checkout/Shipping");
        }

        // Load selected shipping methods and calculate costs
        foreach (var sellerGroup in ItemsBySeller)
        {
            var store = sellerGroup.Key;
            var items = sellerGroup.Value;

            if (selectedShippingMethods.ContainsKey(store.Id))
            {
                var shippingMethod = await _shippingMethodService.GetShippingMethodByIdAsync(selectedShippingMethods[store.Id]);
                if (shippingMethod != null)
                {
                    SelectedShippingMethodsBySeller[store.Id] = shippingMethod;
                    var cost = await _shippingMethodService.CalculateShippingCostAsync(shippingMethod.Id, items);
                    ShippingCostsBySeller[store.Id] = cost;
                    TotalShipping += cost;
                }
            }
        }

        // Get selected payment method from session
        var selectedPaymentMethodId = HttpContext.Session.GetInt32("CheckoutPaymentMethodId");
        if (!selectedPaymentMethodId.HasValue)
        {
            TempData["ErrorMessage"] = "Please select a payment method.";
            return RedirectToPage("/Checkout/Payment");
        }

        SelectedPaymentMethod = await _paymentService.GetPaymentMethodByIdAsync(selectedPaymentMethodId.Value);
        if (SelectedPaymentMethod == null)
        {
            TempData["ErrorMessage"] = "Payment method not found.";
            return RedirectToPage("/Checkout/Payment");
        }

        // Calculate totals
        ItemsSubtotal = ItemsBySeller.SelectMany(s => s.Value).Sum(i => i.PriceAtAdd * i.Quantity);
        TotalAmount = ItemsSubtotal + TotalShipping;

        // Get guest email if available
        if (!userId.HasValue)
        {
            GuestEmail = HttpContext.Session.GetString("CheckoutGuestEmail");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var (userId, sessionId) = GetUserOrSessionId();

        // Get the selected address
        var addressId = HttpContext.Session.GetInt32("CheckoutAddressId");
        if (!addressId.HasValue)
        {
            TempData["ErrorMessage"] = "Please select a delivery address.";
            return RedirectToPage("/Checkout/Address");
        }

        // Get selected shipping methods
        var selectedShippingMethodsJson = HttpContext.Session.GetString("CheckoutShippingMethods");
        if (string.IsNullOrEmpty(selectedShippingMethodsJson))
        {
            TempData["ErrorMessage"] = "Please select shipping methods.";
            return RedirectToPage("/Checkout/Shipping");
        }

        var selectedShippingMethods = JsonSerializer.Deserialize<Dictionary<int, int>>(selectedShippingMethodsJson);
        if (selectedShippingMethods == null)
        {
            TempData["ErrorMessage"] = "Invalid shipping method selection.";
            return RedirectToPage("/Checkout/Shipping");
        }

        // Get selected payment method
        var paymentMethodId = HttpContext.Session.GetInt32("CheckoutPaymentMethodId");
        if (!paymentMethodId.HasValue)
        {
            TempData["ErrorMessage"] = "Please select a payment method.";
            return RedirectToPage("/Checkout/Payment");
        }

        // For guest checkout, validate email
        string? guestEmail = null;
        if (!userId.HasValue)
        {
            if (string.IsNullOrWhiteSpace(GuestEmailInput))
            {
                ModelState.AddModelError(nameof(GuestEmailInput), "Email is required for guest checkout.");
                await OnGetAsync();
                return Page();
            }

            guestEmail = GuestEmailInput;
            HttpContext.Session.SetString("CheckoutGuestEmail", guestEmail);
        }

        try
        {
            // Create the order
            var order = await _orderService.CreateOrderFromCartAsync(
                userId, 
                sessionId, 
                addressId.Value, 
                selectedShippingMethods,
                paymentMethodId.Value,
                guestEmail);

            // Create payment transaction and initiate payment
            var paymentTransaction = await _paymentService.CreatePaymentTransactionAsync(
                order.Id, 
                paymentMethodId.Value, 
                order.TotalAmount);

            var paymentRedirectUrl = await _paymentService.InitiatePaymentAsync(paymentTransaction.Id);

            // Clear checkout session data
            HttpContext.Session.Remove("CheckoutAddressId");
            HttpContext.Session.Remove("CheckoutShippingMethods");
            HttpContext.Session.Remove("CheckoutPaymentMethodId");
            HttpContext.Session.Remove("CheckoutGuestEmail");

            // If payment requires redirect (e.g., credit card, PayPal), redirect to payment page
            if (!string.IsNullOrEmpty(paymentRedirectUrl))
            {
                return Redirect(paymentRedirectUrl);
            }

            // Otherwise (e.g., cash on delivery), proceed directly to confirmation
            return RedirectToPage("/Checkout/Confirmation", new { orderId = order.Id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error creating order");
            TempData["ErrorMessage"] = ex.Message;
            await OnGetAsync();
            return Page();
        }
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
