using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing seller rating moderation and flagging.
/// </summary>
public class SellerRatingModerationService : ISellerRatingModerationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SellerRatingModerationService> _logger;

    // Automated flagging keywords - can be moved to configuration
    private static readonly HashSet<string> InappropriateKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "spam", "fake", "scam", "fraud", "cheat", "lie", "liar", "steal"
    };

    public SellerRatingModerationService(
        ApplicationDbContext context,
        ILogger<SellerRatingModerationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SellerRatingFlag> FlagRatingAsync(int ratingId, ReviewFlagReason reason, string? details, int? flaggedByUserId, bool isAutomated = false)
    {
        var rating = await _context.SellerRatings
            .FirstOrDefaultAsync(r => r.Id == ratingId);

        if (rating == null)
        {
            throw new InvalidOperationException("Seller rating not found.");
        }

        // Check if there's already an active flag from the same user
        // For manual flags (user-initiated), prevent duplicate reports from the same user regardless of reason
        if (!isAutomated && flaggedByUserId.HasValue)
        {
            var existingUserFlag = await _context.SellerRatingFlags
                .FirstOrDefaultAsync(f => f.SellerRatingId == ratingId && 
                                        f.FlaggedByUserId == flaggedByUserId && 
                                        f.IsActive && 
                                        !f.IsAutomated);

            if (existingUserFlag != null)
            {
                _logger.LogInformation("Seller rating {RatingId} already has an active flag from user {UserId}", ratingId, flaggedByUserId);
                throw new InvalidOperationException("You have already reported this seller rating. Our team will review it shortly.");
            }
        }

        // For automated flags, check if there's already an active flag for the same reason
        if (isAutomated)
        {
            var existingFlag = await _context.SellerRatingFlags
                .FirstOrDefaultAsync(f => f.SellerRatingId == ratingId && f.Reason == reason && f.IsActive);

            if (existingFlag != null)
            {
                _logger.LogInformation("Seller rating {RatingId} already has an active automated flag for reason {Reason}", ratingId, reason);
                return existingFlag;
            }
        }

        var flag = new SellerRatingFlag
        {
            SellerRatingId = ratingId,
            Reason = reason,
            Details = details,
            FlaggedByUserId = flaggedByUserId,
            IsAutomated = isAutomated,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.SellerRatingFlags.Add(flag);

        // Update rating status to Flagged if it's currently approved
        if (rating.ModerationStatus == ReviewModerationStatus.Approved)
        {
            var previousStatus = rating.ModerationStatus;
            rating.ModerationStatus = ReviewModerationStatus.Flagged;

            // Log the action
            await LogModerationActionAsync(
                ratingId,
                isAutomated ? ReviewModerationAction.AutoFlagged : ReviewModerationAction.Flagged,
                flaggedByUserId,
                $"Flagged for {reason}: {details}",
                previousStatus,
                ReviewModerationStatus.Flagged,
                isAutomated
            );
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Seller rating {RatingId} flagged for {Reason} by user {UserId} (automated: {IsAutomated})", 
            ratingId, reason, flaggedByUserId, isAutomated);

        return flag;
    }

    /// <inheritdoc />
    public async Task<SellerRating> ApproveRatingAsync(int ratingId, int adminUserId, string? reason = null)
    {
        var rating = await _context.SellerRatings
            .FirstOrDefaultAsync(r => r.Id == ratingId);

        if (rating == null)
        {
            throw new InvalidOperationException("Seller rating not found.");
        }

        var previousStatus = rating.ModerationStatus;

        rating.IsApproved = true;
        rating.ModerationStatus = ReviewModerationStatus.Approved;
        rating.ModeratedByUserId = adminUserId;
        rating.ModeratedAt = DateTime.UtcNow;
        rating.ApprovedAt = DateTime.UtcNow;

        // Resolve any active flags
        var activeFlags = await _context.SellerRatingFlags
            .Where(f => f.SellerRatingId == ratingId && f.IsActive)
            .ToListAsync();

        foreach (var flag in activeFlags)
        {
            flag.IsActive = false;
            flag.ResolvedAt = DateTime.UtcNow;
            flag.ResolvedByUserId = adminUserId;
        }

        // Log the action
        await LogModerationActionAsync(
            ratingId,
            ReviewModerationAction.Approved,
            adminUserId,
            reason ?? "Seller rating approved by admin",
            previousStatus,
            ReviewModerationStatus.Approved,
            false
        );

        await _context.SaveChangesAsync();

        _logger.LogInformation("Seller rating {RatingId} approved by admin {AdminUserId}", ratingId, adminUserId);

        return rating;
    }

    /// <inheritdoc />
    public async Task<SellerRating> RejectRatingAsync(int ratingId, int adminUserId, string reason)
    {
        var rating = await _context.SellerRatings
            .FirstOrDefaultAsync(r => r.Id == ratingId);

        if (rating == null)
        {
            throw new InvalidOperationException("Seller rating not found.");
        }

        var previousStatus = rating.ModerationStatus;

        rating.IsApproved = false;
        rating.ModerationStatus = ReviewModerationStatus.Rejected;
        rating.ModeratedByUserId = adminUserId;
        rating.ModeratedAt = DateTime.UtcNow;

        // Resolve any active flags
        var activeFlags = await _context.SellerRatingFlags
            .Where(f => f.SellerRatingId == ratingId && f.IsActive)
            .ToListAsync();

        foreach (var flag in activeFlags)
        {
            flag.IsActive = false;
            flag.ResolvedAt = DateTime.UtcNow;
            flag.ResolvedByUserId = adminUserId;
        }

        // Log the action
        await LogModerationActionAsync(
            ratingId,
            ReviewModerationAction.Rejected,
            adminUserId,
            reason,
            previousStatus,
            ReviewModerationStatus.Rejected,
            false
        );

        await _context.SaveChangesAsync();

        _logger.LogInformation("Seller rating {RatingId} rejected by admin {AdminUserId}: {Reason}", ratingId, adminUserId, reason);

        return rating;
    }

    /// <inheritdoc />
    public async Task<SellerRating> ToggleRatingVisibilityAsync(int ratingId, int adminUserId, bool isVisible, string? reason = null)
    {
        var rating = await _context.SellerRatings
            .FirstOrDefaultAsync(r => r.Id == ratingId);

        if (rating == null)
        {
            throw new InvalidOperationException("Seller rating not found.");
        }

        var previousStatus = rating.ModerationStatus;

        rating.IsApproved = isVisible;
        rating.ModeratedByUserId = adminUserId;
        rating.ModeratedAt = DateTime.UtcNow;

        // Log the action
        await LogModerationActionAsync(
            ratingId,
            ReviewModerationAction.VisibilityEdited,
            adminUserId,
            reason ?? $"Visibility changed to {(isVisible ? "visible" : "hidden")}",
            previousStatus,
            rating.ModerationStatus,
            false
        );

        await _context.SaveChangesAsync();

        _logger.LogInformation("Seller rating {RatingId} visibility toggled to {IsVisible} by admin {AdminUserId}", 
            ratingId, isVisible, adminUserId);

        return rating;
    }

    /// <inheritdoc />
    public async Task<SellerRatingFlag> ResolveFlagAsync(int flagId, int adminUserId)
    {
        var flag = await _context.SellerRatingFlags
            .FirstOrDefaultAsync(f => f.Id == flagId);

        if (flag == null)
        {
            throw new InvalidOperationException("Flag not found.");
        }

        flag.IsActive = false;
        flag.ResolvedAt = DateTime.UtcNow;
        flag.ResolvedByUserId = adminUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Seller rating flag {FlagId} resolved by admin {AdminUserId}", flagId, adminUserId);

        return flag;
    }

    /// <inheritdoc />
    public async Task<List<SellerRatingFlag>> GetFlaggedRatingsAsync(bool includeResolved = false)
    {
        var query = _context.SellerRatingFlags
            .Include(f => f.SellerRating)
                .ThenInclude(r => r.Store)
            .Include(f => f.SellerRating)
                .ThenInclude(r => r.User)
            .Include(f => f.FlaggedByUser)
            .Include(f => f.ResolvedByUser)
            .AsQueryable();

        if (!includeResolved)
        {
            query = query.Where(f => f.IsActive);
        }

        return await query
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<SellerRating>> GetRatingsByStatusAsync(ReviewModerationStatus status, int page = 1, int pageSize = 20)
    {
        return await _context.SellerRatings
            .Include(r => r.Store)
            .Include(r => r.User)
            .Include(r => r.ModeratedByUser)
            .Include(r => r.Flags)
            .Where(r => r.ModerationStatus == status)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetRatingCountByStatusAsync(ReviewModerationStatus status)
    {
        return await _context.SellerRatings
            .CountAsync(r => r.ModerationStatus == status);
    }

    /// <inheritdoc />
    public async Task<List<SellerRatingModerationLog>> GetRatingModerationHistoryAsync(int ratingId)
    {
        return await _context.SellerRatingModerationLogs
            .Include(l => l.ModeratedByUser)
            .Where(l => l.SellerRatingId == ratingId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> AutoCheckRatingAsync(int ratingId)
    {
        var rating = await _context.SellerRatings
            .FirstOrDefaultAsync(r => r.Id == ratingId);

        if (rating == null || string.IsNullOrWhiteSpace(rating.ReviewText))
        {
            return false;
        }

        var reviewText = rating.ReviewText.ToLowerInvariant();
        var flagged = false;

        // Check for inappropriate keywords
        foreach (var keyword in InappropriateKeywords)
        {
            if (reviewText.Contains(keyword))
            {
                await FlagRatingAsync(ratingId, ReviewFlagReason.InappropriateLanguage, 
                    $"Contains inappropriate keyword: {keyword}", null, true);
                flagged = true;
                break;
            }
        }

        // Check for URLs
        if (!flagged && (reviewText.Contains("http") || reviewText.Contains("www") || 
            Regex.IsMatch(reviewText, @"\.(com|org|net|io)\b", RegexOptions.IgnoreCase)))
        {
            await FlagRatingAsync(ratingId, ReviewFlagReason.Spam, 
                "Contains URL or web address", null, true);
            flagged = true;
        }

        // Check for email addresses
        if (!flagged && Regex.IsMatch(reviewText, @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase))
        {
            await FlagRatingAsync(ratingId, ReviewFlagReason.PersonalInformation, 
                "Contains email address", null, true);
            flagged = true;
        }

        // Check for phone numbers
        if (!flagged && Regex.IsMatch(reviewText, @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b"))
        {
            await FlagRatingAsync(ratingId, ReviewFlagReason.PersonalInformation, 
                "Contains phone number", null, true);
            flagged = true;
        }

        // Check for excessive capitalization (spam indicator)
        if (!flagged)
        {
            var letters = reviewText.Where(char.IsLetter).ToList();
            if (letters.Count > 10)
            {
                var capsCount = letters.Count(char.IsUpper);
                var capsPercentage = (double)capsCount / letters.Count;
                if (capsPercentage > 0.7)
                {
                    await FlagRatingAsync(ratingId, ReviewFlagReason.Spam, 
                        "Excessive capitalization detected", null, true);
                    flagged = true;
                }
            }
        }

        return flagged;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetModerationStatsAsync()
    {
        var stats = new Dictionary<string, int>
        {
            ["ActiveFlags"] = await _context.SellerRatingFlags.CountAsync(f => f.IsActive),
            ["PendingReview"] = await GetRatingCountByStatusAsync(ReviewModerationStatus.PendingReview),
            ["Approved"] = await GetRatingCountByStatusAsync(ReviewModerationStatus.Approved),
            ["Rejected"] = await GetRatingCountByStatusAsync(ReviewModerationStatus.Rejected),
            ["Flagged"] = await GetRatingCountByStatusAsync(ReviewModerationStatus.Flagged)
        };

        return stats;
    }

    /// <inheritdoc />
    public async Task<SellerRating?> GetRatingByIdAsync(int ratingId)
    {
        return await _context.SellerRatings
            .Include(r => r.Store)
            .Include(r => r.User)
            .Include(r => r.ModeratedByUser)
            .Include(r => r.Flags)
                .ThenInclude(f => f.FlaggedByUser)
            .Include(r => r.Flags)
                .ThenInclude(f => f.ResolvedByUser)
            .Include(r => r.ModerationLogs)
                .ThenInclude(l => l.ModeratedByUser)
            .FirstOrDefaultAsync(r => r.Id == ratingId);
    }

    /// <inheritdoc />
    public async Task<List<SellerRatingFlag>> GetFlagsByRatingIdAsync(int ratingId, bool includeResolved = false)
    {
        var query = _context.SellerRatingFlags
            .Include(f => f.FlaggedByUser)
            .Include(f => f.ResolvedByUser)
            .Where(f => f.SellerRatingId == ratingId)
            .AsQueryable();

        if (!includeResolved)
        {
            query = query.Where(f => f.IsActive);
        }

        return await query
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    private async Task LogModerationActionAsync(
        int ratingId, 
        ReviewModerationAction action, 
        int? adminUserId, 
        string? reason, 
        ReviewModerationStatus? previousStatus, 
        ReviewModerationStatus? newStatus,
        bool isAutomated)
    {
        var log = new SellerRatingModerationLog
        {
            SellerRatingId = ratingId,
            Action = action,
            ModeratedByUserId = adminUserId,
            Reason = reason,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            CreatedAt = DateTime.UtcNow
        };

        _context.SellerRatingModerationLogs.Add(log);
    }
}
