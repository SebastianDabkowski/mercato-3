namespace MercatoApp.Services;

/// <summary>
/// Maps external payment provider status codes to internal payment status.
/// Centralizes all provider status mapping logic.
/// </summary>
public static class PaymentStatusMapper
{
    /// <summary>
    /// Maps an external payment provider status code to internal PaymentStatus.
    /// </summary>
    /// <param name="providerStatus">The external provider status code.</param>
    /// <param name="providerId">The payment provider identifier (e.g., "stripe", "paypal").</param>
    /// <returns>The mapped internal PaymentStatus.</returns>
    public static Models.PaymentStatus MapProviderStatus(string providerStatus, string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerStatus))
        {
            return Models.PaymentStatus.Pending;
        }

        // Normalize status to lowercase for comparison
        var normalizedStatus = providerStatus.ToLowerInvariant();

        // Map common provider statuses
        return providerId.ToLowerInvariant() switch
        {
            "stripe" => MapStripeStatus(normalizedStatus),
            "paypal" => MapPayPalStatus(normalizedStatus),
            "card" => MapCardStatus(normalizedStatus),
            "bank_transfer" => MapBankTransferStatus(normalizedStatus),
            "blik" => MapBlikStatus(normalizedStatus),
            "cash_on_delivery" => MapCashOnDeliveryStatus(normalizedStatus),
            _ => MapGenericStatus(normalizedStatus)
        };
    }

    /// <summary>
    /// Maps Stripe payment status to internal status.
    /// See: https://stripe.com/docs/payments/payment-intents/status
    /// </summary>
    private static Models.PaymentStatus MapStripeStatus(string status)
    {
        return status switch
        {
            "succeeded" => Models.PaymentStatus.Completed,
            "processing" => Models.PaymentStatus.Pending,
            "requires_payment_method" => Models.PaymentStatus.Pending,
            "requires_confirmation" => Models.PaymentStatus.Pending,
            "requires_action" => Models.PaymentStatus.Pending,
            // requires_capture: Payment authorized but not yet captured (two-step payment)
            "requires_capture" => Models.PaymentStatus.Authorized,
            "canceled" => Models.PaymentStatus.Cancelled,
            "failed" => Models.PaymentStatus.Failed,
            "refunded" => Models.PaymentStatus.Refunded,
            "partial_refund" => Models.PaymentStatus.Refunded,
            _ => Models.PaymentStatus.Pending
        };
    }

    /// <summary>
    /// Maps PayPal payment status to internal status.
    /// </summary>
    private static Models.PaymentStatus MapPayPalStatus(string status)
    {
        return status switch
        {
            "completed" => Models.PaymentStatus.Completed,
            "approved" => Models.PaymentStatus.Authorized,
            "created" => Models.PaymentStatus.Pending,
            "saved" => Models.PaymentStatus.Pending,
            "voided" => Models.PaymentStatus.Cancelled,
            "payer_action_required" => Models.PaymentStatus.Pending,
            "failed" => Models.PaymentStatus.Failed,
            "denied" => Models.PaymentStatus.Failed,
            "refunded" => Models.PaymentStatus.Refunded,
            "partially_refunded" => Models.PaymentStatus.Refunded,
            _ => Models.PaymentStatus.Pending
        };
    }

    /// <summary>
    /// Maps card payment status to internal status.
    /// </summary>
    private static Models.PaymentStatus MapCardStatus(string status)
    {
        return status switch
        {
            "completed" or "success" or "succeeded" => Models.PaymentStatus.Completed,
            "authorized" => Models.PaymentStatus.Authorized,
            "pending" or "processing" => Models.PaymentStatus.Pending,
            "failed" or "declined" or "error" => Models.PaymentStatus.Failed,
            "cancelled" or "canceled" => Models.PaymentStatus.Cancelled,
            "refunded" => Models.PaymentStatus.Refunded,
            _ => Models.PaymentStatus.Pending
        };
    }

    /// <summary>
    /// Maps bank transfer payment status to internal status.
    /// </summary>
    private static Models.PaymentStatus MapBankTransferStatus(string status)
    {
        return status switch
        {
            "completed" or "cleared" or "settled" => Models.PaymentStatus.Completed,
            "pending" or "processing" or "initiated" => Models.PaymentStatus.Pending,
            "failed" or "rejected" => Models.PaymentStatus.Failed,
            "cancelled" or "canceled" => Models.PaymentStatus.Cancelled,
            "refunded" => Models.PaymentStatus.Refunded,
            _ => Models.PaymentStatus.Pending
        };
    }

    /// <summary>
    /// Maps BLIK payment status to internal status.
    /// </summary>
    private static Models.PaymentStatus MapBlikStatus(string status)
    {
        return status switch
        {
            "completed" or "success" or "accepted" => Models.PaymentStatus.Completed,
            "pending" or "waiting" => Models.PaymentStatus.Pending,
            "failed" or "declined" or "rejected" => Models.PaymentStatus.Failed,
            "cancelled" or "canceled" or "timeout" => Models.PaymentStatus.Cancelled,
            "refunded" => Models.PaymentStatus.Refunded,
            _ => Models.PaymentStatus.Pending
        };
    }

    /// <summary>
    /// Maps cash on delivery payment status to internal status.
    /// Note: COD payments are typically authorized on order placement and completed on delivery.
    /// </summary>
    private static Models.PaymentStatus MapCashOnDeliveryStatus(string status)
    {
        return status switch
        {
            "completed" or "collected" => Models.PaymentStatus.Completed,
            "authorized" or "confirmed" => Models.PaymentStatus.Authorized,
            "pending" => Models.PaymentStatus.Pending,
            "failed" or "not_collected" => Models.PaymentStatus.Failed,
            "cancelled" or "canceled" => Models.PaymentStatus.Cancelled,
            "refunded" => Models.PaymentStatus.Refunded,
            // Default to Authorized for COD as order is confirmed and awaiting delivery
            _ => Models.PaymentStatus.Authorized
        };
    }

    /// <summary>
    /// Maps generic payment status codes to internal status.
    /// Used as fallback for unknown providers.
    /// </summary>
    private static Models.PaymentStatus MapGenericStatus(string status)
    {
        return status switch
        {
            "completed" or "success" or "succeeded" or "paid" => Models.PaymentStatus.Completed,
            "authorized" or "approved" => Models.PaymentStatus.Authorized,
            "pending" or "processing" or "created" => Models.PaymentStatus.Pending,
            "failed" or "declined" or "error" or "rejected" => Models.PaymentStatus.Failed,
            "cancelled" or "canceled" or "voided" => Models.PaymentStatus.Cancelled,
            "refunded" or "refund" => Models.PaymentStatus.Refunded,
            _ => Models.PaymentStatus.Pending
        };
    }

    /// <summary>
    /// Gets a user-friendly message for a payment status.
    /// Does not expose technical details to buyers.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <returns>A user-friendly status message.</returns>
    public static string GetBuyerFriendlyMessage(Models.PaymentStatus status)
    {
        return status switch
        {
            Models.PaymentStatus.Pending => "Your payment is being processed. This may take a few moments.",
            Models.PaymentStatus.Authorized => "Your payment has been authorized and will be captured upon shipment.",
            Models.PaymentStatus.Completed => "Your payment was successful.",
            Models.PaymentStatus.Failed => "We couldn't process your payment. Please try again or use a different payment method.",
            Models.PaymentStatus.Cancelled => "Your payment was cancelled. You can try placing a new order.",
            Models.PaymentStatus.Refunded => "Your payment has been refunded.",
            _ => "Payment status is being updated."
        };
    }

    /// <summary>
    /// Gets a user-friendly error message for a failed payment.
    /// Sanitizes technical error messages for buyer display.
    /// </summary>
    /// <param name="technicalError">The technical error message from the provider.</param>
    /// <returns>A sanitized user-friendly error message.</returns>
    public static string SanitizeErrorForBuyer(string? technicalError)
    {
        if (string.IsNullOrWhiteSpace(technicalError))
        {
            return "We couldn't process your payment. Please try again.";
        }

        // Don't expose technical details, stack traces, or sensitive information
        var lowerError = technicalError.ToLowerInvariant();

        if (lowerError.Contains("insufficient") || lowerError.Contains("balance"))
        {
            return "Payment declined due to insufficient funds. Please use a different payment method.";
        }

        if (lowerError.Contains("expired") || lowerError.Contains("expir"))
        {
            return "Your card has expired. Please use a different payment method.";
        }

        if (lowerError.Contains("declined") || lowerError.Contains("deny") || lowerError.Contains("denied"))
        {
            return "Your payment was declined. Please contact your bank or use a different payment method.";
        }

        if (lowerError.Contains("timeout") || lowerError.Contains("time out"))
        {
            return "The payment request timed out. Please try again.";
        }

        if (lowerError.Contains("cancel") || lowerError.Contains("abort"))
        {
            return "The payment was cancelled. You can try again when ready.";
        }

        if (lowerError.Contains("invalid") || lowerError.Contains("incorrect"))
        {
            return "Payment information is invalid. Please check your details and try again.";
        }

        if (lowerError.Contains("limit") || lowerError.Contains("exceed"))
        {
            return "Payment limit exceeded. Please contact your bank or use a different payment method.";
        }

        // Default generic message for unknown errors
        return "We couldn't process your payment. Please try again or use a different payment method.";
    }
}
