using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for tracking and querying analytics events.
/// Implements fire-and-forget event logging to minimize performance impact.
/// All data collection complies with privacy and consent policies.
/// </summary>
public class AnalyticsEventService : IAnalyticsEventService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsEventService> _logger;
    private readonly IConfiguration _configuration;

    public AnalyticsEventService(
        ApplicationDbContext context,
        ILogger<AnalyticsEventService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public bool IsTrackingEnabled()
    {
        // Check configuration setting, default to true
        return _configuration.GetValue("Analytics:Tracking:Enabled", true);
    }

    /// <inheritdoc />
    public async Task TrackEventAsync(AnalyticsEventData eventData)
    {
        // Check if tracking is enabled
        if (!IsTrackingEnabled())
        {
            _logger.LogDebug("Analytics tracking is disabled, skipping event: {EventType}", eventData.EventType);
            return;
        }

        try
        {
            var analyticsEvent = new AnalyticsEvent
            {
                EventType = eventData.EventType,
                UserId = eventData.UserId,
                SessionId = eventData.SessionId,
                ProductId = eventData.ProductId,
                ProductVariantId = eventData.ProductVariantId,
                CategoryId = eventData.CategoryId,
                StoreId = eventData.StoreId,
                OrderId = eventData.OrderId,
                SearchQuery = eventData.SearchQuery,
                PromoCode = eventData.PromoCode,
                Quantity = eventData.Quantity,
                Value = eventData.Value,
                Metadata = eventData.Metadata,
                Referrer = eventData.Referrer,
                UserAgent = eventData.UserAgent,
                IpAddress = eventData.IpAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.AnalyticsEvents.Add(analyticsEvent);
            await _context.SaveChangesAsync();

            _logger.LogDebug(
                "Analytics event tracked: Type={EventType}, UserId={UserId}, SessionId={SessionId}, ProductId={ProductId}",
                eventData.EventType,
                eventData.UserId,
                eventData.SessionId,
                eventData.ProductId);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - analytics should never break the main flow
            _logger.LogError(ex, "Error tracking analytics event: {EventType}", eventData.EventType);
        }
    }

    /// <inheritdoc />
    public async Task<List<AnalyticsEvent>> GetEventsAsync(AnalyticsEventQuery query)
    {
        try
        {
            var queryable = _context.AnalyticsEvents.AsQueryable();

            // Apply filters
            if (query.StartDate.HasValue)
            {
                queryable = queryable.Where(e => e.CreatedAt >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                // Include the entire end date (use < on the next day for clearer intent)
                var nextDay = query.EndDate.Value.Date.AddDays(1);
                queryable = queryable.Where(e => e.CreatedAt < nextDay);
            }

            if (query.EventType.HasValue)
            {
                queryable = queryable.Where(e => e.EventType == query.EventType.Value);
            }

            if (query.UserId.HasValue)
            {
                queryable = queryable.Where(e => e.UserId == query.UserId.Value);
            }

            if (!string.IsNullOrEmpty(query.SessionId))
            {
                queryable = queryable.Where(e => e.SessionId == query.SessionId);
            }

            if (query.ProductId.HasValue)
            {
                queryable = queryable.Where(e => e.ProductId == query.ProductId.Value);
            }

            if (query.StoreId.HasValue)
            {
                queryable = queryable.Where(e => e.StoreId == query.StoreId.Value);
            }

            if (query.CategoryId.HasValue)
            {
                queryable = queryable.Where(e => e.CategoryId == query.CategoryId.Value);
            }

            // Order by timestamp descending (most recent first)
            queryable = queryable.OrderByDescending(e => e.CreatedAt);

            // Apply pagination
            if (query.Offset.HasValue && query.Offset.Value > 0)
            {
                queryable = queryable.Skip(query.Offset.Value);
            }

            if (query.Limit.HasValue && query.Limit.Value > 0)
            {
                queryable = queryable.Take(query.Limit.Value);
            }

            return await queryable.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying analytics events");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AnalyticsEventSummary> GetEventSummaryAsync(AnalyticsEventQuery query)
    {
        try
        {
            var queryable = _context.AnalyticsEvents.AsQueryable();

            // Apply same filters as GetEventsAsync
            if (query.StartDate.HasValue)
            {
                queryable = queryable.Where(e => e.CreatedAt >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                // Include the entire end date (use < on the next day for clearer intent)
                var nextDay = query.EndDate.Value.Date.AddDays(1);
                queryable = queryable.Where(e => e.CreatedAt < nextDay);
            }

            if (query.EventType.HasValue)
            {
                queryable = queryable.Where(e => e.EventType == query.EventType.Value);
            }

            if (query.UserId.HasValue)
            {
                queryable = queryable.Where(e => e.UserId == query.UserId.Value);
            }

            if (!string.IsNullOrEmpty(query.SessionId))
            {
                queryable = queryable.Where(e => e.SessionId == query.SessionId);
            }

            if (query.ProductId.HasValue)
            {
                queryable = queryable.Where(e => e.ProductId == query.ProductId.Value);
            }

            if (query.StoreId.HasValue)
            {
                queryable = queryable.Where(e => e.StoreId == query.StoreId.Value);
            }

            if (query.CategoryId.HasValue)
            {
                queryable = queryable.Where(e => e.CategoryId == query.CategoryId.Value);
            }

            // Get summary statistics
            var totalEvents = await queryable.CountAsync();
            var uniqueUsers = await queryable.Where(e => e.UserId.HasValue).Select(e => e.UserId!.Value).Distinct().CountAsync();
            var uniqueSessions = await queryable.Where(e => !string.IsNullOrEmpty(e.SessionId)).Select(e => e.SessionId!).Distinct().CountAsync();
            var totalValue = await queryable.Where(e => e.Value.HasValue).SumAsync(e => e.Value!.Value);

            // Get event counts by type
            var eventCountsByType = await queryable
                .GroupBy(e => e.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventType, x => x.Count);

            return new AnalyticsEventSummary
            {
                TotalEvents = totalEvents,
                UniqueUsers = uniqueUsers,
                UniqueSessions = uniqueSessions,
                TotalValue = totalValue,
                EventCountsByType = eventCountsByType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics event summary");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<DateTime, int>> GetEventCountsByDateAsync(AnalyticsEventQuery query)
    {
        try
        {
            var queryable = _context.AnalyticsEvents.AsQueryable();

            // Apply filters
            if (query.StartDate.HasValue)
            {
                queryable = queryable.Where(e => e.CreatedAt >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                // Include the entire end date (use < on the next day for clearer intent)
                var nextDay = query.EndDate.Value.Date.AddDays(1);
                queryable = queryable.Where(e => e.CreatedAt < nextDay);
            }

            if (query.EventType.HasValue)
            {
                queryable = queryable.Where(e => e.EventType == query.EventType.Value);
            }

            if (query.UserId.HasValue)
            {
                queryable = queryable.Where(e => e.UserId == query.UserId.Value);
            }

            if (!string.IsNullOrEmpty(query.SessionId))
            {
                queryable = queryable.Where(e => e.SessionId == query.SessionId);
            }

            if (query.ProductId.HasValue)
            {
                queryable = queryable.Where(e => e.ProductId == query.ProductId.Value);
            }

            if (query.StoreId.HasValue)
            {
                queryable = queryable.Where(e => e.StoreId == query.StoreId.Value);
            }

            if (query.CategoryId.HasValue)
            {
                queryable = queryable.Where(e => e.CategoryId == query.CategoryId.Value);
            }

            // Group by date and count
            var eventsByDate = await queryable
                .GroupBy(e => e.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToDictionaryAsync(x => x.Date, x => x.Count);

            return eventsByDate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event counts by date");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldEventsAsync(int retentionDays = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        const int batchSize = 1000;
        var totalDeleted = 0;

        try
        {
            // Process in batches to avoid memory issues with large datasets
            // Note: For production with SQL Server/PostgreSQL, consider using ExecuteDeleteAsync() 
            // for more efficient bulk deletion without loading entities into memory.
            // Current implementation is compatible with in-memory database.
            while (true)
            {
                var batch = await _context.AnalyticsEvents
                    .Where(e => e.CreatedAt < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync();

                if (batch.Count == 0)
                {
                    break;
                }

                _context.AnalyticsEvents.RemoveRange(batch);
                await _context.SaveChangesAsync();
                totalDeleted += batch.Count;

                // If we got fewer than batch size, we're done
                if (batch.Count < batchSize)
                {
                    break;
                }
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation(
                    "Cleaned up {Count} analytics events older than {RetentionDays} days",
                    totalDeleted,
                    retentionDays);
            }

            return totalDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old analytics events");
            throw;
        }
    }
}
