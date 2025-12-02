using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Manual test scenario for SLA tracking functionality.
/// This file demonstrates how the SLA tracking system works and can be used for testing.
/// </summary>
public class SLATrackingTestScenario
{
    public static async Task RunTestAsync(
        ApplicationDbContext context,
        IReturnRequestService returnRequestService,
        ISLAService slaService)
    {
        Console.WriteLine("=== SLA Tracking Test Scenario ===");
        Console.WriteLine();

        try
        {
            // Step 1: Create a default SLA configuration if it doesn't exist
            var defaultConfig = await context.SLAConfigs
                .Where(c => c.CategoryId == null && c.RequestType == null && c.IsActive)
                .FirstOrDefaultAsync();

            if (defaultConfig == null)
            {
                defaultConfig = new SLAConfig
                {
                    CategoryId = null,
                    RequestType = null,
                    FirstResponseHours = 24,
                    ResolutionHours = 168, // 7 days
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.SLAConfigs.Add(defaultConfig);
                await context.SaveChangesAsync();
                Console.WriteLine("✓ Created default SLA configuration (24h first response, 168h resolution)");
            }
            else
            {
                Console.WriteLine($"✓ Found existing default SLA configuration:");
                Console.WriteLine($"  - First Response: {defaultConfig.FirstResponseHours} hours");
                Console.WriteLine($"  - Resolution: {defaultConfig.ResolutionHours} hours");
            }
            Console.WriteLine();

            // Step 2: Find or create a return request for testing
            var testRequest = await context.ReturnRequests
                .Include(rr => rr.SubOrder)
                    .ThenInclude(so => so.Store)
                .Where(rr => rr.Status == ReturnStatus.Requested || rr.Status == ReturnStatus.Approved)
                .FirstOrDefaultAsync();

            if (testRequest == null)
            {
                // Try to create a new return request
                var subOrder = await context.SellerSubOrders
                    .Include(so => so.ParentOrder)
                    .Include(so => so.Store)
                    .Include(so => so.Items)
                        .ThenInclude(oi => oi.Product)
                    .Include(so => so.StatusHistory)
                    .Where(so => so.Status == OrderStatus.Delivered)
                    .Where(so => so.ParentOrder.UserId != null)
                    .FirstOrDefaultAsync();

                if (subOrder != null)
                {
                    Console.WriteLine($"Found delivered sub-order: {subOrder.SubOrderNumber} from {subOrder.Store.StoreName}");
                    var buyerId = subOrder.ParentOrder.UserId!.Value;
                    
                    // Check if already has a return request - if so, use it
                    var existingReturn = await context.ReturnRequests
                        .Include(rr => rr.SubOrder)
                            .ThenInclude(so => so.Store)
                        .Where(rr => rr.SubOrderId == subOrder.Id)
                        .FirstOrDefaultAsync();

                    if (existingReturn != null)
                    {
                        testRequest = existingReturn;
                        Console.WriteLine($"Using existing return request {testRequest.ReturnNumber}");
                    }
                    else
                    {
                        try
                        {
                            testRequest = await returnRequestService.CreateReturnRequestAsync(
                                subOrder.Id,
                                buyerId,
                                ReturnRequestType.Complaint,
                                ReturnReason.Damaged,
                                "Test case for SLA tracking",
                                isFullReturn: true);

                            Console.WriteLine($"✓ Created new test return request {testRequest.ReturnNumber}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ERROR creating return request: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No delivered sub-orders found");
                }
            }
            else
            {
                Console.WriteLine($"Found existing return request {testRequest.ReturnNumber}");
            }

            if (testRequest == null)
            {
                Console.WriteLine("WARNING: Could not find or create a return request for testing.");
                return;
            }

            // Step 3: Verify SLA deadlines were set
            Console.WriteLine();
            Console.WriteLine($"Return Request: {testRequest.ReturnNumber}");
            Console.WriteLine($"  - Status: {testRequest.Status}");
            Console.WriteLine($"  - Requested At: {testRequest.RequestedAt:yyyy-MM-dd HH:mm} UTC");
            
            if (testRequest.FirstResponseDeadline.HasValue)
            {
                Console.WriteLine($"  - First Response Deadline: {testRequest.FirstResponseDeadline.Value:yyyy-MM-dd HH:mm} UTC");
                var hoursUntilFirstResponse = (testRequest.FirstResponseDeadline.Value - DateTime.UtcNow).TotalHours;
                Console.WriteLine($"    ({hoursUntilFirstResponse:F1} hours remaining)");
            }
            else
            {
                Console.WriteLine("  - WARNING: First Response Deadline not set!");
            }

            if (testRequest.ResolutionDeadline.HasValue)
            {
                Console.WriteLine($"  - Resolution Deadline: {testRequest.ResolutionDeadline.Value:yyyy-MM-dd HH:mm} UTC");
                var hoursUntilResolution = (testRequest.ResolutionDeadline.Value - DateTime.UtcNow).TotalHours;
                Console.WriteLine($"    ({hoursUntilResolution:F1} hours remaining)");
            }
            else
            {
                Console.WriteLine("  - WARNING: Resolution Deadline not set!");
            }

            Console.WriteLine($"  - First Response SLA Breached: {testRequest.FirstResponseSLABreached}");
            Console.WriteLine($"  - Resolution SLA Breached: {testRequest.ResolutionSLABreached}");
            Console.WriteLine();

            // Step 4: Test SLA breach checking
            Console.WriteLine("Testing SLA breach detection...");
            var breachDetected = await slaService.CheckAndUpdateSLABreachesAsync(testRequest.Id);
            
            if (breachDetected)
            {
                // Reload to see updated flags
                testRequest = await context.ReturnRequests.FindAsync(testRequest.Id);
                Console.WriteLine($"✓ SLA breach detected and flagged!");
                Console.WriteLine($"  - First Response Breached: {testRequest!.FirstResponseSLABreached}");
                Console.WriteLine($"  - Resolution Breached: {testRequest.ResolutionSLABreached}");
            }
            else
            {
                Console.WriteLine("✓ No SLA breaches detected (case is within deadlines)");
            }
            Console.WriteLine();

            // Step 5: Test SLA statistics
            Console.WriteLine("Retrieving SLA statistics...");
            var storeId = testRequest.SubOrder.StoreId;
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);

            var storeStats = await slaService.GetSellerSLAStatisticsAsync(storeId, startDate, endDate);
            Console.WriteLine($"✓ Store '{testRequest.SubOrder.Store.StoreName}' SLA statistics (last 30 days):");
            Console.WriteLine($"  - Total Cases: {storeStats.TotalCases}");
            Console.WriteLine($"  - Cases Resolved Within SLA: {storeStats.CasesResolvedWithinSLA}");
            Console.WriteLine($"  - SLA Compliance Rate: {storeStats.PercentageResolvedWithinSLA}%");
            Console.WriteLine($"  - First Response Breaches: {storeStats.FirstResponseSLABreaches}");
            Console.WriteLine($"  - Resolution Breaches: {storeStats.ResolutionSLABreaches}");
            Console.WriteLine($"  - Average Response Time: {storeStats.AverageResponseTimeHours:F1} hours");
            Console.WriteLine($"  - Average Resolution Time: {storeStats.AverageResolutionTimeHours:F1} hours");
            Console.WriteLine();

            var platformStats = await slaService.GetPlatformSLAStatisticsAsync(startDate, endDate);
            Console.WriteLine($"✓ Platform-wide SLA statistics (last 30 days):");
            Console.WriteLine($"  - Total Cases: {platformStats.TotalCases}");
            Console.WriteLine($"  - Cases Resolved Within SLA: {platformStats.CasesResolvedWithinSLA}");
            Console.WriteLine($"  - SLA Compliance Rate: {platformStats.PercentageResolvedWithinSLA}%");
            Console.WriteLine($"  - First Response Breaches: {platformStats.FirstResponseSLABreaches}");
            Console.WriteLine($"  - Resolution Breaches: {platformStats.ResolutionSLABreaches}");
            Console.WriteLine($"  - Average Response Time: {platformStats.AverageResponseTimeHours:F1} hours");
            Console.WriteLine($"  - Average Resolution Time: {platformStats.AverageResolutionTimeHours:F1} hours");
            Console.WriteLine();

            // Step 6: Test processing all SLA breaches
            Console.WriteLine("Processing all pending cases for SLA breaches...");
            var flaggedCount = await slaService.ProcessSLABreachesAsync();
            Console.WriteLine($"✓ Processed SLA breaches. {flaggedCount} case(s) flagged.");
            Console.WriteLine();

            Console.WriteLine("=== SLA Tracking Test Completed Successfully ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
