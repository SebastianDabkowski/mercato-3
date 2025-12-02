using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for payment provider integration.
/// Handles communication with external payment gateway.
/// </summary>
public interface IPaymentProviderService
{
    /// <summary>
    /// Initiates a payment with the payment provider.
    /// </summary>
    /// <param name="transaction">The payment transaction.</param>
    /// <param name="blikCode">Optional BLIK code for BLIK payments.</param>
    /// <returns>Payment initiation result containing redirect URL or status.</returns>
    Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentTransaction transaction, string? blikCode = null);

    /// <summary>
    /// Verifies payment callback from the provider.
    /// </summary>
    /// <param name="providerTransactionId">The provider's transaction ID.</param>
    /// <param name="callbackData">Additional callback data from the provider.</param>
    /// <returns>Payment verification result.</returns>
    Task<PaymentVerificationResult> VerifyPaymentCallbackAsync(string providerTransactionId, Dictionary<string, string>? callbackData);

    /// <summary>
    /// Checks if a payment method is enabled in the current environment.
    /// </summary>
    /// <param name="providerId">The payment method provider ID.</param>
    /// <returns>True if enabled, false otherwise.</returns>
    bool IsPaymentMethodEnabled(string providerId);

    /// <summary>
    /// Processes a refund with the payment provider.
    /// </summary>
    /// <param name="paymentTransaction">The original payment transaction.</param>
    /// <param name="refundAmount">The amount to refund.</param>
    /// <param name="reason">The reason for the refund.</param>
    /// <returns>Refund processing result.</returns>
    Task<RefundProcessingResult> ProcessRefundAsync(
        PaymentTransaction paymentTransaction,
        decimal refundAmount,
        string reason);
}

/// <summary>
/// Result of payment initiation.
/// </summary>
public class PaymentInitiationResult
{
    /// <summary>
    /// Gets or sets the redirect URL for payment authorization (null if no redirect needed).
    /// </summary>
    public string? RedirectUrl { get; set; }

    /// <summary>
    /// Gets or sets the provider's transaction ID.
    /// </summary>
    public string? ProviderTransactionId { get; set; }

    /// <summary>
    /// Gets or sets whether the payment requires user action (redirect/code entry).
    /// </summary>
    public bool RequiresAction { get; set; }

    /// <summary>
    /// Gets or sets whether the payment was immediately successful (e.g., cash on delivery).
    /// </summary>
    public bool IsImmediate { get; set; }

    /// <summary>
    /// Gets or sets the error message if initiation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of payment verification.
/// </summary>
public class PaymentVerificationResult
{
    /// <summary>
    /// Gets or sets whether the payment was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the provider's transaction ID.
    /// </summary>
    public string? ProviderTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the error message if payment failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata from the provider.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Result of refund processing.
/// </summary>
public class RefundProcessingResult
{
    /// <summary>
    /// Gets or sets whether the refund was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the provider's refund transaction ID.
    /// </summary>
    public string? ProviderRefundId { get; set; }

    /// <summary>
    /// Gets or sets the error message if refund failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata from the provider.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
