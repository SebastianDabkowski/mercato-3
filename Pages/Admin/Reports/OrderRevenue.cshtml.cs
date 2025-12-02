using MercatoApp.Authorization;
using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Admin.Reports;

/// <summary>
/// View model for a single report row.
/// </summary>
public class OrderRevenueReportRow
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string SellerStoreName { get; set; } = string.Empty;
    public string SubOrderNumber { get; set; } = string.Empty;
    public OrderStatus OrderStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal OrderValue { get; set; }
    public decimal Commission { get; set; }
    public decimal PayoutAmount { get; set; }
}

/// <summary>
/// Page model for admin order and revenue reports.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class OrderRevenueModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAdminReportService _reportService;
    private readonly ILogger<OrderRevenueModel> _logger;

    public OrderRevenueModel(
        ApplicationDbContext context,
        IAdminReportService reportService,
        ILogger<OrderRevenueModel> logger)
    {
        _context = context;
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of report rows.
    /// </summary>
    public List<OrderRevenueReportRow> ReportRows { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of stores for the filter dropdown.
    /// </summary>
    public List<Store> Stores { get; set; } = new();

    /// <summary>
    /// Gets or sets the from date filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the to date filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Gets or sets the store/seller ID filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the order status filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? OrderStatus { get; set; }

    /// <summary>
    /// Gets or sets the payment status filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? PaymentStatus { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; } = 50;

    /// <summary>
    /// Gets or sets the total count of rows.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the success message to display.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the total order value for the filtered results.
    /// </summary>
    public decimal TotalOrderValue { get; set; }

    /// <summary>
    /// Gets or sets the total commission for the filtered results.
    /// </summary>
    public decimal TotalCommission { get; set; }

    /// <summary>
    /// Gets or sets the total payout amount for the filtered results.
    /// </summary>
    public decimal TotalPayoutAmount { get; set; }

    /// <summary>
    /// Handles GET request to display the report.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Load stores for the filter dropdown
            Stores = await _context.Stores
                .OrderBy(s => s.StoreName)
                .ToListAsync();

            // Build query with filters
            var query = _context.SellerSubOrders
                .Include(so => so.ParentOrder)
                    .ThenInclude(o => o.User)
                .Include(so => so.Store)
                .AsQueryable();

            // Apply filters
            if (FromDate.HasValue)
            {
                query = query.Where(so => so.ParentOrder.OrderedAt >= FromDate.Value);
            }
            if (ToDate.HasValue)
            {
                var endOfDay = ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(so => so.ParentOrder.OrderedAt <= endOfDay);
            }
            if (StoreId.HasValue)
            {
                query = query.Where(so => so.StoreId == StoreId.Value);
            }
            if (!string.IsNullOrEmpty(OrderStatus) && Enum.TryParse<Models.OrderStatus>(OrderStatus, out var parsedOrderStatus))
            {
                query = query.Where(so => so.Status == parsedOrderStatus);
            }
            if (!string.IsNullOrEmpty(PaymentStatus) && Enum.TryParse<Models.PaymentStatus>(PaymentStatus, out var parsedPaymentStatus))
            {
                query = query.Where(so => so.ParentOrder.PaymentStatus == parsedPaymentStatus);
            }

            // Get total count for pagination
            TotalCount = await query.CountAsync();

            // Get paginated results
            var subOrders = await query
                .OrderByDescending(so => so.ParentOrder.OrderedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Build report rows
            ReportRows = new List<OrderRevenueReportRow>();
            foreach (var subOrder in subOrders)
            {
                var commission = await GetCommissionAmountAsync(subOrder.Id);
                var payoutAmount = subOrder.TotalAmount - commission;

                var row = new OrderRevenueReportRow
                {
                    OrderId = subOrder.ParentOrderId,
                    OrderNumber = subOrder.ParentOrder.OrderNumber,
                    OrderDate = subOrder.ParentOrder.OrderedAt,
                    BuyerName = GetBuyerName(subOrder),
                    BuyerEmail = GetBuyerEmail(subOrder),
                    SellerStoreName = subOrder.Store.StoreName,
                    SubOrderNumber = subOrder.SubOrderNumber,
                    OrderStatus = subOrder.Status,
                    PaymentStatus = subOrder.ParentOrder.PaymentStatus,
                    OrderValue = subOrder.TotalAmount,
                    Commission = commission,
                    PayoutAmount = payoutAmount
                };

                ReportRows.Add(row);
            }

            // Calculate totals for current page
            TotalOrderValue = ReportRows.Sum(r => r.OrderValue);
            TotalCommission = ReportRows.Sum(r => r.Commission);
            TotalPayoutAmount = ReportRows.Sum(r => r.PayoutAmount);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin order revenue report");
            ErrorMessage = "An error occurred while loading the report.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST request to export the report to CSV.
    /// </summary>
    public async Task<IActionResult> OnPostExportAsync()
    {
        try
        {
            var result = await _reportService.ExportOrderReportToCsvAsync(
                FromDate,
                ToDate,
                StoreId,
                OrderStatus,
                PaymentStatus);

            if (!result.Success)
            {
                ErrorMessage = string.Join(", ", result.Errors);
                return RedirectToPage(new 
                { 
                    FromDate, 
                    ToDate, 
                    StoreId, 
                    OrderStatus, 
                    PaymentStatus,
                    PageNumber
                });
            }

            return File(result.FileData!, result.ContentType!, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting admin order revenue report");
            ErrorMessage = "An error occurred while exporting the report.";
            return RedirectToPage(new 
            { 
                FromDate, 
                ToDate, 
                StoreId, 
                OrderStatus, 
                PaymentStatus,
                PageNumber
            });
        }
    }

    /// <summary>
    /// Gets the commission amount for a sub-order.
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
}
