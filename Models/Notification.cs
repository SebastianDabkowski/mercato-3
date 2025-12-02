using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a notification sent to a user.
/// </summary>
public class Notification
{
    /// <summary>
    /// Gets or sets the unique identifier for the notification.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who receives this notification.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the user who receives this notification.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of notification.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to navigate to when the notification is clicked.
    /// </summary>
    [MaxLength(500)]
    public string? RelatedUrl { get; set; }

    /// <summary>
    /// Gets or sets whether the notification has been read.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the notification was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the notification was read.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Gets or sets the related entity ID (e.g., OrderId, ReturnRequestId, PayoutId).
    /// </summary>
    public int? RelatedEntityId { get; set; }

    /// <summary>
    /// Gets or sets the related entity type (e.g., "Order", "ReturnRequest", "Payout").
    /// </summary>
    [MaxLength(50)]
    public string? RelatedEntityType { get; set; }
}
