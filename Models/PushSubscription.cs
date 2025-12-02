using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a user's web push notification subscription.
/// </summary>
public class PushSubscription
{
    /// <summary>
    /// Gets or sets the unique identifier for the subscription.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns this subscription.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the user who owns this subscription.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the push service endpoint URL.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the p256dh key for encryption.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string P256dh { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the auth secret for encryption.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Auth { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the subscription was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the subscription was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the user agent of the browser that created this subscription.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
}
