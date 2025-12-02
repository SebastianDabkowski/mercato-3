using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing product photo moderation.
/// </summary>
public class PhotoModerationService : IPhotoModerationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<PhotoModerationService> _logger;

    public PhotoModerationService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<PhotoModerationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProductImage>> GetPhotosByModerationStatusAsync(
        PhotoModerationStatus? status = null,
        int? productId = null,
        int? storeId = null,
        bool flaggedOnly = false,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.ProductImages
            .Include(pi => pi.Product)
                .ThenInclude(p => p.Store)
                    .ThenInclude(s => s.User)
            .Include(pi => pi.Product)
                .ThenInclude(p => p.CategoryEntity)
            .AsQueryable();

        // Filter by moderation status if provided
        if (status.HasValue)
        {
            query = query.Where(pi => pi.ModerationStatus == status.Value);
        }

        // Filter by product if provided
        if (productId.HasValue)
        {
            query = query.Where(pi => pi.ProductId == productId.Value);
        }

        // Filter by store if provided
        if (storeId.HasValue)
        {
            query = query.Where(pi => pi.Product.StoreId == storeId.Value);
        }

        // Filter flagged photos only
        if (flaggedOnly)
        {
            query = query.Where(pi => pi.IsFlagged);
        }

        // Order by creation date descending (newest first)
        query = query.OrderByDescending(pi => pi.CreatedAt);

        // Apply pagination
        var photos = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return photos;
    }

    /// <inheritdoc />
    public async Task<int> GetPhotoCountByModerationStatusAsync(PhotoModerationStatus? status = null, bool flaggedOnly = false)
    {
        var query = _context.ProductImages.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(pi => pi.ModerationStatus == status.Value);
        }

        if (flaggedOnly)
        {
            query = query.Where(pi => pi.IsFlagged);
        }

        return await query.CountAsync();
    }

    /// <inheritdoc />
    public async Task<ProductImage?> GetPhotoByIdAsync(int imageId)
    {
        return await _context.ProductImages
            .Include(pi => pi.Product)
                .ThenInclude(p => p.Store)
                    .ThenInclude(s => s.User)
            .Include(pi => pi.Product)
                .ThenInclude(p => p.CategoryEntity)
            .FirstOrDefaultAsync(pi => pi.Id == imageId);
    }

    /// <inheritdoc />
    public async Task<ProductImage> ApprovePhotoAsync(int imageId, int adminUserId, string? reason = null)
    {
        var photo = await _context.ProductImages
            .Include(pi => pi.Product)
                .ThenInclude(p => p.Store)
                    .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(pi => pi.Id == imageId);

        if (photo == null)
        {
            throw new InvalidOperationException($"Product image with ID {imageId} not found.");
        }

        var previousStatus = photo.ModerationStatus;

        // Update moderation status
        photo.ModerationStatus = PhotoModerationStatus.Approved;
        photo.IsFlagged = false;

        // Log the moderation action
        var log = new PhotoModerationLog
        {
            ProductImageId = imageId,
            Action = PhotoModerationAction.Approved,
            ModeratedByUserId = adminUserId,
            Reason = reason,
            PreviousStatus = previousStatus,
            NewStatus = PhotoModerationStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };

        _context.PhotoModerationLogs.Add(log);

        // Resolve any open flags for this photo
        var unresolvedFlags = await _context.PhotoFlags
            .Where(f => f.ProductImageId == imageId && !f.IsResolved)
            .ToListAsync();

        foreach (var flag in unresolvedFlags)
        {
            flag.IsResolved = true;
            flag.ResolvedAt = DateTime.UtcNow;
            flag.ResolvedByUserId = adminUserId;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product photo {ImageId} for product {ProductId} approved by admin user {AdminUserId}. Reason: {Reason}",
            imageId,
            photo.ProductId,
            adminUserId,
            reason ?? "None provided");

        return photo;
    }

    /// <inheritdoc />
    public async Task<ProductImage> RemovePhotoAsync(int imageId, int adminUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required when removing a photo.", nameof(reason));
        }

        var photo = await _context.ProductImages
            .Include(pi => pi.Product)
                .ThenInclude(p => p.Store)
                    .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(pi => pi.Id == imageId);

        if (photo == null)
        {
            throw new InvalidOperationException($"Product image with ID {imageId} not found.");
        }

        var previousStatus = photo.ModerationStatus;

        // Update moderation status
        photo.ModerationStatus = PhotoModerationStatus.Rejected;
        photo.IsRemoved = true;
        photo.RemovalReason = reason;
        photo.RemovedAt = DateTime.UtcNow;
        photo.IsFlagged = false;

        // Archive the original URL for legal retention
        if (!string.IsNullOrEmpty(photo.ImageUrl))
        {
            photo.ArchivedUrl = photo.ImageUrl;
        }

        // Log the moderation action
        var log = new PhotoModerationLog
        {
            ProductImageId = imageId,
            Action = PhotoModerationAction.Removed,
            ModeratedByUserId = adminUserId,
            Reason = reason,
            PreviousStatus = previousStatus,
            NewStatus = PhotoModerationStatus.Rejected,
            CreatedAt = DateTime.UtcNow
        };

        _context.PhotoModerationLogs.Add(log);

        // Resolve any open flags for this photo
        var unresolvedFlags = await _context.PhotoFlags
            .Where(f => f.ProductImageId == imageId && !f.IsResolved)
            .ToListAsync();

        foreach (var flag in unresolvedFlags)
        {
            flag.IsResolved = true;
            flag.ResolvedAt = DateTime.UtcNow;
            flag.ResolvedByUserId = adminUserId;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product photo {ImageId} for product {ProductId} removed by admin user {AdminUserId}. Reason: {Reason}",
            imageId,
            photo.ProductId,
            adminUserId,
            reason);

        // Send notification email to seller
        try
        {
            await _emailService.SendPhotoRemovedNotificationToSellerAsync(photo, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send photo removal email for photo {ImageId}", imageId);
        }

        return photo;
    }

    /// <inheritdoc />
    public async Task<PhotoFlag> FlagPhotoAsync(int imageId, int? userId, string reason, bool isAutomated = false)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required when flagging a photo.", nameof(reason));
        }

        var photo = await _context.ProductImages
            .Include(pi => pi.Product)
            .FirstOrDefaultAsync(pi => pi.Id == imageId);

        if (photo == null)
        {
            throw new InvalidOperationException($"Product image with ID {imageId} not found.");
        }

        // Create the flag
        var flag = new PhotoFlag
        {
            ProductImageId = imageId,
            FlaggedByUserId = userId,
            Reason = reason,
            IsAutomated = isAutomated,
            CreatedAt = DateTime.UtcNow,
            IsResolved = false
        };

        _context.PhotoFlags.Add(flag);

        // Update photo to be flagged
        photo.IsFlagged = true;
        if (photo.ModerationStatus == PhotoModerationStatus.Approved)
        {
            photo.ModerationStatus = PhotoModerationStatus.Flagged;
        }

        // Log the flag action
        var log = new PhotoModerationLog
        {
            ProductImageId = imageId,
            Action = isAutomated ? PhotoModerationAction.AutoFlagged : PhotoModerationAction.Flagged,
            ModeratedByUserId = userId,
            Reason = reason,
            PreviousStatus = photo.ModerationStatus,
            NewStatus = PhotoModerationStatus.Flagged,
            CreatedAt = DateTime.UtcNow,
            IsAutomated = isAutomated
        };

        _context.PhotoModerationLogs.Add(log);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product photo {ImageId} for product {ProductId} flagged. Automated: {IsAutomated}, Reason: {Reason}",
            imageId,
            photo.ProductId,
            isAutomated,
            reason);

        return flag;
    }

    /// <inheritdoc />
    public async Task<List<PhotoFlag>> GetPhotoFlagsAsync(int imageId)
    {
        return await _context.PhotoFlags
            .Include(f => f.FlaggedByUser)
            .Include(f => f.ResolvedByUser)
            .Where(f => f.ProductImageId == imageId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<PhotoModerationLog>> GetPhotoModerationHistoryAsync(int imageId)
    {
        return await _context.PhotoModerationLogs
            .Include(l => l.ModeratedByUser)
            .Where(l => l.ProductImageId == imageId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetModerationStatsAsync()
    {
        var stats = new Dictionary<string, int>();

        // Count photos by moderation status
        var statusCounts = await _context.ProductImages
            .GroupBy(pi => pi.ModerationStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        stats["Pending"] = statusCounts.FirstOrDefault(s => s.Status == PhotoModerationStatus.PendingReview)?.Count ?? 0;
        stats["Approved"] = statusCounts.FirstOrDefault(s => s.Status == PhotoModerationStatus.Approved)?.Count ?? 0;
        stats["Rejected"] = statusCounts.FirstOrDefault(s => s.Status == PhotoModerationStatus.Rejected)?.Count ?? 0;
        stats["Flagged"] = statusCounts.FirstOrDefault(s => s.Status == PhotoModerationStatus.Flagged)?.Count ?? 0;
        stats["Total"] = await _context.ProductImages.CountAsync();
        stats["TotalFlagged"] = await _context.ProductImages.CountAsync(pi => pi.IsFlagged);

        return stats;
    }

    /// <inheritdoc />
    public async Task<int> BulkApprovePhotosAsync(List<int> imageIds, int adminUserId, string? reason = null)
    {
        if (imageIds == null || imageIds.Count == 0)
        {
            return 0;
        }

        var photos = await _context.ProductImages
            .Include(pi => pi.Product)
                .ThenInclude(p => p.Store)
                    .ThenInclude(s => s.User)
            .Where(pi => imageIds.Contains(pi.Id))
            .ToListAsync();

        int count = 0;
        foreach (var photo in photos)
        {
            var previousStatus = photo.ModerationStatus;

            // Update moderation status
            photo.ModerationStatus = PhotoModerationStatus.Approved;
            photo.IsFlagged = false;

            // Log the moderation action
            var log = new PhotoModerationLog
            {
                ProductImageId = photo.Id,
                Action = PhotoModerationAction.Approved,
                ModeratedByUserId = adminUserId,
                Reason = reason ?? "Bulk approval",
                PreviousStatus = previousStatus,
                NewStatus = PhotoModerationStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };

            _context.PhotoModerationLogs.Add(log);

            // Resolve any open flags for this photo
            var unresolvedFlags = await _context.PhotoFlags
                .Where(f => f.ProductImageId == photo.Id && !f.IsResolved)
                .ToListAsync();

            foreach (var flag in unresolvedFlags)
            {
                flag.IsResolved = true;
                flag.ResolvedAt = DateTime.UtcNow;
                flag.ResolvedByUserId = adminUserId;
            }

            count++;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Bulk approved {Count} photos by admin user {AdminUserId}",
            count,
            adminUserId);

        return count;
    }
}
