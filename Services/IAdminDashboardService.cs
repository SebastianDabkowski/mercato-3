namespace MercatoApp.Services;

/// <summary>
/// Interface for admin dashboard service.
/// </summary>
public interface IAdminDashboardService
{
    /// <summary>
    /// Gets marketplace KPIs for the specified date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <returns>The dashboard metrics.</returns>
    Task<DashboardMetrics> GetMetricsAsync(DateTime startDate, DateTime endDate);
}

/// <summary>
/// Represents dashboard metrics for the marketplace.
/// </summary>
public class DashboardMetrics
{
    /// <summary>
    /// Gets or sets the Gross Merchandise Value (total order value including shipping, before commission).
    /// </summary>
    public decimal GrossMerchandiseValue { get; set; }

    /// <summary>
    /// Gets or sets the total number of orders.
    /// </summary>
    public int TotalOrders { get; set; }

    /// <summary>
    /// Gets or sets the number of active sellers (stores with at least one active product OR at least one order in period).
    /// </summary>
    public int ActiveSellers { get; set; }

    /// <summary>
    /// Gets or sets the number of active products (products with status = Active).
    /// </summary>
    public int ActiveProducts { get; set; }

    /// <summary>
    /// Gets or sets the number of newly registered users in the period.
    /// </summary>
    public int NewUsers { get; set; }

    /// <summary>
    /// Gets or sets the time when the metrics were calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}
