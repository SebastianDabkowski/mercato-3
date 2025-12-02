using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Text;

namespace MercatoApp.Services;

/// <summary>
/// Result of generating an order export file.
/// </summary>
public class OrderExportResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public byte[]? FileData { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}

/// <summary>
/// Interface for order export service.
/// </summary>
public interface IOrderExportService
{
    /// <summary>
    /// Exports seller sub-orders to CSV format.
    /// </summary>
    /// <param name="storeId">The store ID to export orders from.</param>
    /// <param name="statuses">Optional filter by statuses.</param>
    /// <param name="fromDate">Optional filter by minimum date.</param>
    /// <param name="toDate">Optional filter by maximum date.</param>
    /// <param name="buyerEmail">Optional filter by buyer email.</param>
    Task<OrderExportResult> ExportToCsvAsync(
        int storeId,
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? buyerEmail = null);

    /// <summary>
    /// Exports seller sub-orders to Excel format.
    /// </summary>
    /// <param name="storeId">The store ID to export orders from.</param>
    /// <param name="statuses">Optional filter by statuses.</param>
    /// <param name="fromDate">Optional filter by minimum date.</param>
    /// <param name="toDate">Optional filter by maximum date.</param>
    /// <param name="buyerEmail">Optional filter by buyer email.</param>
    Task<OrderExportResult> ExportToExcelAsync(
        int storeId,
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? buyerEmail = null);
}

