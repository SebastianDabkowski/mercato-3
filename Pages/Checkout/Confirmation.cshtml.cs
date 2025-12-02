using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Checkout;

public class ConfirmationModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<ConfirmationModel> _logger;

    public ConfirmationModel(IOrderService orderService, ILogger<ConfirmationModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public Order? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        Order = await _orderService.GetOrderByIdAsync(orderId);

        if (Order == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage("/Index");
        }

        return Page();
    }
}
