using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for SLA (Service Level Agreement) tracking service.
/// </summary>
public interface ISLAService
{
    /// <summary>
    /// Gets the SLA configuration for a specific case based on category and request type.
    /// </summary>
    /// <param name="categoryId">The category ID (optional).</param>
    /// <param name="requestType">The request type.</param>
    /// <returns>The applicable SLA configuration or default if none found.</returns>
    Task<SLAConfig> GetSLAConfigAsync(int? categoryId, ReturnRequestType requestType);

    /// <summary>
    /// Calculates SLA deadlines for a new case.
    /// </summary>
    /// <param name="requestedAt">The date and time the case was requested.</param>
    /// <param name="categoryId">The category ID (optional).</param>
    /// <param name="requestType">The request type.</param>
    /// <returns>A tuple containing first response deadline and resolution deadline.</returns>
    Task<(DateTime FirstResponseDeadline, DateTime ResolutionDeadline)> CalculateSLADeadlinesAsync(
        DateTime requestedAt,
        int? categoryId,
        ReturnRequestType requestType);

    /// <summary>
    /// Checks and updates SLA breach flags for a return request.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <returns>True if any SLA breach was newly detected, false otherwise.</returns>
    Task<bool> CheckAndUpdateSLABreachesAsync(int returnRequestId);

    /// <summary>
    /// Gets SLA statistics for a specific seller over a time period.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="fromDate">Start date for the period.</param>
    /// <param name="toDate">End date for the period.</param>
    /// <returns>SLA statistics for the seller.</returns>
    Task<SLAStatistics> GetSellerSLAStatisticsAsync(int storeId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets SLA statistics across all sellers over a time period.
    /// </summary>
    /// <param name="fromDate">Start date for the period.</param>
    /// <param name="toDate">End date for the period.</param>
    /// <returns>Aggregate SLA statistics.</returns>
    Task<SLAStatistics> GetPlatformSLAStatisticsAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets SLA statistics for each seller over a time period.
    /// </summary>
    /// <param name="fromDate">Start date for the period.</param>
    /// <param name="toDate">End date for the period.</param>
    /// <returns>List of SLA statistics per seller.</returns>
    Task<List<SellerSLAStatistics>> GetAllSellerSLAStatisticsAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Processes all pending cases to check for SLA breaches.
    /// This method is called by a background service.
    /// </summary>
    /// <returns>Number of cases flagged for SLA breach.</returns>
    Task<int> ProcessSLABreachesAsync();
}

/// <summary>
/// Represents SLA statistics for a time period.
/// </summary>
public class SLAStatistics
{
    /// <summary>
    /// Gets or sets the total number of cases.
    /// </summary>
    public int TotalCases { get; set; }

    /// <summary>
    /// Gets or sets the number of cases resolved within SLA.
    /// </summary>
    public int CasesResolvedWithinSLA { get; set; }

    /// <summary>
    /// Gets or sets the percentage of cases resolved within SLA.
    /// </summary>
    public decimal PercentageResolvedWithinSLA => TotalCases > 0 
        ? Math.Round((decimal)CasesResolvedWithinSLA / TotalCases * 100, 2) 
        : 0;

    /// <summary>
    /// Gets or sets the number of first response SLA breaches.
    /// </summary>
    public int FirstResponseSLABreaches { get; set; }

    /// <summary>
    /// Gets or sets the number of resolution SLA breaches.
    /// </summary>
    public int ResolutionSLABreaches { get; set; }

    /// <summary>
    /// Gets or sets the average response time in hours.
    /// </summary>
    public double AverageResponseTimeHours { get; set; }

    /// <summary>
    /// Gets or sets the average resolution time in hours.
    /// </summary>
    public double AverageResolutionTimeHours { get; set; }
}

/// <summary>
/// Represents SLA statistics for a specific seller.
/// </summary>
public class SellerSLAStatistics : SLAStatistics
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;
}
