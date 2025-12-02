using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MercatoApp.Data;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = "SellerOnly")]
public class OrdersModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrdersModel> _logger;

    public OrdersModel(
        IOrderService orderService,
        ApplicationDbContext context,
        ILogger<OrdersModel> logger)
    {
        _orderService = orderService;
        _context = context;
        _logger = logger;
    }

    public List<SellerSubOrder> SubOrders { get; set; } = new();
    public Store? CurrentStore { get; set; }
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Filter properties
    [BindProperty(SupportsGet = true)]
    public List<OrderStatus>? SelectedStatuses { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BuyerEmail { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Get the seller's store
        CurrentStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (CurrentStore == null)
        {
            TempData["ErrorMessage"] = "Store not found.";
            return RedirectToPage("/Index");
        }

        CurrentPage = PageNumber;

        // Get filtered and paginated sub-orders
        var (subOrders, totalCount) = await _orderService.GetSubOrdersFilteredAsync(
            CurrentStore.Id,
            SelectedStatuses,
            FromDate,
            ToDate,
            BuyerEmail,
            CurrentPage,
            PageSize);

        SubOrders = subOrders;
        TotalCount = totalCount;

        return Page();
    }
}
