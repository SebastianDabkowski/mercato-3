using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for generating user analytics reports.
/// Provides aggregated, anonymized metrics for admin reporting.
/// </summary>
public class UserAnalyticsService : IUserAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserAnalyticsService> _logger;

    public UserAnalyticsService(
        ApplicationDbContext context,
        ILogger<UserAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserAnalyticsMetrics> GetUserAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation(
                "Calculating user analytics for period {StartDate} to {EndDate}",
                startDate,
                endDate);

            // Ensure dates are at the start and end of the day
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1).AddTicks(-1);

            // Get new buyer accounts created in the period
            var newBuyerAccounts = await _context.Users
                .Where(u => u.UserType == UserType.Buyer 
                    && u.CreatedAt >= start 
                    && u.CreatedAt <= end)
                .CountAsync();

            // Get new seller accounts created in the period
            var newSellerAccounts = await _context.Users
                .Where(u => u.UserType == UserType.Seller 
                    && u.CreatedAt >= start 
                    && u.CreatedAt <= end)
                .CountAsync();

            // Get users who logged in during the period (from successful login events)
            var usersWhoLoggedIn = await _context.LoginEvents
                .Where(le => le.IsSuccessful 
                    && le.CreatedAt >= start 
                    && le.CreatedAt <= end
                    && le.UserId.HasValue)
                .Select(le => le.UserId!.Value)
                .Distinct()
                .CountAsync();

            // Get users who placed orders during the period
            var usersWhoPlacedOrders = await _context.Orders
                .Where(o => o.OrderedAt >= start 
                    && o.OrderedAt <= end
                    && o.UserId.HasValue)
                .Select(o => o.UserId!.Value)
                .Distinct()
                .CountAsync();

            // Total active users = unique users who either logged in OR placed an order
            // (Some users might have placed orders without logging in separately, e.g., session was already active)
            var activeUserIds = await _context.LoginEvents
                .Where(le => le.IsSuccessful 
                    && le.CreatedAt >= start 
                    && le.CreatedAt <= end
                    && le.UserId.HasValue)
                .Select(le => le.UserId!.Value)
                .Union(
                    _context.Orders
                        .Where(o => o.OrderedAt >= start 
                            && o.OrderedAt <= end
                            && o.UserId.HasValue)
                        .Select(o => o.UserId!.Value)
                )
                .Distinct()
                .CountAsync();

            var metrics = new UserAnalyticsMetrics
            {
                NewBuyerAccounts = newBuyerAccounts,
                NewSellerAccounts = newSellerAccounts,
                TotalActiveUsers = activeUserIds,
                UsersWhoPlacedOrders = usersWhoPlacedOrders,
                UsersWhoLoggedIn = usersWhoLoggedIn,
                CalculatedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "User analytics calculated: {NewBuyers} new buyers, {NewSellers} new sellers, {ActiveUsers} active users",
                metrics.NewBuyerAccounts,
                metrics.NewSellerAccounts,
                metrics.TotalActiveUsers);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating user analytics");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<DailyRegistrationData>> GetDailyRegistrationDataAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1).AddTicks(-1);

            // Get all user registrations in the period grouped by date
            var registrations = await _context.Users
                .Where(u => u.CreatedAt >= start && u.CreatedAt <= end)
                .GroupBy(u => new 
                { 
                    Date = u.CreatedAt.Date,
                    UserType = u.UserType
                })
                .Select(g => new 
                { 
                    Date = g.Key.Date,
                    UserType = g.Key.UserType,
                    Count = g.Count()
                })
                .ToListAsync();

            // Create a complete date range with zero counts for missing days
            var result = new List<DailyRegistrationData>();
            for (var date = start; date <= end.Date; date = date.AddDays(1))
            {
                var buyerCount = registrations
                    .Where(r => r.Date == date && r.UserType == UserType.Buyer)
                    .Sum(r => r.Count);

                var sellerCount = registrations
                    .Where(r => r.Date == date && r.UserType == UserType.Seller)
                    .Sum(r => r.Count);

                result.Add(new DailyRegistrationData
                {
                    Date = date,
                    BuyerCount = buyerCount,
                    SellerCount = sellerCount
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily registration data");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<DailyActivityData>> GetDailyActivityDataAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1).AddTicks(-1);

            // Get login events grouped by date
            var loginsByDate = await _context.LoginEvents
                .Where(le => le.IsSuccessful 
                    && le.CreatedAt >= start 
                    && le.CreatedAt <= end
                    && le.UserId.HasValue)
                .GroupBy(le => le.CreatedAt.Date)
                .Select(g => new 
                { 
                    Date = g.Key,
                    Count = g.Select(le => le.UserId!.Value).Distinct().Count()
                })
                .ToListAsync();

            // Get orders grouped by date
            var ordersByDate = await _context.Orders
                .Where(o => o.OrderedAt >= start 
                    && o.OrderedAt <= end
                    && o.UserId.HasValue)
                .GroupBy(o => o.OrderedAt.Date)
                .Select(g => new 
                { 
                    Date = g.Key,
                    Count = g.Select(o => o.UserId!.Value).Distinct().Count()
                })
                .ToListAsync();

            // Create a complete date range with zero counts for missing days
            var result = new List<DailyActivityData>();
            for (var date = start; date <= end.Date; date = date.AddDays(1))
            {
                var loginCount = loginsByDate
                    .Where(l => l.Date == date)
                    .Sum(l => l.Count);

                var orderCount = ordersByDate
                    .Where(o => o.Date == date)
                    .Sum(o => o.Count);

                result.Add(new DailyActivityData
                {
                    Date = date,
                    LoginCount = loginCount,
                    OrderCount = orderCount
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily activity data");
            throw;
        }
    }
}
