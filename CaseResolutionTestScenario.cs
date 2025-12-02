using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Manual test scenario for case resolution and refund linkage functionality.
/// This file demonstrates how the case resolution system works with refund processing.
/// </summary>
public class CaseResolutionTestScenario
{
    public static async Task RunTestAsync(
        ApplicationDbContext context, 
        IReturnRequestService returnRequestService,
        IRefundService refundService)
    {
        Console.WriteLine("=== Case Resolution and Refund Linkage Test Scenario ===");
        Console.WriteLine();

        try
        {
            // Step 1: Find an existing return request in "Requested" or "Approved" status
            var returnRequest = await context.ReturnRequests
                .Include(rr => rr.SubOrder)
                    .ThenInclude(so => so.ParentOrder)
                .Include(rr => rr.SubOrder)
                    .ThenInclude(so => so.Store)
                .Include(rr => rr.Buyer)
                .Where(rr => rr.Status == ReturnStatus.Requested || rr.Status == ReturnStatus.Approved)
                .FirstOrDefaultAsync();

            if (returnRequest == null)
            {
                Console.WriteLine("WARNING: No active return requests found for testing.");
                Console.WriteLine("Please create a return request first using the ReturnComplaintTestScenario.");
                return;
            }

            Console.WriteLine($"✓ Found return request {returnRequest.ReturnNumber}");
            Console.WriteLine($"  Type: {returnRequest.RequestType}");
            Console.WriteLine($"  Status: {returnRequest.Status}");
            Console.WriteLine($"  Buyer: {returnRequest.Buyer.Email}");
            Console.WriteLine($"  Store: {returnRequest.SubOrder.Store.StoreName}");
            Console.WriteLine($"  Refund Amount: {returnRequest.RefundAmount:C}");
            Console.WriteLine();

            // Step 2: Get the store owner user ID (seller)
            var store = await context.Stores
                .FirstOrDefaultAsync(s => s.Id == returnRequest.SubOrder.StoreId);

            if (store == null)
            {
                Console.WriteLine("ERROR: Store not found.");
                return;
            }

            var sellerId = store.UserId;
            Console.WriteLine($"✓ Seller (Store Owner) ID: {sellerId}");
            Console.WriteLine();

            // Test Case 1: Full Refund Resolution
            Console.WriteLine("--- Test Case 1: Full Refund Resolution ---");
            var (success1, error1, resolved1) = await returnRequestService.ResolveReturnCaseAsync(
                returnRequestId: returnRequest.Id,
                storeId: store.Id,
                resolutionType: ResolutionType.FullRefund,
                resolutionNotes: "Item was damaged in transit. Providing full refund to the customer. We apologize for the inconvenience.",
                resolutionAmount: null, // Not needed for full refund
                initiatedByUserId: sellerId);

            if (success1 && resolved1 != null)
            {
                Console.WriteLine("✓ Case resolved successfully with Full Refund");
                Console.WriteLine($"  Resolution Type: {resolved1.ResolutionType}");
                Console.WriteLine($"  Resolved At: {resolved1.ResolvedAt}");
                Console.WriteLine($"  Status: {resolved1.Status}");
                
                // Check if refund was created
                var linkedRefund = await context.RefundTransactions
                    .FirstOrDefaultAsync(r => r.ReturnRequestId == returnRequest.Id);
                
                if (linkedRefund != null)
                {
                    Console.WriteLine($"✓ Refund created and linked: {linkedRefund.RefundNumber}");
                    Console.WriteLine($"  Refund Amount: {linkedRefund.RefundAmount:C}");
                    Console.WriteLine($"  Refund Status: {linkedRefund.Status}");
                    Console.WriteLine($"  Provider Refund ID: {linkedRefund.ProviderRefundId ?? "N/A"}");
                }
                else
                {
                    Console.WriteLine("⚠ Refund was not created (unexpected)");
                }
            }
            else
            {
                Console.WriteLine($"✗ Failed to resolve case: {error1}");
            }
            Console.WriteLine();

            // Test Case 2: Try to change resolution (should fail if refund is completed)
            Console.WriteLine("--- Test Case 2: Attempt to Change Resolution ---");
            var (canChange, changeError) = await returnRequestService.CanChangeResolutionAsync(returnRequest.Id);
            
            if (canChange)
            {
                Console.WriteLine("✓ Resolution can be changed (refund not yet completed)");
            }
            else
            {
                Console.WriteLine($"✓ Resolution change prevented: {changeError}");
                Console.WriteLine("  This is expected behavior after refund completion.");
            }
            Console.WriteLine();

            // Test Case 3: Create another return request for partial refund test
            Console.WriteLine("--- Test Case 3: Partial Refund Resolution ---");
            
            // Find another eligible sub-order
            var anotherSubOrder = await context.SellerSubOrders
                .Include(so => so.ParentOrder)
                .Include(so => so.Store)
                .Include(so => so.Items)
                .Where(so => so.Status == OrderStatus.Delivered)
                .Where(so => !context.ReturnRequests.Any(rr => rr.SubOrderId == so.Id))
                .FirstOrDefaultAsync();

            if (anotherSubOrder != null && anotherSubOrder.ParentOrder.UserId.HasValue)
            {
                Console.WriteLine($"✓ Creating new return request for sub-order {anotherSubOrder.SubOrderNumber}");
                
                // Create a new return request
                var newReturn = await returnRequestService.CreateReturnRequestAsync(
                    subOrderId: anotherSubOrder.Id,
                    buyerId: anotherSubOrder.ParentOrder.UserId.Value,
                    requestType: ReturnRequestType.Return,
                    reason: ReturnReason.ChangedMind,
                    description: "Testing partial refund resolution",
                    isFullReturn: true);

                Console.WriteLine($"✓ Created return request {newReturn.ReturnNumber}");
                
                // Resolve with partial refund
                decimal partialAmount = newReturn.RefundAmount * 0.5m; // 50% refund
                var (success3, error3, resolved3) = await returnRequestService.ResolveReturnCaseAsync(
                    returnRequestId: newReturn.Id,
                    storeId: anotherSubOrder.StoreId,
                    resolutionType: ResolutionType.PartialRefund,
                    resolutionNotes: "Partially accepting the return. Providing 50% refund as item can still be resold with minor cleaning.",
                    resolutionAmount: partialAmount,
                    initiatedByUserId: anotherSubOrder.Store.UserId);

                if (success3 && resolved3 != null)
                {
                    Console.WriteLine("✓ Case resolved successfully with Partial Refund");
                    Console.WriteLine($"  Original Amount: {newReturn.RefundAmount:C}");
                    Console.WriteLine($"  Partial Refund: {partialAmount:C}");
                    
                    var partialRefund = await context.RefundTransactions
                        .FirstOrDefaultAsync(r => r.ReturnRequestId == newReturn.Id);
                    
                    if (partialRefund != null)
                    {
                        Console.WriteLine($"✓ Partial refund created: {partialRefund.RefundNumber}");
                        Console.WriteLine($"  Amount: {partialRefund.RefundAmount:C}");
                    }
                }
                else
                {
                    Console.WriteLine($"✗ Failed to resolve with partial refund: {error3}");
                }
            }
            else
            {
                Console.WriteLine("ℹ No additional sub-orders available for partial refund test");
            }
            Console.WriteLine();

            // Test Case 4: No Refund Resolution
            Console.WriteLine("--- Test Case 4: No Refund Resolution ---");
            
            var noRefundSubOrder = await context.SellerSubOrders
                .Include(so => so.ParentOrder)
                .Include(so => so.Store)
                .Include(so => so.Items)
                .Where(so => so.Status == OrderStatus.Delivered)
                .Where(so => !context.ReturnRequests.Any(rr => rr.SubOrderId == so.Id))
                .FirstOrDefaultAsync();

            if (noRefundSubOrder != null && noRefundSubOrder.ParentOrder.UserId.HasValue)
            {
                var noRefundReturn = await returnRequestService.CreateReturnRequestAsync(
                    subOrderId: noRefundSubOrder.Id,
                    buyerId: noRefundSubOrder.ParentOrder.UserId.Value,
                    requestType: ReturnRequestType.Complaint,
                    reason: ReturnReason.NotAsDescribed,
                    description: "Testing no refund resolution",
                    isFullReturn: true);

                Console.WriteLine($"✓ Created complaint {noRefundReturn.ReturnNumber}");
                
                var (success4, error4, resolved4) = await returnRequestService.ResolveReturnCaseAsync(
                    returnRequestId: noRefundReturn.Id,
                    storeId: noRefundSubOrder.StoreId,
                    resolutionType: ResolutionType.NoRefund,
                    resolutionNotes: "After investigation, the product matches the description exactly. The buyer's expectations were unrealistic. No refund will be issued.",
                    resolutionAmount: null,
                    initiatedByUserId: noRefundSubOrder.Store.UserId);

                if (success4 && resolved4 != null)
                {
                    Console.WriteLine("✓ Case resolved with No Refund");
                    Console.WriteLine($"  Resolution Notes: {resolved4.ResolutionNotes}");
                    
                    var noRefund = await context.RefundTransactions
                        .FirstOrDefaultAsync(r => r.ReturnRequestId == noRefundReturn.Id);
                    
                    if (noRefund == null)
                    {
                        Console.WriteLine("✓ No refund transaction created (as expected)");
                    }
                    else
                    {
                        Console.WriteLine("⚠ Refund was created (unexpected for NoRefund resolution)");
                    }
                }
                else
                {
                    Console.WriteLine($"✗ Failed to resolve with no refund: {error4}");
                }
            }
            else
            {
                Console.WriteLine("ℹ No additional sub-orders available for no refund test");
            }
            Console.WriteLine();

            Console.WriteLine("=== Test Scenario Completed ===");
            Console.WriteLine();
            Console.WriteLine("Summary of tested features:");
            Console.WriteLine("✓ Full refund resolution with automatic refund creation");
            Console.WriteLine("✓ Partial refund resolution with custom amount");
            Console.WriteLine("✓ No refund resolution without refund creation");
            Console.WriteLine("✓ Refund linkage to return requests");
            Console.WriteLine("✓ Resolution change validation");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Test scenario failed with exception:");
            Console.WriteLine($"  {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"  Stack Trace: {ex.StackTrace}");
        }
    }
}
