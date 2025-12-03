namespace MercatoApp.Models;

/// <summary>
/// Status of an external integration.
/// </summary>
public enum IntegrationStatus
{
    /// <summary>
    /// Integration is active and operational.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Integration is temporarily disabled.
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Integration has encountered an error.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Integration is in testing/configuration mode.
    /// </summary>
    Testing = 4
}
