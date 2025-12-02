using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Checkout;

public class PaymentAuthorizeModel : PageModel
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentAuthorizeModel> _logger;

    public PaymentAuthorizeModel(
        IPaymentService paymentService,
        ILogger<PaymentAuthorizeModel> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int transactionId)
    {
        var transaction = await _paymentService.GetPaymentTransactionByIdAsync(transactionId);
        
        if (transaction == null)
        {
            TempData["ErrorMessage"] = "Payment transaction not found.";
            return RedirectToPage("/Cart");
        }

        TransactionId = transactionId;
        Amount = transaction.Amount;
        PaymentMethodName = transaction.PaymentMethod?.Name ?? "Unknown";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int transactionId, string action)
    {
        if (action == "approve")
        {
            // Simulate successful payment
            var providerTransactionId = $"SIM-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            await _paymentService.HandlePaymentCallbackAsync(transactionId, true, providerTransactionId, null);

            var transaction = await _paymentService.GetPaymentTransactionByIdAsync(transactionId);
            if (transaction?.Order != null)
            {
                return RedirectToPage("/Checkout/Confirmation", new { orderId = transaction.Order.Id });
            }
        }
        else if (action == "cancel")
        {
            // Simulate cancelled payment
            await _paymentService.HandlePaymentCallbackAsync(transactionId, false, null, "Payment cancelled by user");

            TempData["ErrorMessage"] = "Payment was cancelled. Please try again or choose a different payment method.";
            return RedirectToPage("/Checkout/Payment");
        }

        return RedirectToPage("/Cart");
    }
}
