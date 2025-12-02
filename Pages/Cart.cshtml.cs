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
    private readonly ILogger<CartModel> _logger;

    public CartModel(
        ICartService cartService,
        ICartTotalsService cartTotalsService,
        ILogger<CartModel> logger)
    {
        _cartService = cartService;
        _cartTotalsService = cartTotalsService;
        _logger = logger;
    }

    public Dictionary<Store, List<CartItem>> ItemsBySeller { get; set; } = new();
    public CartTotals CartTotals { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var (userId, sessionId) = GetUserOrSessionId();
        ItemsBySeller = await _cartService.GetCartItemsBySellerAsync(userId, sessionId);
        
        // Calculate totals using the new service
        CartTotals = await _cartTotalsService.CalculateCartTotalsAsync(userId, sessionId);
        
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
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return BadRequest("Failed to clear cart.");
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

        // Ensure session is loaded
        _ = HttpContext.Session.GetString("_init");
        
        // Use HTTP session ID for anonymous users
        var sessionId = HttpContext.Session.Id;
        if (string.IsNullOrEmpty(sessionId))
        {
            // Create a session if it doesn't exist
            HttpContext.Session.SetString("_init", "1");
            sessionId = HttpContext.Session.Id;
        }

        return (null, sessionId);
    }
}
