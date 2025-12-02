using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing review moderation and flagging.
/// </summary>
public class ReviewModerationService : IReviewModerationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReviewModerationService> _logger;

    // Automated flagging keywords - can be moved to configuration
    private static readonly HashSet<string> InappropriateKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "spam", "fake", "scam", "fraud", "cheat", "lie", "liar", "steal"
    };

    public ReviewModerationService(
        ApplicationDbContext context,
        ILogger<ReviewModerationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReviewFlag> FlagReviewAsync(int reviewId, ReviewFlagReason reason, string? details, int? flaggedByUserId, bool isAutomated = false)
    {
        var review = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
        {
            throw new InvalidOperationException("Review not found.");
        }

        // Check if there's already an active flag for the same reason
        var existingFlag = await _context.ReviewFlags
            .FirstOrDefaultAsync(f => f.ProductReviewId == reviewId && f.Reason == reason && f.IsActive);

        if (existingFlag != null)
        {
            _logger.LogInformation("Review {ReviewId} already has an active flag for reason {Reason}", reviewId, reason);
            return existingFlag;
        }

        var flag = new ReviewFlag
        {
            ProductReviewId = reviewId,
            Reason = reason,
            Details = details,
            FlaggedByUserId = flaggedByUserId,
            IsAutomated = isAutomated,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.ReviewFlags.Add(flag);

        // Update review status to Flagged if it's currently approved
        if (review.ModerationStatus == ReviewModerationStatus.Approved)
        {
            var previousStatus = review.ModerationStatus;
            review.ModerationStatus = ReviewModerationStatus.Flagged;

            // Log the action
            await LogModerationActionAsync(
                reviewId,
                isAutomated ? ReviewModerationAction.AutoFlagged : ReviewModerationAction.Flagged,
                flaggedByUserId,
                $"Flagged for {reason}: {details}",
                previousStatus,
                ReviewModerationStatus.Flagged,
                isAutomated
            );
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} flagged for {Reason} by user {UserId} (automated: {IsAutomated})", 
            reviewId, reason, flaggedByUserId, isAutomated);

        return flag;
    }

    /// <inheritdoc />
    public async Task<ProductReview> ApproveReviewAsync(int reviewId, int adminUserId, string? reason = null)
    {
        var review = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
        {
            throw new InvalidOperationException("Review not found.");
        }

        var previousStatus = review.ModerationStatus;

        review.IsApproved = true;
        review.ModerationStatus = ReviewModerationStatus.Approved;
        review.ModeratedByUserId = adminUserId;
        review.ModeratedAt = DateTime.UtcNow;
        review.ApprovedAt = DateTime.UtcNow;

        // Resolve any active flags
        var activeFlags = await _context.ReviewFlags
            .Where(f => f.ProductReviewId == reviewId && f.IsActive)
            .ToListAsync();

        foreach (var flag in activeFlags)
        {
            flag.IsActive = false;
            flag.ResolvedAt = DateTime.UtcNow;
            flag.ResolvedByUserId = adminUserId;
        }

        // Log the action
        await LogModerationActionAsync(
            reviewId,
            ReviewModerationAction.Approved,
            adminUserId,
            reason ?? "Review approved by admin",
            previousStatus,
            ReviewModerationStatus.Approved,
            false
        );

        await _context.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} approved by admin {AdminUserId}", reviewId, adminUserId);

        return review;
    }

    /// <inheritdoc />
    public async Task<ProductReview> RejectReviewAsync(int reviewId, int adminUserId, string reason)
    {
        var review = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
        {
            throw new InvalidOperationException("Review not found.");
        }

        var previousStatus = review.ModerationStatus;

        review.IsApproved = false;
        review.ModerationStatus = ReviewModerationStatus.Rejected;
        review.ModeratedByUserId = adminUserId;
        review.ModeratedAt = DateTime.UtcNow;

        // Resolve any active flags
        var activeFlags = await _context.ReviewFlags
            .Where(f => f.ProductReviewId == reviewId && f.IsActive)
            .ToListAsync();

        foreach (var flag in activeFlags)
        {
            flag.IsActive = false;
            flag.ResolvedAt = DateTime.UtcNow;
            flag.ResolvedByUserId = adminUserId;
        }

        // Log the action
        await LogModerationActionAsync(
            reviewId,
            ReviewModerationAction.Rejected,
            adminUserId,
            reason,
            previousStatus,
            ReviewModerationStatus.Rejected,
            false
        );

        await _context.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} rejected by admin {AdminUserId}: {Reason}", reviewId, adminUserId, reason);

        return review;
    }

    /// <inheritdoc />
    public async Task<ProductReview> ToggleReviewVisibilityAsync(int reviewId, int adminUserId, bool isVisible, string? reason = null)
    {
        var review = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
        {
            throw new InvalidOperationException("Review not found.");
        }

        var previousStatus = review.ModerationStatus;

        review.IsApproved = isVisible;
        review.ModeratedByUserId = adminUserId;
        review.ModeratedAt = DateTime.UtcNow;

        // Log the action
        await LogModerationActionAsync(
            reviewId,
            ReviewModerationAction.VisibilityEdited,
            adminUserId,
            reason ?? $"Visibility changed to {(isVisible ? "visible" : "hidden")}",
            previousStatus,
            review.ModerationStatus,
            false
        );

        await _context.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} visibility toggled to {IsVisible} by admin {AdminUserId}", 
            reviewId, isVisible, adminUserId);

        return review;
    }

    /// <inheritdoc />
    public async Task<ReviewFlag> ResolveFlagAsync(int flagId, int adminUserId)
    {
        var flag = await _context.ReviewFlags
            .FirstOrDefaultAsync(f => f.Id == flagId);

        if (flag == null)
        {
            throw new InvalidOperationException("Flag not found.");
        }

        flag.IsActive = false;
        flag.ResolvedAt = DateTime.UtcNow;
        flag.ResolvedByUserId = adminUserId;

        // Check if there are any other active flags for this review
        var otherActiveFlags = await _context.ReviewFlags
            .AnyAsync(f => f.ProductReviewId == flag.ProductReviewId && f.Id != flagId && f.IsActive);

        // If no other active flags, update review status from Flagged back to Approved
        if (!otherActiveFlags)
        {
            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.Id == flag.ProductReviewId);

            if (review != null && review.ModerationStatus == ReviewModerationStatus.Flagged)
            {
                var previousStatus = review.ModerationStatus;
                review.ModerationStatus = ReviewModerationStatus.Approved;

                await LogModerationActionAsync(
                    review.Id,
                    ReviewModerationAction.Unflagged,
                    adminUserId,
                    "All flags resolved",
                    previousStatus,
                    ReviewModerationStatus.Approved,
                    false
                );
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Flag {FlagId} resolved by admin {AdminUserId}", flagId, adminUserId);

        return flag;
    }

    /// <inheritdoc />
    public async Task<List<ReviewFlag>> GetFlaggedReviewsAsync(bool includeResolved = false)
    {
        var query = _context.ReviewFlags
            .Include(f => f.ProductReview)
                .ThenInclude(r => r.Product)
            .Include(f => f.ProductReview)
                .ThenInclude(r => r.User)
            .Include(f => f.FlaggedByUser)
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
    public async Task<List<ProductReview>> GetReviewsByStatusAsync(ReviewModerationStatus status, int page = 1, int pageSize = 20)
    {
        if (page < 1)
        {
            _logger.LogWarning("Invalid page number {Page} provided, defaulting to 1", page);
            page = 1;
        }

        if (pageSize < 1 || pageSize > 100)
        {
            _logger.LogWarning("Invalid page size {PageSize} provided, defaulting to 20", pageSize);
            pageSize = 20;
        }

        return await _context.ProductReviews
            .Include(r => r.Product)
            .Include(r => r.User)
            .Include(r => r.ModeratedByUser)
            .Where(r => r.ModerationStatus == status)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetReviewCountByStatusAsync(ReviewModerationStatus status)
    {
        return await _context.ProductReviews
            .CountAsync(r => r.ModerationStatus == status);
    }

    /// <inheritdoc />
    public async Task<List<ReviewModerationLog>> GetReviewModerationHistoryAsync(int reviewId)
    {
        return await _context.ReviewModerationLogs
            .Include(l => l.ModeratedByUser)
            .Where(l => l.ProductReviewId == reviewId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> AutoCheckReviewAsync(int reviewId)
    {
        var review = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
        {
            throw new InvalidOperationException("Review not found.");
        }

        if (string.IsNullOrWhiteSpace(review.ReviewText))
        {
            return false;
        }

        var reviewText = review.ReviewText.ToLowerInvariant();
        var flagged = false;

        // Check for inappropriate keywords
        foreach (var keyword in InappropriateKeywords)
        {
            if (Regex.IsMatch(reviewText, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase))
            {
                await FlagReviewAsync(
                    reviewId,
                    ReviewFlagReason.InappropriateLanguage,
                    $"Contains keyword: {keyword}",
                    null,
                    true
                );
                flagged = true;
                break;
            }
        }

        // Check for excessive caps (might indicate shouting/spam)
        if (!flagged)
        {
            var capsCount = review.ReviewText.Count(c => char.IsUpper(c));
            var lettersCount = review.ReviewText.Count(c => char.IsLetter(c));
            if (lettersCount > 20 && capsCount > lettersCount * 0.7)
            {
                await FlagReviewAsync(
                    reviewId,
                    ReviewFlagReason.Spam,
                    "Excessive use of capital letters",
                    null,
                    true
                );
                flagged = true;
            }
        }

        // Check for URL patterns (might be spam)
        if (!flagged && Regex.IsMatch(reviewText, @"(http|www\.|\w+\.(com|org|net|io))", RegexOptions.IgnoreCase))
        {
            await FlagReviewAsync(
                reviewId,
                ReviewFlagReason.Spam,
                "Contains URL or website reference",
                null,
                true
            );
            flagged = true;
        }

        // Check for email patterns (personal information)
        if (!flagged && Regex.IsMatch(reviewText, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b"))
        {
            await FlagReviewAsync(
                reviewId,
                ReviewFlagReason.PersonalInformation,
                "Contains email address",
                null,
                true
            );
            flagged = true;
        }

        // Check for phone number patterns (personal information)
        if (!flagged && Regex.IsMatch(reviewText, @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b"))
        {
            await FlagReviewAsync(
                reviewId,
                ReviewFlagReason.PersonalInformation,
                "Contains phone number",
                null,
                true
            );
            flagged = true;
        }

        return flagged;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetModerationStatsAsync()
    {
        var stats = new Dictionary<string, int>();

        foreach (ReviewModerationStatus status in Enum.GetValues(typeof(ReviewModerationStatus)))
        {
            var count = await GetReviewCountByStatusAsync(status);
            stats[status.ToString()] = count;
        }

        var activeFlagsCount = await _context.ReviewFlags.CountAsync(f => f.IsActive);
        stats["ActiveFlags"] = activeFlagsCount;

        var totalReviews = await _context.ProductReviews.CountAsync();
        stats["TotalReviews"] = totalReviews;

        return stats;
    }

    /// <summary>
    /// Logs a moderation action to the audit trail.
    /// </summary>
    private async Task LogModerationActionAsync(
        int reviewId,
        ReviewModerationAction action,
        int? moderatedByUserId,
        string? reason,
        ReviewModerationStatus? previousStatus,
        ReviewModerationStatus? newStatus,
        bool isAutomated)
    {
        var log = new ReviewModerationLog
        {
            ProductReviewId = reviewId,
            Action = action,
            ModeratedByUserId = moderatedByUserId,
            Reason = reason,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            CreatedAt = DateTime.UtcNow,
            IsAutomated = isAutomated
        };

        _context.ReviewModerationLogs.Add(log);
    }
}
