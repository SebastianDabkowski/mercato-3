using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace MercatoApp.Services;

/// <summary>
/// Service for generating seller revenue reports with commission calculations.
/// </summary>
public class SellerRevenueReportService : ISellerRevenueReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SellerRevenueReportService> _logger;

    public SellerRevenueReportService(
        ApplicationDbContext context,
        ILogger<SellerRevenueReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(List<RevenueReportItem> Items, RevenueReportSummary Summary)> GetRevenueReportAsync(
        int storeId,
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.SellerSubOrders
            .Include(so => so.ParentOrder.User)
            .Where(so => so.StoreId == storeId);

        // Apply status filter
        if (statuses != null && statuses.Any())
        {
            query = query.Where(so => statuses.Contains(so.Status));
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            query = query.Where(so => so.CreatedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            // Include the entire day for toDate
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(so => so.CreatedAt <= endOfDay);
        }

        var subOrders = await query
            .OrderByDescending(so => so.CreatedAt)
            .ToListAsync();

        // Get escrow transactions to find commission amounts
        var subOrderIds = subOrders.Select(so => so.Id).ToList();
        var escrowTransactions = await _context.EscrowTransactions
            .Where(et => subOrderIds.Contains(et.SellerSubOrderId))
            .ToDictionaryAsync(et => et.SellerSubOrderId, et => et);

        var reportItems = new List<RevenueReportItem>();
        foreach (var subOrder in subOrders)
        {
            var buyerName = GetBuyerName(subOrder);
            var buyerEmail = GetBuyerEmail(subOrder);
            
            // Get commission from escrow transaction if it exists
            decimal commissionCharged = 0;
            if (escrowTransactions.TryGetValue(subOrder.Id, out var escrow))
            {
                commissionCharged = escrow.CommissionAmount;
            }

            var item = new RevenueReportItem
            {
                SubOrderNumber = subOrder.SubOrderNumber,
                ParentOrderNumber = subOrder.ParentOrder.OrderNumber,
                CreatedAt = subOrder.CreatedAt,
                Status = subOrder.Status,
                BuyerName = buyerName,
                BuyerEmail = buyerEmail,
                OrderValue = subOrder.TotalAmount,
                CommissionCharged = commissionCharged,
                NetAmountToSeller = subOrder.TotalAmount - commissionCharged,
                RefundedAmount = subOrder.RefundedAmount
            };

            reportItems.Add(item);
        }

        // Calculate summary
        var summary = new RevenueReportSummary
        {
            TotalOrders = reportItems.Count,
            TotalOrderValue = reportItems.Sum(i => i.OrderValue),
            TotalCommissionCharged = reportItems.Sum(i => i.CommissionCharged),
            TotalNetAmountToSeller = reportItems.Sum(i => i.NetAmountToSeller),
            TotalRefundedAmount = reportItems.Sum(i => i.RefundedAmount)
        };

        return (reportItems, summary);
    }

    /// <inheritdoc />
    public async Task<RevenueReportExportResult> ExportToCsvAsync(
        int storeId,
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var result = new RevenueReportExportResult();

        try
        {
            var (items, summary) = await GetRevenueReportAsync(storeId, statuses, fromDate, toDate);

            if (items.Count == 0)
            {
                // Generate empty CSV with headers
                var emptyCSV = new StringBuilder();
                emptyCSV.AppendLine(FormatCsvRow(
                    "Sub-Order Number",
                    "Parent Order Number",
                    "Created Date",
                    "Status",
                    "Buyer Name",
                    "Buyer Email",
                    "Order Value",
                    "Commission Charged",
                    "Net Amount to Seller",
                    "Refunded Amount"
                ));

                result.FileData = Encoding.UTF8.GetBytes(emptyCSV.ToString());
                result.FileName = $"revenue_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}_utc.csv";
                result.ContentType = "text/csv";
                result.Success = true;
                result.Errors.Add("No orders found for the selected period. Generated empty file with headers.");
                
                _logger.LogInformation("Generated empty revenue report CSV for store {StoreId}", storeId);
                return result;
            }

            var csv = new StringBuilder();

            // Header row
            csv.AppendLine(FormatCsvRow(
                "Sub-Order Number",
                "Parent Order Number",
                "Created Date",
                "Status",
                "Buyer Name",
                "Buyer Email",
                "Order Value",
                "Commission Charged",
                "Net Amount to Seller",
                "Refunded Amount"
            ));

            // Data rows
            foreach (var item in items)
            {
                csv.AppendLine(FormatCsvRow(
                    EscapeCsvValue(item.SubOrderNumber),
                    EscapeCsvValue(item.ParentOrderNumber),
                    item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    EscapeCsvValue(item.Status.ToString()),
                    EscapeCsvValue(item.BuyerName),
                    EscapeCsvValue(item.BuyerEmail),
                    item.OrderValue.ToString("F2", CultureInfo.InvariantCulture),
                    item.CommissionCharged.ToString("F2", CultureInfo.InvariantCulture),
                    item.NetAmountToSeller.ToString("F2", CultureInfo.InvariantCulture),
                    item.RefundedAmount.ToString("F2", CultureInfo.InvariantCulture)
                ));
            }

            // Add summary rows
            csv.AppendLine(); // Empty line separator
            csv.AppendLine(FormatCsvRow("SUMMARY", "", "", "", "", "", "", "", "", ""));
            csv.AppendLine(FormatCsvRow(
                "Total Orders",
                summary.TotalOrders.ToString(),
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                ""
            ));
            csv.AppendLine(FormatCsvRow(
                "Total Order Value",
                "",
                "",
                "",
                "",
                "",
                summary.TotalOrderValue.ToString("F2", CultureInfo.InvariantCulture),
                "",
                "",
                ""
            ));
            csv.AppendLine(FormatCsvRow(
                "Total Commission Charged",
                "",
                "",
                "",
                "",
                "",
                "",
                summary.TotalCommissionCharged.ToString("F2", CultureInfo.InvariantCulture),
                "",
                ""
            ));
            csv.AppendLine(FormatCsvRow(
                "Total Net Amount to Seller",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                summary.TotalNetAmountToSeller.ToString("F2", CultureInfo.InvariantCulture),
                ""
            ));
            csv.AppendLine(FormatCsvRow(
                "Total Refunded Amount",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                summary.TotalRefundedAmount.ToString("F2", CultureInfo.InvariantCulture)
            ));

            var fileName = $"revenue_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}_utc.csv";
            result.FileData = Encoding.UTF8.GetBytes(csv.ToString());
            result.FileName = fileName;
            result.ContentType = "text/csv";
            result.Success = true;

            _logger.LogInformation("Exported revenue report with {Count} orders to CSV for store {StoreId}", 
                items.Count, storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting revenue report to CSV for store {StoreId}", storeId);
            result.Errors.Add($"An error occurred while exporting: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Gets the buyer name from a seller sub-order.
    /// </summary>
    private static string GetBuyerName(SellerSubOrder subOrder)
    {
        if (subOrder.ParentOrder.User != null)
        {
            return $"{subOrder.ParentOrder.User.FirstName} {subOrder.ParentOrder.User.LastName}";
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
