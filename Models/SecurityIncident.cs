using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a security incident detected by the system.
/// Supports incident tracking, status management, and compliance reporting.
/// </summary>
public class SecurityIncident
{
    /// <summary>
    /// Gets or sets the unique identifier for the security incident.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the incident number for external reference.
    /// Format: SI-YYYYMMDD-XXXXX (e.g., SI-20250103-00001).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string IncidentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of security incident.
    /// </summary>
    public SecurityIncidentType IncidentType { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the incident.
    /// </summary>
    public SecurityIncidentSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the current status of the incident.
    /// </summary>
    public SecurityIncidentStatus Status { get; set; } = SecurityIncidentStatus.New;

    /// <summary>
    /// Gets or sets the detection rule or mechanism that triggered this incident.
    /// For example: "Multiple failed logins (5+ in 10 minutes)".
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string DetectionRule { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source IP address or identifier related to the incident.
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with the incident (if applicable).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user associated with the incident (navigation property).
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets detailed description of the incident.
    /// </summary>
    [MaxLength(2000)]
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the incident as JSON.
    /// Can include: affected resources, attack vectors, relevant logs, etc.
    /// </summary>
    [MaxLength(4000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the incident was first detected.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the incident was created in the system.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the ID of the user who last updated the incident status.
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the incident (navigation property).
    /// </summary>
    public User? UpdatedByUser { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the incident was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the resolution notes for the incident.
    /// </summary>
    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the incident was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets whether an alert was sent for this incident.
    /// </summary>
    public bool AlertSent { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the alert was sent.
    /// </summary>
    public DateTime? AlertSentAt { get; set; }

    /// <summary>
    /// Gets or sets the recipients who were alerted (comma-separated emails).
    /// </summary>
    [MaxLength(500)]
    public string? AlertRecipients { get; set; }
}
