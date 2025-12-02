namespace MercatoApp.Services;

/// <summary>
/// Interface for seller dashboard service.
/// </summary>
public interface ISellerDashboardService
{
    /// <summary>
    /// Gets sales metrics for a specific store over time with the specified granularity.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="granularity">The time granularity (day, week, month).</param>
    /// <param name="productId">Optional product ID filter.</param>
    /// <param name="categoryId">Optional category ID filter.</param>
    /// <returns>The seller dashboard metrics.</returns>
    Task<SellerDashboardMetrics> GetMetricsAsync(
        int storeId,
        DateTime startDate,
        DateTime endDate,
        TimeGranularity granularity,
        int? productId = null,
        int? categoryId = null);
}

/// <summary>
/// Time granularity for dashboard metrics.
/// </summary>
public enum TimeGranularity
{
    /// <summary>
    /// Daily granularity.
    /// </summary>
    Day,

    /// <summary>
    /// Weekly granularity.
    /// </summary>
    Week,

    /// <summary>
    /// Monthly granularity.
    /// </summary>
    Month
}

/// <summary>
/// Represents seller dashboard metrics over time.
/// </summary>
public class SellerDashboardMetrics
{
    /// <summary>
    /// Gets or sets the total Gross Merchandise Value (GMV) for the period.
    /// GMV is the sum of all order item subtotals (quantity * unit price) for this seller.
    /// </summary>
    public decimal TotalGMV { get; set; }

    /// <summary>
    /// Gets or sets the total number of orders for the period.
    /// </summary>
    public int TotalOrders { get; set; }

    /// <summary>
    /// Gets or sets the average order value for the period.
    /// </summary>
    public decimal AverageOrderValue { get; set; }

    /// <summary>
    /// Gets or sets the total number of items sold for the period.
    /// </summary>
    public int TotalItemsSold { get; set; }

    /// <summary>
    /// Gets or sets the time series data points for the chart.
    /// </summary>
    public List<TimeSeriesDataPoint> TimeSeriesData { get; set; } = new();

    /// <summary>
    /// Gets or sets the time when the metrics were calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// Represents a single data point in a time series.
/// </summary>
public class TimeSeriesDataPoint
{
    /// <summary>
    /// Gets or sets the date for this data point.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the GMV for this time period.
    /// </summary>
    public decimal GMV { get; set; }

    /// <summary>
    /// Gets or sets the number of orders for this time period.
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// Gets or sets the label for display (e.g., "Jan 15", "Week of Jan 15", "January 2024").
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
