namespace MercatoApp.Models;

/// <summary>
/// Represents the type of security incident.
/// </summary>
public enum SecurityIncidentType
{
    /// <summary>
    /// Multiple failed login attempts detected.
    /// </summary>
    MultipleFailedLogins,

    /// <summary>
    /// Suspicious API usage pattern detected.
    /// </summary>
    SuspiciousApiUsage,

    /// <summary>
    /// Unusual data access pattern detected.
    /// </summary>
    DataAccessAnomaly,

    /// <summary>
    /// Potential brute force attack detected.
    /// </summary>
    BruteForceAttempt,

    /// <summary>
    /// Account compromise suspected.
    /// </summary>
    SuspectedAccountCompromise,

    /// <summary>
    /// Unauthorized access attempt detected.
    /// </summary>
    UnauthorizedAccessAttempt,

    /// <summary>
    /// Rate limit exceeded.
    /// </summary>
    RateLimitExceeded,

    /// <summary>
    /// SQL injection attempt detected.
    /// </summary>
    SqlInjectionAttempt,

    /// <summary>
    /// Cross-site scripting (XSS) attempt detected.
    /// </summary>
    XssAttempt,

    /// <summary>
    /// Other security incident not covered by specific types.
    /// </summary>
    Other
}
