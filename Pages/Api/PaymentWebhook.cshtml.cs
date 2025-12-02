using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace MercatoApp.Pages.Api;

/// <summary>
/// Webhook endpoint for payment provider callbacks.
/// Handles payment status updates from external payment providers.
/// </summary>
public class PaymentWebhookModel : PageModel
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentProviderService _paymentProviderService;
    private readonly ILogger<PaymentWebhookModel> _logger;

    public PaymentWebhookModel(
        IPaymentService paymentService,
        IPaymentProviderService paymentProviderService,
        ILogger<PaymentWebhookModel> logger)
    {
        _paymentService = paymentService;
        _paymentProviderService = paymentProviderService;
        _logger = logger;
    }

    /// <summary>
    /// Handles GET requests - webhook info.
    /// </summary>
    public IActionResult OnGet()
    {
        return new JsonResult(new
        {
            status = "active",
            message = "Payment webhook endpoint",
            version = "1.0"
        });
    }

    /// <summary>
    /// Handles POST requests - payment provider webhook callbacks.
    /// </summary>
    /// <param name="providerId">The payment provider ID (from query or form).</param>
    /// <param name="providerTransactionId">The provider's transaction ID.</param>
    /// <param name="status">The payment status from provider.</param>
    /// <param name="transactionId">Our internal transaction ID.</param>
    /// <param name="errorMessage">Optional error message if payment failed.</param>
    /// <returns>JSON response.</returns>
    public async Task<IActionResult> OnPostAsync(
        [FromQuery] string? providerId = null,
        [FromForm] string? providerTransactionId = null,
        [FromForm] string? status = null,
        [FromForm] int? transactionId = null,
        [FromForm] string? errorMessage = null)
    {
        try
        {
            // Read the request body for additional data
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            _logger.LogInformation("Received payment webhook from provider {ProviderId}: {Body}", 
                providerId ?? "unknown", body);

            // Validate required parameters
            if (!transactionId.HasValue)
            {
                _logger.LogWarning("Payment webhook missing transaction ID");
                return BadRequest(new { error = "Transaction ID is required" });
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                _logger.LogWarning("Payment webhook missing status for transaction {TransactionId}", transactionId);
                return BadRequest(new { error = "Payment status is required" });
            }

            // Get the transaction
            var transaction = await _paymentService.GetPaymentTransactionByIdAsync(transactionId.Value);
            if (transaction == null)
            {
                _logger.LogWarning("Payment transaction {TransactionId} not found", transactionId);
                return NotFound(new { error = "Transaction not found" });
            }

            // Determine provider ID from transaction if not in query
            var effectiveProviderId = providerId ?? transaction.PaymentMethod?.ProviderId ?? "unknown";

            // Map external provider status to internal status
            var internalStatus = PaymentStatusMapper.MapProviderStatus(status, effectiveProviderId);

            // Prepare callback data for verification
            var callbackData = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(providerTransactionId))
            {
                callbackData["providerTransactionId"] = providerTransactionId;
            }
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                callbackData["errorMessage"] = errorMessage;
            }
            callbackData["status"] = status;
            callbackData["providerId"] = effectiveProviderId;

            // Verify callback with provider (optional - for added security)
            if (!string.IsNullOrWhiteSpace(providerTransactionId))
            {
                try
                {
                    var verification = await _paymentProviderService.VerifyPaymentCallbackAsync(
                        providerTransactionId, callbackData);
                    
                    // Use verification result if available
                    if (!verification.Success && internalStatus == Models.PaymentStatus.Completed)
                    {
                        _logger.LogWarning("Payment verification failed for transaction {TransactionId}, " +
                            "provider transaction {ProviderTransactionId}", 
                            transactionId, providerTransactionId);
                        internalStatus = Models.PaymentStatus.Failed;
                        errorMessage = verification.ErrorMessage ?? "Payment verification failed";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verifying payment callback for transaction {TransactionId}", 
                        transactionId);
                    // Continue processing - verification is optional
                }
            }

            // Determine if payment succeeded based on internal status
            var success = internalStatus == Models.PaymentStatus.Completed || 
                         internalStatus == Models.PaymentStatus.Authorized;

            // Sanitize error message for storage
            var sanitizedError = string.IsNullOrWhiteSpace(errorMessage) 
                ? null 
                : PaymentStatusMapper.SanitizeErrorForBuyer(errorMessage);

            // Handle the payment callback
            await _paymentService.HandlePaymentCallbackAsync(
                transactionId.Value,
                success,
                providerTransactionId,
                sanitizedError);

            _logger.LogInformation("Payment webhook processed successfully for transaction {TransactionId}, " +
                "status: {Status} -> {InternalStatus}", 
                transactionId, status, internalStatus);

            return new JsonResult(new
            {
                success = true,
                transactionId = transactionId.Value,
                status = internalStatus.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
