using MercatoApp.Authorization;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Admin.Orders;

/// <summary>
/// Page model for the admin order details page.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        ApplicationDbContext context,
        ILogger<DetailsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the order.
    /// </summary>
    public Order? Order { get; set; }

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
    /// Handles GET request to display order details.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.DeliveryAddress)
                .Include(o => o.PaymentMethod)
                .Include(o => o.PaymentTransactions)
                .Include(o => o.SubOrders)
                    .ThenInclude(so => so.Store)
                .Include(o => o.SubOrders)
                    .ThenInclude(so => so.ShippingMethod)
                .Include(o => o.SubOrders)
                    .ThenInclude(so => so.Items)
                        .ThenInclude(oi => oi.Product)
                .Include(o => o.SubOrders)
                    .ThenInclude(so => so.StatusHistory)
                        .ThenInclude(sh => sh.ChangedByUser)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (Order == null)
            {
                ErrorMessage = "Order not found.";
                return RedirectToPage("/Admin/Orders/Index");
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading order details for admin, order ID: {OrderId}", id);
            ErrorMessage = "An error occurred while loading order details.";
            return RedirectToPage("/Admin/Orders/Index");
        }
    }
}
