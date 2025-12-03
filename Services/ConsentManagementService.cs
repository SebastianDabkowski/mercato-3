using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing user consents for data processing and communication.
/// Implements GDPR-compliant consent tracking with version control and audit trail.
/// </summary>
public class ConsentManagementService : IConsentManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConsentManagementService> _logger;

    public ConsentManagementService(
        ApplicationDbContext context,
        ILogger<ConsentManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserConsent> RecordConsentAsync(
        int userId,
        ConsentType consentType,
        bool isGranted,
        string? version,
        string? consentText,
        string? ipAddress,
        string? userAgent,
        string? context,
        int? legalDocumentId = null)
    {
        // Supersede any existing non-superseded consents for this user and type
        var existingConsents = await _context.UserConsents
            .Where(c => c.UserId == userId 
                && c.ConsentType == consentType 
                && c.SupersededAt == null)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var existing in existingConsents)
        {
            existing.SupersededAt = now;
        }

        // Create new consent record
        var consent = new UserConsent
        {
            UserId = userId,
            ConsentType = consentType,
            IsGranted = isGranted,
            ConsentVersion = version,
            ConsentText = consentText,
            LegalDocumentId = legalDocumentId,
            ConsentedAt = now,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ConsentContext = context
        };

        _context.UserConsents.Add(consent);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Consent recorded: UserId={UserId}, Type={ConsentType}, Granted={IsGranted}, Context={Context}",
            userId, consentType, isGranted, context);

        return consent;
    }

    /// <inheritdoc />
    public async Task<UserConsent> GrantConsentAsync(
        int userId,
        ConsentType consentType,
        string? version,
        string? consentText,
        string? ipAddress,
        string? userAgent,
        string? context)
    {
        return await RecordConsentAsync(
            userId,
            consentType,
            isGranted: true,
            version,
            consentText,
            ipAddress,
            userAgent,
            context);
    }

    /// <inheritdoc />
    public async Task<UserConsent> WithdrawConsentAsync(
        int userId,
        ConsentType consentType,
        string? ipAddress,
        string? userAgent,
        string? context)
    {
        return await RecordConsentAsync(
            userId,
            consentType,
            isGranted: false,
            version: null,
            consentText: null,
            ipAddress,
            userAgent,
            context);
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveConsentAsync(int userId, ConsentType consentType)
    {
        var currentConsent = await GetCurrentConsentAsync(userId, consentType);
        return currentConsent?.IsGranted == true;
    }

    /// <inheritdoc />
    public async Task<UserConsent?> GetCurrentConsentAsync(int userId, ConsentType consentType)
    {
        return await _context.UserConsents
            .Include(c => c.LegalDocument)
            .Where(c => c.UserId == userId 
                && c.ConsentType == consentType 
                && c.SupersededAt == null)
            .OrderByDescending(c => c.ConsentedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Dictionary<ConsentType, UserConsent>> GetCurrentConsentsAsync(int userId)
    {
        var consents = await _context.UserConsents
            .Include(c => c.LegalDocument)
            .Where(c => c.UserId == userId && c.SupersededAt == null)
            .ToListAsync();

        var result = new Dictionary<ConsentType, UserConsent>();
        
        // Group by consent type and take the most recent for each
        foreach (var consentGroup in consents.GroupBy(c => c.ConsentType))
        {
            var mostRecent = consentGroup.OrderByDescending(c => c.ConsentedAt).First();
            result[consentGroup.Key] = mostRecent;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<UserConsent>> GetConsentHistoryAsync(int userId, ConsentType consentType)
    {
        return await _context.UserConsents
            .Include(c => c.LegalDocument)
            .Where(c => c.UserId == userId && c.ConsentType == consentType)
            .OrderByDescending(c => c.ConsentedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<UserConsent>> GetAllConsentHistoryAsync(int userId)
    {
        return await _context.UserConsents
            .Include(c => c.LegalDocument)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.ConsentedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IsEligibleForCommunicationAsync(int userId, ConsentType communicationType)
    {
        // Verify the consent type is a communication type
        if (!Helpers.ConsentHelper.IsCommunicationConsent(communicationType))
        {
            _logger.LogWarning(
                "Invalid communication type: {ConsentType}",
                communicationType);
            return false;
        }

        return await HasActiveConsentAsync(userId, communicationType);
    }

    /// <inheritdoc />
    public async Task<List<int>> GetUsersWithActiveConsentAsync(ConsentType communicationType)
    {
        // Get all current consents for this type
        var activeConsents = await _context.UserConsents
            .Where(c => c.ConsentType == communicationType 
                && c.SupersededAt == null
                && c.IsGranted)
            .Select(c => c.UserId)
            .Distinct()
            .ToListAsync();

        _logger.LogInformation(
            "Found {Count} users with active {ConsentType} consent",
            activeConsents.Count, communicationType);

        return activeConsents;
    }
}
