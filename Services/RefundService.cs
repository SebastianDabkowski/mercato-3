using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing refund transactions.
/// </summary>
public class RefundService : IRefundService
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentProviderService _paymentProviderService;
    private readonly IEscrowService _escrowService;
    private readonly ILogger<RefundService> _logger;

    /// <summary>
    /// Tolerance for decimal currency comparisons.
    /// </summary>
    private const decimal CurrencyTolerance = 0.01m;

    public RefundService(
        ApplicationDbContext context,
        IPaymentProviderService paymentProviderService,
        IEscrowService escrowService,
        ILogger<RefundService> logger)
    {
        _context = context;
        _paymentProviderService = paymentProviderService;
        _escrowService = escrowService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RefundTransaction> ProcessFullRefundAsync(
        int orderId,
        string reason,
        int initiatedByUserId,
        string? notes = null)
    {
        // Load order with all necessary data
        var order = await _context.Orders
            .Include(o => o.PaymentTransactions)
            .Include(o => o.SubOrders)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        // Validate refund eligibility
        var (isValid, errorMessage) = await ValidateRefundEligibilityAsync(orderId, order.TotalAmount);
        if (!isValid)
        {
            throw new InvalidOperationException(errorMessage ?? "Refund is not eligible.");
        }

        // Get the completed payment transaction
        var paymentTransaction = order.PaymentTransactions
            .Where(pt => pt.Status == PaymentStatus.Completed || pt.Status == PaymentStatus.Authorized)
            .OrderByDescending(pt => pt.CompletedAt ?? pt.CreatedAt)
            .FirstOrDefault();

        if (paymentTransaction == null)
        {
            throw new InvalidOperationException("No completed payment transaction found for this order.");
        }

        // Calculate refund amount (total minus already refunded)
        var refundAmount = order.TotalAmount - order.RefundedAmount;

        if (refundAmount <= 0)
        {
            throw new InvalidOperationException("No amount available to refund.");
        }

        // Create refund transaction
        var refundTransaction = new RefundTransaction
        {
            RefundNumber = GenerateRefundNumber(),
            OrderId = orderId,
            PaymentTransactionId = paymentTransaction.Id,
            SellerSubOrderId = null, // Full refund
            RefundType = RefundType.Full,
            RefundAmount = refundAmount,
            CurrencyCode = paymentTransaction.CurrencyCode,
            Status = RefundStatus.Requested,
            Reason = reason,
            InitiatedByUserId = initiatedByUserId,
            Notes = notes,
            RequestedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.RefundTransactions.Add(refundTransaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created full refund transaction {RefundNumber} for order {OrderId}, amount: {Amount}",
            refundTransaction.RefundNumber, orderId, refundAmount);

        // Process refund with payment provider
        try
        {
            refundTransaction.Status = RefundStatus.Processing;
            refundTransaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var providerResult = await _paymentProviderService.ProcessRefundAsync(
                paymentTransaction,
                refundAmount,
                reason);

            if (providerResult.Success)
            {
                refundTransaction.Status = RefundStatus.Completed;
                refundTransaction.ProviderRefundId = providerResult.ProviderRefundId;
                refundTransaction.CompletedAt = DateTime.UtcNow;
                refundTransaction.ProviderMetadata = System.Text.Json.JsonSerializer.Serialize(providerResult.Metadata);

                // Update order refunded amount
                order.RefundedAmount += refundAmount;
                order.PaymentStatus = PaymentStatus.Refunded;
                order.UpdatedAt = DateTime.UtcNow;

                // Return escrow for all sub-orders
                var escrowTransactions = await _context.EscrowTransactions
                    .Where(et => et.PaymentTransactionId == paymentTransaction.Id)
                    .ToListAsync();

                foreach (var escrow in escrowTransactions)
                {
                    var availableAmount = escrow.GrossAmount - escrow.RefundedAmount;
                    if (availableAmount > CurrencyTolerance)
                    {
                        await _escrowService.ReturnEscrowToBuyerAsync(
                            escrow.Id,
                            availableAmount,
                            $"Full order refund: {refundTransaction.RefundNumber}");
                    }
                }

                _logger.LogInformation("Full refund {RefundNumber} completed successfully", refundTransaction.RefundNumber);
            }
            else
            {
                refundTransaction.Status = RefundStatus.Failed;
                refundTransaction.ErrorMessage = providerResult.ErrorMessage;
                
                _logger.LogError("Full refund {RefundNumber} failed: {Error}",
                    refundTransaction.RefundNumber, providerResult.ErrorMessage);
            }

            refundTransaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing full refund {RefundNumber}", refundTransaction.RefundNumber);
            
            refundTransaction.Status = RefundStatus.Failed;
            refundTransaction.ErrorMessage = ex.Message;
            refundTransaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            throw;
        }

        return refundTransaction;
    }

    /// <inheritdoc />
    public async Task<RefundTransaction> ProcessPartialRefundAsync(
        int orderId,
        int sellerSubOrderId,
        decimal refundAmount,
        string reason,
        int initiatedByUserId,
        string? notes = null)
    {
        // Validate partial refund eligibility
        var (isValid, errorMessage) = await ValidatePartialRefundEligibilityAsync(sellerSubOrderId, refundAmount);
        if (!isValid)
        {
            throw new InvalidOperationException(errorMessage ?? "Partial refund is not eligible.");
        }

        // Load order and sub-order with necessary data
        var order = await _context.Orders
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        var subOrder = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
            .FirstOrDefaultAsync(so => so.Id == sellerSubOrderId);

        if (subOrder == null)
        {
            throw new InvalidOperationException("Seller sub-order not found.");
        }

        if (subOrder.ParentOrderId != orderId)
        {
            throw new InvalidOperationException("Seller sub-order does not belong to the specified order.");
        }

        // Get the completed payment transaction
        var paymentTransaction = order.PaymentTransactions
            .Where(pt => pt.Status == PaymentStatus.Completed || pt.Status == PaymentStatus.Authorized)
            .OrderByDescending(pt => pt.CompletedAt ?? pt.CreatedAt)
            .FirstOrDefault();

        if (paymentTransaction == null)
        {
            throw new InvalidOperationException("No completed payment transaction found for this order.");
        }

        // Create refund transaction
        var refundTransaction = new RefundTransaction
        {
            RefundNumber = GenerateRefundNumber(),
            OrderId = orderId,
            PaymentTransactionId = paymentTransaction.Id,
            SellerSubOrderId = sellerSubOrderId,
            RefundType = RefundType.Partial,
            RefundAmount = refundAmount,
            CurrencyCode = paymentTransaction.CurrencyCode,
            Status = RefundStatus.Requested,
            Reason = reason,
            InitiatedByUserId = initiatedByUserId,
            Notes = notes,
            RequestedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.RefundTransactions.Add(refundTransaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created partial refund transaction {RefundNumber} for sub-order {SubOrderId}, amount: {Amount}",
            refundTransaction.RefundNumber, sellerSubOrderId, refundAmount);

        // Process refund with payment provider
        try
        {
            refundTransaction.Status = RefundStatus.Processing;
            refundTransaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var providerResult = await _paymentProviderService.ProcessRefundAsync(
                paymentTransaction,
                refundAmount,
                reason);

            if (providerResult.Success)
            {
                refundTransaction.Status = RefundStatus.Completed;
                refundTransaction.ProviderRefundId = providerResult.ProviderRefundId;
                refundTransaction.CompletedAt = DateTime.UtcNow;
                refundTransaction.ProviderMetadata = System.Text.Json.JsonSerializer.Serialize(providerResult.Metadata);

                // Update order refunded amount
                order.RefundedAmount += refundAmount;
                order.UpdatedAt = DateTime.UtcNow;

                // Update sub-order refunded amount
                subOrder.RefundedAmount += refundAmount;
                subOrder.UpdatedAt = DateTime.UtcNow;

                // Return escrow for this sub-order
                var escrowTransaction = await _context.EscrowTransactions
                    .FirstOrDefaultAsync(et => et.SellerSubOrderId == sellerSubOrderId);

                if (escrowTransaction != null)
                {
                    await _escrowService.ReturnEscrowToBuyerAsync(
                        escrowTransaction.Id,
                        refundAmount,
                        $"Partial refund: {refundTransaction.RefundNumber}");
                }

                _logger.LogInformation("Partial refund {RefundNumber} completed successfully", refundTransaction.RefundNumber);
            }
            else
            {
                refundTransaction.Status = RefundStatus.Failed;
                refundTransaction.ErrorMessage = providerResult.ErrorMessage;
                
                _logger.LogError("Partial refund {RefundNumber} failed: {Error}",
                    refundTransaction.RefundNumber, providerResult.ErrorMessage);
            }

            refundTransaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing partial refund {RefundNumber}", refundTransaction.RefundNumber);
            
            refundTransaction.Status = RefundStatus.Failed;
            refundTransaction.ErrorMessage = ex.Message;
            refundTransaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            throw;
        }

        return refundTransaction;
    }

    /// <inheritdoc />
    public async Task<(bool IsValid, string? ErrorMessage)> ValidateRefundEligibilityAsync(
        int orderId,
        decimal? refundAmount = null)
    {
        var order = await _context.Orders
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return (false, "Order not found.");
        }

        // Check if order has a completed payment
        var hasCompletedPayment = order.PaymentTransactions
            .Any(pt => pt.Status == PaymentStatus.Completed || pt.Status == PaymentStatus.Authorized);

        if (!hasCompletedPayment)
        {
            return (false, "Order does not have a completed payment transaction.");
        }

        // Check if full refund amount is available
        var availableRefundAmount = order.TotalAmount - order.RefundedAmount;

        if (availableRefundAmount <= 0)
        {
            return (false, "No amount available to refund. Order has been fully refunded.");
        }

        // If specific refund amount requested, validate it
        if (refundAmount.HasValue)
        {
            if (refundAmount.Value <= 0)
            {
                return (false, "Refund amount must be greater than zero.");
            }

            if (refundAmount.Value > availableRefundAmount)
            {
                return (false, $"Refund amount ({refundAmount.Value:C}) exceeds available refund amount ({availableRefundAmount:C}).");
            }
        }

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool IsValid, string? ErrorMessage)> ValidatePartialRefundEligibilityAsync(
        int sellerSubOrderId,
        decimal refundAmount)
    {
        if (refundAmount <= 0)
        {
            return (false, "Refund amount must be greater than zero.");
        }

        var subOrder = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(so => so.Id == sellerSubOrderId);

        if (subOrder == null)
        {
            return (false, "Seller sub-order not found.");
        }

        // Check if parent order has a completed payment
        var hasCompletedPayment = subOrder.ParentOrder.PaymentTransactions
            .Any(pt => pt.Status == PaymentStatus.Completed || pt.Status == PaymentStatus.Authorized);

        if (!hasCompletedPayment)
        {
            return (false, "Order does not have a completed payment transaction.");
        }

        // Check available refund amount for this sub-order
        var availableRefundAmount = subOrder.TotalAmount - subOrder.RefundedAmount;

        if (availableRefundAmount <= 0)
        {
            return (false, "No amount available to refund for this sub-order. It has been fully refunded.");
        }

        if (refundAmount > availableRefundAmount)
        {
            return (false, $"Refund amount ({refundAmount:C}) exceeds available refund amount for this sub-order ({availableRefundAmount:C}).");
        }

        // Validate that refund won't create negative escrow balance
        var escrowTransaction = await _context.EscrowTransactions
            .FirstOrDefaultAsync(et => et.SellerSubOrderId == sellerSubOrderId);

        if (escrowTransaction != null)
        {
            var escrowAvailable = escrowTransaction.GrossAmount - escrowTransaction.RefundedAmount;
            
            if (refundAmount > escrowAvailable + CurrencyTolerance)
            {
                return (false, $"Refund amount exceeds available escrow balance ({escrowAvailable:C}).");
            }

            // Prevent negative net amount after commission adjustment
            // The commission will be proportionally adjusted, so we just check gross amount
            if (escrowTransaction.Status == EscrowStatus.Released)
            {
                return (false, "Cannot refund - escrow has already been released to seller.");
            }
        }

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<List<RefundTransaction>> GetRefundsByOrderAsync(int orderId)
    {
        return await _context.RefundTransactions
            .Where(rt => rt.OrderId == orderId)
            .Include(rt => rt.InitiatedBy)
            .Include(rt => rt.SellerSubOrder)
            .OrderByDescending(rt => rt.RequestedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<RefundTransaction>> GetAllRefundsAsync(
        RefundStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.RefundTransactions
            .Include(rt => rt.Order)
            .Include(rt => rt.InitiatedBy)
            .Include(rt => rt.SellerSubOrder)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(rt => rt.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(rt => rt.RequestedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(rt => rt.RequestedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(rt => rt.RequestedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<RefundTransaction?> GetRefundByIdAsync(int refundId)
    {
        return await _context.RefundTransactions
            .Include(rt => rt.Order)
            .Include(rt => rt.PaymentTransaction)
            .Include(rt => rt.SellerSubOrder)
            .Include(rt => rt.InitiatedBy)
            .FirstOrDefaultAsync(rt => rt.Id == refundId);
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalRefundedAmountAsync(int orderId)
    {
        return await _context.RefundTransactions
            .Where(rt => rt.OrderId == orderId && rt.Status == RefundStatus.Completed)
            .SumAsync(rt => rt.RefundAmount);
    }

    /// <inheritdoc />
    public async Task<List<RefundTransaction>> GetRefundsByStoreAsync(int storeId, RefundStatus? status = null)
    {
        var query = _context.RefundTransactions
            .Include(rt => rt.Order)
            .Include(rt => rt.InitiatedBy)
            .Include(rt => rt.SellerSubOrder)
            .Where(rt => rt.SellerSubOrder != null && rt.SellerSubOrder.StoreId == storeId);

        if (status.HasValue)
        {
            query = query.Where(rt => rt.Status == status.Value);
        }

        return await query
            .OrderByDescending(rt => rt.RequestedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<RefundTransaction> RetryFailedRefundAsync(int refundId)
    {
        var refundTransaction = await _context.RefundTransactions
            .Include(rt => rt.Order)
                .ThenInclude(o => o.PaymentTransactions)
            .Include(rt => rt.SellerSubOrder)
            .FirstOrDefaultAsync(rt => rt.Id == refundId);

        if (refundTransaction == null)
        {
            throw new InvalidOperationException("Refund transaction not found.");
        }

        if (refundTransaction.Status != RefundStatus.Failed)
        {
            throw new InvalidOperationException($"Refund transaction is not in Failed status (current: {refundTransaction.Status}).");
        }

        _logger.LogInformation("Retrying failed refund {RefundNumber}", refundTransaction.RefundNumber);

        // Get the original payment transaction
        var paymentTransaction = refundTransaction.Order.PaymentTransactions
            .FirstOrDefault(pt => pt.Id == refundTransaction.PaymentTransactionId);

        if (paymentTransaction == null)
        {
            throw new InvalidOperationException("Original payment transaction not found.");
        }

        // Retry the refund processing
        try
        {
            refundTransaction.Status = RefundStatus.Processing;
            refundTransaction.ErrorMessage = null;
            refundTransaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var providerResult = await _paymentProviderService.ProcessRefundAsync(
                paymentTransaction,
                refundTransaction.RefundAmount,
                refundTransaction.Reason ?? "Retry of failed refund");

            if (providerResult.Success)
            {
                refundTransaction.Status = RefundStatus.Completed;
                refundTransaction.ProviderRefundId = providerResult.ProviderRefundId;
                refundTransaction.CompletedAt = DateTime.UtcNow;
                refundTransaction.ProviderMetadata = System.Text.Json.JsonSerializer.Serialize(providerResult.Metadata);

                // Update order and sub-order refunded amounts if not already updated
                var order = refundTransaction.Order;
                order.RefundedAmount += refundTransaction.RefundAmount;
                order.UpdatedAt = DateTime.UtcNow;

                if (refundTransaction.SellerSubOrderId.HasValue)
                {
                    var subOrder = refundTransaction.SellerSubOrder;
                    if (subOrder != null)
                    {
                        subOrder.RefundedAmount += refundTransaction.RefundAmount;
                        subOrder.UpdatedAt = DateTime.UtcNow;
                    }

                    // Return escrow
                    var escrowTransaction = await _context.EscrowTransactions
                        .FirstOrDefaultAsync(et => et.SellerSubOrderId == refundTransaction.SellerSubOrderId.Value);

                    if (escrowTransaction != null)
                    {
                        await _escrowService.ReturnEscrowToBuyerAsync(
                            escrowTransaction.Id,
                            refundTransaction.RefundAmount,
                            $"Refund retry: {refundTransaction.RefundNumber}");
                    }
                }
                else
                {
                    // Full refund - return all escrow
                    var escrowTransactions = await _context.EscrowTransactions
                        .Where(et => et.PaymentTransactionId == refundTransaction.PaymentTransactionId)
                        .ToListAsync();

                    foreach (var escrow in escrowTransactions)
                    {
                        var availableAmount = escrow.GrossAmount - escrow.RefundedAmount;
                        if (availableAmount > CurrencyTolerance)
                        {
                            await _escrowService.ReturnEscrowToBuyerAsync(
                                escrow.Id,
                                availableAmount,
                                $"Full refund retry: {refundTransaction.RefundNumber}");
                        }
                    }
                }

                _logger.LogInformation("Refund {RefundNumber} retry completed successfully", refundTransaction.RefundNumber);
            }
            else
            {
                refundTransaction.Status = RefundStatus.Failed;
                refundTransaction.ErrorMessage = providerResult.ErrorMessage;
                
                _logger.LogError("Refund {RefundNumber} retry failed: {Error}",
                    refundTransaction.RefundNumber, providerResult.ErrorMessage);
            }

            refundTransaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying refund {RefundNumber}", refundTransaction.RefundNumber);
            
            refundTransaction.Status = RefundStatus.Failed;
            refundTransaction.ErrorMessage = ex.Message;
            refundTransaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            throw;
        }

        return refundTransaction;
    }

    /// <summary>
    /// Generates a unique refund reference number.
    /// </summary>
    /// <returns>The generated refund number.</returns>
    private static string GenerateRefundNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Random.Shared.Next(1000, 9999);
        return $"REF-{timestamp}-{random}";
    }
}
