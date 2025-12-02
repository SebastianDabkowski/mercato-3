using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for payment management service.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Gets all active payment methods.
    /// </summary>
    /// <returns>A list of active payment methods.</returns>
    Task<List<PaymentMethod>> GetActivePaymentMethodsAsync();

    /// <summary>
    /// Gets or creates default payment methods.
    /// If no payment methods exist, creates default ones.
    /// </summary>
    /// <returns>A list of payment methods.</returns>
    Task<List<PaymentMethod>> GetOrCreateDefaultPaymentMethodsAsync();

    /// <summary>
    /// Gets a payment method by ID.
    /// </summary>
    /// <param name="id">The payment method ID.</param>
    /// <returns>The payment method, or null if not found.</returns>
    Task<PaymentMethod?> GetPaymentMethodByIdAsync(int id);

    /// <summary>
    /// Creates a payment transaction for an order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="paymentMethodId">The payment method ID.</param>
    /// <param name="amount">The payment amount.</param>
    /// <returns>The created payment transaction.</returns>
    Task<PaymentTransaction> CreatePaymentTransactionAsync(int orderId, int paymentMethodId, decimal amount);

    /// <summary>
    /// Initiates payment authorization with the payment provider.
    /// </summary>
    /// <param name="transactionId">The payment transaction ID.</param>
    /// <returns>The redirect URL for payment authorization, or null for cash on delivery.</returns>
    Task<string?> InitiatePaymentAsync(int transactionId);

    /// <summary>
    /// Handles payment callback/webhook from the payment provider.
    /// </summary>
    /// <param name="transactionId">The payment transaction ID.</param>
    /// <param name="success">Whether the payment was successful.</param>
    /// <param name="providerTransactionId">The provider's transaction ID.</param>
    /// <param name="errorMessage">The error message if payment failed.</param>
    /// <returns>The updated payment transaction.</returns>
    Task<PaymentTransaction> HandlePaymentCallbackAsync(int transactionId, bool success, string? providerTransactionId, string? errorMessage);

    /// <summary>
    /// Gets a payment transaction by ID.
    /// </summary>
    /// <param name="id">The payment transaction ID.</param>
    /// <returns>The payment transaction, or null if not found.</returns>
    Task<PaymentTransaction?> GetPaymentTransactionByIdAsync(int id);
}
