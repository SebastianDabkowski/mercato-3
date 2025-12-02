using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing payments.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext context,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<PaymentMethod>> GetActivePaymentMethodsAsync()
    {
        return await _context.PaymentMethods
            .Where(pm => pm.IsActive)
            .OrderBy(pm => pm.DisplayOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<PaymentMethod>> GetOrCreateDefaultPaymentMethodsAsync()
    {
        var existingMethods = await GetActivePaymentMethodsAsync();
        
        if (existingMethods.Any())
        {
            return existingMethods;
        }

        // Create default payment methods
        var defaultMethods = new List<PaymentMethod>
        {
            new PaymentMethod
            {
                Name = "Credit/Debit Card",
                Description = "Pay securely with your credit or debit card",
                ProviderId = "stripe",
                IconClass = "bi-credit-card",
                IsActive = true,
                DisplayOrder = 1
            },
            new PaymentMethod
            {
                Name = "PayPal",
                Description = "Pay with your PayPal account",
                ProviderId = "paypal",
                IconClass = "bi-paypal",
                IsActive = true,
                DisplayOrder = 2
            },
            new PaymentMethod
            {
                Name = "Cash on Delivery",
                Description = "Pay when you receive your order",
                ProviderId = "cash_on_delivery",
                IconClass = "bi-cash",
                IsActive = true,
                DisplayOrder = 3
            }
        };

        _context.PaymentMethods.AddRange(defaultMethods);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created default payment methods");

        return defaultMethods;
    }

    /// <inheritdoc />
    public async Task<PaymentMethod?> GetPaymentMethodByIdAsync(int id)
    {
        return await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Id == id);
    }

    /// <inheritdoc />
    public async Task<PaymentTransaction> CreatePaymentTransactionAsync(int orderId, int paymentMethodId, decimal amount)
    {
        var paymentMethod = await GetPaymentMethodByIdAsync(paymentMethodId);
        
        if (paymentMethod == null)
        {
            throw new InvalidOperationException("Payment method not found.");
        }

        var transaction = new PaymentTransaction
        {
            OrderId = orderId,
            PaymentMethodId = paymentMethodId,
            Amount = amount,
            CurrencyCode = "USD",
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created payment transaction {TransactionId} for order {OrderId}", transaction.Id, orderId);

        return transaction;
    }

    /// <inheritdoc />
    public async Task<string?> InitiatePaymentAsync(int transactionId)
    {
        var transaction = await _context.PaymentTransactions
            .Include(t => t.PaymentMethod)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            throw new InvalidOperationException("Payment transaction not found.");
        }

        // For cash on delivery, mark as authorized immediately
        if (transaction.PaymentMethod.ProviderId == "cash_on_delivery")
        {
            transaction.Status = PaymentStatus.Authorized;
            transaction.ProviderTransactionId = $"COD-{transaction.OrderId}-{transaction.Id}";
            transaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment transaction {TransactionId} authorized (cash on delivery)", transactionId);
            return null; // No redirect needed for cash on delivery
        }

        // For other payment providers, generate a redirect URL
        // In a real implementation, this would integrate with actual payment providers
        // For now, we'll simulate with a mock payment page
        var redirectUrl = $"/Checkout/PaymentAuthorize?transactionId={transactionId}";
        
        _logger.LogInformation("Initiated payment for transaction {TransactionId} with provider {ProviderId}", 
            transactionId, transaction.PaymentMethod.ProviderId);

        return redirectUrl;
    }

    /// <inheritdoc />
    public async Task<PaymentTransaction> HandlePaymentCallbackAsync(int transactionId, bool success, string? providerTransactionId, string? errorMessage)
    {
        var transaction = await _context.PaymentTransactions
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            throw new InvalidOperationException("Payment transaction not found.");
        }

        if (success)
        {
            transaction.Status = PaymentStatus.Completed;
            transaction.ProviderTransactionId = providerTransactionId;
            transaction.CompletedAt = DateTime.UtcNow;
            
            // Update order payment status
            if (transaction.Order != null)
            {
                transaction.Order.PaymentStatus = PaymentStatus.Completed;
            }

            _logger.LogInformation("Payment transaction {TransactionId} completed successfully", transactionId);
        }
        else
        {
            transaction.Status = PaymentStatus.Failed;
            transaction.ErrorMessage = errorMessage;
            
            _logger.LogWarning("Payment transaction {TransactionId} failed: {ErrorMessage}", transactionId, errorMessage);
        }

        transaction.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return transaction;
    }

    /// <inheritdoc />
    public async Task<PaymentTransaction?> GetPaymentTransactionByIdAsync(int id)
    {
        return await _context.PaymentTransactions
            .Include(t => t.PaymentMethod)
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
