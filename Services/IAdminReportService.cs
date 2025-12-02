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
/// Interface for admin reporting service.
/// </summary>
public interface IAdminReportService
{
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
