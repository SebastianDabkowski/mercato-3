using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing user notifications.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Notification> CreateNotificationAsync(
        int userId,
        NotificationType type,
        string title,
        string message,
        string? relatedUrl = null,
        int? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedUrl = relatedUrl,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created notification {NotificationId} of type {Type} for user {UserId}",
            notification.Id,
            type,
            userId);

        return notification;
    }

    /// <inheritdoc />
    public async Task<List<Notification>> GetNotificationsAsync(
        int userId,
        int pageNumber = 1,
        int pageSize = 20,
        bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return notifications;
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId)
            .FirstOrDefaultAsync();

        if (notification == null)
        {
            _logger.LogWarning(
                "Notification {NotificationId} not found for user {UserId}",
                notificationId,
                userId);
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Marked notification {NotificationId} as read for user {UserId}",
                notificationId,
                userId);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unreadNotifications.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Marked {Count} notifications as read for user {UserId}",
            unreadNotifications.Count,
            userId);

        return unreadNotifications.Count;
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(int userId, bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.CountAsync();
    }
}
