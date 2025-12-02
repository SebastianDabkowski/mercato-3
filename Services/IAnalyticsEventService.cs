using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Data required to track an analytics event.
/// </summary>
public class AnalyticsEventData
{
    /// <summary>
    /// Gets or sets the type of event being tracked.
    /// </summary>
    public AnalyticsEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the user ID (null for anonymous users).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the session ID for anonymous tracking.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the product ID (if applicable).
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product variant ID (if applicable).
    /// </summary>
    public int? ProductVariantId { get; set; }

    /// <summary>
    /// Gets or sets the category ID (if applicable).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the store ID (if applicable).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the order ID (if applicable).
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the search query text (for search events).
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the promo code (for promo code events).
    /// </summary>
    public string? PromoCode { get; set; }

    /// <summary>
    /// Gets or sets the quantity (for cart events).
    /// </summary>
    public int? Quantity { get; set; }

    /// <summary>
    /// Gets or sets the monetary value (for order/cart events).
    /// </summary>
    public decimal? Value { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the referrer URL.
    /// </summary>
    public string? Referrer { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the IP address.
    /// </summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// Query parameters for retrieving analytics events.
/// </summary>
public class AnalyticsEventQuery
{
    /// <summary>
    /// Gets or sets the start date for the query range (inclusive).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the query range (inclusive).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the event type filter.
    /// </summary>
    public AnalyticsEventType? EventType { get; set; }

    /// <summary>
    /// Gets or sets the user ID filter.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the session ID filter.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the product ID filter.
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID filter.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the category ID filter.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int? Limit { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the offset for pagination.
    /// </summary>
    public int? Offset { get; set; } = 0;
}

/// <summary>
/// Summary statistics for analytics events.
/// </summary>
public class AnalyticsEventSummary
{
    /// <summary>
    /// Gets or sets the total count of events.
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Gets or sets the unique user count.
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Gets or sets the unique session count.
    /// </summary>
    public int UniqueSessions { get; set; }

    /// <summary>
    /// Gets or sets the total value (sum of all event values).
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Gets or sets the event counts by type.
    /// </summary>
    public Dictionary<AnalyticsEventType, int> EventCountsByType { get; set; } = new();
}

/// <summary>
/// Interface for analytics event tracking service.
/// Provides methods to log and query user behavior events for Phase 2 analytics.
/// </summary>
public interface IAnalyticsEventService
{
    /// <summary>
    /// Tracks an analytics event asynchronously without blocking the caller.
    /// Fire-and-forget pattern to minimize performance impact.
    /// </summary>
    /// <param name="eventData">The event data to track.</param>
    /// <returns>Task that completes when event is queued for processing.</returns>
    Task TrackEventAsync(AnalyticsEventData eventData);

    /// <summary>
    /// Gets analytics events based on query parameters.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <returns>List of analytics events matching the query.</returns>
    Task<List<AnalyticsEvent>> GetEventsAsync(AnalyticsEventQuery query);

    /// <summary>
    /// Gets summary statistics for analytics events based on query parameters.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <returns>Summary statistics for the matching events.</returns>
    Task<AnalyticsEventSummary> GetEventSummaryAsync(AnalyticsEventQuery query);

    /// <summary>
    /// Gets event counts grouped by date for charting over time.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <returns>Dictionary mapping dates to event counts.</returns>
    Task<Dictionary<DateTime, int>> GetEventCountsByDateAsync(AnalyticsEventQuery query);

    /// <summary>
    /// Cleans up old analytics events based on retention policy.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain events.</param>
    /// <returns>Number of events deleted.</returns>
    Task<int> CleanupOldEventsAsync(int retentionDays = 90);

    /// <summary>
    /// Checks if analytics tracking is enabled in configuration.
    /// </summary>
    /// <returns>True if tracking is enabled.</returns>
    bool IsTrackingEnabled();
}
