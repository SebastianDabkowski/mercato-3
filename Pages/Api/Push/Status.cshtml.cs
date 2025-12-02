using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Api.Push;

/// <summary>
/// API endpoint for getting push notification status and VAPID public key.
/// </summary>
public class StatusModel : PageModel
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<StatusModel> _logger;

    public StatusModel(
        IPushNotificationService pushNotificationService,
        ILogger<StatusModel> logger)
    {
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Check if user is authenticated
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Unauthorized();
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var subscriptions = await _pushNotificationService.GetUserSubscriptionsAsync(userId);
            var vapidPublicKey = _pushNotificationService.GetVapidPublicKey();

            return new JsonResult(new
            {
                vapidPublicKey,
                subscriptionCount = subscriptions.Count,
                subscriptions = subscriptions.Select(s => new
                {
                    id = s.Id,
                    endpoint = s.Endpoint,
                    createdAt = s.CreatedAt,
                    lastUsedAt = s.LastUsedAt,
                    userAgent = s.UserAgent
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting push notification status for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to get push notification status" });
        }
    }
}
