using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an admin action taken on a return/complaint case for audit trail and compliance.
/// </summary>
public class ReturnRequestAdminAction
{
    /// <summary>
    /// Gets or sets the unique identifier for the admin action.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the return request ID this action applies to.
    /// </summary>
    public int ReturnRequestId { get; set; }

    /// <summary>
    /// Gets or sets the return request (navigation property).
    /// </summary>
    public ReturnRequest ReturnRequest { get; set; } = null!;

    /// <summary>
    /// Gets or sets the admin user ID who performed the action.
    /// </summary>
    public int AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the admin user (navigation property).
    /// </summary>
    public User AdminUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the action type performed by the admin.
    /// </summary>
    public AdminActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the previous status before the admin action.
    /// </summary>
    public ReturnStatus? PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new status after the admin action.
    /// </summary>
    public ReturnStatus? NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the admin's notes/decision details.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolution type if applicable (for admin override decisions).
    /// </summary>
    public ResolutionType? ResolutionType { get; set; }

    /// <summary>
    /// Gets or sets the resolution amount if applicable (for admin-imposed refunds).
    /// </summary>
    public decimal? ResolutionAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the action was taken.
    /// </summary>
    public DateTime ActionTakenAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether notifications were sent to buyer and seller.
    /// </summary>
    public bool NotificationsSent { get; set; }
}
