using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

[Authorize]
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

    public List<Order> Orders { get; set; } = new();
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
    public int? SellerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    // Available sellers for filter dropdown
    public List<Store> AvailableSellers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        CurrentPage = PageNumber;

        // Get filtered and paginated orders
        var (orders, totalCount) = await _orderService.GetUserOrdersFilteredAsync(
            userId,
            SelectedStatuses,
            FromDate,
            ToDate,
            SellerId,
            CurrentPage,
            PageSize);

        Orders = orders;
        TotalCount = totalCount;

        // Load available sellers from user's orders for the filter dropdown
        await LoadAvailableSellersAsync(userId);

        return Page();
    }

    private async Task LoadAvailableSellersAsync(int userId)
    {
        // Get unique seller IDs from user's orders
        var sellerIds = await _context.Orders
            .Where(o => o.UserId == userId)
            .SelectMany(o => o.SubOrders)
            .Select(so => so.StoreId)
            .Distinct()
            .ToListAsync();

        if (sellerIds.Any())
        {
            AvailableSellers = await _context.Stores
                .Where(s => sellerIds.Contains(s.Id))
                .OrderBy(s => s.StoreName)
                .ToListAsync();
        }
    }
}
