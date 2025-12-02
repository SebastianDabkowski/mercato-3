using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for calculating admin dashboard metrics.
/// </summary>
public class AdminDashboardService : IAdminDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(
        ApplicationDbContext context,
        ILogger<AdminDashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DashboardMetrics> GetMetricsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            // Ensure end date includes the entire day
            var endDateTime = endDate.Date.AddDays(1).AddTicks(-1);
            var startDateTime = startDate.Date;

            // Calculate GMV: sum of all order totals in the period
            var gmv = await _context.Orders
                .Where(o => o.OrderedAt >= startDateTime && o.OrderedAt <= endDateTime)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // Count total orders in the period
            var totalOrders = await _context.Orders
                .Where(o => o.OrderedAt >= startDateTime && o.OrderedAt <= endDateTime)
                .CountAsync();

            // Count active sellers: stores with at least one active product OR at least one order in period
            var storesWithActiveProducts = await _context.Products
                .Where(p => p.Status == ProductStatus.Active)
                .Select(p => p.StoreId)
                .Distinct()
                .ToListAsync();

            var storesWithOrders = await _context.SellerSubOrders
                .Where(so => so.CreatedAt >= startDateTime && so.CreatedAt <= endDateTime)
                .Select(so => so.StoreId)
                .Distinct()
                .ToListAsync();

            var activeSellers = storesWithActiveProducts
                .Union(storesWithOrders)
                .Distinct()
                .Count();

            // Count active products (status = Active)
            var activeProducts = await _context.Products
                .Where(p => p.Status == ProductStatus.Active)
                .CountAsync();

            // Count new users registered in the period
            var newUsers = await _context.Users
                .Where(u => u.CreatedAt >= startDateTime && u.CreatedAt <= endDateTime)
                .CountAsync();

            return new DashboardMetrics
            {
                GrossMerchandiseValue = gmv,
                TotalOrders = totalOrders,
                ActiveSellers = activeSellers,
                ActiveProducts = activeProducts,
                NewUsers = newUsers,
                CalculatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dashboard metrics for period {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }
}
