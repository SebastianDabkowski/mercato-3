using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Revenue report item for a single seller sub-order.
/// </summary>
public class RevenueReportItem
{
    /// <summary>
    /// Gets or sets the sub-order number.
    /// </summary>
    public string SubOrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent order number.
    /// </summary>
    public string ParentOrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the order status.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the buyer name.
    /// </summary>
    public string BuyerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the buyer email.
    /// </summary>
    public string BuyerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order value (subtotal + shipping).
    /// </summary>
    public decimal OrderValue { get; set; }

    /// <summary>
    /// Gets or sets the commission charged by the platform.
    /// </summary>
    public decimal CommissionCharged { get; set; }

    /// <summary>
    /// Gets or sets the net amount to the seller (order value - commission).
    /// </summary>
    public decimal NetAmountToSeller { get; set; }

    /// <summary>
    /// Gets or sets the refunded amount (if any).
    /// </summary>
    public decimal RefundedAmount { get; set; }
}

/// <summary>
/// Summary totals for a revenue report.
/// </summary>
public class RevenueReportSummary
{
    /// <summary>
    /// Gets or sets the total number of orders.
    /// </summary>
    public int TotalOrders { get; set; }

    /// <summary>
    /// Gets or sets the total order value.
    /// </summary>
    public decimal TotalOrderValue { get; set; }

    /// <summary>
    /// Gets or sets the total commission charged.
    /// </summary>
    public decimal TotalCommissionCharged { get; set; }

    /// <summary>
    /// Gets or sets the total net amount to seller.
    /// </summary>
    public decimal TotalNetAmountToSeller { get; set; }

    /// <summary>
    /// Gets or sets the total refunded amount.
    /// </summary>
    public decimal TotalRefundedAmount { get; set; }
}

/// <summary>
/// Result of generating a revenue report export file.
/// </summary>
public class RevenueReportExportResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public byte[]? FileData { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}

/// <summary>
/// Interface for seller revenue report service.
/// </summary>
public interface ISellerRevenueReportService
{
    /// <summary>
    /// Gets revenue report data for a seller's store with optional filters.
    /// </summary>
    /// <param name="storeId">The store ID to generate report for.</param>
    /// <param name="statuses">Optional filter by statuses.</param>
    /// <param name="fromDate">Optional filter by minimum date.</param>
    /// <param name="toDate">Optional filter by maximum date.</param>
    /// <returns>Tuple of report items and summary.</returns>
    Task<(List<RevenueReportItem> Items, RevenueReportSummary Summary)> GetRevenueReportAsync(
        int storeId,
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Exports revenue report to CSV format.
    /// </summary>
    /// <param name="storeId">The store ID to export report for.</param>
    /// <param name="statuses">Optional filter by statuses.</param>
    /// <param name="fromDate">Optional filter by minimum date.</param>
    /// <param name="toDate">Optional filter by maximum date.</param>
    Task<RevenueReportExportResult> ExportToCsvAsync(
        int storeId,
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
}
