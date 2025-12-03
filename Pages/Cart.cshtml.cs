using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages;

public class CartModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly ICartTotalsService _cartTotalsService;
    private readonly IGuestCartService _guestCartService;
    private readonly IPromoCodeService _promoCodeService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly ILogger<CartModel> _logger;

    public CartModel(
        ICartService cartService,
        ICartTotalsService cartTotalsService,
        IGuestCartService guestCartService,
        IPromoCodeService promoCodeService,
        IFeatureFlagService featureFlagService,
        ILogger<CartModel> logger)
    {
        _cartService = cartService;
        _cartTotalsService = cartTotalsService;
        _guestCartService = guestCartService;
        _promoCodeService = promoCodeService;
        _featureFlagService = featureFlagService;
        _logger = logger;
    }

    public Dictionary<Store, List<CartItem>> ItemsBySeller { get; set; } = new();
    public CartTotals CartTotals { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
    public bool IsPromoCodeFeatureEnabled { get; set; }
    public string? AppliedPromoCodeString { get; set; }

    [BindProperty]
    public string? PromoCodeInput { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var (userId, sessionId) = GetUserOrSessionId();
        ItemsBySeller = await _cartService.GetCartItemsBySellerAsync(userId, sessionId);
        
        IsPromoCodeFeatureEnabled = _featureFlagService.IsPromoCodeEnabled;

        // Get applied promo code from session if exists
        PromoCode? appliedPromoCode = null;
        var promoCodeFromSession = HttpContext.Session.GetString("AppliedPromoCode");
        if (!string.IsNullOrEmpty(promoCodeFromSession))
        {
            appliedPromoCode = await _promoCodeService.ValidatePromoCodeAsync(promoCodeFromSession, userId, sessionId);
            if (appliedPromoCode != null)
            {
                AppliedPromoCodeString = appliedPromoCode.Code;
            }
            else
            {
                // Promo code is no longer valid, clear it from session
                HttpContext.Session.Remove("AppliedPromoCode");
            }
        }

        // Calculate totals using the new service (no delivery address in cart view yet)
        CartTotals = await _cartTotalsService.CalculateCartTotalsAsync(userId, sessionId, null, false, appliedPromoCode);
        
        TotalAmount = CartTotals.TotalAmount;
        TotalItems = ItemsBySeller.Values
            .SelectMany(items => items)
            .Sum(item => item.Quantity);

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int cartItemId, int quantity)
    {
        try
        {
            if (quantity < 0)
            {
                return BadRequest("Quantity cannot be negative.");
            }

            await _cartService.UpdateCartItemQuantityAsync(cartItemId, quantity);
            return RedirectToPage();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("exceeds available stock"))
        {
            _logger.LogWarning(ex, "Insufficient stock for cart item {CartItemId}", cartItemId);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item quantity");
            TempData["ErrorMessage"] = "Failed to update quantity. Please try again.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostRemoveItemAsync(int cartItemId)
    {
        try
        {
            await _cartService.RemoveFromCartAsync(cartItemId);
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item");
            return BadRequest("Failed to remove item.");
        }
    }

    public async Task<IActionResult> OnPostClearCartAsync()
    {
        try
        {
            var (userId, sessionId) = GetUserOrSessionId();
            await _cartService.ClearCartAsync(userId, sessionId);
            
            // Clear promo code when cart is cleared
            HttpContext.Session.Remove("AppliedPromoCode");
            
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return BadRequest("Failed to clear cart.");
        }
    }

    public async Task<IActionResult> OnPostApplyPromoCodeAsync()
    {
        if (!_featureFlagService.IsPromoCodeEnabled)
        {
            TempData["ErrorMessage"] = "Promo code feature is not enabled.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(PromoCodeInput))
        {
            TempData["ErrorMessage"] = "Please enter a promo code.";
            return RedirectToPage();
        }

        var (userId, sessionId) = GetUserOrSessionId();

        // Validate the promo code
        var promoCode = await _promoCodeService.ValidatePromoCodeAsync(PromoCodeInput, userId, sessionId);

        if (promoCode == null)
        {
            TempData["ErrorMessage"] = "Invalid, expired, or ineligible promo code.";
            return RedirectToPage();
        }

        // Get cart items to validate minimum order requirement
        ItemsBySeller = await _cartService.GetCartItemsBySellerAsync(userId, sessionId);
        var itemsSubtotal = ItemsBySeller.SelectMany(kvp => kvp.Value).Sum(item => item.PriceAtAdd * item.Quantity);

        // Check if minimum order requirement is met
        if (promoCode.MinimumOrderSubtotal.HasValue && itemsSubtotal < promoCode.MinimumOrderSubtotal.Value)
        {
            TempData["ErrorMessage"] = $"Minimum order of {promoCode.MinimumOrderSubtotal.Value:C} required to use this promo code.";
            return RedirectToPage();
        }

        // Store promo code in session
        HttpContext.Session.SetString("AppliedPromoCode", promoCode.Code);
        TempData["SuccessMessage"] = $"Promo code '{promoCode.Code}' applied successfully!";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemovePromoCodeAsync()
    {
        HttpContext.Session.Remove("AppliedPromoCode");
        TempData["SuccessMessage"] = "Promo code removed.";
        return RedirectToPage();
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

        // Use persistent guest cart ID for anonymous users
        var guestCartId = _guestCartService.GetOrCreateGuestCartId();
        return (null, guestCartId);
    }
}
