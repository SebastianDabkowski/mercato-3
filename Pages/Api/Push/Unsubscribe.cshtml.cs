using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace MercatoApp.Pages.Api.Push;

/// <summary>
/// API endpoint for unsubscribing from push notifications.
/// </summary>
public class UnsubscribeModel : PageModel
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<UnsubscribeModel> _logger;

    public UnsubscribeModel(
        IPushNotificationService pushNotificationService,
        ILogger<UnsubscribeModel> logger)
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
            var data = JsonSerializer.Deserialize<UnsubscribeData>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data == null || string.IsNullOrEmpty(data.Endpoint))
            {
                return BadRequest(new { error = "Invalid request data" });
            }

            var success = await _pushNotificationService.UnsubscribeAsync(userId, data.Endpoint);

            if (success)
            {
                _logger.LogInformation(
                    "User {UserId} unsubscribed from push notifications",
                    userId);

                return new JsonResult(new { success = true });
            }
            else
            {
                return NotFound(new { error = "Subscription not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing user {UserId} from push notifications", userId);
            return StatusCode(500, new { error = "Failed to unsubscribe from push notifications" });
        }
    }

    private class UnsubscribeData
    {
        public string Endpoint { get; set; } = string.Empty;
    }
}
