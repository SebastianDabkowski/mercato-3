namespace MercatoApp.Models;

/// <summary>
/// Represents the KYC (Know Your Customer) verification status for sellers.
/// </summary>
public enum KycStatus
{
    /// <summary>
    /// KYC verification has not been started.
    /// </summary>
    NotStarted,

    /// <summary>
    /// KYC verification is pending review.
    /// </summary>
    Pending,

    /// <summary>
    /// KYC verification has been approved.
    /// </summary>
    Approved,

    /// <summary>
    /// KYC verification has been rejected.
    /// </summary>
    Rejected
}
