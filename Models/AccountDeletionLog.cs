using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an audit log entry for account deletion events.
/// </summary>
public class AccountDeletionLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the deletion log entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the account that was deleted.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the anonymized identifier used to replace user email.
    /// Format: "deleted-user-{UserId}@anonymized.local"
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string AnonymizedEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user type at the time of deletion.
    /// </summary>
    public UserType UserType { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account deletion was requested.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the account deletion was completed.
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the IP address from which the deletion was requested.
    /// </summary>
    [MaxLength(45)]
    public string? RequestIpAddress { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the deletion (e.g., reason provided by user).
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the count of orders associated with the account at deletion time.
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// Gets or sets the count of return requests associated with the account at deletion time.
    /// </summary>
    public int ReturnRequestCount { get; set; }
}
