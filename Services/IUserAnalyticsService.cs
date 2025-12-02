namespace MercatoApp.Services;

/// <summary>
/// Represents aggregated user analytics metrics for a specific period.
/// All data is anonymized and aggregated to comply with privacy requirements.
/// </summary>
public class UserAnalyticsMetrics
{
    /// <summary>
    /// Gets or sets the number of new buyer accounts created in the period.
    /// </summary>
    public int NewBuyerAccounts { get; set; }

    /// <summary>
    /// Gets or sets the number of new seller accounts created in the period.
    /// </summary>
    public int NewSellerAccounts { get; set; }

    /// <summary>
    /// Gets or sets the total number of active users in the period.
    /// Active users are defined as users who logged in or placed an order at least once during the period.
    /// </summary>
    public int TotalActiveUsers { get; set; }

    /// <summary>
    /// Gets or sets the number of users who placed at least one order in the period.
    /// </summary>
    public int UsersWhoPlacedOrders { get; set; }

    /// <summary>
    /// Gets or sets the number of users who logged in at least once in the period.
    /// </summary>
    public int UsersWhoLoggedIn { get; set; }

    /// <summary>
    /// Gets or sets the date and time when these metrics were calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a daily breakdown of user registration data.
/// Used for charting registration trends over time.
/// </summary>
public class DailyRegistrationData
{
    /// <summary>
    /// Gets or sets the date for this data point.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the number of buyer accounts created on this date.
    /// </summary>
    public int BuyerCount { get; set; }

    /// <summary>
    /// Gets or sets the number of seller accounts created on this date.
    /// </summary>
    public int SellerCount { get; set; }
}

/// <summary>
/// Represents a daily breakdown of user activity data.
/// Used for charting activity trends over time.
/// </summary>
public class DailyActivityData
{
    /// <summary>
    /// Gets or sets the date for this data point.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the number of unique users who logged in on this date.
    /// </summary>
    public int LoginCount { get; set; }

    /// <summary>
    /// Gets or sets the number of unique users who placed orders on this date.
    /// </summary>
    public int OrderCount { get; set; }
}

/// <summary>
/// Interface for user analytics service.
/// Provides aggregated, anonymized analytics data for admin reporting.
/// </summary>
public interface IUserAnalyticsService
{
    /// <summary>
    /// Gets aggregated user analytics metrics for a specific period.
    /// All data is anonymized and aggregated to comply with GDPR and privacy requirements.
    /// </summary>
    /// <param name="startDate">Start date of the period (inclusive).</param>
    /// <param name="endDate">End date of the period (inclusive).</param>
    /// <returns>Aggregated user analytics metrics.</returns>
    Task<UserAnalyticsMetrics> GetUserAnalyticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets daily registration data for charting registration trends.
    /// </summary>
    /// <param name="startDate">Start date of the period (inclusive).</param>
    /// <param name="endDate">End date of the period (inclusive).</param>
    /// <returns>List of daily registration data points.</returns>
    Task<List<DailyRegistrationData>> GetDailyRegistrationDataAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets daily activity data for charting user engagement trends.
    /// </summary>
    /// <param name="startDate">Start date of the period (inclusive).</param>
    /// <param name="endDate">End date of the period (inclusive).</param>
    /// <returns>List of daily activity data points.</returns>
    Task<List<DailyActivityData>> GetDailyActivityDataAsync(DateTime startDate, DateTime endDate);
}
