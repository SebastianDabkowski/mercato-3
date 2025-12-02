using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Checkout;

public class ReviewModel : PageModel
{
    private readonly IAddressService _addressService;
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly ICartTotalsService _cartTotalsService;
    private readonly IGuestCartService _guestCartService;
    private readonly ILogger<ReviewModel> _logger;

    public ReviewModel(
        IAddressService addressService,
        IOrderService orderService,
        ICartService cartService,
        ICartTotalsService cartTotalsService,
        IGuestCartService guestCartService,
        ILogger<ReviewModel> logger)
    {
        _addressService = addressService;
        _orderService = orderService;
        _cartService = cartService;
        _cartTotalsService = cartTotalsService;
        _guestCartService = guestCartService;
        _logger = logger;
    }

    public Address? DeliveryAddress { get; set; }
    public Dictionary<Store, List<CartItem>> ItemsBySeller { get; set; } = new();
    public CartTotals CartTotals { get; set; } = new();
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

        // Calculate totals
        CartTotals = await _cartTotalsService.CalculateCartTotalsAsync(userId, sessionId);

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
            var order = await _orderService.CreateOrderFromCartAsync(userId, sessionId, addressId.Value, guestEmail);

            // Clear checkout session data
            HttpContext.Session.Remove("CheckoutAddressId");
            HttpContext.Session.Remove("CheckoutGuestEmail");

            // Redirect to confirmation page
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
