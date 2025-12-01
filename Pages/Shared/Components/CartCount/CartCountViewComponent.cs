using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MercatoApp.Pages.Shared.Components.CartCount;

public class CartCountViewComponent : ViewComponent
{
    private readonly ICartService _cartService;

    public CartCountViewComponent(ICartService cartService)
    {
        _cartService = cartService;
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
