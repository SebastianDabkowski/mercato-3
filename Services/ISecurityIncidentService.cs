using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Data required to create a security incident.
/// </summary>
public class CreateSecurityIncidentData
{
    /// <summary>
    /// Gets or sets the type of security incident.
    /// </summary>
    public SecurityIncidentType IncidentType { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the incident.
    /// </summary>
    public SecurityIncidentSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the detection rule that triggered this incident.
    /// </summary>
    public string DetectionRule { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source IP address or identifier.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with the incident.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets detailed description of the incident.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the incident was detected.
    /// Defaults to current UTC time if not specified.
    /// </summary>
    public DateTime? DetectedAt { get; set; }
}

/// <summary>
/// Data required to update a security incident status.
/// </summary>
public class UpdateSecurityIncidentStatusData
{
    /// <summary>
    /// Gets or sets the new status.
    /// </summary>
    public SecurityIncidentStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the user ID who is updating the status.
    /// </summary>
    public int UpdatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets notes about the status change.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets resolution notes (for resolved/closed statuses).
    /// </summary>
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// Filter for querying security incidents.
/// </summary>
public class SecurityIncidentFilter
{
    /// <summary>
    /// Gets or sets the incident type to filter by.
    /// </summary>
    public SecurityIncidentType? IncidentType { get; set; }

    /// <summary>
    /// Gets or sets the severity level to filter by.
    /// </summary>
    public SecurityIncidentSeverity? Severity { get; set; }

    /// <summary>
    /// Gets or sets the status to filter by.
    /// </summary>
    public SecurityIncidentStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the start date for filtering.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for filtering.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the user ID to filter by.
    /// </summary>
    public int? UserId { get; set; }
}

/// <summary>
/// Interface for security incident management service.
/// </summary>
public interface ISecurityIncidentService
{
    /// <summary>
    /// Creates a new security incident record.
    /// </summary>
    /// <param name="data">The incident data.</param>
    /// <returns>The created security incident.</returns>
    Task<SecurityIncident> CreateIncidentAsync(CreateSecurityIncidentData data);

    /// <summary>
    /// Updates the status of a security incident.
    /// </summary>
    /// <param name="incidentId">The incident ID.</param>
    /// <param name="data">The status update data.</param>
    /// <returns>The updated security incident.</returns>
    Task<SecurityIncident> UpdateIncidentStatusAsync(int incidentId, UpdateSecurityIncidentStatusData data);

    /// <summary>
    /// Gets a security incident by ID.
    /// </summary>
    /// <param name="incidentId">The incident ID.</param>
    /// <returns>The security incident or null if not found.</returns>
    Task<SecurityIncident?> GetIncidentByIdAsync(int incidentId);

    /// <summary>
    /// Gets a security incident by incident number.
    /// </summary>
    /// <param name="incidentNumber">The incident number.</param>
    /// <returns>The security incident or null if not found.</returns>
    Task<SecurityIncident?> GetIncidentByNumberAsync(string incidentNumber);

    /// <summary>
    /// Gets security incidents with optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paginated list of security incidents.</returns>
    Task<PaginatedList<SecurityIncident>> GetIncidentsAsync(
        SecurityIncidentFilter? filter = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets status history for a security incident.
    /// </summary>
    /// <param name="incidentId">The incident ID.</param>
    /// <returns>List of status history entries.</returns>
    Task<List<SecurityIncidentStatusHistory>> GetIncidentStatusHistoryAsync(int incidentId);

    /// <summary>
    /// Sends alerts for high-severity incidents.
    /// </summary>
    /// <param name="incident">The security incident.</param>
    /// <returns>True if alert was sent successfully.</returns>
    Task<bool> SendIncidentAlertAsync(SecurityIncident incident);

    /// <summary>
    /// Exports incidents for compliance reporting.
    /// </summary>
    /// <param name="filter">Filter criteria for the export.</param>
    /// <returns>List of incidents in export format.</returns>
    Task<List<SecurityIncident>> ExportIncidentsAsync(SecurityIncidentFilter filter);
}
