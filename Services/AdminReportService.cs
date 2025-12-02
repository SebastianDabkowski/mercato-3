using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace MercatoApp.Services;

/// <summary>
/// Service for generating admin reports and exports.
/// </summary>
public class AdminReportService : IAdminReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminReportService> _logger;
    private readonly IConfiguration _configuration;

    // Column headers for order/revenue report
    private const string COL_ORDER_ID = "Order ID";
    private const string COL_ORDER_NUMBER = "Order Number";
    private const string COL_ORDER_DATE = "Order Date";
    private const string COL_BUYER_NAME = "Buyer Name";
    private const string COL_BUYER_EMAIL = "Buyer Email";
    private const string COL_SELLER_STORE_NAME = "Seller Store Name";
    private const string COL_SUB_ORDER_NUMBER = "Sub-Order Number";
    private const string COL_ORDER_STATUS = "Order Status";
    private const string COL_PAYMENT_STATUS = "Payment Status";
    private const string COL_ORDER_VALUE = "Order Value";
    private const string COL_COMMISSION = "Commission";
    private const string COL_PAYOUT_AMOUNT = "Payout Amount";

    public AdminReportService(
        ApplicationDbContext context,
        ILogger<AdminReportService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<List<OrderRevenueReportData>> GetOrderReportDataAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? storeId = null,
        string? orderStatus = null,
        string? paymentStatus = null,
        int skip = 0,
        int take = 50)
    {
        var subOrders = await GetSubOrdersForReportAsync(fromDate, toDate, storeId, orderStatus, paymentStatus, skip, take);

        // Batch load commissions to avoid N+1 queries
        var subOrderIds = subOrders.Select(so => so.Id).ToList();
        var commissions = await GetCommissionAmountsAsync(subOrderIds);

        // Build report data
        var reportData = new List<OrderRevenueReportData>();
        foreach (var subOrder in subOrders)
        {
            var commission = commissions.GetValueOrDefault(subOrder.Id, 0);
            var payoutAmount = subOrder.TotalAmount - commission;

            reportData.Add(new OrderRevenueReportData
            {
                OrderId = subOrder.ParentOrderId,
                OrderNumber = subOrder.ParentOrder.OrderNumber,
                OrderDate = subOrder.ParentOrder.OrderedAt,
                BuyerName = GetBuyerName(subOrder),
                BuyerEmail = GetBuyerEmail(subOrder),
                SellerStoreName = subOrder.Store.StoreName,
                SubOrderNumber = subOrder.SubOrderNumber,
                OrderStatus = subOrder.Status.ToString(),
                PaymentStatus = subOrder.ParentOrder.PaymentStatus.ToString(),
                OrderValue = subOrder.TotalAmount,
                Commission = commission,
                PayoutAmount = payoutAmount
            });
        }

        return reportData;
    }

    /// <inheritdoc />
    public async Task<int> GetOrderReportCountAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? storeId = null,
        string? orderStatus = null,
        string? paymentStatus = null)
    {
        var query = BuildReportQuery(fromDate, toDate, storeId, orderStatus, paymentStatus);
        return await query.CountAsync();
    }

    /// <inheritdoc />
    public async Task<AdminReportExportResult> ExportOrderReportToCsvAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? storeId = null,
        string? orderStatus = null,
        string? paymentStatus = null)
    {
        var result = new AdminReportExportResult();

        try
        {
            // Get the configured threshold for large exports (default to 10000)
            var largeExportThreshold = _configuration.GetValue<int>("AdminReports:LargeExportThreshold", 10000);

            // Get sub-orders with filters
            var subOrders = await GetSubOrdersForReportAsync(fromDate, toDate, storeId, orderStatus, paymentStatus);

            if (subOrders.Count == 0)
            {
                result.Errors.Add("No orders found matching the specified filters.");
                return result;
            }

            // Check if export is too large
            if (subOrders.Count > largeExportThreshold)
            {
                result.Errors.Add($"Export contains {subOrders.Count} rows which exceeds the threshold of {largeExportThreshold}. Please apply additional filters to reduce the result set.");
                return result;
            }

            var csv = new StringBuilder();

            // Header row
            csv.AppendLine(FormatCsvRow(
                COL_ORDER_ID,
                COL_ORDER_NUMBER,
                COL_ORDER_DATE,
                COL_BUYER_NAME,
                COL_BUYER_EMAIL,
                COL_SELLER_STORE_NAME,
                COL_SUB_ORDER_NUMBER,
                COL_ORDER_STATUS,
                COL_PAYMENT_STATUS,
                COL_ORDER_VALUE,
                COL_COMMISSION,
                COL_PAYOUT_AMOUNT
            ));

            // Batch load commissions to avoid N+1 queries
            var subOrderIds = subOrders.Select(so => so.Id).ToList();
            var commissions = await GetCommissionAmountsAsync(subOrderIds);

            // Data rows
            foreach (var subOrder in subOrders)
            {
                var buyerName = GetBuyerName(subOrder);
                var buyerEmail = GetBuyerEmail(subOrder);
                var commission = commissions.GetValueOrDefault(subOrder.Id, 0);
                var payoutAmount = subOrder.TotalAmount - commission;

                csv.AppendLine(FormatCsvRow(
                    subOrder.ParentOrderId.ToString(),
                    EscapeCsvValue(subOrder.ParentOrder.OrderNumber),
                    subOrder.ParentOrder.OrderedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    EscapeCsvValue(buyerName),
                    EscapeCsvValue(buyerEmail),
                    EscapeCsvValue(subOrder.Store.StoreName),
                    EscapeCsvValue(subOrder.SubOrderNumber),
                    EscapeCsvValue(subOrder.Status.ToString()),
                    EscapeCsvValue(subOrder.ParentOrder.PaymentStatus.ToString()),
                    subOrder.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),
                    commission.ToString("F2", CultureInfo.InvariantCulture),
                    payoutAmount.ToString("F2", CultureInfo.InvariantCulture)
                ));
            }

            var fileName = $"order_revenue_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}_utc.csv";
            result.FileData = Encoding.UTF8.GetBytes(csv.ToString());
            result.FileName = fileName;
            result.ContentType = "text/csv";
            result.Success = true;

            _logger.LogInformation("Exported {Count} sub-orders to admin report CSV", subOrders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting admin order report to CSV");
            result.Errors.Add($"An error occurred while exporting: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Builds the base query for report data with filters applied.
    /// </summary>
    private IQueryable<SellerSubOrder> BuildReportQuery(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? storeId = null,
        string? orderStatus = null,
        string? paymentStatus = null)
    {
        var query = _context.SellerSubOrders
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.User)
            .Include(so => so.Store)
            .AsQueryable();

        // Apply date range filter
        if (fromDate.HasValue)
        {
            query = query.Where(so => so.ParentOrder.OrderedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            // Include the entire day for toDate
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(so => so.ParentOrder.OrderedAt <= endOfDay);
        }

        // Apply store/seller filter
        if (storeId.HasValue)
        {
            query = query.Where(so => so.StoreId == storeId.Value);
        }

        // Apply order status filter
        if (!string.IsNullOrEmpty(orderStatus) && Enum.TryParse<OrderStatus>(orderStatus, out var parsedOrderStatus))
        {
            query = query.Where(so => so.Status == parsedOrderStatus);
        }

        // Apply payment status filter
        if (!string.IsNullOrEmpty(paymentStatus) && Enum.TryParse<PaymentStatus>(paymentStatus, out var parsedPaymentStatus))
        {
            query = query.Where(so => so.ParentOrder.PaymentStatus == parsedPaymentStatus);
        }

        return query.OrderByDescending(so => so.ParentOrder.OrderedAt);
    }

    /// <summary>
    /// Gets seller sub-orders for report with optional filters and pagination.
    /// </summary>
    private async Task<List<SellerSubOrder>> GetSubOrdersForReportAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? storeId = null,
        string? orderStatus = null,
        string? paymentStatus = null,
        int skip = 0,
        int take = 0)
    {
        var query = BuildReportQuery(fromDate, toDate, storeId, orderStatus, paymentStatus);

        if (skip > 0)
        {
            query = query.Skip(skip);
        }

        if (take > 0)
        {
            query = query.Take(take);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Gets the commission amounts for multiple sub-orders in a single query.
    /// </summary>
    private async Task<Dictionary<int, decimal>> GetCommissionAmountsAsync(List<int> subOrderIds)
    {
        if (!subOrderIds.Any())
            return new Dictionary<int, decimal>();

        var commissions = await _context.CommissionTransactions
            .Include(ct => ct.EscrowTransaction)
            .Where(ct => subOrderIds.Contains(ct.EscrowTransaction.SellerSubOrderId))
            .GroupBy(ct => ct.EscrowTransaction.SellerSubOrderId)
            .Select(g => new { SubOrderId = g.Key, TotalCommission = g.Sum(ct => ct.CommissionAmount) })
            .ToListAsync();

        return commissions.ToDictionary(c => c.SubOrderId, c => c.TotalCommission);
    }

    /// <summary>
    /// Gets the buyer name from a seller sub-order.
    /// </summary>
    private static string GetBuyerName(SellerSubOrder subOrder)
    {
        if (subOrder.ParentOrder.User != null)
        {
            return $"{subOrder.ParentOrder.User.FirstName} {subOrder.ParentOrder.User.LastName}".Trim();
        }
        return "Guest";
    }

    /// <summary>
    /// Gets the buyer email from a seller sub-order.
    /// </summary>
    private static string GetBuyerEmail(SellerSubOrder subOrder)
    {
        if (subOrder.ParentOrder.User != null)
        {
            return subOrder.ParentOrder.User.Email;
        }
        if (!string.IsNullOrEmpty(subOrder.ParentOrder.GuestEmail))
        {
            return subOrder.ParentOrder.GuestEmail;
        }
        return "N/A";
    }

    /// <summary>
    /// Escapes a CSV value by wrapping it in quotes if it contains special characters.
    /// </summary>
    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // If value contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Formats a CSV row from individual values.
    /// </summary>
    private static string FormatCsvRow(params string[] values)
    {
        return string.Join(",", values);
    }

    /// <inheritdoc />
    public async Task<List<CommissionSummaryData>> GetCommissionSummaryAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        // Include the entire day for toDate
        var endOfDay = toDate.Date.AddDays(1).AddTicks(-1);

        // Get all commission transactions for the period
        var summaryData = await _context.CommissionTransactions
            .Include(ct => ct.Store)
            .Where(ct => ct.CreatedAt >= fromDate && ct.CreatedAt <= endOfDay)
            .GroupBy(ct => new { ct.StoreId, ct.Store.StoreName })
            .Select(g => new
            {
                StoreId = g.Key.StoreId,
                StoreName = g.Key.StoreName,
                TotalCommission = g.Sum(ct => ct.CommissionAmount),
                TotalGrossAmount = g.Sum(ct => ct.GrossAmount)
            })
            .ToListAsync();

        // Get order counts for all sellers in a single query to avoid N+1
        var storeIds = summaryData.Select(s => s.StoreId).ToList();
        var orderCounts = await _context.SellerSubOrders
            .Where(so => storeIds.Contains(so.StoreId)
                && so.ParentOrder.OrderedAt >= fromDate 
                && so.ParentOrder.OrderedAt <= endOfDay)
            .GroupBy(so => so.StoreId)
            .Select(g => new { StoreId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StoreId, x => x.Count);

        // Build commission summary data
        var result = new List<CommissionSummaryData>();
        foreach (var item in summaryData)
        {
            var netPayout = item.TotalGrossAmount - item.TotalCommission;
            var orderCount = orderCounts.GetValueOrDefault(item.StoreId, 0);

            result.Add(new CommissionSummaryData
            {
                StoreId = item.StoreId,
                StoreName = item.StoreName,
                TotalGMV = item.TotalGrossAmount,
                TotalCommission = item.TotalCommission,
                TotalNetPayout = netPayout,
                OrderCount = orderCount
            });
        }

        return result.OrderByDescending(r => r.TotalGMV).ToList();
    }

    /// <inheritdoc />
    public async Task<List<CommissionOrderDetail>> GetCommissionOrderDetailsAsync(
        int storeId,
        DateTime fromDate,
        DateTime toDate)
    {
        // Include the entire day for toDate
        var endOfDay = toDate.Date.AddDays(1).AddTicks(-1);

        // Get all sub-orders for this seller in the period
        var subOrders = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.User)
            .Where(so => so.StoreId == storeId 
                && so.ParentOrder.OrderedAt >= fromDate 
                && so.ParentOrder.OrderedAt <= endOfDay)
            .OrderByDescending(so => so.ParentOrder.OrderedAt)
            .ToListAsync();

        // Get commissions for these sub-orders
        var subOrderIds = subOrders.Select(so => so.Id).ToList();
        var commissions = await GetCommissionAmountsAsync(subOrderIds);

        // Build order details
        var result = new List<CommissionOrderDetail>();
        foreach (var subOrder in subOrders)
        {
            var commission = commissions.GetValueOrDefault(subOrder.Id, 0);
            var netPayout = subOrder.TotalAmount - commission;

            result.Add(new CommissionOrderDetail
            {
                OrderId = subOrder.ParentOrderId,
                OrderNumber = subOrder.ParentOrder.OrderNumber,
                SubOrderNumber = subOrder.SubOrderNumber,
                OrderDate = subOrder.ParentOrder.OrderedAt,
                BuyerName = GetBuyerName(subOrder),
                OrderValue = subOrder.TotalAmount,
                Commission = commission,
                NetPayout = netPayout,
                OrderStatus = subOrder.Status.ToString()
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<AdminReportExportResult> ExportCommissionSummaryToCsvAsync(
        DateTime fromDate,
        DateTime toDate)
    {
        var result = new AdminReportExportResult();

        try
        {
            var summaryData = await GetCommissionSummaryAsync(fromDate, toDate);

            if (summaryData.Count == 0)
            {
                result.Errors.Add("No commission data found for the specified period.");
                return result;
            }

            var csv = new StringBuilder();

            // Header row
            csv.AppendLine(FormatCsvRow(
                "Store ID",
                "Store Name",
                "Total GMV",
                "Total Commission",
                "Total Net Payout",
                "Order Count"
            ));

            // Data rows
            foreach (var summary in summaryData)
            {
                csv.AppendLine(FormatCsvRow(
                    summary.StoreId.ToString(),
                    EscapeCsvValue(summary.StoreName),
                    summary.TotalGMV.ToString("F2", CultureInfo.InvariantCulture),
                    summary.TotalCommission.ToString("F2", CultureInfo.InvariantCulture),
                    summary.TotalNetPayout.ToString("F2", CultureInfo.InvariantCulture),
                    summary.OrderCount.ToString()
                ));
            }

            // Summary totals row
            var totalGMV = summaryData.Sum(s => s.TotalGMV);
            var totalCommission = summaryData.Sum(s => s.TotalCommission);
            var totalNetPayout = summaryData.Sum(s => s.TotalNetPayout);
            var totalOrders = summaryData.Sum(s => s.OrderCount);

            csv.AppendLine();
            csv.AppendLine(FormatCsvRow(
                "",
                "TOTAL",
                totalGMV.ToString("F2", CultureInfo.InvariantCulture),
                totalCommission.ToString("F2", CultureInfo.InvariantCulture),
                totalNetPayout.ToString("F2", CultureInfo.InvariantCulture),
                totalOrders.ToString()
            ));

            var fileName = $"commission_summary_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_utc.csv";
            result.FileData = Encoding.UTF8.GetBytes(csv.ToString());
            result.FileName = fileName;
            result.ContentType = "text/csv";
            result.Success = true;

            _logger.LogInformation("Exported commission summary for {Count} sellers from {FromDate} to {ToDate}", 
                summaryData.Count, fromDate, toDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting commission summary to CSV");
            result.Errors.Add($"An error occurred while exporting: {ex.Message}");
        }

        return result;
    }
}
