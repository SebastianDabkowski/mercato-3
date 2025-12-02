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

            // Get total count for pagination
            TotalCount = await _reportService.GetOrderReportCountAsync(
                FromDate,
                ToDate,
                StoreId,
                OrderStatus,
                PaymentStatus);

            // Get paginated report data
            var reportData = await _reportService.GetOrderReportDataAsync(
                FromDate,
                ToDate,
                StoreId,
                OrderStatus,
                PaymentStatus,
                (PageNumber - 1) * PageSize,
                PageSize);

            // Convert to report rows
            ReportRows = reportData.Select(d => new OrderRevenueReportRow
            {
                OrderId = d.OrderId,
                OrderNumber = d.OrderNumber,
                OrderDate = d.OrderDate,
                BuyerName = d.BuyerName,
                BuyerEmail = d.BuyerEmail,
                SellerStoreName = d.SellerStoreName,
                SubOrderNumber = d.SubOrderNumber,
                OrderStatus = Enum.Parse<Models.OrderStatus>(d.OrderStatus),
                PaymentStatus = Enum.Parse<Models.PaymentStatus>(d.PaymentStatus),
                OrderValue = d.OrderValue,
                Commission = d.Commission,
                PayoutAmount = d.PayoutAmount
            }).ToList();

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
}
