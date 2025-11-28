namespace MercatoApp.Models;

/// <summary>
/// Represents the status of a user account.
/// </summary>
public enum AccountStatus
{
    /// <summary>
    /// Account is created but email is not yet verified.
    /// </summary>
    Unverified,

    /// <summary>
    /// Account email has been verified and is active.
    /// </summary>
    Active,

    /// <summary>
    /// Account has been suspended by admin.
    /// </summary>
    Suspended
}
