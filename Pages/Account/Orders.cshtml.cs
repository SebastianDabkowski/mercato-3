using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

[Authorize]
public class OrdersModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersModel> _logger;

    public OrdersModel(
        IOrderService orderService,
        ILogger<OrdersModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public List<Order> Orders { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        Orders = await _orderService.GetUserOrdersAsync(userId);

        return Page();
    }
}
