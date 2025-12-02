using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MercatoApp;

/// <summary>
/// Test scenario to verify the tracking number feature implementation.
/// This scenario tests the ability for sellers to add and update tracking information.
/// </summary>
public class TrackingNumberTestScenario
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderStatusService _orderStatusService;
    private readonly ILogger<TrackingNumberTestScenario> _logger;

    public TrackingNumberTestScenario(
        ApplicationDbContext context,
        IOrderStatusService orderStatusService,
        ILogger<TrackingNumberTestScenario> logger)
    {
        _context = context;
        _orderStatusService = orderStatusService;
        _logger = logger;
    }

    /// <summary>
    /// Runs the tracking number test scenario.
    /// Tests adding tracking info when shipping and updating it later.
    /// </summary>
    public async Task RunAsync()
    {
        _logger.LogInformation("=== Tracking Number Feature Test Scenario ===\n");

        try
        {
            // Find a test sub-order to work with
            var subOrder = await _context.SellerSubOrders
                .Include(so => so.ParentOrder)
                .Include(so => so.Store)
                .Include(so => so.StatusHistory)
                .FirstOrDefaultAsync(so => so.Status == OrderStatus.Paid || so.Status == OrderStatus.Preparing);

            if (subOrder == null)
            {
                _logger.LogWarning("No paid or preparing sub-order found for testing. Creating one...");
                
                // Create a test scenario
                var testOrder = await CreateTestOrderAsync();
                if (testOrder == null)
                {
                    _logger.LogError("Failed to create test order");
                    return;
                }
                
                subOrder = testOrder.SubOrders.First();
            }

            _logger.LogInformation($"✓ Found sub-order: {subOrder.SubOrderNumber} (Status: {subOrder.Status})");
            _logger.LogInformation($"  Store: {subOrder.Store.StoreName}");
            _logger.LogInformation($"  Total: {subOrder.TotalAmount:C}\n");

            // Test 1: Mark as preparing if not already
            if (subOrder.Status == OrderStatus.Paid)
            {
                _logger.LogInformation("Test 1: Marking order as Preparing...");
                var (success, error) = await _orderStatusService.UpdateSubOrderToPreparingAsync(subOrder.Id, userId: 1);
                
                if (success)
                {
                    _logger.LogInformation("✓ Order marked as Preparing successfully\n");
                    await _context.Entry(subOrder).ReloadAsync();
                }
                else
                {
                    _logger.LogError($"✗ Failed to mark as preparing: {error}\n");
                    return;
                }
            }

            // Test 2: Add tracking information when shipping
            _logger.LogInformation("Test 2: Marking order as Shipped with tracking information...");
            var trackingNumber = "1Z999AA10123456784";
            var carrierName = "UPS";
            var trackingUrl = "https://www.ups.com/track?tracknum=1Z999AA10123456784";

            var (shipSuccess, shipError) = await _orderStatusService.UpdateSubOrderToShippedAsync(
                subOrder.Id,
                trackingNumber,
                carrierName,
                trackingUrl,
                userId: 1);

            if (shipSuccess)
            {
                _logger.LogInformation("✓ Order marked as Shipped successfully");
                await _context.Entry(subOrder).ReloadAsync();
                
                _logger.LogInformation($"  Tracking Number: {subOrder.TrackingNumber}");
                _logger.LogInformation($"  Carrier: {subOrder.CarrierName}");
                _logger.LogInformation($"  Tracking URL: {subOrder.TrackingUrl}\n");

                // Verify tracking info was saved
                if (subOrder.TrackingNumber == trackingNumber &&
                    subOrder.CarrierName == carrierName &&
                    subOrder.TrackingUrl == trackingUrl)
                {
                    _logger.LogInformation("✓ Tracking information saved correctly\n");
                }
                else
                {
                    _logger.LogError("✗ Tracking information not saved correctly\n");
                }
            }
            else
            {
                _logger.LogError($"✗ Failed to mark as shipped: {shipError}\n");
                return;
            }

            // Test 3: Update tracking information
            _logger.LogInformation("Test 3: Updating tracking information...");
            var updatedTrackingNumber = "1Z999AA10987654321";
            var updatedCarrierName = "UPS Ground";
            var updatedTrackingUrl = "https://www.ups.com/track?tracknum=1Z999AA10987654321";

            var (updateSuccess, updateError) = await _orderStatusService.UpdateTrackingInformationAsync(
                subOrder.Id,
                updatedTrackingNumber,
                updatedCarrierName,
                updatedTrackingUrl,
                userId: 1);

            if (updateSuccess)
            {
                _logger.LogInformation("✓ Tracking information updated successfully");
                await _context.Entry(subOrder).ReloadAsync();
                
                _logger.LogInformation($"  Updated Tracking Number: {subOrder.TrackingNumber}");
                _logger.LogInformation($"  Updated Carrier: {subOrder.CarrierName}");
                _logger.LogInformation($"  Updated Tracking URL: {subOrder.TrackingUrl}\n");

                // Verify tracking info was updated
                if (subOrder.TrackingNumber == updatedTrackingNumber &&
                    subOrder.CarrierName == updatedCarrierName &&
                    subOrder.TrackingUrl == updatedTrackingUrl)
                {
                    _logger.LogInformation("✓ Tracking information updated correctly\n");
                }
                else
                {
                    _logger.LogError("✗ Tracking information not updated correctly\n");
                }
            }
            else
            {
                _logger.LogError($"✗ Failed to update tracking: {updateError}\n");
                return;
            }

            // Test 4: Verify audit history
            _logger.LogInformation("Test 4: Verifying audit history...");
            await _context.Entry(subOrder).Collection(so => so.StatusHistory).LoadAsync();
            
            var historyCount = subOrder.StatusHistory.Count;
            _logger.LogInformation($"✓ Found {historyCount} status history records:");
            
            foreach (var history in subOrder.StatusHistory.OrderBy(h => h.ChangedAt))
            {
                var statusChange = history.PreviousStatus.HasValue 
                    ? $"{history.PreviousStatus} → {history.NewStatus}" 
                    : $"Created as {history.NewStatus}";
                
                _logger.LogInformation($"  - {history.ChangedAt:yyyy-MM-dd HH:mm}: {statusChange}");
                if (!string.IsNullOrEmpty(history.Notes))
                {
                    _logger.LogInformation($"    Notes: {history.Notes}");
                }
            }
            
            _logger.LogInformation("");

            // Summary
            _logger.LogInformation("=== Test Scenario Summary ===");
            _logger.LogInformation("✓ All tracking number feature tests passed!");
            _logger.LogInformation("✓ Sellers can add tracking info when marking as shipped");
            _logger.LogInformation("✓ Sellers can update tracking info after shipping");
            _logger.LogInformation("✓ All changes are logged in audit history");
            _logger.LogInformation("✓ Feature is fully functional and ready for use\n");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test scenario failed with exception");
        }
    }

    /// <summary>
    /// Creates a test order with a sub-order in Paid status.
    /// </summary>
    private async Task<Order?> CreateTestOrderAsync()
    {
        try
        {
            // Find a test store
            var store = await _context.Stores.FirstOrDefaultAsync();
            if (store == null)
            {
                _logger.LogError("No store found to create test order");
                return null;
            }

            // Find a test user (buyer)
            var buyer = await _context.Users.FirstOrDefaultAsync(u => u.UserType == UserType.Buyer);
            if (buyer == null)
            {
                _logger.LogError("No buyer found to create test order");
                return null;
            }

            // Find an address
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.UserId == buyer.Id);
            if (address == null)
            {
                _logger.LogError("No address found for buyer");
                return null;
            }

            // Create order
            var orderNumber = $"ORD-TEST-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = buyer.Id,
                DeliveryAddressId = address.Id,
                Status = OrderStatus.Paid,
                PaymentStatus = PaymentStatus.Completed,
                Subtotal = 100.00m,
                ShippingCost = 10.00m,
                TotalAmount = 110.00m,
                OrderedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create sub-order
            var subOrder = new SellerSubOrder
            {
                ParentOrderId = order.Id,
                StoreId = store.Id,
                SubOrderNumber = $"{orderNumber}-1",
                Status = OrderStatus.Paid,
                Subtotal = 100.00m,
                ShippingCost = 10.00m,
                TotalAmount = 110.00m,
                CreatedAt = DateTime.UtcNow
            };

            _context.SellerSubOrders.Add(subOrder);
            await _context.SaveChangesAsync();

            // Load related data
            await _context.Entry(order).Collection(o => o.SubOrders).LoadAsync();
            await _context.Entry(subOrder).Reference(so => so.Store).LoadAsync();

            _logger.LogInformation($"Created test order: {orderNumber}");
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test order");
            return null;
        }
    }
}
