using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing legal documents and user consent tracking.
/// </summary>
public class LegalDocumentService : ILegalDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LegalDocumentService> _logger;

    public LegalDocumentService(
        ApplicationDbContext context,
        ILogger<LegalDocumentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LegalDocument?> GetActiveDocumentAsync(LegalDocumentType documentType, string languageCode = "en")
    {
        var now = DateTime.UtcNow;
        return await _context.LegalDocuments
            .Include(d => d.CreatedByUser)
            .Include(d => d.UpdatedByUser)
            .Where(d => d.DocumentType == documentType 
                && d.LanguageCode == languageCode
                && d.IsActive
                && d.EffectiveDate <= now)
            .OrderByDescending(d => d.EffectiveDate)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<LegalDocument?> GetDocumentByIdAsync(int id)
    {
        return await _context.LegalDocuments
            .Include(d => d.CreatedByUser)
            .Include(d => d.UpdatedByUser)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<LegalDocument>> GetDocumentHistoryAsync(LegalDocumentType documentType, string languageCode = "en")
    {
        return await _context.LegalDocuments
            .Include(d => d.CreatedByUser)
            .Include(d => d.UpdatedByUser)
            .Where(d => d.DocumentType == documentType && d.LanguageCode == languageCode)
            .OrderByDescending(d => d.EffectiveDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Dictionary<LegalDocumentType, LegalDocument?>> GetAllActiveDocumentsAsync(string languageCode = "en")
    {
        var now = DateTime.UtcNow;
        var activeDocuments = await _context.LegalDocuments
            .Include(d => d.CreatedByUser)
            .Include(d => d.UpdatedByUser)
            .Where(d => d.LanguageCode == languageCode
                && d.IsActive
                && d.EffectiveDate <= now)
            .ToListAsync();

        var result = new Dictionary<LegalDocumentType, LegalDocument?>();
        
        // Initialize all document types
        foreach (LegalDocumentType type in Enum.GetValues(typeof(LegalDocumentType)))
        {
            result[type] = activeDocuments
                .Where(d => d.DocumentType == type)
                .OrderByDescending(d => d.EffectiveDate)
                .FirstOrDefault();
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<LegalDocument> CreateDocumentAsync(LegalDocument document, int adminUserId)
    {
        document.CreatedAt = DateTime.UtcNow;
        document.CreatedByUserId = adminUserId;
        
        // If this is being created with a current or past effective date and marked as active,
        // deactivate other active documents of the same type
        if (document.IsActive && document.EffectiveDate <= DateTime.UtcNow)
        {
            await DeactivateOtherVersionsAsync(document.DocumentType, document.LanguageCode);
        }

        _context.LegalDocuments.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Legal document created: Type={DocumentType}, Version={Version}, Admin={AdminUserId}",
            document.DocumentType, document.Version, adminUserId);

        return document;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateDocumentAsync(LegalDocument document, int adminUserId)
    {
        var existing = await _context.LegalDocuments.FindAsync(document.Id);
        if (existing == null)
        {
            return false;
        }

        existing.Title = document.Title;
        existing.Content = document.Content;
        existing.Version = document.Version;
        existing.EffectiveDate = document.EffectiveDate;
        existing.ChangeNotes = document.ChangeNotes;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedByUserId = adminUserId;

        // If being activated with current or past effective date, deactivate others
        if (document.IsActive && !existing.IsActive && document.EffectiveDate <= DateTime.UtcNow)
        {
            await DeactivateOtherVersionsAsync(existing.DocumentType, existing.LanguageCode);
        }
        
        existing.IsActive = document.IsActive;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Legal document updated: Id={DocumentId}, Type={DocumentType}, Version={Version}, Admin={AdminUserId}",
            document.Id, existing.DocumentType, existing.Version, adminUserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentAsync(int id)
    {
        var document = await _context.LegalDocuments.FindAsync(id);
        if (document == null)
        {
            return false;
        }

        // Check if it's the active version
        if (document.IsActive)
        {
            _logger.LogWarning(
                "Cannot delete active legal document: Id={DocumentId}, Type={DocumentType}",
                id, document.DocumentType);
            return false;
        }

        // Check if there are any consents associated with this document
        var hasConsents = await _context.UserConsents
            .AnyAsync(c => c.LegalDocumentId == id);

        if (hasConsents)
        {
            _logger.LogWarning(
                "Cannot delete legal document with associated consents: Id={DocumentId}, Type={DocumentType}",
                id, document.DocumentType);
            return false;
        }

        _context.LegalDocuments.Remove(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Legal document deleted: Id={DocumentId}, Type={DocumentType}",
            id, document.DocumentType);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateDocumentAsync(int id)
    {
        var document = await _context.LegalDocuments.FindAsync(id);
        if (document == null)
        {
            return false;
        }

        // Only activate if effective date is in the past or present
        if (document.EffectiveDate > DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Cannot activate future-dated legal document: Id={DocumentId}, EffectiveDate={EffectiveDate}",
                id, document.EffectiveDate);
            return false;
        }

        // Deactivate other versions
        await DeactivateOtherVersionsAsync(document.DocumentType, document.LanguageCode);

        document.IsActive = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Legal document activated: Id={DocumentId}, Type={DocumentType}, Version={Version}",
            id, document.DocumentType, document.Version);

        return true;
    }

    /// <inheritdoc />
    public async Task<UserConsent> RecordConsentAsync(
        int userId, 
        int documentId, 
        string? ipAddress, 
        string? userAgent, 
        string? context)
    {
        var consent = new UserConsent
        {
            UserId = userId,
            LegalDocumentId = documentId,
            ConsentedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ConsentContext = context
        };

        _context.UserConsents.Add(consent);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User consent recorded: UserId={UserId}, DocumentId={DocumentId}, Context={Context}",
            userId, documentId, context);

        return consent;
    }

    /// <inheritdoc />
    public async Task<bool> HasUserConsentedAsync(int userId, LegalDocumentType documentType)
    {
        var activeDocument = await GetActiveDocumentAsync(documentType);
        if (activeDocument == null)
        {
            return false;
        }

        return await _context.UserConsents
            .AnyAsync(c => c.UserId == userId && c.LegalDocumentId == activeDocument.Id);
    }

    /// <inheritdoc />
    public async Task<List<UserConsent>> GetUserConsentsAsync(int userId)
    {
        return await _context.UserConsents
            .Include(c => c.LegalDocument)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.ConsentedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<UserConsent>> GetDocumentConsentsAsync(int documentId)
    {
        return await _context.UserConsents
            .Include(c => c.User)
            .Where(c => c.LegalDocumentId == documentId)
            .OrderByDescending(c => c.ConsentedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Deactivates all active versions of a document type except the one being processed.
    /// </summary>
    private async Task DeactivateOtherVersionsAsync(LegalDocumentType documentType, string languageCode)
    {
        var activeDocuments = await _context.LegalDocuments
            .Where(d => d.DocumentType == documentType 
                && d.LanguageCode == languageCode
                && d.IsActive)
            .ToListAsync();

        foreach (var doc in activeDocuments)
        {
            doc.IsActive = false;
        }

        await _context.SaveChangesAsync();
    }
}