/// <summary>
/// Service for exporting seller orders to CSV/Excel files.
/// </summary>
public class OrderExportService : IOrderExportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderExportService> _logger;

    // Column headers for export
    private const string COL_SUB_ORDER_NUMBER = "Sub-Order Number";
    private const string COL_PARENT_ORDER_NUMBER = "Parent Order Number";
    private const string COL_CREATED_DATE = "Created Date";
    private const string COL_STATUS = "Status";
    private const string COL_BUYER_NAME = "Buyer Name";
    private const string COL_BUYER_EMAIL = "Buyer Email";
    private const string COL_BUYER_PHONE = "Buyer Phone";
    private const string COL_ADDRESS_LINE1 = "Address Line 1";
    private const string COL_ADDRESS_LINE2 = "Address Line 2";
    private const string COL_CITY = "City";
    private const string COL_STATE_PROVINCE = "State/Province";
    private const string COL_POSTAL_CODE = "Postal Code";
    private const string COL_COUNTRY_CODE = "Country Code";
    private const string COL_DELIVERY_INSTRUCTIONS = "Delivery Instructions";
    private const string COL_TOTAL_AMOUNT = "Total Amount";
    private const string COL_SHIPPING_COST = "Shipping Cost";
    private const string COL_SUBTOTAL = "Subtotal";
    private const string COL_SHIPPING_METHOD = "Shipping Method";
    private const string COL_TRACKING_NUMBER = "Tracking Number";
    private const string COL_CARRIER_NAME = "Carrier Name";
    private const string COL_ITEMS_COUNT = "Items Count";
    private const string COL_ITEMS_DETAILS = "Items Details";

    public OrderExportService(
        ApplicationDbContext context,
        ILogger<OrderExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OrderExportResult> ExportToCsvAsync(
        int storeId,
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? buyerEmail = null)
    {
        var result = new OrderExportResult();

        try
        {
            var subOrders = await GetSubOrdersForExportAsync(storeId, statuses, fromDate, toDate, buyerEmail);

            if (subOrders.Count == 0)
            {
                result.Errors.Add("No orders found to export.");
                return result;
            }

            var csv = new StringBuilder();

            // Header row
            csv.AppendLine(FormatCsvRow(
                COL_SUB_ORDER_NUMBER,
                COL_PARENT_ORDER_NUMBER,
                COL_CREATED_DATE,
                COL_STATUS,
                COL_BUYER_NAME,
                COL_BUYER_EMAIL,
                COL_BUYER_PHONE,
                COL_ADDRESS_LINE1,
                COL_ADDRESS_LINE2,
                COL_CITY,
                COL_STATE_PROVINCE,
                COL_POSTAL_CODE,
                COL_COUNTRY_CODE,
                COL_DELIVERY_INSTRUCTIONS,
                COL_TOTAL_AMOUNT,
                COL_SHIPPING_COST,
                COL_SUBTOTAL,
                COL_SHIPPING_METHOD,
                COL_TRACKING_NUMBER,
                COL_CARRIER_NAME,
                COL_ITEMS_COUNT,
                COL_ITEMS_DETAILS
            ));

            // Data rows
            foreach (var subOrder in subOrders)
            {
                var buyerName = GetBuyerName(subOrder);
                var buyerEmailValue = GetBuyerEmail(subOrder);
                var buyerPhone = GetBuyerPhone(subOrder);
                var address = subOrder.ParentOrder.DeliveryAddress;
                var shippingMethod = subOrder.ShippingMethod?.Name ?? "N/A";
                var itemsDetails = GetItemsDetails(subOrder);

                csv.AppendLine(FormatCsvRow(
                    EscapeCsvValue(subOrder.SubOrderNumber),
                    EscapeCsvValue(subOrder.ParentOrder.OrderNumber),
                    subOrder.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    EscapeCsvValue(subOrder.Status.ToString()),
                    EscapeCsvValue(buyerName),
                    EscapeCsvValue(buyerEmailValue),
                    EscapeCsvValue(buyerPhone),
                    EscapeCsvValue(address.AddressLine1),
                    EscapeCsvValue(address.AddressLine2 ?? string.Empty),
                    EscapeCsvValue(address.City),
                    EscapeCsvValue(address.StateProvince ?? string.Empty),
                    EscapeCsvValue(address.PostalCode),
                    EscapeCsvValue(address.CountryCode),
                    EscapeCsvValue(address.DeliveryInstructions ?? string.Empty),
                    subOrder.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),
                    subOrder.ShippingCost.ToString("F2", CultureInfo.InvariantCulture),
                    subOrder.Subtotal.ToString("F2", CultureInfo.InvariantCulture),
                    EscapeCsvValue(shippingMethod),
                    EscapeCsvValue(subOrder.TrackingNumber ?? string.Empty),
                    EscapeCsvValue(subOrder.CarrierName ?? string.Empty),
                    subOrder.Items.Count.ToString(),
                    EscapeCsvValue(itemsDetails)
                ));
            }

            var fileName = $"orders_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}_utc.csv";
            result.FileData = Encoding.UTF8.GetBytes(csv.ToString());
            result.FileName = fileName;
            result.ContentType = "text/csv";
            result.Success = true;

            _logger.LogInformation("Exported {Count} sub-orders to CSV for store {StoreId}", subOrders.Count, storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting orders to CSV for store {StoreId}", storeId);
            result.Errors.Add($"An error occurred while exporting: {ex.Message}");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<OrderExportResult> ExportToExcelAsync(
        int storeId,
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? buyerEmail = null)
    {
        var result = new OrderExportResult();

        try
        {
            var subOrders = await GetSubOrdersForExportAsync(storeId, statuses, fromDate, toDate, buyerEmail);

            if (subOrders.Count == 0)
            {
                result.Errors.Add("No orders found to export.");
                return result;
            }

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Orders");

            // Header row
            worksheet.Cells[1, 1].Value = COL_SUB_ORDER_NUMBER;
            worksheet.Cells[1, 2].Value = COL_PARENT_ORDER_NUMBER;
            worksheet.Cells[1, 3].Value = COL_CREATED_DATE;
            worksheet.Cells[1, 4].Value = COL_STATUS;
            worksheet.Cells[1, 5].Value = COL_BUYER_NAME;
            worksheet.Cells[1, 6].Value = COL_BUYER_EMAIL;
            worksheet.Cells[1, 7].Value = COL_BUYER_PHONE;
            worksheet.Cells[1, 8].Value = COL_ADDRESS_LINE1;
            worksheet.Cells[1, 9].Value = COL_ADDRESS_LINE2;
            worksheet.Cells[1, 10].Value = COL_CITY;
            worksheet.Cells[1, 11].Value = COL_STATE_PROVINCE;
            worksheet.Cells[1, 12].Value = COL_POSTAL_CODE;
            worksheet.Cells[1, 13].Value = COL_COUNTRY_CODE;
            worksheet.Cells[1, 14].Value = COL_DELIVERY_INSTRUCTIONS;
            worksheet.Cells[1, 15].Value = COL_TOTAL_AMOUNT;
            worksheet.Cells[1, 16].Value = COL_SHIPPING_COST;
            worksheet.Cells[1, 17].Value = COL_SUBTOTAL;
            worksheet.Cells[1, 18].Value = COL_SHIPPING_METHOD;
            worksheet.Cells[1, 19].Value = COL_TRACKING_NUMBER;
            worksheet.Cells[1, 20].Value = COL_CARRIER_NAME;
            worksheet.Cells[1, 21].Value = COL_ITEMS_COUNT;
            worksheet.Cells[1, 22].Value = COL_ITEMS_DETAILS;

            // Style header row
            using (var range = worksheet.Cells[1, 1, 1, 22])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data rows
            int row = 2;
            foreach (var subOrder in subOrders)
            {
                var buyerName = GetBuyerName(subOrder);
                var buyerEmailValue = GetBuyerEmail(subOrder);
                var buyerPhone = GetBuyerPhone(subOrder);
                var address = subOrder.ParentOrder.DeliveryAddress;
                var shippingMethod = subOrder.ShippingMethod?.Name ?? "N/A";
                var itemsDetails = GetItemsDetails(subOrder);

                worksheet.Cells[row, 1].Value = subOrder.SubOrderNumber;
                worksheet.Cells[row, 2].Value = subOrder.ParentOrder.OrderNumber;
                worksheet.Cells[row, 3].Value = subOrder.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 4].Value = subOrder.Status.ToString();
                worksheet.Cells[row, 5].Value = buyerName;
                worksheet.Cells[row, 6].Value = buyerEmailValue;
                worksheet.Cells[row, 7].Value = buyerPhone;
                worksheet.Cells[row, 8].Value = address.AddressLine1;
                worksheet.Cells[row, 9].Value = address.AddressLine2 ?? string.Empty;
                worksheet.Cells[row, 10].Value = address.City;
                worksheet.Cells[row, 11].Value = address.StateProvince ?? string.Empty;
                worksheet.Cells[row, 12].Value = address.PostalCode;
                worksheet.Cells[row, 13].Value = address.CountryCode;
                worksheet.Cells[row, 14].Value = address.DeliveryInstructions ?? string.Empty;
                worksheet.Cells[row, 15].Value = subOrder.TotalAmount;
                worksheet.Cells[row, 16].Value = subOrder.ShippingCost;
                worksheet.Cells[row, 17].Value = subOrder.Subtotal;
                worksheet.Cells[row, 18].Value = shippingMethod;
                worksheet.Cells[row, 19].Value = subOrder.TrackingNumber ?? string.Empty;
                worksheet.Cells[row, 20].Value = subOrder.CarrierName ?? string.Empty;
                worksheet.Cells[row, 21].Value = subOrder.Items.Count;
                worksheet.Cells[row, 22].Value = itemsDetails;
                row++;
            }

            // Format currency columns
            worksheet.Cells[2, 15, row - 1, 15].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[2, 16, row - 1, 16].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells[2, 17, row - 1, 17].Style.Numberformat.Format = "$#,##0.00";

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            var fileName = $"orders_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}_utc.xlsx";
            result.FileData = package.GetAsByteArray();
            result.FileName = fileName;
            result.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            result.Success = true;

            _logger.LogInformation("Exported {Count} sub-orders to Excel for store {StoreId}", subOrders.Count, storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting orders to Excel for store {StoreId}", storeId);
            result.Errors.Add($"An error occurred while exporting: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Gets seller sub-orders for export with optional filters.
    /// </summary>
    private async Task<List<SellerSubOrder>> GetSubOrdersForExportAsync(
        int storeId,
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? buyerEmail = null)
    {
        var query = _context.SellerSubOrders
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.User)
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.DeliveryAddress)
            .Include(so => so.Items)
            .Include(so => so.ShippingMethod)
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

        // Apply buyer email filter (partial match, case-insensitive)
        if (!string.IsNullOrWhiteSpace(buyerEmail))
        {
            query = query.Where(so =>
                (so.ParentOrder.User != null && EF.Functions.Like(so.ParentOrder.User.Email, $"%{buyerEmail}%")) ||
                (so.ParentOrder.GuestEmail != null && EF.Functions.Like(so.ParentOrder.GuestEmail, $"%{buyerEmail}%")));
        }

        return await query
            .OrderByDescending(so => so.CreatedAt)
            .ToListAsync();
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
    /// Gets the buyer phone from a seller sub-order.
    /// </summary>
    private static string GetBuyerPhone(SellerSubOrder subOrder)
    {
        return subOrder.ParentOrder.DeliveryAddress?.PhoneNumber ?? "N/A";
    }

    /// <summary>
    /// Gets the items details from a seller sub-order formatted for export.
    /// Format: "SKU: {sku}, Product: {name}, Variant: {variant}, Qty: {qty}, Price: ${price}"
    /// Multiple items are separated by semicolons.
    /// </summary>
    private static string GetItemsDetails(SellerSubOrder subOrder)
    {
        if (subOrder.Items == null || !subOrder.Items.Any())
        {
            return string.Empty;
        }

        var itemDescriptions = new List<string>();
        foreach (var item in subOrder.Items)
        {
            var parts = new List<string>
            {
                $"Product: {item.ProductTitle}"
            };

            if (!string.IsNullOrWhiteSpace(item.VariantDescription))
            {
                parts.Add($"Variant: {item.VariantDescription}");
            }

            parts.Add($"Qty: {item.Quantity}");
            parts.Add($"Price: ${item.UnitPrice:F2}");

            itemDescriptions.Add(string.Join(", ", parts));
        }

        return string.Join("; ", itemDescriptions);
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
