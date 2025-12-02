namespace MercatoApp.Services;

/// <summary>
/// Result of generating an admin report export file.
/// </summary>
public class AdminReportExportResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public byte[]? FileData { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}

/// <summary>
/// Represents a single row in the order/revenue report.
/// </summary>
public class OrderRevenueReportData
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string SellerStoreName { get; set; } = string.Empty;
    public string SubOrderNumber { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal OrderValue { get; set; }
    public decimal Commission { get; set; }
    public decimal PayoutAmount { get; set; }
}

/// <summary>
/// Interface for admin reporting service.
/// </summary>
public interface IAdminReportService
{
    /// <summary>
    /// Gets order and revenue report data with optional filters.
    /// </summary>
    /// <param name="fromDate">Optional filter by minimum date.</param>
    /// <param name="toDate">Optional filter by maximum date.</param>
    /// <param name="storeId">Optional filter by store/seller.</param>
    /// <param name="orderStatus">Optional filter by order status.</param>
    /// <param name="paymentStatus">Optional filter by payment status.</param>
    /// <param name="skip">Number of records to skip (for pagination).</param>
    /// <param name="take">Number of records to take (for pagination).</param>
    Task<List<OrderRevenueReportData>> GetOrderReportDataAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? storeId = null,
        string? orderStatus = null,
        string? paymentStatus = null,
        int skip = 0,
        int take = 50);

    /// <summary>
    /// Gets the total count of orders matching the filters.
    /// </summary>
    /// <param name="fromDate">Optional filter by minimum date.</param>
    /// <param name="toDate">Optional filter by maximum date.</param>
    /// <param name="storeId">Optional filter by store/seller.</param>
    /// <param name="orderStatus">Optional filter by order status.</param>
    /// <param name="paymentStatus">Optional filter by payment status.</param>
    Task<int> GetOrderReportCountAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? storeId = null,
        string? orderStatus = null,
        string? paymentStatus = null);

    /// <summary>
    /// Exports order and revenue report to CSV format.
    /// </summary>
    /// <param name="fromDate">Optional filter by minimum date.</param>
    /// <param name="toDate">Optional filter by maximum date.</param>
    /// <param name="storeId">Optional filter by store/seller.</param>
    /// <param name="orderStatus">Optional filter by order status.</param>
    /// <param name="paymentStatus">Optional filter by payment status.</param>
    Task<AdminReportExportResult> ExportOrderReportToCsvAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? storeId = null,
        string? orderStatus = null,
        string? paymentStatus = null);
}
