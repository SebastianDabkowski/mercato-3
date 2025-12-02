using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MercatoApp.Pages.Shared.Components.NotificationCenter;

/// <summary>
/// View component for displaying notification icon with unread count.
/// </summary>
public class NotificationCenterViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;

    public NotificationCenterViewComponent(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Only show notification icon for authenticated users
        if (UserClaimsPrincipal?.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var userIdClaim = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Content(string.Empty);
        }

        var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
        return View(unreadCount);
    }
}
