using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for managing seller reputation scores.
/// Reputation scores are calculated based on seller ratings, dispute rate, on-time shipping, and cancelled orders.
/// </summary>
public interface ISellerReputationService
{
    /// <summary>
    /// Calculates and updates the reputation score for a specific seller.
    /// </summary>
    /// <param name="storeId">The store ID to calculate reputation for.</param>
    /// <returns>The calculated reputation score (0-100), or null if insufficient data.</returns>
    Task<decimal?> CalculateReputationScoreAsync(int storeId);

    /// <summary>
    /// Recalculates reputation scores for all active sellers in the marketplace.
    /// This method is designed for batch processing (e.g., scheduled jobs).
    /// </summary>
    /// <returns>The number of stores updated.</returns>
    Task<int> RecalculateAllReputationScoresAsync();

    /// <summary>
    /// Gets the reputation metrics breakdown for a seller.
    /// Useful for debugging and displaying detailed reputation information.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The reputation metrics for the store.</returns>
    Task<SellerReputationMetrics> GetReputationMetricsAsync(int storeId);
}

/// <summary>
/// Represents the detailed metrics used to calculate a seller's reputation score.
/// </summary>
public class SellerReputationMetrics
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the average seller rating (1-5 stars).
    /// </summary>
    public decimal? AverageRating { get; set; }

    /// <summary>
    /// Gets or sets the total number of ratings received.
    /// </summary>
    public int RatingCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of delivered orders.
    /// </summary>
    public int TotalDeliveredOrders { get; set; }

    /// <summary>
    /// Gets or sets the total number of shipped orders (including delivered).
    /// </summary>
    public int TotalShippedOrders { get; set; }

    /// <summary>
    /// Gets or sets the total number of cancelled orders.
    /// </summary>
    public int TotalCancelledOrders { get; set; }

    /// <summary>
    /// Gets or sets the total number of return/complaint requests.
    /// </summary>
    public int TotalDisputedOrders { get; set; }

    /// <summary>
    /// Gets or sets the total number of completed orders (for calculating rates).
    /// </summary>
    public int TotalCompletedOrders { get; set; }

    /// <summary>
    /// Gets or sets the on-time shipping rate (0-100%).
    /// Calculated as: (TotalDeliveredOrders / TotalShippedOrders) * 100
    /// </summary>
    public decimal OnTimeShippingRate { get; set; }

    /// <summary>
    /// Gets or sets the dispute rate (0-100%).
    /// Calculated as: (TotalDisputedOrders / TotalCompletedOrders) * 100
    /// </summary>
    public decimal DisputeRate { get; set; }

    /// <summary>
    /// Gets or sets the cancellation rate (0-100%).
    /// Calculated as: (TotalCancelledOrders / (TotalCompletedOrders + TotalCancelledOrders)) * 100
    /// </summary>
    public decimal CancellationRate { get; set; }

    /// <summary>
    /// Gets or sets the calculated reputation score (0-100).
    /// </summary>
    public decimal? ReputationScore { get; set; }
}
