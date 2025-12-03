using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for managing legal documents and user consent tracking.
/// </summary>
public interface ILegalDocumentService
{
    /// <summary>
    /// Gets the currently active version of a legal document by type.
    /// </summary>
    /// <param name="documentType">The type of legal document.</param>
    /// <param name="languageCode">The language code (defaults to "en").</param>
    /// <returns>The active legal document or null if none exists.</returns>
    Task<LegalDocument?> GetActiveDocumentAsync(LegalDocumentType documentType, string languageCode = "en");

    /// <summary>
    /// Gets a specific legal document by ID.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <returns>The legal document or null if not found.</returns>
    Task<LegalDocument?> GetDocumentByIdAsync(int id);

    /// <summary>
    /// Gets all versions of a legal document by type.
    /// </summary>
    /// <param name="documentType">The type of legal document.</param>
    /// <param name="languageCode">The language code (defaults to "en").</param>
    /// <returns>List of all versions, ordered by effective date descending.</returns>
    Task<List<LegalDocument>> GetDocumentHistoryAsync(LegalDocumentType documentType, string languageCode = "en");

    /// <summary>
    /// Gets all legal documents grouped by type.
    /// </summary>
    /// <param name="languageCode">The language code (defaults to "en").</param>
    /// <returns>Dictionary of document types to their active versions.</returns>
    Task<Dictionary<LegalDocumentType, LegalDocument?>> GetAllActiveDocumentsAsync(string languageCode = "en");

    /// <summary>
    /// Creates a new version of a legal document.
    /// </summary>
    /// <param name="document">The legal document to create.</param>
    /// <param name="adminUserId">The ID of the admin creating the document.</param>
    /// <returns>The created legal document with ID assigned.</returns>
    Task<LegalDocument> CreateDocumentAsync(LegalDocument document, int adminUserId);

    /// <summary>
    /// Updates an existing legal document version.
    /// </summary>
    /// <param name="document">The legal document to update.</param>
    /// <param name="adminUserId">The ID of the admin updating the document.</param>
    /// <returns>True if update was successful, false otherwise.</returns>
    Task<bool> UpdateDocumentAsync(LegalDocument document, int adminUserId);

    /// <summary>
    /// Deletes a legal document version.
    /// Only allows deletion if it's not the active version and has no associated consents.
    /// </summary>
    /// <param name="id">The document ID to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    Task<bool> DeleteDocumentAsync(int id);

    /// <summary>
    /// Activates a specific document version, deactivating all others of the same type.
    /// </summary>
    /// <param name="id">The document ID to activate.</param>
    /// <returns>True if activation was successful, false otherwise.</returns>
    Task<bool> ActivateDocumentAsync(int id);

    /// <summary>
    /// Records a user's consent to a legal document.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="documentId">The legal document ID.</param>
    /// <param name="ipAddress">The IP address from which consent was given.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="context">The context in which consent was given (e.g., "registration").</param>
    /// <returns>The created user consent record.</returns>
    Task<UserConsent> RecordConsentAsync(int userId, int documentId, string? ipAddress, string? userAgent, string? context);

    /// <summary>
    /// Checks if a user has consented to a specific document type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="documentType">The document type.</param>
    /// <returns>True if user has consented to the current active version, false otherwise.</returns>
    Task<bool> HasUserConsentedAsync(int userId, LegalDocumentType documentType);

    /// <summary>
    /// Gets all consent records for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of consent records.</returns>
    Task<List<UserConsent>> GetUserConsentsAsync(int userId);

    /// <summary>
    /// Gets all consent records for a specific legal document.
    /// </summary>
    /// <param name="documentId">The legal document ID.</param>
    /// <returns>List of consent records.</returns>
    Task<List<UserConsent>> GetDocumentConsentsAsync(int documentId);
}
