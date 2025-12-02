using MercatoApp.Models;
using Microsoft.Extensions.Configuration;

namespace MercatoApp.Services;

/// <summary>
/// Mock payment provider service for testing and development.
/// In production, this would integrate with real payment gateways.
/// </summary>
public class MockPaymentProviderService : IPaymentProviderService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MockPaymentProviderService> _logger;
    private readonly List<string> _enabledMethods;

    public MockPaymentProviderService(
        IConfiguration configuration,
        ILogger<MockPaymentProviderService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Load enabled payment methods from configuration
        _enabledMethods = _configuration.GetSection("PaymentProvider:EnabledMethods")
            .Get<List<string>>() ?? new List<string> { "card", "bank_transfer", "blik", "cash_on_delivery" };
    }

    /// <inheritdoc />
    public async Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentTransaction transaction, string? blikCode = null)
    {
        await Task.Delay(100); // Simulate API call

        var paymentMethod = transaction.PaymentMethod;
        if (paymentMethod == null)
        {
            return new PaymentInitiationResult
            {
                ErrorMessage = "Payment method not found"
            };
        }

        _logger.LogInformation("Initiating payment for transaction {TransactionId} with method {ProviderId}",
            transaction.Id, paymentMethod.ProviderId);

        // Check if payment method is enabled
        if (!IsPaymentMethodEnabled(paymentMethod.ProviderId))
        {
            return new PaymentInitiationResult
            {
                ErrorMessage = $"Payment method {paymentMethod.Name} is not available in this environment"
            };
        }

        // Handle different payment methods
        switch (paymentMethod.ProviderId)
        {
            case "cash_on_delivery":
                // Cash on delivery requires no external authorization
                return new PaymentInitiationResult
                {
                    IsImmediate = true,
                    RequiresAction = false,
                    ProviderTransactionId = $"COD-{transaction.OrderId}-{transaction.Id}"
                };

            case "blik":
                // BLIK requires a 6-digit code entry
                if (string.IsNullOrEmpty(blikCode))
                {
                    // First call - need to collect BLIK code
                    return new PaymentInitiationResult
                    {
                        RequiresAction = true,
                        RedirectUrl = $"/Checkout/PaymentAuthorize?transactionId={transaction.Id}&requiresBlik=true"
                    };
                }
                else
                {
                    // Validate BLIK code (6 digits)
                    if (blikCode.Length != 6 || !blikCode.All(char.IsDigit))
                    {
                        return new PaymentInitiationResult
                        {
                            ErrorMessage = "Invalid BLIK code. Please enter a 6-digit code."
                        };
                    }

                    // In a real implementation, this would send the BLIK code to the payment provider
                    // For mock, we'll simulate success/failure based on code pattern
                    var providerTransactionId = $"BLIK-{Guid.NewGuid().ToString("N")[..20]}";
                    
                    return new PaymentInitiationResult
                    {
                        RequiresAction = true,
                        RedirectUrl = $"/Checkout/PaymentAuthorize?transactionId={transaction.Id}&blikCode={blikCode}",
                        ProviderTransactionId = providerTransactionId
                    };
                }

            case "card":
                // Card payments require redirect to payment page
                var cardTransactionId = $"CARD-{Guid.NewGuid().ToString("N")[..20]}";
                return new PaymentInitiationResult
                {
                    RequiresAction = true,
                    RedirectUrl = $"/Checkout/PaymentAuthorize?transactionId={transaction.Id}",
                    ProviderTransactionId = cardTransactionId
                };

            case "bank_transfer":
                // Bank transfer requires redirect to select bank
                var bankTransactionId = $"BANK-{Guid.NewGuid().ToString("N")[..20]}";
                return new PaymentInitiationResult
                {
                    RequiresAction = true,
                    RedirectUrl = $"/Checkout/PaymentAuthorize?transactionId={transaction.Id}&paymentType=bank",
                    ProviderTransactionId = bankTransactionId
                };

            default:
                return new PaymentInitiationResult
                {
                    ErrorMessage = $"Unsupported payment method: {paymentMethod.ProviderId}"
                };
        }
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> VerifyPaymentCallbackAsync(string providerTransactionId, Dictionary<string, string>? callbackData)
    {
        await Task.Delay(50); // Simulate API call

        _logger.LogInformation("Verifying payment callback for provider transaction {ProviderTransactionId}",
            providerTransactionId);

        // In a real implementation, this would verify the callback signature and status with the provider
        // For mock, we'll simulate based on transaction ID pattern
        
        // Simulate some failed transactions for testing (if transaction ID contains "FAIL")
        if (providerTransactionId.Contains("FAIL", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentVerificationResult
            {
                Success = false,
                ProviderTransactionId = providerTransactionId,
                ErrorMessage = "Payment declined by provider"
            };
        }

        return new PaymentVerificationResult
        {
            Success = true,
            ProviderTransactionId = providerTransactionId,
            Metadata = callbackData
        };
    }

    /// <inheritdoc />
    public bool IsPaymentMethodEnabled(string providerId)
    {
        return _enabledMethods.Contains(providerId);
    }

    /// <inheritdoc />
    public async Task<RefundProcessingResult> ProcessRefundAsync(
        PaymentTransaction paymentTransaction,
        decimal refundAmount,
        string reason)
    {
        await Task.Delay(100); // Simulate API call

        _logger.LogInformation("Processing refund for payment transaction {TransactionId}, amount: {Amount}, reason: {Reason}",
            paymentTransaction.Id, refundAmount, reason);

        // Validate refund amount
        if (refundAmount <= 0)
        {
            return new RefundProcessingResult
            {
                Success = false,
                ErrorMessage = "Refund amount must be greater than zero"
            };
        }

        if (refundAmount > paymentTransaction.Amount)
        {
            return new RefundProcessingResult
            {
                Success = false,
                ErrorMessage = $"Refund amount ({refundAmount}) cannot exceed original payment amount ({paymentTransaction.Amount})"
            };
        }

        // Check payment status
        if (paymentTransaction.Status != PaymentStatus.Completed && 
            paymentTransaction.Status != PaymentStatus.Authorized)
        {
            return new RefundProcessingResult
            {
                Success = false,
                ErrorMessage = $"Cannot refund payment with status {paymentTransaction.Status}"
            };
        }

        // For cash on delivery, refunds need to be handled manually
        if (paymentTransaction.PaymentMethod?.ProviderId == "cash_on_delivery")
        {
            _logger.LogInformation("Cash on delivery refund - must be processed manually");
            
            // In a real system, this might create a task for manual processing
            var providerRefundId = $"COD-REFUND-{Guid.NewGuid().ToString("N")[..20]}";
            
            return new RefundProcessingResult
            {
                Success = true,
                ProviderRefundId = providerRefundId,
                Metadata = new Dictionary<string, string>
                {
                    { "RefundMethod", "Manual" },
                    { "Note", "Cash on delivery refund requires manual processing" }
                }
            };
        }

        // For other payment methods, simulate provider refund processing
        // In production, this would call the actual payment provider API
        var refundId = $"REFUND-{Guid.NewGuid().ToString("N")[..20]}";

        _logger.LogInformation("Refund processed successfully with ID {RefundId}", refundId);

        return new RefundProcessingResult
        {
            Success = true,
            ProviderRefundId = refundId,
            Metadata = new Dictionary<string, string>
            {
                { "ProcessedAt", DateTime.UtcNow.ToString("O") },
                { "OriginalTransactionId", paymentTransaction.ProviderTransactionId ?? "N/A" }
            }
        };
    }
}
