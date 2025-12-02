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
    public string PaymentMethodProviderId { get; set; } = string.Empty;
    public bool RequiresBlik { get; set; }
    public string? PaymentType { get; set; }

    [BindProperty]
    public string? BlikCode { get; set; }

    public async Task<IActionResult> OnGetAsync(int transactionId, bool requiresBlik = false, string? paymentType = null, string? blikCode = null)
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
        PaymentMethodProviderId = transaction.PaymentMethod?.ProviderId ?? "";
        RequiresBlik = requiresBlik;
        PaymentType = paymentType;
        BlikCode = blikCode;

        return Page();
    }

    public async Task<IActionResult> OnPostSubmitBlikCodeAsync(int transactionId, string blikCode)
    {
        // Validate BLIK code
        if (string.IsNullOrWhiteSpace(blikCode) || blikCode.Length != 6 || !blikCode.All(char.IsDigit))
        {
            TempData["ErrorMessage"] = "Invalid BLIK code. Please enter a 6-digit code.";
            return RedirectToPage(new { transactionId, requiresBlik = true });
        }

        // Redirect to the same page with the BLIK code in the query (for display only)
        return RedirectToPage(new { transactionId, requiresBlik = true, blikCode });
    }

    public async Task<IActionResult> OnPostAsync(int transactionId, string action)
    {
        if (action == "approve")
        {
            // Simulate successful payment with a unique transaction ID
            var providerTransactionId = $"SIM-{transactionId}-{Guid.NewGuid():N}".Substring(0, 24);
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
