using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing seller ratings.
/// </summary>
public class SellerRatingService : ISellerRatingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SellerRatingService> _logger;

    public SellerRatingService(
        ApplicationDbContext context,
        ILogger<SellerRatingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SellerRating> SubmitRatingAsync(int userId, int sellerSubOrderId, int rating, string? reviewText = null)
    {
        // Validate rating range
        if (rating < 1 || rating > 5)
        {
            throw new InvalidOperationException("Rating must be between 1 and 5 stars.");
        }

        // Get the sub-order with necessary related data
        var subOrder = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
            .FirstOrDefaultAsync(so => so.Id == sellerSubOrderId);

        if (subOrder == null)
        {
            _logger.LogWarning("Sub-order {SubOrderId} not found", sellerSubOrderId);
            throw new InvalidOperationException("Sub-order not found.");
        }

        // Verify the user is the buyer of this sub-order
        if (subOrder.ParentOrder.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to rate sub-order {SubOrderId} they don't own", userId, sellerSubOrderId);
            throw new InvalidOperationException("You can only rate sellers for your own orders.");
        }

        // Verify the sub-order is delivered
        if (subOrder.Status != OrderStatus.Delivered)
        {
            _logger.LogWarning("User {UserId} attempted to rate sub-order {SubOrderId} with status {Status}", userId, sellerSubOrderId, subOrder.Status);
            throw new InvalidOperationException("You can only rate sellers for delivered orders.");
        }

        // Check if the user has already rated this sub-order
        var existingRating = await _context.SellerRatings
            .FirstOrDefaultAsync(sr => sr.UserId == userId && sr.SellerSubOrderId == sellerSubOrderId);

        if (existingRating != null)
        {
            _logger.LogWarning("User {UserId} attempted to rate sub-order {SubOrderId} multiple times", userId, sellerSubOrderId);
            throw new InvalidOperationException("You have already rated this seller for this order.");
        }

        // Create the seller rating
        var sellerRating = new SellerRating
        {
            StoreId = subOrder.StoreId,
            UserId = userId,
            SellerSubOrderId = sellerSubOrderId,
            Rating = rating,
            ReviewText = reviewText,
            CreatedAt = DateTime.UtcNow,
            // Approve by default - auto-check will flag if needed
            IsApproved = true,
            ModerationStatus = ReviewModerationStatus.Approved,
            ApprovedAt = DateTime.UtcNow
        };

        _context.SellerRatings.Add(sellerRating);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} rated seller {StoreId} with {Rating} stars for sub-order {SubOrderId}", 
            userId, subOrder.StoreId, rating, sellerSubOrderId);

        return sellerRating;
    }

    /// <inheritdoc />
    public async Task<decimal?> GetAverageRatingAsync(int storeId)
    {
        // Only include approved ratings in the average calculation
        var average = await _context.SellerRatings
            .Where(sr => sr.StoreId == storeId && sr.ModerationStatus == ReviewModerationStatus.Approved)
            .Select(sr => (decimal?)sr.Rating)
            .AverageAsync();

        return average;
    }

    /// <inheritdoc />
    public async Task<int> GetRatingCountAsync(int storeId)
    {
        // Only count approved ratings
        return await _context.SellerRatings
            .CountAsync(sr => sr.StoreId == storeId && sr.ModerationStatus == ReviewModerationStatus.Approved);
    }

    /// <inheritdoc />
    public async Task<bool> HasUserRatedSubOrderAsync(int userId, int sellerSubOrderId)
    {
        return await _context.SellerRatings
            .AnyAsync(sr => sr.UserId == userId && sr.SellerSubOrderId == sellerSubOrderId);
    }
}
