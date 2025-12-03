namespace MercatoApp.Models;

/// <summary>
/// Represents the status of a security incident.
/// </summary>
public enum SecurityIncidentStatus
{
    /// <summary>
    /// Incident has been detected but not yet reviewed.
    /// </summary>
    New,

    /// <summary>
    /// Incident has been reviewed and categorized.
    /// </summary>
    Triaged,

    /// <summary>
    /// Incident is currently under investigation.
    /// </summary>
    InInvestigation,

    /// <summary>
    /// Incident has been resolved.
    /// </summary>
    Resolved,

    /// <summary>
    /// Incident was a false positive.
    /// </summary>
    FalsePositive,

    /// <summary>
    /// Incident has been closed without action.
    /// </summary>
    Closed
}
