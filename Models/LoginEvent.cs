using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MercatoApp.Models;

/// <summary>
/// Represents the type of login event.
/// </summary>
public enum LoginEventType
{
    /// <summary>
    /// Standard email/password login attempt.
    /// </summary>
    PasswordLogin,

    /// <summary>
    /// Social login attempt (Google, Facebook, etc.).
    /// </summary>
    SocialLogin,

    /// <summary>
    /// Two-factor authentication verification.
    /// </summary>
    TwoFactorVerification,

    /// <summary>
    /// Session validation during request processing.
    /// </summary>
    SessionValidation,

    /// <summary>
    /// User logout event.
    /// </summary>
    Logout,

    /// <summary>
    /// Password reset initiated.
    /// </summary>
    PasswordReset,

    /// <summary>
    /// Account locked due to too many failed attempts.
    /// </summary>
    AccountLocked
}

/// <summary>
/// Represents a login event for security auditing and history tracking.
/// Login events follow retention rules and support security alerting.
/// </summary>
public class LoginEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for the login event.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with this event.
    /// May be null for failed login attempts with unknown email.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the User.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the email address used in the login attempt.
    /// Stored for audit purposes even when user is not found.
    /// </summary>
    [MaxLength(256)]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the type of login event.
    /// </summary>
    public LoginEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets whether the login attempt was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the failure reason if the login was not successful.
    /// </summary>
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the login was attempted.
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string from the login request.
    /// </summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the geographic location derived from IP address.
    /// Populated asynchronously when geolocation service is available.
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the country code derived from IP address (ISO 3166-1 alpha-2).
    /// </summary>
    [MaxLength(2)]
    public string? CountryCode { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the event occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether this event triggered a security alert.
    /// </summary>
    public bool TriggeredSecurityAlert { get; set; }

    /// <summary>
    /// Gets or sets the session token associated with this login event (if successful).
    /// Useful for correlating login events with sessions.
    /// </summary>
    [MaxLength(256)]
    public string? SessionToken { get; set; }
}
