using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MercatoApp.Pages.Shared.Components.CartCount;

public class CartCountViewComponent : ViewComponent
{
    private readonly ICartService _cartService;
    private readonly IGuestCartService _guestCartService;

    public CartCountViewComponent(ICartService cartService, IGuestCartService guestCartService)
    {
        _cartService = cartService;
        _guestCartService = guestCartService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var (userId, sessionId) = GetUserOrSessionId();
        var count = await _cartService.GetCartItemCountAsync(userId, sessionId);
        return View(count);
    }

    private (int? userId, string? sessionId) GetUserOrSessionId()
    {
        if (UserClaimsPrincipal?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
