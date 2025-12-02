using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing SLA (Service Level Agreement) tracking.
/// </summary>
public class SLAService : ISLAService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SLAService> _logger;

    // Default SLA values
    private const int DefaultFirstResponseHours = 24;
    private const int DefaultResolutionHours = 168; // 7 days

    public SLAService(
        ApplicationDbContext context,
        ILogger<SLAService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SLAConfig> GetSLAConfigAsync(int? categoryId, ReturnRequestType requestType)
    {
        // Try to find specific config for category and request type
        var config = await _context.SLAConfigs
            .Where(c => c.IsActive)
            .Where(c => c.CategoryId == categoryId && c.RequestType == requestType)
            .FirstOrDefaultAsync();

        if (config != null)
        {
            return config;
        }

        // Try to find config for category only (applies to all request types)
        config = await _context.SLAConfigs
            .Where(c => c.IsActive)
            .Where(c => c.CategoryId == categoryId && c.RequestType == null)
            .FirstOrDefaultAsync();

        if (config != null)
        {
            return config;
        }

        // Try to find config for request type only (applies to all categories)
        config = await _context.SLAConfigs
            .Where(c => c.IsActive)
            .Where(c => c.CategoryId == null && c.RequestType == requestType)
            .FirstOrDefaultAsync();

        if (config != null)
        {
            return config;
        }

        // Return default config
        config = await _context.SLAConfigs
            .Where(c => c.IsActive)
            .Where(c => c.CategoryId == null && c.RequestType == null)
            .FirstOrDefaultAsync();

        if (config != null)
        {
            return config;
        }

        // If no config exists, return hardcoded defaults
        return new SLAConfig
        {
            FirstResponseHours = DefaultFirstResponseHours,
            ResolutionHours = DefaultResolutionHours
        };
    }

    /// <inheritdoc />
    public async Task<(DateTime FirstResponseDeadline, DateTime ResolutionDeadline)> CalculateSLADeadlinesAsync(
        DateTime requestedAt,
        int? categoryId,
        ReturnRequestType requestType)
    {
        var config = await GetSLAConfigAsync(categoryId, requestType);

        var firstResponseDeadline = requestedAt.AddHours(config.FirstResponseHours);
        var resolutionDeadline = requestedAt.AddHours(config.ResolutionHours);

        return (firstResponseDeadline, resolutionDeadline);
    }

    /// <inheritdoc />
    public async Task<bool> CheckAndUpdateSLABreachesAsync(int returnRequestId)
    {
        var returnRequest = await _context.ReturnRequests
            .Include(rr => rr.Messages)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

        if (returnRequest == null)
        {
            _logger.LogWarning("Return request {ReturnRequestId} not found for SLA breach check", returnRequestId);
            return false;
        }

        bool breachDetected = false;
        var now = DateTime.UtcNow;

        // Check first response SLA
        if (!returnRequest.FirstResponseSLABreached && 
            returnRequest.FirstResponseDeadline.HasValue &&
            returnRequest.SellerFirstResponseAt == null &&
            now > returnRequest.FirstResponseDeadline.Value)
        {
            returnRequest.FirstResponseSLABreached = true;
            breachDetected = true;
            _logger.LogInformation(
                "First response SLA breached for return request {ReturnNumber} (ID: {ReturnRequestId})",
                returnRequest.ReturnNumber,
                returnRequestId);
        }

        // Check resolution SLA
        if (!returnRequest.ResolutionSLABreached &&
            returnRequest.ResolutionDeadline.HasValue &&
            returnRequest.Status != ReturnStatus.Resolved &&
            returnRequest.Status != ReturnStatus.Completed &&
            now > returnRequest.ResolutionDeadline.Value)
        {
            returnRequest.ResolutionSLABreached = true;
            breachDetected = true;
            _logger.LogInformation(
                "Resolution SLA breached for return request {ReturnNumber} (ID: {ReturnRequestId})",
                returnRequest.ReturnNumber,
                returnRequestId);
        }

        if (breachDetected)
        {
            returnRequest.UpdatedAt = now;
            await _context.SaveChangesAsync();
        }

        return breachDetected;
    }

    /// <inheritdoc />
    public async Task<SLAStatistics> GetSellerSLAStatisticsAsync(int storeId, DateTime fromDate, DateTime toDate)
    {
        var cases = await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
            .Where(rr => rr.SubOrder.StoreId == storeId)
            .Where(rr => rr.RequestedAt >= fromDate && rr.RequestedAt <= toDate)
            .ToListAsync();

        return CalculateSLAStatistics(cases);
    }

    /// <inheritdoc />
    public async Task<SLAStatistics> GetPlatformSLAStatisticsAsync(DateTime fromDate, DateTime toDate)
    {
        var cases = await _context.ReturnRequests
            .Where(rr => rr.RequestedAt >= fromDate && rr.RequestedAt <= toDate)
            .ToListAsync();

        return CalculateSLAStatistics(cases);
    }

    /// <inheritdoc />
    public async Task<List<SellerSLAStatistics>> GetAllSellerSLAStatisticsAsync(DateTime fromDate, DateTime toDate)
    {
        var casesByStore = await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.Store)
            .Where(rr => rr.RequestedAt >= fromDate && rr.RequestedAt <= toDate)
            .GroupBy(rr => new { rr.SubOrder.StoreId, rr.SubOrder.Store.StoreName })
            .ToListAsync();

        var results = new List<SellerSLAStatistics>();

        foreach (var group in casesByStore)
        {
            var stats = CalculateSLAStatistics(group.ToList());
            results.Add(new SellerSLAStatistics
            {
                StoreId = group.Key.StoreId,
                StoreName = group.Key.StoreName,
                TotalCases = stats.TotalCases,
                CasesResolvedWithinSLA = stats.CasesResolvedWithinSLA,
                FirstResponseSLABreaches = stats.FirstResponseSLABreaches,
                ResolutionSLABreaches = stats.ResolutionSLABreaches,
                AverageResponseTimeHours = stats.AverageResponseTimeHours,
                AverageResolutionTimeHours = stats.AverageResolutionTimeHours
            });
        }

        return results.OrderByDescending(s => s.TotalCases).ToList();
    }

    /// <inheritdoc />
    public async Task<int> ProcessSLABreachesAsync()
    {
        // Get all pending cases that might have SLA breaches
        var pendingCases = await _context.ReturnRequests
            .Where(rr => rr.Status == ReturnStatus.Requested || 
                         rr.Status == ReturnStatus.Approved ||
                         rr.Status == ReturnStatus.UnderAdminReview)
            .Where(rr => rr.FirstResponseDeadline.HasValue || rr.ResolutionDeadline.HasValue)
            .Select(rr => rr.Id)
            .ToListAsync();

        int breachCount = 0;

        foreach (var caseId in pendingCases)
        {
            if (await CheckAndUpdateSLABreachesAsync(caseId))
            {
                breachCount++;
            }
        }

        if (breachCount > 0)
        {
            _logger.LogInformation(
                "Processed SLA breaches: {BreachCount} cases flagged",
                breachCount);
        }

        return breachCount;
    }

    /// <summary>
    /// Helper method to calculate SLA statistics from a list of return requests.
    /// </summary>
    private SLAStatistics CalculateSLAStatistics(List<ReturnRequest> cases)
    {
        var stats = new SLAStatistics
        {
            TotalCases = cases.Count,
            FirstResponseSLABreaches = cases.Count(c => c.FirstResponseSLABreached),
            ResolutionSLABreaches = cases.Count(c => c.ResolutionSLABreached)
        };

        // Count cases resolved within SLA (not breached and resolved/completed)
        stats.CasesResolvedWithinSLA = cases.Count(c => 
            !c.ResolutionSLABreached && 
            (c.Status == ReturnStatus.Resolved || c.Status == ReturnStatus.Completed));

        // Calculate average response time
        var casesWithResponse = cases
            .Where(c => c.SellerFirstResponseAt.HasValue)
            .ToList();

        if (casesWithResponse.Any())
        {
            stats.AverageResponseTimeHours = casesWithResponse
                .Average(c => (c.SellerFirstResponseAt!.Value - c.RequestedAt).TotalHours);
        }

        // Calculate average resolution time
        var resolvedCases = cases
            .Where(c => c.ResolvedAt.HasValue || c.CompletedAt.HasValue)
            .ToList();

        if (resolvedCases.Any())
        {
            stats.AverageResolutionTimeHours = resolvedCases
                .Average(c => 
                {
                    var endTime = c.ResolvedAt ?? c.CompletedAt!.Value;
                    return (endTime - c.RequestedAt).TotalHours;
                });
        }

        return stats;
    }
}
