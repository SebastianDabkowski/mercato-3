using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MercatoApp.Models;

/// <summary>
/// Represents a user session for authenticated access.
/// Sessions are stored in the database to support horizontal scaling.
/// </summary>
public class UserSession
{
    /// <summary>
    /// Gets or sets the unique identifier for the session.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the secure session token.
    /// This is a cryptographically random value used to identify the session.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID associated with this session.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the User.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets when the session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the session expires.
    /// Sessions that exceed this time require re-authentication.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets when the session was last accessed.
    /// Used for sliding expiration.
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the session is still valid.
    /// Set to false when the user logs out or the session is invalidated.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Gets or sets the IP address from which the session was created.
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string from the session creation request.
    /// </summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the security stamp at the time of session creation.
    /// If the user's security stamp changes (e.g., password change), this session becomes invalid.
    /// </summary>
    [MaxLength(256)]
    public string? SecurityStamp { get; set; }

    /// <summary>
    /// Gets or sets whether the session should persist (Remember Me functionality).
    /// </summary>
    public bool IsPersistent { get; set; }
}
