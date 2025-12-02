using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for commission calculation and management.
/// Supports global, seller-specific, and category-specific commission rules.
/// </summary>
public class CommissionService : ICommissionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CommissionService> _logger;

    /// <summary>
    /// Percentage divisor for commission calculations.
    /// </summary>
    private const decimal PercentageDivisor = 100m;

    public CommissionService(
        ApplicationDbContext context,
        ILogger<CommissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(decimal CommissionAmount, decimal Percentage, decimal FixedAmount, string Source, int? CategoryId)> CalculateCommissionAsync(
        decimal grossAmount,
        int storeId,
        int? categoryId = null)
    {
        decimal percentage = 0;
        decimal fixedAmount = 0;
        string source = CommissionSource.Global;
        int? appliedCategoryId = null;

        // Priority 1: Category-specific commission (if category provided)
        if (categoryId.HasValue)
        {
            var category = await _context.Categories
                .Where(c => c.Id == categoryId.Value)
                .FirstOrDefaultAsync();

            if (category?.CommissionPercentageOverride.HasValue == true ||
                category?.FixedCommissionAmountOverride.HasValue == true)
            {
                percentage = category.CommissionPercentageOverride ?? 0;
                fixedAmount = category.FixedCommissionAmountOverride ?? 0;
                source = CommissionSource.Category;
                appliedCategoryId = categoryId.Value;

                _logger.LogDebug("Using category-specific commission for category {CategoryId}: {Percentage}% + {FixedAmount}",
                    categoryId.Value, percentage, fixedAmount);
            }
        }

        // Priority 2: Seller-specific commission (if no category override)
        if (source == CommissionSource.Global)
        {
            var store = await _context.Stores
                .Where(s => s.Id == storeId)
                .FirstOrDefaultAsync();

            if (store?.CommissionPercentageOverride.HasValue == true ||
                store?.FixedCommissionAmountOverride.HasValue == true)
            {
                percentage = store.CommissionPercentageOverride ?? 0;
                fixedAmount = store.FixedCommissionAmountOverride ?? 0;
                source = CommissionSource.Seller;

                _logger.LogDebug("Using seller-specific commission for store {StoreId}: {Percentage}% + {FixedAmount}",
                    storeId, percentage, fixedAmount);
            }
        }

        // Priority 3: Global platform commission (if no overrides)
        if (source == CommissionSource.Global)
        {
            var commissionConfig = await _context.CommissionConfigs
                .Where(cc => cc.IsActive)
                .OrderByDescending(cc => cc.CreatedAt)
                .FirstOrDefaultAsync();

            if (commissionConfig != null)
            {
                percentage = commissionConfig.CommissionPercentage;
                fixedAmount = commissionConfig.FixedCommissionAmount;

                _logger.LogDebug("Using global commission configuration: {Percentage}% + {FixedAmount}",
                    percentage, fixedAmount);
            }
            else
            {
                _logger.LogWarning("No active commission configuration found for store {StoreId}, using 0% commission", storeId);
            }
        }

        // Calculate total commission
        var percentageCommission = grossAmount * (percentage / PercentageDivisor);
        var totalCommission = percentageCommission + fixedAmount;
        var roundedCommission = Math.Round(totalCommission, 2);

        _logger.LogInformation("Calculated commission for store {StoreId}: {Commission} (source: {Source}, percentage: {Percentage}%, fixed: {FixedAmount})",
            storeId, roundedCommission, source, percentage, fixedAmount);

        return (roundedCommission, percentage, fixedAmount, source, appliedCategoryId);
    }

    /// <inheritdoc />
    public async Task<CommissionTransaction> RecordCommissionTransactionAsync(
        int escrowTransactionId,
        int storeId,
        int? categoryId,
        string transactionType,
        decimal grossAmount,
        decimal commissionAmount,
        decimal percentage,
        decimal fixedAmount,
        string source,
        string? notes = null)
    {
        // Validate transaction type
        if (transactionType != CommissionTransactionType.Initial && 
            transactionType != CommissionTransactionType.RefundAdjustment)
        {
            throw new ArgumentException($"Invalid transaction type: {transactionType}. Must be one of: {CommissionTransactionType.Initial}, {CommissionTransactionType.RefundAdjustment}", nameof(transactionType));
        }

        // Validate commission source
        if (source != CommissionSource.Global && 
            source != CommissionSource.Seller && 
            source != CommissionSource.Category)
        {
            throw new ArgumentException($"Invalid commission source: {source}. Must be one of: {CommissionSource.Global}, {CommissionSource.Seller}, {CommissionSource.Category}", nameof(source));
        }

        var commissionTransaction = new CommissionTransaction
        {
            EscrowTransactionId = escrowTransactionId,
            StoreId = storeId,
            CategoryId = categoryId,
            TransactionType = transactionType,
            GrossAmount = grossAmount,
            CommissionPercentage = percentage,
            FixedCommissionAmount = fixedAmount,
            CommissionAmount = commissionAmount,
            CommissionSource = source,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.CommissionTransactions.Add(commissionTransaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Recorded commission transaction {CommissionTransactionId} for escrow {EscrowTransactionId}: {Amount} ({Type})",
            commissionTransaction.Id, escrowTransactionId, commissionAmount, transactionType);

        return commissionTransaction;
    }

    /// <inheritdoc />
    public async Task<List<CommissionTransaction>> GetCommissionTransactionsByStoreAsync(
        int storeId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.CommissionTransactions
            .Include(ct => ct.EscrowTransaction)
            .Include(ct => ct.Category)
            .Where(ct => ct.StoreId == storeId);

        if (fromDate.HasValue)
        {
            query = query.Where(ct => ct.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(ct => ct.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(ct => ct.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<CommissionTransaction>> GetCommissionTransactionsByEscrowAsync(int escrowTransactionId)
    {
        return await _context.CommissionTransactions
            .Include(ct => ct.Category)
            .Where(ct => ct.EscrowTransactionId == escrowTransactionId)
            .OrderBy(ct => ct.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalCommissionAsync(DateTime fromDate, DateTime toDate)
    {
        var total = await _context.CommissionTransactions
            .Where(ct => ct.CreatedAt >= fromDate && ct.CreatedAt <= toDate)
            .SumAsync(ct => ct.CommissionAmount);

        return total;
    }

    /// <inheritdoc />
    public async Task<decimal> RecalculateCommissionForRefundAsync(
        int escrowTransactionId,
        decimal refundAmount,
        decimal originalCommissionAmount)
    {
        var escrowTransaction = await _context.EscrowTransactions
            .Include(et => et.SellerSubOrder)
            .FirstOrDefaultAsync(et => et.Id == escrowTransactionId);

        if (escrowTransaction == null)
        {
            throw new InvalidOperationException($"Escrow transaction {escrowTransactionId} not found.");
        }

        // Get the original commission transaction to determine the rules used
        var originalCommission = await _context.CommissionTransactions
            .Where(ct => ct.EscrowTransactionId == escrowTransactionId 
                      && ct.TransactionType == CommissionTransactionType.Initial)
            .OrderByDescending(ct => ct.CreatedAt)
            .FirstOrDefaultAsync();

        if (originalCommission == null)
        {
            _logger.LogWarning("No original commission transaction found for escrow {EscrowTransactionId}, cannot recalculate", escrowTransactionId);
            return 0;
        }

        // Guard against division by zero or negative amounts
        if (originalCommission.GrossAmount <= 0)
        {
            _logger.LogWarning("Original gross amount is zero or negative ({GrossAmount}) for escrow {EscrowTransactionId}, cannot calculate refund ratio", 
                originalCommission.GrossAmount, escrowTransactionId);
            return 0;
        }

        // Calculate the refund ratio
        var refundRatio = refundAmount / originalCommission.GrossAmount;

        // Calculate proportional commission to refund
        var commissionRefund = Math.Round(originalCommissionAmount * refundRatio, 2);

        // Record the refund adjustment as a negative commission transaction
        await RecordCommissionTransactionAsync(
            escrowTransactionId,
            escrowTransaction.StoreId,
            originalCommission.CategoryId,
            CommissionTransactionType.RefundAdjustment,
            refundAmount,
            -commissionRefund, // Negative for refund
            originalCommission.CommissionPercentage,
            0, // Fixed amount already factored in the ratio calculation
            originalCommission.CommissionSource,
            $"Partial refund: {refundAmount:C} of {originalCommission.GrossAmount:C}");

        _logger.LogInformation("Recalculated commission for refund on escrow {EscrowTransactionId}: -{CommissionRefund} (refund: {RefundAmount}, ratio: {RefundRatio:P})",
            escrowTransactionId, commissionRefund, refundAmount, refundRatio);

        return -commissionRefund;
    }
}
