namespace MercatoApp.Models;

/// <summary>
/// Represents the severity level of a security incident.
/// </summary>
public enum SecurityIncidentSeverity
{
    /// <summary>
    /// Low severity - informational or minor security event.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity - potential security concern requiring attention.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity - significant security issue requiring immediate response.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity - severe security breach requiring urgent action.
    /// </summary>
    Critical
}
