using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for calculating seller dashboard metrics.
/// </summary>
public class SellerDashboardService : ISellerDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SellerDashboardService> _logger;

    public SellerDashboardService(
        ApplicationDbContext context,
        ILogger<SellerDashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SellerDashboardMetrics> GetMetricsAsync(
        int storeId,
        DateTime startDate,
        DateTime endDate,
        TimeGranularity granularity,
        int? productId = null,
        int? categoryId = null)
    {
        try
        {
            // Ensure end date includes the entire day
            var endDateTime = endDate.Date.AddDays(1).AddTicks(-1);
            var startDateTime = startDate.Date;

            // Build base query for order items belonging to this seller
            var query = _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Where(oi => oi.StoreId == storeId
                    && oi.Order.OrderedAt >= startDateTime
                    && oi.Order.OrderedAt <= endDateTime);

            // Apply product filter if specified
            if (productId.HasValue)
            {
                query = query.Where(oi => oi.ProductId == productId.Value);
            }

            // Apply category filter if specified
            if (categoryId.HasValue)
            {
                query = query.Where(oi => oi.Product.CategoryId == categoryId.Value);
            }

            var orderItems = await query.ToListAsync();

            // Calculate total metrics
            var totalGMV = orderItems.Sum(oi => oi.Subtotal);
            var totalOrders = orderItems.Select(oi => oi.OrderId).Distinct().Count();
            var totalItemsSold = orderItems.Sum(oi => oi.Quantity);
            var averageOrderValue = totalOrders > 0 ? totalGMV / totalOrders : 0;

            // Generate time series data based on granularity
            var timeSeriesData = GenerateTimeSeriesData(
                orderItems,
                startDateTime,
                endDateTime,
                granularity);

            return new SellerDashboardMetrics
            {
                TotalGMV = totalGMV,
                TotalOrders = totalOrders,
                AverageOrderValue = averageOrderValue,
                TotalItemsSold = totalItemsSold,
                TimeSeriesData = timeSeriesData,
                CalculatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating seller dashboard metrics for store {StoreId} from {StartDate} to {EndDate}",
                storeId, startDate, endDate);
            throw;
        }
    }

    /// <summary>
    /// Generates time series data points based on the specified granularity.
    /// </summary>
    private List<TimeSeriesDataPoint> GenerateTimeSeriesData(
        List<OrderItem> orderItems,
        DateTime startDate,
        DateTime endDate,
        TimeGranularity granularity)
    {
        var dataPoints = new List<TimeSeriesDataPoint>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            DateTime periodEnd;
            string label;

            switch (granularity)
            {
                case TimeGranularity.Day:
                    periodEnd = currentDate.AddDays(1).AddTicks(-1);
                    label = currentDate.ToString("MMM dd");
                    break;

                case TimeGranularity.Week:
                    // Start week on Monday
                    var weekStart = currentDate.AddDays(-(int)currentDate.DayOfWeek + (currentDate.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));
                    periodEnd = weekStart.AddDays(7).AddTicks(-1);
                    label = $"Week of {weekStart:MMM dd}";
                    currentDate = weekStart;
                    break;

                case TimeGranularity.Month:
                    var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                    periodEnd = monthStart.AddMonths(1).AddTicks(-1);
                    label = monthStart.ToString("MMMM yyyy");
                    currentDate = monthStart;
                    break;

                default:
                    periodEnd = currentDate.AddDays(1).AddTicks(-1);
                    label = currentDate.ToString("MMM dd");
                    break;
            }

            // Filter items for this period
            var periodItems = orderItems
                .Where(oi => oi.Order.OrderedAt >= currentDate && oi.Order.OrderedAt <= periodEnd)
                .ToList();

            var gmv = periodItems.Sum(oi => oi.Subtotal);
            var orderCount = periodItems.Select(oi => oi.OrderId).Distinct().Count();

            dataPoints.Add(new TimeSeriesDataPoint
            {
                Date = currentDate,
                GMV = gmv,
                OrderCount = orderCount,
                Label = label
            });

            // Move to next period
            switch (granularity)
            {
                case TimeGranularity.Day:
                    currentDate = currentDate.AddDays(1);
                    break;

                case TimeGranularity.Week:
                    currentDate = currentDate.AddDays(7);
                    break;

                case TimeGranularity.Month:
                    currentDate = currentDate.AddMonths(1);
                    break;
            }
        }

        return dataPoints;
    }
}
