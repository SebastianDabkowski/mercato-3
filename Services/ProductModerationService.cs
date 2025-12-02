using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing product moderation.
/// </summary>
public class ProductModerationService : IProductModerationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProductModerationService> _logger;

    public ProductModerationService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<ProductModerationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<Product>> GetProductsByModerationStatusAsync(
        ProductModerationStatus? status = null,
        int? categoryId = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.Products
            .Include(p => p.Store)
                .ThenInclude(s => s.User)
            .Include(p => p.CategoryEntity)
            .AsQueryable();

        // Filter by moderation status if provided
        if (status.HasValue)
        {
            query = query.Where(p => p.ModerationStatus == status.Value);
        }

        // Filter by category if provided
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // Order by creation date descending (newest first)
        query = query.OrderByDescending(p => p.CreatedAt);

        // Apply pagination
        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return products;
    }

    /// <inheritdoc />
    public async Task<int> GetProductCountByModerationStatusAsync(ProductModerationStatus? status = null)
    {
        var query = _context.Products.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.ModerationStatus == status.Value);
        }

        return await query.CountAsync();
    }

    /// <inheritdoc />
    public async Task<Product?> GetProductByIdAsync(int productId)
    {
        return await _context.Products
            .Include(p => p.Store)
                .ThenInclude(s => s.User)
            .Include(p => p.CategoryEntity)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId);
    }

    /// <inheritdoc />
    public async Task<Product> ApproveProductAsync(int productId, int adminUserId, string? reason = null)
    {
        var product = await _context.Products
            .Include(p => p.Store)
                .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {productId} not found.");
        }

        var previousStatus = product.ModerationStatus;

        // Update moderation status
        product.ModerationStatus = ProductModerationStatus.Approved;
        product.UpdatedAt = DateTime.UtcNow;

        // Log the moderation action
        var log = new ProductModerationLog
        {
            ProductId = productId,
            Action = ProductModerationAction.Approved,
            ModeratedByUserId = adminUserId,
            Reason = reason,
            PreviousStatus = previousStatus,
            NewStatus = ProductModerationStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductModerationLogs.Add(log);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product {ProductId} approved by admin user {AdminUserId}. Reason: {Reason}",
            productId,
            adminUserId,
            reason ?? "None provided");

        // Send notification email to seller
        try
        {
            await _emailService.SendProductModerationNotificationToSellerAsync(
                product,
                ProductModerationStatus.Approved,
                reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send product approval email for product {ProductId}", productId);
        }

        return product;
    }

    /// <inheritdoc />
    public async Task<Product> RejectProductAsync(int productId, int adminUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required when rejecting a product.", nameof(reason));
        }

        var product = await _context.Products
            .Include(p => p.Store)
                .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {productId} not found.");
        }

        var previousStatus = product.ModerationStatus;

        // Update moderation status
        product.ModerationStatus = ProductModerationStatus.Rejected;
        product.UpdatedAt = DateTime.UtcNow;

        // If product was active, set it to Draft so it's not visible
        if (product.Status == ProductStatus.Active)
        {
            product.Status = ProductStatus.Draft;
        }

        // Log the moderation action
        var log = new ProductModerationLog
        {
            ProductId = productId,
            Action = ProductModerationAction.Rejected,
            ModeratedByUserId = adminUserId,
            Reason = reason,
            PreviousStatus = previousStatus,
            NewStatus = ProductModerationStatus.Rejected,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductModerationLogs.Add(log);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product {ProductId} rejected by admin user {AdminUserId}. Reason: {Reason}",
            productId,
            adminUserId,
            reason);

        // Send notification email to seller
        try
        {
            await _emailService.SendProductModerationNotificationToSellerAsync(
                product,
                ProductModerationStatus.Rejected,
                reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send product rejection email for product {ProductId}", productId);
        }

        return product;
    }

    /// <inheritdoc />
    public async Task<int> BulkApproveProductsAsync(List<int> productIds, int adminUserId, string? reason = null)
    {
        if (productIds == null || productIds.Count == 0)
        {
            return 0;
        }

        var products = await _context.Products
            .Include(p => p.Store)
                .ThenInclude(s => s.User)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        int count = 0;
        foreach (var product in products)
        {
            var previousStatus = product.ModerationStatus;

            // Update moderation status
            product.ModerationStatus = ProductModerationStatus.Approved;
            product.UpdatedAt = DateTime.UtcNow;

            // Log the moderation action
            var log = new ProductModerationLog
            {
                ProductId = product.Id,
                Action = ProductModerationAction.Approved,
                ModeratedByUserId = adminUserId,
                Reason = reason ?? "Bulk approval",
                PreviousStatus = previousStatus,
                NewStatus = ProductModerationStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductModerationLogs.Add(log);
            count++;

            // Send notification email to seller
            try
            {
                await _emailService.SendProductModerationNotificationToSellerAsync(
                    product,
                    ProductModerationStatus.Approved,
                    reason ?? "Your product has been approved as part of a bulk approval.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk approval email for product {ProductId}", product.Id);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Bulk approved {Count} products by admin user {AdminUserId}",
            count,
            adminUserId);

        return count;
    }

    /// <inheritdoc />
    public async Task<int> BulkRejectProductsAsync(List<int> productIds, int adminUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required when rejecting products.", nameof(reason));
        }

        if (productIds == null || productIds.Count == 0)
        {
            return 0;
        }

        var products = await _context.Products
            .Include(p => p.Store)
                .ThenInclude(s => s.User)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        int count = 0;
        foreach (var product in products)
        {
            var previousStatus = product.ModerationStatus;

            // Update moderation status
            product.ModerationStatus = ProductModerationStatus.Rejected;
            product.UpdatedAt = DateTime.UtcNow;

            // If product was active, set it to Draft so it's not visible
            if (product.Status == ProductStatus.Active)
            {
                product.Status = ProductStatus.Draft;
            }

            // Log the moderation action
            var log = new ProductModerationLog
            {
                ProductId = product.Id,
                Action = ProductModerationAction.Rejected,
                ModeratedByUserId = adminUserId,
                Reason = reason,
                PreviousStatus = previousStatus,
                NewStatus = ProductModerationStatus.Rejected,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductModerationLogs.Add(log);
            count++;

            // Send notification email to seller
            try
            {
                await _emailService.SendProductModerationNotificationToSellerAsync(
                    product,
                    ProductModerationStatus.Rejected,
                    reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk rejection email for product {ProductId}", product.Id);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Bulk rejected {Count} products by admin user {AdminUserId}. Reason: {Reason}",
            count,
            adminUserId,
            reason);

        return count;
    }

    /// <inheritdoc />
    public async Task<List<ProductModerationLog>> GetProductModerationHistoryAsync(int productId)
    {
        return await _context.ProductModerationLogs
            .Include(l => l.ModeratedByUser)
            .Where(l => l.ProductId == productId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetModerationStatsAsync()
    {
        var stats = new Dictionary<string, int>();

        // Count products by moderation status
        var statusCounts = await _context.Products
            .GroupBy(p => p.ModerationStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        stats["Pending"] = statusCounts.FirstOrDefault(s => s.Status == ProductModerationStatus.Pending)?.Count ?? 0;
        stats["Approved"] = statusCounts.FirstOrDefault(s => s.Status == ProductModerationStatus.Approved)?.Count ?? 0;
        stats["Rejected"] = statusCounts.FirstOrDefault(s => s.Status == ProductModerationStatus.Rejected)?.Count ?? 0;
        stats["Total"] = await _context.Products.CountAsync();

        return stats;
    }
}
