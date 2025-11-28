using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MercatoApp.Models;

/// <summary>
/// Represents an invitation for a new internal user to join a store.
/// </summary>
public class StoreUserInvitation
{
    /// <summary>
    /// Gets or sets the unique identifier for the invitation.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID for which the user is invited.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store navigation property.
    /// </summary>
    [ForeignKey(nameof(StoreId))]
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the email address of the invited user.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role to be assigned to the user upon acceptance.
    /// </summary>
    public StoreRole Role { get; set; }

    /// <summary>
    /// Gets or sets the unique invitation token.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string InvitationToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the invitation.
    /// </summary>
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    /// <summary>
    /// Gets or sets the date and time when the invitation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the invitation expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the invitation was accepted.
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who sent the invitation.
    /// </summary>
    public int InvitedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who sent the invitation (navigation property).
    /// </summary>
    [ForeignKey(nameof(InvitedByUserId))]
    public User InvitedByUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ID of the user who accepted the invitation (after acceptance).
    /// </summary>
    public int? AcceptedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who accepted the invitation (navigation property).
    /// </summary>
    [ForeignKey(nameof(AcceptedByUserId))]
    public User? AcceptedByUser { get; set; }
}

/// <summary>
/// Represents the status of a store user invitation.
/// </summary>
public enum InvitationStatus
{
    /// <summary>
    /// Invitation has been sent and is awaiting acceptance.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Invitation has been accepted by the user.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Invitation has expired.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Invitation has been revoked by the store owner.
    /// </summary>
    Revoked = 3
}
