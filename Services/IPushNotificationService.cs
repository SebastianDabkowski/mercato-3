using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for push notification service operations.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Subscribes a user to push notifications.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="endpoint">The push service endpoint URL.</param>
    /// <param name="p256dh">The p256dh encryption key.</param>
    /// <param name="auth">The auth secret.</param>
    /// <param name="userAgent">Optional user agent of the browser.</param>
    /// <returns>The created subscription.</returns>
    Task<PushSubscription> SubscribeAsync(
        int userId,
        string endpoint,
        string p256dh,
        string auth,
        string? userAgent = null);

    /// <summary>
    /// Unsubscribes a user from push notifications for a specific endpoint.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="endpoint">The push service endpoint URL to unsubscribe.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> UnsubscribeAsync(int userId, string endpoint);

    /// <summary>
    /// Gets all active subscriptions for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A list of active push subscriptions.</returns>
    Task<List<PushSubscription>> GetUserSubscriptionsAsync(int userId);

    /// <summary>
    /// Sends a push notification to a user.
    /// </summary>
    /// <param name="userId">The ID of the user to send the notification to.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    /// <param name="url">Optional URL to navigate to when clicked.</param>
    /// <param name="icon">Optional icon URL.</param>
    /// <returns>The number of successful deliveries.</returns>
    Task<int> SendPushNotificationAsync(
        int userId,
        string title,
        string message,
        string? url = null,
        string? icon = null);

    /// <summary>
    /// Gets the VAPID public key for client-side subscription.
    /// </summary>
    /// <returns>The VAPID public key.</returns>
    string GetVapidPublicKey();
}
