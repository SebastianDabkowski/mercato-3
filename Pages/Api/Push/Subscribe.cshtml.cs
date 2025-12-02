using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace MercatoApp.Pages.Api.Push;

/// <summary>
/// API endpoint for subscribing to push notifications.
/// </summary>
public class SubscribeModel : PageModel
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<SubscribeModel> _logger;

    public SubscribeModel(
        IPushNotificationService pushNotificationService,
        ILogger<SubscribeModel> logger)
    {
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
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
            // Read the request body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var subscriptionData = JsonSerializer.Deserialize<SubscriptionData>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (subscriptionData == null ||
                string.IsNullOrEmpty(subscriptionData.Endpoint) ||
                subscriptionData.Keys == null ||
                string.IsNullOrEmpty(subscriptionData.Keys.P256dh) ||
                string.IsNullOrEmpty(subscriptionData.Keys.Auth))
            {
                return BadRequest(new { error = "Invalid subscription data" });
            }

            var userAgent = Request.Headers.UserAgent.ToString();

            var subscription = await _pushNotificationService.SubscribeAsync(
                userId,
                subscriptionData.Endpoint,
                subscriptionData.Keys.P256dh,
                subscriptionData.Keys.Auth,
                userAgent);

            _logger.LogInformation(
                "User {UserId} subscribed to push notifications",
                userId);

            return new JsonResult(new
            {
                success = true,
                subscriptionId = subscription.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing user {UserId} to push notifications", userId);
            return StatusCode(500, new { error = "Failed to subscribe to push notifications" });
        }
    }

    private class SubscriptionData
    {
        public string Endpoint { get; set; } = string.Empty;
        public SubscriptionKeys? Keys { get; set; }
    }

    private class SubscriptionKeys
    {
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }
}
