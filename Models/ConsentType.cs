namespace MercatoApp.Models;

/// <summary>
/// Enumeration of consent types for data processing and communication.
/// Used for GDPR compliance and marketing communication preferences.
/// </summary>
public enum ConsentType
{
    /// <summary>
    /// Consent to receive newsletter and general updates.
    /// </summary>
    Newsletter = 1,

    /// <summary>
    /// Consent to receive marketing communications and promotional offers.
    /// </summary>
    Marketing = 2,

    /// <summary>
    /// Consent for user profiling and personalized recommendations.
    /// </summary>
    Profiling = 3,

    /// <summary>
    /// Consent to share data with third-party partners.
    /// </summary>
    ThirdPartySharing = 4,

    /// <summary>
    /// Consent to Terms of Service (required).
    /// </summary>
    TermsOfService = 5,

    /// <summary>
    /// Consent to Privacy Policy (required).
    /// </summary>
    PrivacyPolicy = 6
}
