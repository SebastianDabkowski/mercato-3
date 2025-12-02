using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing product reviews.
/// </summary>
public class ProductReviewService : IProductReviewService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductReviewService> _logger;

    public ProductReviewService(
        ApplicationDbContext context,
        ILogger<ProductReviewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProductReview> SubmitReviewAsync(int userId, int orderItemId, int rating, string? reviewText)
    {
        // Validate rating
        if (rating < 1 || rating > 5)
        {
            throw new InvalidOperationException("Rating must be between 1 and 5 stars.");
        }

        // Validate review text length
        if (!string.IsNullOrEmpty(reviewText) && reviewText.Length > 2000)
        {
            throw new InvalidOperationException("Review text must not exceed 2000 characters.");
        }

        // Get the order item with related data
        var orderItem = await _context.OrderItems
            .Include(oi => oi.SellerSubOrder)
                .ThenInclude(so => so!.ParentOrder)
            .Include(oi => oi.Product)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

        if (orderItem == null)
        {
            throw new InvalidOperationException("Order item not found.");
        }

        if (orderItem.SellerSubOrder == null)
        {
            throw new InvalidOperationException("Order item is not associated with a sub-order.");
        }

        // Verify the user owns this order
        if (orderItem.SellerSubOrder.ParentOrder.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to review order item {OrderItemId} they don't own", userId, orderItemId);
            throw new InvalidOperationException("You are not authorized to review this product.");
        }

        // Verify the sub-order has been delivered
        if (orderItem.SellerSubOrder.Status != OrderStatus.Delivered)
        {
            throw new InvalidOperationException("You can only review products from delivered orders.");
        }

        // Check if user has already reviewed this order item (rate limiting per order item)
        var existingReview = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.OrderItemId == orderItemId);

        if (existingReview != null)
        {
            throw new InvalidOperationException("You have already reviewed this product from this order.");
        }

        // Check rate limiting: max 10 reviews per user per day
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var reviewsToday = await _context.ProductReviews
            .CountAsync(r => r.UserId == userId && r.CreatedAt >= today && r.CreatedAt < tomorrow);

        if (reviewsToday >= 10)
        {
            _logger.LogWarning("User {UserId} exceeded daily review limit", userId);
            throw new InvalidOperationException("You have reached the maximum number of reviews for today. Please try again tomorrow.");
        }

        // Create the review
        var review = new ProductReview
        {
            ProductId = orderItem.ProductId,
            UserId = userId,
            OrderItemId = orderItemId,
            Rating = rating,
            ReviewText = reviewText,
            IsApproved = true, // Auto-approve for now; can add moderation later
            CreatedAt = DateTime.UtcNow,
            ApprovedAt = DateTime.UtcNow
        };

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} submitted review for product {ProductId} with rating {Rating}", userId, orderItem.ProductId, rating);

        return review;
    }

    /// <inheritdoc />
    public async Task<List<ProductReview>> GetApprovedReviewsForProductAsync(int productId)
    {
        return await _context.ProductReviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ProductReview>> GetApprovedReviewsForProductAsync(int productId, ReviewSortOption sortOption, int page, int pageSize)
    {
        if (page < 1)
        {
            _logger.LogWarning("Invalid page number {Page} provided, defaulting to 1", page);
            page = 1;
        }

        if (pageSize < 1 || pageSize > 100)
        {
            _logger.LogWarning("Invalid page size {PageSize} provided, defaulting to 10", pageSize);
            pageSize = 10;
        }

        var query = _context.ProductReviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId && r.IsApproved);

        // Apply sorting
        query = sortOption switch
        {
            ReviewSortOption.HighestRating => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            ReviewSortOption.LowestRating => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            ReviewSortOption.Newest or _ => query.OrderByDescending(r => r.CreatedAt)
        };

        // Apply pagination
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetApprovedReviewCountAsync(int productId)
    {
        return await _context.ProductReviews
            .CountAsync(r => r.ProductId == productId && r.IsApproved);
    }

    /// <inheritdoc />
    public async Task<decimal?> GetAverageRatingAsync(int productId)
    {
        var hasReviews = await _context.ProductReviews
            .AnyAsync(r => r.ProductId == productId && r.IsApproved);

        if (!hasReviews)
        {
            return null;
        }

        return await _context.ProductReviews
            .Where(r => r.ProductId == productId && r.IsApproved)
            .AverageAsync(r => (decimal)r.Rating);
    }

    /// <inheritdoc />
    public async Task<bool> HasUserReviewedOrderItemAsync(int userId, int orderItemId)
    {
        return await _context.ProductReviews
            .AnyAsync(r => r.UserId == userId && r.OrderItemId == orderItemId);
    }
}
