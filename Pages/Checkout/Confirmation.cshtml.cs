using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Checkout;

public class ConfirmationModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ConfirmationModel> _logger;

    public ConfirmationModel(
        IOrderService orderService, 
        IPaymentService paymentService,
        IEmailService emailService,
        ILogger<ConfirmationModel> logger)
    {
        _orderService = orderService;
        _paymentService = paymentService;
        _emailService = emailService;
        _logger = logger;
    }

    public Order? Order { get; set; }
    public PaymentTransaction? LatestPaymentTransaction { get; set; }
    public bool EmailSent { get; set; }

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        Order = await _orderService.GetOrderByIdAsync(orderId);

        if (Order == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage("/Index");
        }

        // Get the latest payment transaction for this order
        if (Order.PaymentTransactions.Any())
        {
            LatestPaymentTransaction = Order.PaymentTransactions
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();
        }

        // Send confirmation email only once (idempotency check)
        // Use a session key to track if we've already sent the email for this order
        var emailSentKey = $"OrderConfirmationEmailSent_{orderId}";
        var alreadySent = HttpContext.Session.GetString(emailSentKey);
        
        if (string.IsNullOrEmpty(alreadySent))
        {
            try
            {
                // Send confirmation email
                await _emailService.SendOrderConfirmationEmailAsync(Order);
                
                // Mark as sent to prevent duplicate emails on page refresh
                HttpContext.Session.SetString(emailSentKey, "true");
                EmailSent = true;
                
                _logger.LogInformation("Order confirmation email sent for order {OrderNumber}", Order.OrderNumber);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the page load
                _logger.LogError(ex, "Failed to send order confirmation email for order {OrderNumber}", Order.OrderNumber);
                EmailSent = false;
            }
        }
        else
        {
            EmailSent = false; // Already sent previously
        }

        return Page();
    }
}
