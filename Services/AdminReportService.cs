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

            // Data rows
            foreach (var subOrder in subOrders)
            {
                var buyerName = GetBuyerName(subOrder);
                var buyerEmail = GetBuyerEmail(subOrder);
                var commission = await GetCommissionAmountAsync(subOrder.Id);
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
    /// Gets seller sub-orders for report with optional filters.
    /// </summary>
    private async Task<List<SellerSubOrder>> GetSubOrdersForReportAsync(
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

        return await query
            .OrderByDescending(so => so.ParentOrder.OrderedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the commission amount for a sub-order by summing up commission transactions.
    /// </summary>
    private async Task<decimal> GetCommissionAmountAsync(int subOrderId)
    {
        var commission = await _context.CommissionTransactions
            .Where(ct => ct.EscrowTransaction.SellerSubOrderId == subOrderId)
            .SumAsync(ct => ct.CommissionAmount);

        return commission;
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
}
