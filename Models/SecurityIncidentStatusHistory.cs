using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a status change history entry for a security incident.
/// Maintains an audit trail of all status transitions.
/// </summary>
public class SecurityIncidentStatusHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the status history entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the security incident ID.
    /// </summary>
    public int SecurityIncidentId { get; set; }

    /// <summary>
    /// Gets or sets the security incident (navigation property).
    /// </summary>
    public SecurityIncident SecurityIncident { get; set; } = null!;

    /// <summary>
    /// Gets or sets the previous status.
    /// </summary>
    public SecurityIncidentStatus PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new status.
    /// </summary>
    public SecurityIncidentStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the user ID who changed the status.
    /// </summary>
    public int? ChangedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who changed the status (navigation property).
    /// </summary>
    public User? ChangedByUser { get; set; }

    /// <summary>
    /// Gets or sets notes about the status change.
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the status was changed.
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
