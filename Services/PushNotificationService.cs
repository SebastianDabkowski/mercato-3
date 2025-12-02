using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing web push notifications.
/// </summary>
public class PushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _vapidPublicKey;
    private readonly string _vapidPrivateKey;
    private readonly string _vapidSubject;

    public PushNotificationService(
        ApplicationDbContext context,
        ILogger<PushNotificationService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;

        // Get VAPID keys from configuration
        _vapidPublicKey = _configuration["Push:VapidPublicKey"] ?? GenerateVapidKeys().publicKey;
        _vapidPrivateKey = _configuration["Push:VapidPrivateKey"] ?? GenerateVapidKeys().privateKey;
        _vapidSubject = _configuration["Push:VapidSubject"] ?? "mailto:admin@mercato.app";

        // Log if using generated keys (for development)
        if (string.IsNullOrEmpty(_configuration["Push:VapidPublicKey"]))
        {
            _logger.LogWarning("VAPID keys not configured. Using auto-generated keys. For production, configure keys in appsettings.json");
            _logger.LogInformation("Generated VAPID Public Key: {PublicKey}", _vapidPublicKey);
            _logger.LogInformation("Generated VAPID Private Key: {PrivateKey}", _vapidPrivateKey);
        }
    }

    /// <inheritdoc />
    public async Task<Models.PushSubscription> SubscribeAsync(
        int userId,
        string endpoint,
        string p256dh,
        string auth,
        string? userAgent = null)
    {
        // Check if subscription already exists for this endpoint
        var existingSubscription = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

        if (existingSubscription != null)
        {
            // Update existing subscription
            existingSubscription.P256dh = p256dh;
            existingSubscription.Auth = auth;
            existingSubscription.UserAgent = userAgent;
            existingSubscription.IsActive = true;
            existingSubscription.LastUsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated push subscription {SubscriptionId} for user {UserId}",
                existingSubscription.Id,
                userId);

            return existingSubscription;
        }

        // Create new subscription
        var subscription = new Models.PushSubscription
        {
            UserId = userId,
            Endpoint = endpoint,
            P256dh = p256dh,
            Auth = auth,
            UserAgent = userAgent,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.PushSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created push subscription {SubscriptionId} for user {UserId}",
            subscription.Id,
            userId);

        return subscription;
    }

    /// <inheritdoc />
    public async Task<bool> UnsubscribeAsync(int userId, string endpoint)
    {
        var subscription = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

        if (subscription == null)
        {
            _logger.LogWarning(
                "Push subscription not found for user {UserId} with endpoint {Endpoint}",
                userId,
                endpoint);
            return false;
        }

        subscription.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Deactivated push subscription {SubscriptionId} for user {UserId}",
            subscription.Id,
            userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<Models.PushSubscription>> GetUserSubscriptionsAsync(int userId)
    {
        return await _context.PushSubscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> SendPushNotificationAsync(
        int userId,
        string title,
        string message,
        string? url = null,
        string? icon = null)
    {
        var subscriptions = await GetUserSubscriptionsAsync(userId);

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug("No active push subscriptions for user {UserId}", userId);
            return 0;
        }

        var payload = JsonSerializer.Serialize(new
        {
            title,
            body = message,
            url,
            icon = icon ?? "/favicon.ico"
        });

        var webPushClient = new WebPush.WebPushClient();
        var vapidDetails = new WebPush.VapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey);

        var successCount = 0;

        foreach (var subscription in subscriptions)
        {
            try
            {
                var pushSubscription = new WebPush.PushSubscription(
                    subscription.Endpoint,
                    subscription.P256dh,
                    subscription.Auth);

                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);

                subscription.LastUsedAt = DateTime.UtcNow;
                successCount++;

                _logger.LogInformation(
                    "Sent push notification to subscription {SubscriptionId} for user {UserId}",
                    subscription.Id,
                    userId);
            }
            catch (WebPush.WebPushException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send push notification to subscription {SubscriptionId} for user {UserId}. Status: {StatusCode}",
                    subscription.Id,
                    userId,
                    ex.StatusCode);

                // If subscription is no longer valid (410 Gone), deactivate it
                if (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    subscription.IsActive = false;
                    _logger.LogInformation(
                        "Deactivated invalid push subscription {SubscriptionId}",
                        subscription.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error sending push notification to subscription {SubscriptionId}",
                    subscription.Id);
            }
        }

        await _context.SaveChangesAsync();

        return successCount;
    }

    /// <inheritdoc />
    public string GetVapidPublicKey()
    {
        return _vapidPublicKey;
    }

    private (string publicKey, string privateKey) GenerateVapidKeys()
    {
        var vapidKeys = WebPush.VapidHelper.GenerateVapidKeys();
        return (vapidKeys.PublicKey, vapidKeys.PrivateKey);
    }
}
