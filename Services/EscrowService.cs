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
    private readonly ICommissionService _commissionService;
    private readonly ILogger<EscrowService> _logger;

    /// <summary>
    /// Default number of days after delivery before funds are eligible for payout.
    /// </summary>
    private const int DefaultPayoutEligibilityDays = 7;

    /// <summary>
    /// Tolerance for decimal currency comparisons.
    /// </summary>
    private const decimal CurrencyTolerance = 0.01m;

    public EscrowService(
        ApplicationDbContext context,
        ICommissionService commissionService,
        ILogger<EscrowService> logger)
    {
        _context = context;
        _commissionService = commissionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<EscrowTransaction>> CreateEscrowAllocationsAsync(int paymentTransactionId)
    {
        var paymentTransaction = await _context.PaymentTransactions
            .Include(pt => pt.Order)
                .ThenInclude(o => o.SubOrders)
                    .ThenInclude(so => so.Items)
                        .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(pt => pt.Id == paymentTransactionId);

        if (paymentTransaction == null)
        {
            throw new InvalidOperationException("Payment transaction not found.");
        }

        if (paymentTransaction.Status != PaymentStatus.Completed && paymentTransaction.Status != PaymentStatus.Authorized)
        {
            throw new InvalidOperationException("Payment must be completed or authorized before creating escrow allocations.");
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
        var commissionDetails = new List<(EscrowTransaction Escrow, int? CategoryId, decimal Percentage, decimal FixedAmount, string Source)>();

        // Create escrow allocations for each seller sub-order
        foreach (var subOrder in paymentTransaction.Order.SubOrders)
        {
            var grossAmount = subOrder.TotalAmount;

            // Determine the category ID for commission calculation
            // LIMITATION: Uses the category from the first item in the sub-order.
            // If a sub-order contains items from multiple categories with different commission rates,
            // only the first item's category commission will be applied to the entire sub-order.
            // Future enhancement: Consider calculating commission per item and aggregating.
            int? categoryId = subOrder.Items.FirstOrDefault()?.Product?.CategoryId;

            // Calculate commission using the new commission service
            var (commissionAmount, percentage, fixedAmount, source, appliedCategoryId) = 
                await _commissionService.CalculateCommissionAsync(grossAmount, subOrder.StoreId, categoryId);

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

            // Store commission details for later recording
            commissionDetails.Add((escrowTransaction, appliedCategoryId, percentage, fixedAmount, source));
        }

        // Save all escrow transactions in a single batch
        await _context.SaveChangesAsync();

        // Batch record commission transactions for audit trail
        var commissionTransactions = new List<CommissionTransaction>();
        foreach (var (escrow, categoryId, percentage, fixedAmount, source) in commissionDetails)
        {
            commissionTransactions.Add(new CommissionTransaction
            {
                EscrowTransactionId = escrow.Id,
                StoreId = escrow.StoreId,
                CategoryId = categoryId,
                TransactionType = CommissionTransactionType.Initial,
                GrossAmount = escrow.GrossAmount,
                CommissionPercentage = percentage,
                FixedCommissionAmount = fixedAmount,
                CommissionAmount = escrow.CommissionAmount,
                CommissionSource = source,
                Notes = $"Initial commission for sub-order {escrow.SellerSubOrderId}",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Save all commission transactions in a single batch
        _context.CommissionTransactions.AddRange(commissionTransactions);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created {Count} escrow allocations with {CommissionCount} commission transactions for payment transaction {PaymentTransactionId}",
            escrowTransactions.Count, commissionTransactions.Count, paymentTransactionId);

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

        // Recalculate commission for the refund
        var commissionAdjustment = await _commissionService.RecalculateCommissionForRefundAsync(
            escrowTransactionId,
            refundAmount,
            escrowTransaction.CommissionAmount);

        // Update escrow transaction
        escrowTransaction.RefundedAmount += refundAmount;
        escrowTransaction.CommissionAmount += commissionAdjustment; // Adjustment is negative for refunds
        escrowTransaction.NetAmount = escrowTransaction.GrossAmount - escrowTransaction.RefundedAmount - escrowTransaction.CommissionAmount;
        escrowTransaction.UpdatedAt = DateTime.UtcNow;
        
        // Determine new status based on refund amount (use tolerance for decimal comparison)
        var remainingAmount = escrowTransaction.GrossAmount - escrowTransaction.RefundedAmount;
        if (remainingAmount <= CurrencyTolerance)
        {
            // Full refund (or close enough to avoid rounding errors)
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

        _logger.LogInformation("Returned {RefundAmount} from escrow transaction {EscrowTransactionId} to buyer (total refunded: {TotalRefunded}, commission adjusted: {CommissionAdjustment})",
            refundAmount, escrowTransactionId, escrowTransaction.RefundedAmount, commissionAdjustment);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkEscrowEligibleForPayoutAsync(int sellerSubOrderId, int daysUntilEligible = DefaultPayoutEligibilityDays)
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
