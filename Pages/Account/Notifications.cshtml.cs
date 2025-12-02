using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

/// <summary>
/// Page model for the notifications page.
/// </summary>
[Authorize]
public class NotificationsModel : PageModel
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsModel> _logger;

    public NotificationsModel(
        INotificationService notificationService,
        ILogger<NotificationsModel> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of notifications.
    /// </summary>
    public List<Notification> Notifications { get; set; } = new();

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to show only unread notifications.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool UnreadOnly { get; set; }

    /// <summary>
    /// Gets or sets the total count of notifications.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the count of unread notifications.
    /// </summary>
    public int UnreadCount { get; set; }

    private const int PageSize = 20;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            Notifications = await _notificationService.GetNotificationsAsync(
                userId.Value,
                PageNumber,
                PageSize,
                UnreadOnly);

            TotalCount = await _notificationService.GetTotalCountAsync(userId.Value, UnreadOnly);
            UnreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading notifications for user {UserId}", userId);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(int notificationId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            await _notificationService.MarkAsReadAsync(notificationId, userId.Value);
            return RedirectToPage(new { PageNumber, UnreadOnly });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            return RedirectToPage(new { PageNumber, UnreadOnly });
        }
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            await _notificationService.MarkAllAsReadAsync(userId.Value);
            return RedirectToPage(new { PageNumber, UnreadOnly });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return RedirectToPage(new { PageNumber, UnreadOnly });
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Gets a friendly display time for a notification.
    /// </summary>
    public string GetFriendlyTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)}w ago";
        
        return dateTime.ToString("MMM dd, yyyy");
    }

    /// <summary>
    /// Gets the icon class for a notification type.
    /// </summary>
    public string GetNotificationIcon(NotificationType type)
    {
        return type switch
        {
            NotificationType.OrderPlaced => "bi-bag-check",
            NotificationType.OrderStatusUpdate => "bi-box-seam",
            NotificationType.OrderShipped => "bi-truck",
            NotificationType.OrderDelivered => "bi-check-circle",
            NotificationType.ReturnRequest => "bi-arrow-return-left",
            NotificationType.ReturnStatusUpdate => "bi-arrow-repeat",
            NotificationType.PayoutScheduled => "bi-cash-stack",
            NotificationType.PayoutCompleted => "bi-cash-coin",
            NotificationType.NewMessage => "bi-chat-dots",
            NotificationType.SystemUpdate => "bi-info-circle",
            NotificationType.ProductReview => "bi-star",
            NotificationType.SellerRating => "bi-award",
            _ => "bi-bell"
        };
    }
}
