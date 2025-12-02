using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing escrow transactions in the marketplace.
/// </summary>
public class EscrowService : IEscrowService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EscrowService> _logger;

    public EscrowService(
        ApplicationDbContext context,
        ILogger<EscrowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<EscrowTransaction>> CreateEscrowAllocationsAsync(int paymentTransactionId)
    {
        var paymentTransaction = await _context.PaymentTransactions
            .Include(pt => pt.Order)
                .ThenInclude(o => o.SubOrders)
            .FirstOrDefaultAsync(pt => pt.Id == paymentTransactionId);

        if (paymentTransaction == null)
        {
            throw new InvalidOperationException("Payment transaction not found.");
        }

        if (paymentTransaction.Status != PaymentStatus.Completed)
        {
            throw new InvalidOperationException("Payment must be completed before creating escrow allocations.");
        }

        // Check if escrow allocations already exist (idempotency)
        var existingEscrows = await _context.EscrowTransactions
            .Where(et => et.PaymentTransactionId == paymentTransactionId)
            .ToListAsync();

        if (existingEscrows.Any())
        {
            _logger.LogInformation("Escrow allocations already exist for payment transaction {PaymentTransactionId}", paymentTransactionId);
            return existingEscrows;
        }

        var escrowTransactions = new List<EscrowTransaction>();

        // Create escrow allocation for each seller sub-order
        foreach (var subOrder in paymentTransaction.Order.SubOrders)
        {
            var grossAmount = subOrder.TotalAmount;
            var commissionAmount = await CalculateCommissionAsync(grossAmount);
            var netAmount = grossAmount - commissionAmount;

            var escrowTransaction = new EscrowTransaction
            {
                PaymentTransactionId = paymentTransactionId,
                SellerSubOrderId = subOrder.Id,
                StoreId = subOrder.StoreId,
                GrossAmount = grossAmount,
                CommissionAmount = commissionAmount,
                NetAmount = netAmount,
                Status = EscrowStatus.Held,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.EscrowTransactions.Add(escrowTransaction);
            escrowTransactions.Add(escrowTransaction);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created {Count} escrow allocations for payment transaction {PaymentTransactionId}",
            escrowTransactions.Count, paymentTransactionId);

        return escrowTransactions;
    }

    /// <inheritdoc />
    public async Task<bool> ReleaseEscrowAsync(int escrowTransactionId)
    {
        var escrowTransaction = await _context.EscrowTransactions
            .Include(et => et.SellerSubOrder)
            .FirstOrDefaultAsync(et => et.Id == escrowTransactionId);

        if (escrowTransaction == null)
        {
            _logger.LogWarning("Escrow transaction {EscrowTransactionId} not found", escrowTransactionId);
            return false;
        }

        if (escrowTransaction.Status == EscrowStatus.Released)
        {
            _logger.LogInformation("Escrow transaction {EscrowTransactionId} already released", escrowTransactionId);
            return true;
        }

        if (escrowTransaction.Status != EscrowStatus.EligibleForPayout && escrowTransaction.Status != EscrowStatus.Held)
        {
            _logger.LogWarning("Escrow transaction {EscrowTransactionId} cannot be released from status {Status}",
                escrowTransactionId, escrowTransaction.Status);
            return false;
        }

        // Check if sub-order is in a state that allows payout
        if (escrowTransaction.SellerSubOrder.Status != OrderStatus.Delivered)
        {
            _logger.LogWarning("Sub-order {SubOrderId} is not in Delivered state for escrow release (status: {Status})",
                escrowTransaction.SellerSubOrderId, escrowTransaction.SellerSubOrder.Status);
            return false;
        }

        escrowTransaction.Status = EscrowStatus.Released;
        escrowTransaction.ReleasedAt = DateTime.UtcNow;
        escrowTransaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Released escrow transaction {EscrowTransactionId} for sub-order {SubOrderId}, net amount: {NetAmount}",
            escrowTransactionId, escrowTransaction.SellerSubOrderId, escrowTransaction.NetAmount);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ReturnEscrowToBuyerAsync(int escrowTransactionId, decimal refundAmount, string? notes = null)
    {
        var escrowTransaction = await _context.EscrowTransactions
            .FirstOrDefaultAsync(et => et.Id == escrowTransactionId);

        if (escrowTransaction == null)
        {
            _logger.LogWarning("Escrow transaction {EscrowTransactionId} not found", escrowTransactionId);
            return false;
        }

        if (escrowTransaction.Status == EscrowStatus.Released)
        {
            _logger.LogWarning("Cannot return escrow transaction {EscrowTransactionId} - already released to seller",
                escrowTransactionId);
            return false;
        }

        if (refundAmount <= 0)
        {
            throw new ArgumentException("Refund amount must be greater than zero.", nameof(refundAmount));
        }

        var availableAmount = escrowTransaction.GrossAmount - escrowTransaction.RefundedAmount;
        if (refundAmount > availableAmount)
        {
            throw new ArgumentException($"Refund amount ({refundAmount}) exceeds available escrow amount ({availableAmount}).", nameof(refundAmount));
        }

        escrowTransaction.RefundedAmount += refundAmount;
        escrowTransaction.UpdatedAt = DateTime.UtcNow;
        
        // Determine new status based on refund amount
        if (escrowTransaction.RefundedAmount >= escrowTransaction.GrossAmount)
        {
            // Full refund
            escrowTransaction.Status = EscrowStatus.ReturnedToBuyer;
            escrowTransaction.ReturnedToBuyerAt = DateTime.UtcNow;
        }
        else
        {
            // Partial refund
            escrowTransaction.Status = EscrowStatus.PartiallyRefunded;
        }

        if (!string.IsNullOrEmpty(notes))
        {
            escrowTransaction.Notes = notes;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Returned {RefundAmount} from escrow transaction {EscrowTransactionId} to buyer (total refunded: {TotalRefunded})",
            refundAmount, escrowTransactionId, escrowTransaction.RefundedAmount);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkEscrowEligibleForPayoutAsync(int sellerSubOrderId, int daysUntilEligible = 7)
    {
        var escrowTransaction = await GetEscrowTransactionBySubOrderAsync(sellerSubOrderId);

        if (escrowTransaction == null)
        {
            _logger.LogWarning("No escrow transaction found for sub-order {SubOrderId}", sellerSubOrderId);
            return false;
        }

        if (escrowTransaction.Status != EscrowStatus.Held)
        {
            _logger.LogInformation("Escrow transaction {EscrowTransactionId} is not in Held status (current: {Status})",
                escrowTransaction.Id, escrowTransaction.Status);
            return true; // Already processed
        }

        escrowTransaction.Status = EscrowStatus.EligibleForPayout;
        escrowTransaction.EligibleForPayoutAt = DateTime.UtcNow.AddDays(daysUntilEligible);
        escrowTransaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Marked escrow transaction {EscrowTransactionId} as eligible for payout on {EligibleDate}",
            escrowTransaction.Id, escrowTransaction.EligibleForPayoutAt);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<EscrowTransaction>> GetEscrowTransactionsByPaymentAsync(int paymentTransactionId)
    {
        return await _context.EscrowTransactions
            .Include(et => et.SellerSubOrder)
            .Include(et => et.Store)
            .Where(et => et.PaymentTransactionId == paymentTransactionId)
            .OrderBy(et => et.Id)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<EscrowTransaction>> GetEscrowTransactionsByStoreAsync(int storeId, EscrowStatus? status = null)
    {
        var query = _context.EscrowTransactions
            .Include(et => et.SellerSubOrder)
            .Include(et => et.PaymentTransaction)
            .Where(et => et.StoreId == storeId);

        if (status.HasValue)
        {
            query = query.Where(et => et.Status == status.Value);
        }

        return await query
            .OrderByDescending(et => et.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<EscrowTransaction?> GetEscrowTransactionBySubOrderAsync(int sellerSubOrderId)
    {
        return await _context.EscrowTransactions
            .Include(et => et.SellerSubOrder)
            .Include(et => et.Store)
            .Include(et => et.PaymentTransaction)
            .FirstOrDefaultAsync(et => et.SellerSubOrderId == sellerSubOrderId);
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateCommissionAsync(decimal grossAmount)
    {
        var commissionConfig = await _context.CommissionConfigs
            .Where(cc => cc.IsActive)
            .OrderByDescending(cc => cc.CreatedAt)
            .FirstOrDefaultAsync();

        if (commissionConfig == null)
        {
            _logger.LogWarning("No active commission configuration found, using 0% commission");
            return 0;
        }

        // Calculate percentage-based commission
        var percentageCommission = grossAmount * (commissionConfig.CommissionPercentage / 100);

        // Add fixed commission amount
        var totalCommission = percentageCommission + commissionConfig.FixedCommissionAmount;

        return Math.Round(totalCommission, 2);
    }

    /// <inheritdoc />
    public async Task<int> ProcessEligiblePayoutsAsync()
    {
        var now = DateTime.UtcNow;
        
        var eligibleEscrows = await _context.EscrowTransactions
            .Include(et => et.SellerSubOrder)
            .Where(et => et.Status == EscrowStatus.EligibleForPayout 
                      && et.EligibleForPayoutAt != null 
                      && et.EligibleForPayoutAt <= now)
            .ToListAsync();

        int releasedCount = 0;

        foreach (var escrow in eligibleEscrows)
        {
            var released = await ReleaseEscrowAsync(escrow.Id);
            if (released)
            {
                releasedCount++;
            }
        }

        if (releasedCount > 0)
        {
            _logger.LogInformation("Processed {Count} eligible escrow payouts", releasedCount);
        }

        return releasedCount;
    }
}
