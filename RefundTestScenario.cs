using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Manual test scenario for refund functionality.
/// This file demonstrates how the refund system works and can be used for testing.
/// </summary>
public class RefundTestScenario
{
    private readonly ApplicationDbContext _context;
    private readonly IRefundService _refundService;
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;
    private readonly ILogger<RefundTestScenario> _logger;

    public RefundTestScenario(
        ApplicationDbContext context,
        IRefundService refundService,
        IPaymentService paymentService,
        IOrderService orderService,
        ILogger<RefundTestScenario> logger)
    {
        _context = context;
        _refundService = refundService;
        _paymentService = paymentService;
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Runs a full refund test scenario.
    /// </summary>
    public async Task RunFullRefundTestAsync()
    {
        _logger.LogInformation("=== Starting Full Refund Test Scenario ===");

        try
        {
            // Step 1: Find a completed order
            var order = await _context.Orders
                .Include(o => o.PaymentTransactions)
                .Include(o => o.SubOrders)
                .Where(o => o.PaymentStatus == PaymentStatus.Completed)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                _logger.LogWarning("No completed orders found for testing.");
                return;
            }

            _logger.LogInformation("Found order {OrderNumber} with total amount {Amount}",
                order.OrderNumber, order.TotalAmount);

            // Step 2: Validate refund eligibility
            var (isValid, errorMessage) = await _refundService.ValidateRefundEligibilityAsync(order.Id);
            _logger.LogInformation("Refund eligibility validation: {IsValid}, Error: {Error}",
                isValid, errorMessage ?? "None");

            if (!isValid)
            {
                _logger.LogWarning("Order is not eligible for refund: {Error}", errorMessage);
                return;
            }

            // Step 3: Get admin user for refund processing
            var adminUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "admin@example.com");

            if (adminUser == null)
            {
                _logger.LogWarning("Admin user not found.");
                return;
            }

            // Step 4: Process full refund
            _logger.LogInformation("Processing full refund for order {OrderNumber}...", order.OrderNumber);
            
            var refundTransaction = await _refundService.ProcessFullRefundAsync(
                order.Id,
                "Test full refund - manual test scenario",
                adminUser.Id,
                "Testing full refund functionality");

            _logger.LogInformation("Full refund {RefundNumber} created with status: {Status}",
                refundTransaction.RefundNumber, refundTransaction.Status);

            if (refundTransaction.Status == RefundStatus.Completed)
            {
                _logger.LogInformation("✓ Full refund completed successfully!");
                _logger.LogInformation("  - Refund Amount: {Amount}", refundTransaction.RefundAmount);
                _logger.LogInformation("  - Provider Refund ID: {ProviderId}", refundTransaction.ProviderRefundId);
            }
            else if (refundTransaction.Status == RefundStatus.Failed)
            {
                _logger.LogError("✗ Full refund failed: {Error}", refundTransaction.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full refund test");
        }

        _logger.LogInformation("=== Full Refund Test Scenario Complete ===");
    }

    /// <summary>
    /// Runs a partial refund test scenario.
    /// </summary>
    public async Task RunPartialRefundTestAsync()
    {
        _logger.LogInformation("=== Starting Partial Refund Test Scenario ===");

        try
        {
            // Step 1: Find a completed order with sub-orders
            var subOrder = await _context.SellerSubOrders
                .Include(so => so.ParentOrder)
                    .ThenInclude(o => o.PaymentTransactions)
                .Where(so => so.ParentOrder.PaymentStatus == PaymentStatus.Completed)
                .Where(so => so.RefundedAmount == 0)
                .FirstOrDefaultAsync();

            if (subOrder == null)
            {
                _logger.LogWarning("No suitable sub-order found for partial refund testing.");
                return;
            }

            _logger.LogInformation("Found sub-order {SubOrderNumber} with total amount {Amount}",
                subOrder.SubOrderNumber, subOrder.TotalAmount);

            // Step 2: Calculate partial refund amount (50% of total)
            var partialRefundAmount = Math.Round(subOrder.TotalAmount * 0.5m, 2);
            _logger.LogInformation("Testing partial refund of {Amount} (50% of total)", partialRefundAmount);

            // Step 3: Validate partial refund eligibility
            var (isValid, errorMessage) = await _refundService.ValidatePartialRefundEligibilityAsync(
                subOrder.Id,
                partialRefundAmount);

            _logger.LogInformation("Partial refund eligibility validation: {IsValid}, Error: {Error}",
                isValid, errorMessage ?? "None");

            if (!isValid)
            {
                _logger.LogWarning("Sub-order is not eligible for partial refund: {Error}", errorMessage);
                return;
            }

            // Step 4: Get admin user
            var adminUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "admin@example.com");

            if (adminUser == null)
            {
                _logger.LogWarning("Admin user not found.");
                return;
            }

            // Step 5: Process partial refund
            _logger.LogInformation("Processing partial refund for sub-order {SubOrderNumber}...",
                subOrder.SubOrderNumber);

            var refundTransaction = await _refundService.ProcessPartialRefundAsync(
                subOrder.ParentOrderId,
                subOrder.Id,
                partialRefundAmount,
                "Test partial refund - manual test scenario",
                adminUser.Id,
                "Testing partial refund functionality");

            _logger.LogInformation("Partial refund {RefundNumber} created with status: {Status}",
                refundTransaction.RefundNumber, refundTransaction.Status);

            if (refundTransaction.Status == RefundStatus.Completed)
            {
                _logger.LogInformation("✓ Partial refund completed successfully!");
                _logger.LogInformation("  - Refund Amount: {Amount}", refundTransaction.RefundAmount);
                _logger.LogInformation("  - Provider Refund ID: {ProviderId}", refundTransaction.ProviderRefundId);

                // Step 6: Verify escrow adjustments
                var escrow = await _context.EscrowTransactions
                    .FirstOrDefaultAsync(et => et.SellerSubOrderId == subOrder.Id);

                if (escrow != null)
                {
                    _logger.LogInformation("  - Escrow Status: {Status}", escrow.Status);
                    _logger.LogInformation("  - Escrow Refunded Amount: {Amount}", escrow.RefundedAmount);
                    _logger.LogInformation("  - Escrow Net Amount: {Amount}", escrow.NetAmount);
                }
            }
            else if (refundTransaction.Status == RefundStatus.Failed)
            {
                _logger.LogError("✗ Partial refund failed: {Error}", refundTransaction.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during partial refund test");
        }

        _logger.LogInformation("=== Partial Refund Test Scenario Complete ===");
    }

    /// <summary>
    /// Tests multiple partial refunds to ensure negative balance prevention.
    /// </summary>
    public async Task RunMultiplePartialRefundsTestAsync()
    {
        _logger.LogInformation("=== Starting Multiple Partial Refunds Test (Negative Balance Prevention) ===");

        try
        {
            // Find a sub-order
            var subOrder = await _context.SellerSubOrders
                .Include(so => so.ParentOrder)
                    .ThenInclude(o => o.PaymentTransactions)
                .Where(so => so.ParentOrder.PaymentStatus == PaymentStatus.Completed)
                .Where(so => so.RefundedAmount == 0)
                .FirstOrDefaultAsync();

            if (subOrder == null)
            {
                _logger.LogWarning("No suitable sub-order found for testing.");
                return;
            }

            var adminUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "admin@example.com");

            if (adminUser == null)
            {
                _logger.LogWarning("Admin user not found.");
                return;
            }

            _logger.LogInformation("Testing multiple partial refunds on sub-order {SubOrderNumber} (Total: {Amount})",
                subOrder.SubOrderNumber, subOrder.TotalAmount);

            // First partial refund: 30%
            var refund1Amount = Math.Round(subOrder.TotalAmount * 0.3m, 2);
            _logger.LogInformation("Attempting first partial refund: {Amount}", refund1Amount);
            
            var refund1 = await _refundService.ProcessPartialRefundAsync(
                subOrder.ParentOrderId, subOrder.Id, refund1Amount,
                "First partial refund", adminUser.Id);
            
            _logger.LogInformation("First refund status: {Status}", refund1.Status);

            // Reload sub-order to see updated values
            await _context.Entry(subOrder).ReloadAsync();
            _logger.LogInformation("Remaining balance: {Amount}", subOrder.TotalAmount - subOrder.RefundedAmount);

            // Second partial refund: 50%
            var refund2Amount = Math.Round(subOrder.TotalAmount * 0.5m, 2);
            _logger.LogInformation("Attempting second partial refund: {Amount}", refund2Amount);
            
            var refund2 = await _refundService.ProcessPartialRefundAsync(
                subOrder.ParentOrderId, subOrder.Id, refund2Amount,
                "Second partial refund", adminUser.Id);
            
            _logger.LogInformation("Second refund status: {Status}", refund2.Status);

            // Reload again
            await _context.Entry(subOrder).ReloadAsync();
            _logger.LogInformation("Final remaining balance: {Amount}", subOrder.TotalAmount - subOrder.RefundedAmount);

            // Try to refund more than available (should fail)
            var excessAmount = (subOrder.TotalAmount - subOrder.RefundedAmount) + 10m;
            _logger.LogInformation("Attempting excess refund (should fail): {Amount}", excessAmount);
            
            try
            {
                var excessRefund = await _refundService.ProcessPartialRefundAsync(
                    subOrder.ParentOrderId, subOrder.Id, excessAmount,
                    "Excess refund (should fail)", adminUser.Id);
                
                _logger.LogError("✗ Excess refund should have failed but didn't!");
            }
            catch (Exception ex)
            {
                _logger.LogInformation("✓ Excess refund correctly rejected: {Error}", ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during multiple partial refunds test");
        }

        _logger.LogInformation("=== Multiple Partial Refunds Test Complete ===");
    }
}
