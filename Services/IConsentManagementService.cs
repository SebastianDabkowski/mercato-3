using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for managing user consents for data processing and communication.
/// </summary>
public interface IConsentManagementService
{
    /// <summary>
    /// Records a user's consent for a specific consent type.
    /// Previous consents of the same type are superseded.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="consentType">The type of consent.</param>
    /// <param name="isGranted">Whether consent is granted (true) or withdrawn (false).</param>
    /// <param name="version">The version of the consent text.</param>
    /// <param name="consentText">The consent text presented to the user.</param>
    /// <param name="ipAddress">The IP address from which consent was given.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="context">The context in which consent was given.</param>
    /// <param name="legalDocumentId">Optional legal document ID if consent is linked to a document.</param>
    /// <returns>The created consent record.</returns>
    Task<UserConsent> RecordConsentAsync(
        int userId,
        ConsentType consentType,
        bool isGranted,
        string? version,
        string? consentText,
        string? ipAddress,
        string? userAgent,
        string? context,
        int? legalDocumentId = null);

    /// <summary>
    /// Grants consent for a specific consent type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="consentType">The type of consent.</param>
    /// <param name="version">The version of the consent text.</param>
    /// <param name="consentText">The consent text presented to the user.</param>
    /// <param name="ipAddress">The IP address from which consent was given.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="context">The context in which consent was given.</param>
    /// <returns>The created consent record.</returns>
    Task<UserConsent> GrantConsentAsync(
        int userId,
        ConsentType consentType,
        string? version,
        string? consentText,
        string? ipAddress,
        string? userAgent,
        string? context);

    /// <summary>
    /// Withdraws consent for a specific consent type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="consentType">The type of consent.</param>
    /// <param name="ipAddress">The IP address from which consent was withdrawn.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="context">The context in which consent was withdrawn.</param>
    /// <returns>The created withdrawal record.</returns>
    Task<UserConsent> WithdrawConsentAsync(
        int userId,
        ConsentType consentType,
        string? ipAddress,
        string? userAgent,
        string? context);

    /// <summary>
    /// Checks if a user has active consent for a specific consent type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="consentType">The type of consent.</param>
    /// <returns>True if user has active consent, false otherwise.</returns>
    Task<bool> HasActiveConsentAsync(int userId, ConsentType consentType);

    /// <summary>
    /// Gets the current active consent record for a user and consent type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="consentType">The type of consent.</param>
    /// <returns>The current consent record or null if none exists.</returns>
    Task<UserConsent?> GetCurrentConsentAsync(int userId, ConsentType consentType);

    /// <summary>
    /// Gets all current consents for a user (not superseded).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Dictionary of consent types to their current consent records.</returns>
    Task<Dictionary<ConsentType, UserConsent>> GetCurrentConsentsAsync(int userId);

    /// <summary>
    /// Gets the complete consent history for a user and consent type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="consentType">The type of consent.</param>
    /// <returns>List of all consent records ordered by date descending.</returns>
    Task<List<UserConsent>> GetConsentHistoryAsync(int userId, ConsentType consentType);

    /// <summary>
    /// Gets all consent history for a user across all consent types.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of all consent records ordered by date descending.</returns>
    Task<List<UserConsent>> GetAllConsentHistoryAsync(int userId);

    /// <summary>
    /// Checks if a user is eligible to receive a specific type of communication.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="communicationType">The communication type (Newsletter or Marketing).</param>
    /// <returns>True if eligible, false otherwise.</returns>
    Task<bool> IsEligibleForCommunicationAsync(int userId, ConsentType communicationType);

    /// <summary>
    /// Gets a list of user IDs who have active consent for a specific communication type.
    /// </summary>
    /// <param name="communicationType">The communication type.</param>
    /// <returns>List of user IDs with active consent.</returns>
    Task<List<int>> GetUsersWithActiveConsentAsync(ConsentType communicationType);
}
