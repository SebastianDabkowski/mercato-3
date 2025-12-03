namespace MercatoApp.Models;

/// <summary>
/// Environment mode for an integration.
/// </summary>
public enum IntegrationEnvironment
{
    /// <summary>
    /// Development/testing environment with test credentials.
    /// </summary>
    Development = 1,

    /// <summary>
    /// Sandbox/staging environment for pre-production testing.
    /// </summary>
    Sandbox = 2,

    /// <summary>
    /// Production environment with live credentials.
    /// </summary>
    Production = 3
}
