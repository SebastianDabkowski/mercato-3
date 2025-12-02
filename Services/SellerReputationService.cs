using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing seller reputation scores.
/// Reputation scores are calculated based on seller ratings, dispute rate, on-time shipping, and cancelled orders.
/// </summary>
public class SellerReputationService : ISellerReputationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SellerReputationService> _logger;

    // Weights for reputation score calculation (total must equal 100)
    private const decimal RATING_WEIGHT = 40m;           // 40% - Seller ratings
    private const decimal ON_TIME_WEIGHT = 30m;          // 30% - On-time delivery rate
    private const decimal DISPUTE_WEIGHT = 20m;          // 20% - Low dispute rate (inverted)
    private const decimal CANCELLATION_WEIGHT = 10m;     // 10% - Low cancellation rate (inverted)

    // Minimum orders required to calculate a reputation score
    private const int MINIMUM_ORDERS_FOR_REPUTATION = 5;

    public SellerReputationService(
        ApplicationDbContext context,
        ILogger<SellerReputationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<decimal?> CalculateReputationScoreAsync(int storeId)
    {
        var metrics = await GetReputationMetricsAsync(storeId);

        // Check if the store has enough orders to calculate reputation
        if (metrics.TotalCompletedOrders < MINIMUM_ORDERS_FOR_REPUTATION)
        {
            _logger.LogInformation(
                "Store {StoreId} has insufficient orders ({Count}) for reputation calculation (minimum: {Min})",
                storeId, metrics.TotalCompletedOrders, MINIMUM_ORDERS_FOR_REPUTATION);
            
            // Clear the reputation score if it exists
            var store = await _context.Stores.FindAsync(storeId);
            if (store != null && store.ReputationScore.HasValue)
            {
                store.ReputationScore = null;
                store.ReputationScoreUpdatedAt = null;
                await _context.SaveChangesAsync();
            }
            
            return null;
        }

        // Calculate reputation score based on weighted formula
        decimal reputationScore = 0m;

        // 1. Rating Score (40%) - Convert 1-5 stars to 0-100 scale
        if (metrics.AverageRating.HasValue)
        {
            decimal ratingScore = ((metrics.AverageRating.Value - 1) / 4m) * 100m;
            reputationScore += ratingScore * (RATING_WEIGHT / 100m);
        }

        // 2. On-Time Shipping Score (30%) - Already in 0-100 scale
        reputationScore += metrics.OnTimeShippingRate * (ON_TIME_WEIGHT / 100m);

        // 3. Dispute Score (20%) - Invert because lower is better
        decimal disputeScore = Math.Max(0, 100m - metrics.DisputeRate);
        reputationScore += disputeScore * (DISPUTE_WEIGHT / 100m);

        // 4. Cancellation Score (10%) - Invert because lower is better
        decimal cancellationScore = Math.Max(0, 100m - metrics.CancellationRate);
        reputationScore += cancellationScore * (CANCELLATION_WEIGHT / 100m);

        // Ensure score is within 0-100 range
        reputationScore = Math.Max(0, Math.Min(100, reputationScore));

        // Round to 2 decimal places
        reputationScore = Math.Round(reputationScore, 2);

        // Update the store's reputation score
        var storeToUpdate = await _context.Stores.FindAsync(storeId);
        if (storeToUpdate != null)
        {
            storeToUpdate.ReputationScore = reputationScore;
            storeToUpdate.ReputationScoreUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated reputation score for Store {StoreId}: {Score} (Rating: {Rating}, OnTime: {OnTime}%, Disputes: {Dispute}%, Cancellations: {Cancel}%)",
                storeId, reputationScore, metrics.AverageRating, metrics.OnTimeShippingRate, metrics.DisputeRate, metrics.CancellationRate);
        }

        return reputationScore;
    }

    /// <inheritdoc />
    public async Task<int> RecalculateAllReputationScoresAsync()
    {
        _logger.LogInformation("Starting batch recalculation of all seller reputation scores");

        var activeStores = await _context.Stores
            .Where(s => s.Status == StoreStatus.Active || s.Status == StoreStatus.LimitedActive)
            .Select(s => s.Id)
            .ToListAsync();

        int updatedCount = 0;

        foreach (var storeId in activeStores)
        {
            try
            {
                var score = await CalculateReputationScoreAsync(storeId);
                if (score.HasValue)
                {
                    updatedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating reputation score for Store {StoreId}", storeId);
            }
        }

        _logger.LogInformation(
            "Completed batch recalculation: {Updated} stores updated out of {Total} active stores",
            updatedCount, activeStores.Count);

        return updatedCount;
    }

    /// <inheritdoc />
    public async Task<SellerReputationMetrics> GetReputationMetricsAsync(int storeId)
    {
        var metrics = new SellerReputationMetrics
        {
            StoreId = storeId
        };

        // Get average rating and count
        var ratings = await _context.SellerRatings
            .Where(sr => sr.StoreId == storeId)
            .Select(sr => sr.Rating)
            .ToListAsync();

        if (ratings.Any())
        {
            metrics.AverageRating = (decimal)ratings.Average();
            metrics.RatingCount = ratings.Count;
        }

        // Get order statistics for this store
        var subOrders = await _context.SellerSubOrders
            .Where(so => so.StoreId == storeId)
            .ToListAsync();

        // Count delivered orders
        metrics.TotalDeliveredOrders = subOrders.Count(so => so.Status == OrderStatus.Delivered);

        // Count shipped orders (including delivered, as delivered means it was shipped first)
        metrics.TotalShippedOrders = subOrders.Count(so => 
            so.Status == OrderStatus.Shipped || so.Status == OrderStatus.Delivered);

        // Count cancelled orders
        metrics.TotalCancelledOrders = subOrders.Count(so => so.Status == OrderStatus.Cancelled);

        // Count disputed orders (return requests)
        var disputedSubOrderIds = await _context.ReturnRequests
            .Where(rr => subOrders.Select(so => so.Id).Contains(rr.SubOrderId))
            .Select(rr => rr.SubOrderId)
            .Distinct()
            .ToListAsync();
        metrics.TotalDisputedOrders = disputedSubOrderIds.Count;

        // Count completed orders (delivered + refunded)
        metrics.TotalCompletedOrders = subOrders.Count(so => 
            so.Status == OrderStatus.Delivered || so.Status == OrderStatus.Refunded);

        // Calculate rates
        if (metrics.TotalShippedOrders > 0)
        {
            metrics.OnTimeShippingRate = Math.Round(
                (decimal)metrics.TotalDeliveredOrders / metrics.TotalShippedOrders * 100m, 2);
        }

        if (metrics.TotalCompletedOrders > 0)
        {
            metrics.DisputeRate = Math.Round(
                (decimal)metrics.TotalDisputedOrders / metrics.TotalCompletedOrders * 100m, 2);
        }

        var totalOrdersIncludingCancelled = metrics.TotalCompletedOrders + metrics.TotalCancelledOrders;
        if (totalOrdersIncludingCancelled > 0)
        {
            metrics.CancellationRate = Math.Round(
                (decimal)metrics.TotalCancelledOrders / totalOrdersIncludingCancelled * 100m, 2);
        }

        // Get current reputation score from database
        var store = await _context.Stores.FindAsync(storeId);
        if (store != null)
        {
            metrics.ReputationScore = store.ReputationScore;
        }

        return metrics;
    }
}
