using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing seller settlement reports.
/// Handles settlement generation, reporting, and export.
/// </summary>
public class SettlementService : ISettlementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettlementService> _logger;

    public SettlementService(
        ApplicationDbContext context,
        ILogger<SettlementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SettlementResult> GenerateSettlementAsync(int storeId, DateTime periodStartDate, DateTime periodEndDate)
    {
        var result = new SettlementResult();

        // Validate store exists
        var store = await _context.Stores.FindAsync(storeId);
        if (store == null)
        {
            result.Errors.Add($"Store with ID {storeId} not found.");
            return result;
        }

        // Validate dates
        if (periodEndDate <= periodStartDate)
        {
            result.Errors.Add("Period end date must be after start date.");
            return result;
        }

        // Check if a current settlement already exists for this period
        var existingSettlement = await _context.Settlements
            .Where(s => s.StoreId == storeId
                && s.PeriodStartDate == periodStartDate
                && s.PeriodEndDate == periodEndDate
                && s.IsCurrentVersion)
            .FirstOrDefaultAsync();

        if (existingSettlement != null)
        {
            result.Errors.Add($"A settlement already exists for this period (ID: {existingSettlement.Id}).");
            return result;
        }

        var now = DateTime.UtcNow;

        // Get all orders placed during the period
        var orders = await _context.Orders
            .Include(o => o.SubOrders)
            .Include(o => o.Items)
            .Where(o => o.OrderedAt >= periodStartDate && o.OrderedAt <= periodEndDate)
            .ToListAsync();

        // Filter to only this seller's sub-orders
        var sellerSubOrders = orders
            .SelectMany(o => o.SubOrders.Where(so => so.StoreId == storeId))
            .ToList();

        // Get escrow transactions for these sub-orders
        var sellerSubOrderIds = sellerSubOrders.Select(so => so.Id).ToList();
        var escrowTransactions = await _context.EscrowTransactions
            .Where(e => sellerSubOrderIds.Contains(e.SellerSubOrderId))
            .ToListAsync();

        // Calculate totals
        decimal grossSales = 0;
        decimal refunds = 0;
        decimal commission = 0;

        foreach (var escrow in escrowTransactions)
        {
            grossSales += escrow.GrossAmount;
            refunds += escrow.RefundedAmount;
            commission += escrow.CommissionAmount;
        }

        decimal netAmount = grossSales - refunds - commission;

        // Get payouts made during this period
        var payouts = await _context.Payouts
            .Where(p => p.StoreId == storeId
                && p.Status == PayoutStatus.Paid
                && p.CompletedAt >= periodStartDate
                && p.CompletedAt <= periodEndDate)
            .ToListAsync();

        decimal totalPayouts = payouts.Sum(p => p.Amount);

        // Create the settlement
        var settlement = new Settlement
        {
            SettlementNumber = GenerateSettlementNumber(storeId, periodStartDate),
            StoreId = storeId,
            PeriodStartDate = periodStartDate,
            PeriodEndDate = periodEndDate,
            GrossSales = grossSales,
            Refunds = refunds,
            Commission = commission,
            Adjustments = 0, // Will be updated when adjustments are added
            NetAmount = netAmount,
            TotalPayouts = totalPayouts,
            Status = SettlementStatus.Draft,
            GeneratedAt = now,
            Version = 1,
            IsCurrentVersion = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Settlements.Add(settlement);
        await _context.SaveChangesAsync();

        // Create settlement items for each order
        foreach (var subOrder in sellerSubOrders)
        {
            var order = orders.First(o => o.SubOrders.Any(so => so.Id == subOrder.Id));
            var escrow = escrowTransactions.FirstOrDefault(e => e.SellerSubOrderId == subOrder.Id);

            var item = new SettlementItem
            {
                SettlementId = settlement.Id,
                OrderId = order.Id,
                SellerSubOrderId = subOrder.Id,
                EscrowTransactionId = escrow?.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderedAt,
                GrossAmount = escrow?.GrossAmount ?? 0,
                RefundAmount = escrow?.RefundedAmount ?? 0,
                CommissionAmount = escrow?.CommissionAmount ?? 0,
                NetAmount = (escrow?.NetAmount ?? 0) - (escrow?.RefundedAmount ?? 0),
                CreatedAt = now
            };

            _context.SettlementItems.Add(item);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Generated settlement {SettlementNumber} for store {StoreId}: Period {Start} to {End}, {OrderCount} orders, Net: {NetAmount:C}",
            settlement.SettlementNumber,
            storeId,
            periodStartDate,
            periodEndDate,
            sellerSubOrders.Count,
            netAmount);

        result.Success = true;
        result.Settlement = settlement;
        return result;
    }

    /// <inheritdoc />
    public async Task<int> GenerateMonthlySettlementsAsync(int year, int month)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentException("Month must be between 1 and 12.", nameof(month));
        }

        // Get all active stores
        var stores = await _context.Stores
            .Where(s => s.Status == StoreStatus.Active || s.Status == StoreStatus.LimitedActive)
            .ToListAsync();

        // Calculate period dates
        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddSeconds(-1);

        var generatedCount = 0;

        foreach (var store in stores)
        {
            try
            {
                var result = await GenerateSettlementAsync(store.Id, periodStart, periodEnd);
                if (result.Success)
                {
                    generatedCount++;
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to generate settlement for store {StoreId}: {Errors}",
                        store.Id,
                        string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error generating settlement for store {StoreId}",
                    store.Id);
            }
        }

        _logger.LogInformation(
            "Generated {GeneratedCount} settlements for {Year}-{Month} from {TotalStores} stores",
            generatedCount,
            year,
            month,
            stores.Count);

        return generatedCount;
    }

    /// <inheritdoc />
    public async Task<SettlementResult> RegenerateSettlementAsync(int settlementId)
    {
        var result = new SettlementResult();

        // Get the existing settlement
        var existingSettlement = await _context.Settlements
            .Include(s => s.SettlementAdjustments)
            .FirstOrDefaultAsync(s => s.Id == settlementId);

        if (existingSettlement == null)
        {
            result.Errors.Add($"Settlement with ID {settlementId} not found.");
            return result;
        }

        // Cannot regenerate a finalized settlement
        if (existingSettlement.Status == SettlementStatus.Finalized)
        {
            result.Errors.Add("Cannot regenerate a finalized settlement.");
            return result;
        }

        // Mark existing settlement as superseded
        existingSettlement.Status = SettlementStatus.Superseded;
        existingSettlement.IsCurrentVersion = false;
        existingSettlement.UpdatedAt = DateTime.UtcNow;

        // Generate new settlement for the same period
        var generateResult = await GenerateSettlementAsync(
            existingSettlement.StoreId,
            existingSettlement.PeriodStartDate,
            existingSettlement.PeriodEndDate);

        if (!generateResult.Success)
        {
            // Revert the superseded status if generation failed
            existingSettlement.Status = SettlementStatus.Draft;
            existingSettlement.IsCurrentVersion = true;
            await _context.SaveChangesAsync();
            return generateResult;
        }

        // Link to previous settlement
        var newSettlement = generateResult.Settlement!;
        newSettlement.PreviousSettlementId = existingSettlement.Id;
        newSettlement.Version = existingSettlement.Version + 1;

        // Copy adjustments from previous settlement
        foreach (var adj in existingSettlement.SettlementAdjustments)
        {
            var newAdj = new SettlementAdjustment
            {
                SettlementId = newSettlement.Id,
                Type = adj.Type,
                Amount = adj.Amount,
                Description = adj.Description + " (Copied from previous version)",
                RelatedSettlementId = adj.RelatedSettlementId,
                IsPriorPeriodAdjustment = adj.IsPriorPeriodAdjustment,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = adj.CreatedByUserId
            };
            _context.SettlementAdjustments.Add(newAdj);
        }

        // Recalculate totals with adjustments
        newSettlement.Adjustments = existingSettlement.Adjustments;
        newSettlement.NetAmount = newSettlement.GrossSales - newSettlement.Refunds - newSettlement.Commission + newSettlement.Adjustments;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Regenerated settlement {SettlementNumber} (v{Version}), superseding settlement {OldId}",
            newSettlement.SettlementNumber,
            newSettlement.Version,
            settlementId);

        result.Success = true;
        result.Settlement = newSettlement;
        return result;
    }

    /// <inheritdoc />
    public async Task<Settlement?> GetSettlementAsync(int settlementId)
    {
        return await _context.Settlements
            .Include(s => s.Store)
            .Include(s => s.Items)
                .ThenInclude(i => i.Order)
            .Include(s => s.SettlementAdjustments)
                .ThenInclude(a => a.RelatedSettlement)
            .Include(s => s.PreviousSettlement)
            .FirstOrDefaultAsync(s => s.Id == settlementId);
    }

    /// <inheritdoc />
    public async Task<List<Settlement>> GetSettlementsAsync(int storeId, bool includeSuperseded = false)
    {
        var query = _context.Settlements
            .Include(s => s.Store)
            .Include(s => s.SettlementAdjustments)
            .AsQueryable();

        // If storeId is 0, get all settlements
        if (storeId > 0)
        {
            query = query.Where(s => s.StoreId == storeId);
        }

        if (!includeSuperseded)
        {
            query = query.Where(s => s.IsCurrentVersion);
        }

        return await query
            .OrderByDescending(s => s.PeriodStartDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SettlementSummary> GetSettlementSummaryAsync(int storeId, DateTime periodStartDate, DateTime periodEndDate)
    {
        // Get all orders placed during the period
        var orders = await _context.Orders
            .Include(o => o.SubOrders)
            .Where(o => o.OrderedAt >= periodStartDate && o.OrderedAt <= periodEndDate)
            .ToListAsync();

        // Filter to only this seller's sub-orders
        var sellerSubOrders = orders
            .SelectMany(o => o.SubOrders.Where(so => so.StoreId == storeId))
            .ToList();

        // Get escrow transactions for these sub-orders
        var sellerSubOrderIds = sellerSubOrders.Select(so => so.Id).ToList();
        var escrowTransactions = await _context.EscrowTransactions
            .Where(e => sellerSubOrderIds.Contains(e.SellerSubOrderId))
            .ToListAsync();

        // Calculate totals
        decimal grossSales = escrowTransactions.Sum(e => e.GrossAmount);
        decimal refunds = escrowTransactions.Sum(e => e.RefundedAmount);
        decimal commission = escrowTransactions.Sum(e => e.CommissionAmount);
        decimal netAmount = grossSales - refunds - commission;

        // Check for existing settlement
        var existingSettlement = await _context.Settlements
            .Where(s => s.StoreId == storeId
                && s.PeriodStartDate == periodStartDate
                && s.PeriodEndDate == periodEndDate
                && s.IsCurrentVersion)
            .FirstOrDefaultAsync();

        return new SettlementSummary
        {
            StoreId = storeId,
            PeriodStartDate = periodStartDate,
            PeriodEndDate = periodEndDate,
            OrderCount = sellerSubOrders.Count,
            GrossSales = grossSales,
            Refunds = refunds,
            Commission = commission,
            NetAmount = netAmount,
            HasExistingSettlement = existingSettlement != null,
            ExistingSettlementId = existingSettlement?.Id
        };
    }

    /// <inheritdoc />
    public async Task<bool> FinalizeSettlementAsync(int settlementId)
    {
        var settlement = await _context.Settlements.FindAsync(settlementId);
        if (settlement == null)
        {
            return false;
        }

        if (settlement.Status == SettlementStatus.Finalized)
        {
            return true; // Already finalized
        }

        settlement.Status = SettlementStatus.Finalized;
        settlement.FinalizedAt = DateTime.UtcNow;
        settlement.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Finalized settlement {SettlementNumber} (ID: {SettlementId})",
            settlement.SettlementNumber,
            settlementId);

        return true;
    }

    /// <inheritdoc />
    public async Task<SettlementAdjustment> AddAdjustmentAsync(
        int settlementId,
        SettlementAdjustmentType type,
        decimal amount,
        string description,
        int? relatedSettlementId = null,
        int? createdByUserId = null)
    {
        var settlement = await _context.Settlements.FindAsync(settlementId);
        if (settlement == null)
        {
            throw new InvalidOperationException($"Settlement with ID {settlementId} not found.");
        }

        if (settlement.Status == SettlementStatus.Finalized)
        {
            throw new InvalidOperationException("Cannot add adjustment to a finalized settlement.");
        }

        var adjustment = new SettlementAdjustment
        {
            SettlementId = settlementId,
            Type = type,
            Amount = amount,
            Description = description,
            RelatedSettlementId = relatedSettlementId,
            IsPriorPeriodAdjustment = type == SettlementAdjustmentType.PriorPeriodAdjustment,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        _context.SettlementAdjustments.Add(adjustment);

        // Update settlement totals
        settlement.Adjustments += amount;
        settlement.NetAmount = settlement.GrossSales - settlement.Refunds - settlement.Commission + settlement.Adjustments;
        settlement.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Added {Type} adjustment of {Amount:C} to settlement {SettlementNumber}",
            type,
            amount,
            settlement.SettlementNumber);

        return adjustment;
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportSettlementToCsvAsync(int settlementId)
    {
        var settlement = await GetSettlementAsync(settlementId);
        if (settlement == null)
        {
            throw new InvalidOperationException($"Settlement with ID {settlementId} not found.");
        }

        var sb = new StringBuilder();

        // Header information
        sb.AppendLine($"Settlement Report");
        sb.AppendLine($"Settlement Number,{settlement.SettlementNumber}");
        sb.AppendLine($"Store,{settlement.Store.StoreName}");
        sb.AppendLine($"Period,{settlement.PeriodStartDate:yyyy-MM-dd} to {settlement.PeriodEndDate:yyyy-MM-dd}");
        sb.AppendLine($"Generated,{settlement.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Version,{settlement.Version}");
        sb.AppendLine($"Status,{settlement.Status}");
        sb.AppendLine();

        // Summary
        sb.AppendLine($"Summary");
        sb.AppendLine($"Gross Sales,{settlement.GrossSales:F2}");
        sb.AppendLine($"Refunds,{settlement.Refunds:F2}");
        sb.AppendLine($"Commission,{settlement.Commission:F2}");
        sb.AppendLine($"Adjustments,{settlement.Adjustments:F2}");
        sb.AppendLine($"Net Amount,{settlement.NetAmount:F2}");
        sb.AppendLine($"Total Payouts,{settlement.TotalPayouts:F2}");
        sb.AppendLine();

        // Items header
        sb.AppendLine($"Order Details");
        sb.AppendLine($"Order Number,Order Date,Gross Amount,Refund Amount,Commission Amount,Net Amount");

        // Items
        foreach (var item in settlement.Items.OrderBy(i => i.OrderDate))
        {
            sb.AppendLine($"{item.OrderNumber},{item.OrderDate:yyyy-MM-dd},{item.GrossAmount:F2},{item.RefundAmount:F2},{item.CommissionAmount:F2},{item.NetAmount:F2}");
        }

        sb.AppendLine();

        // Adjustments
        if (settlement.SettlementAdjustments.Any())
        {
            sb.AppendLine($"Adjustments");
            sb.AppendLine($"Type,Amount,Description,Prior Period");

            foreach (var adj in settlement.SettlementAdjustments.OrderBy(a => a.CreatedAt))
            {
                sb.AppendLine($"{adj.Type},{adj.Amount:F2},\"{adj.Description}\",{adj.IsPriorPeriodAdjustment}");
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <inheritdoc />
    public async Task<SettlementConfig> GetOrCreateSettlementConfigAsync()
    {
        var config = await _context.SettlementConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            var now = DateTime.UtcNow;
            config = new SettlementConfig
            {
                GenerationDayOfMonth = 1,
                AutoGenerateEnabled = true,
                GracePeriodDays = 0,
                UseCalendarMonth = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.SettlementConfigs.Add(config);
            await _context.SaveChangesAsync();
        }
        return config;
    }

    /// <inheritdoc />
    public async Task<SettlementConfig> UpdateSettlementConfigAsync(SettlementConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        _context.SettlementConfigs.Update(config);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated settlement configuration: GenerationDay={Day}, AutoGenerate={Auto}",
            config.GenerationDayOfMonth,
            config.AutoGenerateEnabled);

        return config;
    }

    /// <summary>
    /// Generates a unique settlement number.
    /// </summary>
    private static string GenerateSettlementNumber(int storeId, DateTime periodStart)
    {
        var yearMonth = periodStart.ToString("yyyyMM");
        return $"STL-{storeId:D6}-{yearMonth}";
    }
}
