using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for notification service operations.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a new notification for a user.
    /// </summary>
    /// <param name="userId">The ID of the user to notify.</param>
    /// <param name="type">The type of notification.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    /// <param name="relatedUrl">Optional URL to navigate to when clicked.</param>
    /// <param name="relatedEntityId">Optional related entity ID.</param>
    /// <param name="relatedEntityType">Optional related entity type.</param>
    /// <returns>The created notification.</returns>
    Task<Notification> CreateNotificationAsync(
        int userId,
        NotificationType type,
        string title,
        string message,
        string? relatedUrl = null,
        int? relatedEntityId = null,
        string? relatedEntityType = null);

    /// <summary>
    /// Gets recent notifications for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="pageNumber">Page number for pagination (starts at 1).</param>
    /// <param name="pageSize">Number of notifications per page.</param>
    /// <param name="unreadOnly">Whether to return only unread notifications.</param>
    /// <returns>A list of notifications.</returns>
    Task<List<Notification>> GetNotificationsAsync(
        int userId,
        int pageNumber = 1,
        int pageSize = 20,
        bool unreadOnly = false);

    /// <summary>
    /// Gets the count of unread notifications for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The count of unread notifications.</returns>
    Task<int> GetUnreadCountAsync(int userId);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    /// <param name="notificationId">The ID of the notification.</param>
    /// <param name="userId">The ID of the user (for authorization).</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> MarkAsReadAsync(int notificationId, int userId);

    /// <summary>
    /// Marks all notifications as read for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The number of notifications marked as read.</returns>
    Task<int> MarkAllAsReadAsync(int userId);

    /// <summary>
    /// Gets the total count of notifications for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="unreadOnly">Whether to count only unread notifications.</param>
    /// <returns>The total count of notifications.</returns>
    Task<int> GetTotalCountAsync(int userId, bool unreadOnly = false);
}
