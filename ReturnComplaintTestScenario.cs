using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Manual test scenario for return/complaint request functionality.
/// This file demonstrates how the return/complaint system works and can be used for testing.
/// </summary>
public class ReturnComplaintTestScenario
{
    public static async Task RunTestAsync(ApplicationDbContext context, IReturnRequestService returnRequestService)
    {
        Console.WriteLine("=== Return/Complaint Request Test Scenario ===");
        Console.WriteLine();

        try
        {
            // Step 1: Find a delivered sub-order
            var subOrder = await context.SellerSubOrders
                .Include(so => so.ParentOrder)
                .Include(so => so.Store)
                .Include(so => so.Items)
                .Include(so => so.StatusHistory)
                .Where(so => so.Status == OrderStatus.Delivered)
                .FirstOrDefaultAsync();

            if (subOrder == null)
            {
                Console.WriteLine("WARNING: No delivered sub-orders found for testing.");
                return;
            }

            Console.WriteLine($"✓ Found delivered sub-order {subOrder.SubOrderNumber} from store {subOrder.Store.StoreName}");
            Console.WriteLine();

            // Step 2: Get the buyer ID
            var buyerId = subOrder.ParentOrder.UserId;
            if (buyerId == null)
            {
                Console.WriteLine("WARNING: Sub-order does not have an associated buyer (guest order).");
                return;
            }

            // Step 3: Validate return eligibility
            var (isEligible, errorMessage) = await returnRequestService.ValidateReturnEligibilityAsync(
                subOrder.Id, 
                buyerId.Value);

            Console.WriteLine($"Return eligibility validation: {isEligible}");
            if (errorMessage != null)
            {
                Console.WriteLine($"  Error: {errorMessage}");
            }

            if (!isEligible)
            {
                Console.WriteLine("Sub-order is not eligible for return/complaint.");
                return;
            }

            Console.WriteLine();

            // Step 4: Create a return request
            Console.WriteLine("Creating return request...");
            
            var returnRequest = await returnRequestService.CreateReturnRequestAsync(
                subOrder.Id,
                buyerId.Value,
                ReturnRequestType.Return,
                ReturnReason.Damaged,
                "Item arrived with visible damage to the packaging and product.",
                isFullReturn: true);

            Console.WriteLine($"✓ Return request {returnRequest.ReturnNumber} created successfully!");
            Console.WriteLine($"  - Request Type: {returnRequest.RequestType}");
            Console.WriteLine($"  - Reason: {returnRequest.Reason}");
            Console.WriteLine($"  - Status: {returnRequest.Status}");
            Console.WriteLine($"  - Refund Amount: {returnRequest.RefundAmount:C}");
            Console.WriteLine();

            // Step 5: Create a complaint request on another sub-order
            Console.WriteLine("Creating complaint request...");
            
            var anotherSubOrder = await context.SellerSubOrders
                .Include(so => so.ParentOrder)
                .Include(so => so.Store)
                .Where(so => so.Status == OrderStatus.Delivered)
                .Where(so => so.Id != subOrder.Id)
                .Where(so => !context.ReturnRequests.Any(rr => rr.SubOrderId == so.Id))
                .FirstOrDefaultAsync();

            if (anotherSubOrder != null && anotherSubOrder.ParentOrder.UserId != null)
            {
                var complaintRequest = await returnRequestService.CreateReturnRequestAsync(
                    anotherSubOrder.Id,
                    anotherSubOrder.ParentOrder.UserId.Value,
                    ReturnRequestType.Complaint,
                    ReturnReason.NotAsDescribed,
                    "The product does not match the description on the listing.",
                    isFullReturn: true);

                Console.WriteLine($"✓ Complaint request {complaintRequest.ReturnNumber} created successfully!");
                Console.WriteLine($"  - Request Type: {complaintRequest.RequestType}");
                Console.WriteLine($"  - Reason: {complaintRequest.Reason}");
                Console.WriteLine($"  - Status: {complaintRequest.Status}");
                Console.WriteLine();
            }

            // Step 6: List all requests for the buyer
            Console.WriteLine("Listing all requests for buyer...");
            var requests = await returnRequestService.GetReturnRequestsByBuyerAsync(buyerId.Value);
            Console.WriteLine($"Found {requests.Count} request(s) for buyer ID {buyerId.Value}");
            
            foreach (var request in requests)
            {
                Console.WriteLine($"  - {request.ReturnNumber}: {request.RequestType} | Status: {request.Status} | Amount: {request.RefundAmount:C}");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("=== Return/Complaint Request Test Scenario Complete ===");
        Console.WriteLine();
    }
}
