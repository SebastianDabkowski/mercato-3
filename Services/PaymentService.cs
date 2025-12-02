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
    private readonly IOrderStatusService _orderStatusService;
    private readonly IPaymentProviderService _paymentProviderService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext context,
        IOrderStatusService orderStatusService,
        IPaymentProviderService paymentProviderService,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _orderStatusService = orderStatusService;
        _paymentProviderService = paymentProviderService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<PaymentMethod>> GetActivePaymentMethodsAsync()
    {
        var allMethods = await _context.PaymentMethods
            .Where(pm => pm.IsActive)
            .OrderBy(pm => pm.DisplayOrder)
            .ToListAsync();

        // Filter by enabled methods in current environment
        return allMethods
            .Where(pm => _paymentProviderService.IsPaymentMethodEnabled(pm.ProviderId))
            .ToList();
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
                ProviderId = "card",
                IconClass = "bi-credit-card",
                IsActive = true,
                DisplayOrder = 1
            },
            new PaymentMethod
            {
                Name = "Bank Transfer",
                Description = "Pay directly from your bank account",
                ProviderId = "bank_transfer",
                IconClass = "bi-bank",
                IsActive = true,
                DisplayOrder = 2
            },
            new PaymentMethod
            {
                Name = "BLIK",
                Description = "Pay with BLIK code (Poland only)",
                ProviderId = "blik",
                IconClass = "bi-phone",
                IsActive = true,
                DisplayOrder = 3
            },
            new PaymentMethod
            {
                Name = "Cash on Delivery",
                Description = "Pay when you receive your order",
                ProviderId = "cash_on_delivery",
                IconClass = "bi-cash",
                IsActive = true,
                DisplayOrder = 4
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

        // Generate idempotency key to prevent duplicate transactions
        var idempotencyKey = $"order-{orderId}-{Guid.NewGuid():N}";

        var transaction = new PaymentTransaction
        {
            OrderId = orderId,
            PaymentMethodId = paymentMethodId,
            Amount = amount,
            CurrencyCode = "USD",
            Status = PaymentStatus.Pending,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created payment transaction {TransactionId} for order {OrderId} with idempotency key {IdempotencyKey}", 
            transaction.Id, orderId, idempotencyKey);

        return transaction;
    }

    /// <inheritdoc />
    public async Task<string?> InitiatePaymentAsync(int transactionId)
    {
        var transaction = await _context.PaymentTransactions
            .Include(t => t.PaymentMethod)
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            throw new InvalidOperationException("Payment transaction not found.");
        }

        // Use payment provider to initiate payment
        var result = await _paymentProviderService.InitiatePaymentAsync(transaction);

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            // Payment initiation failed
            transaction.Status = PaymentStatus.Failed;
            transaction.ErrorMessage = result.ErrorMessage;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            throw new InvalidOperationException(result.ErrorMessage);
        }

        // Update transaction with provider ID if available
        if (!string.IsNullOrEmpty(result.ProviderTransactionId))
        {
            transaction.ProviderTransactionId = result.ProviderTransactionId;
        }

        // For immediate payments (e.g., cash on delivery)
        if (result.IsImmediate)
        {
            transaction.Status = PaymentStatus.Authorized;
            transaction.UpdatedAt = DateTime.UtcNow;
            
            // Update order status to Paid for immediate payments
            if (transaction.Order != null && transaction.Order.Status == OrderStatus.New)
            {
                transaction.Order.PaymentStatus = PaymentStatus.Authorized;
                transaction.Order.UpdatedAt = DateTime.UtcNow;
                
                var paymentSuccess = await _orderStatusService.MarkOrderAsPaidAsync(transaction.Order.Id);
                if (!paymentSuccess)
                {
                    _logger.LogError("Failed to mark order {OrderId} as paid", transaction.Order.Id);
                    throw new InvalidOperationException("Failed to update order status to paid.");
                }
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Payment transaction {TransactionId} authorized immediately", transactionId);
            return null; // No redirect needed
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Initiated payment for transaction {TransactionId} with provider {ProviderId}", 
            transactionId, transaction.PaymentMethod.ProviderId);

        return result.RedirectUrl;
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

        // Prevent duplicate processing (idempotency check)
        if (transaction.Status == PaymentStatus.Completed || transaction.Status == PaymentStatus.Failed)
        {
            _logger.LogWarning("Payment transaction {TransactionId} already processed with status {Status}",
                transactionId, transaction.Status);
            return transaction;
        }

        if (success)
        {
            transaction.Status = PaymentStatus.Completed;
            transaction.ProviderTransactionId = providerTransactionId;
            transaction.CompletedAt = DateTime.UtcNow;
            
            // Update order payment status and order status
            if (transaction.Order != null)
            {
                transaction.Order.PaymentStatus = PaymentStatus.Completed;
                
                // Mark the order and sub-orders as Paid when payment is confirmed
                if (transaction.Order.Status == OrderStatus.New)
                {
                    transaction.Order.UpdatedAt = DateTime.UtcNow;
                    var paymentSuccess = await _orderStatusService.MarkOrderAsPaidAsync(transaction.Order.Id);
                    if (!paymentSuccess)
                    {
                        _logger.LogError("Failed to mark order {OrderId} as paid", transaction.Order.Id);
                        throw new InvalidOperationException("Failed to update order status to paid.");
                    }
                }
            }

            _logger.LogInformation("Payment transaction {TransactionId} completed successfully", transactionId);
        }
        else
        {
            transaction.Status = PaymentStatus.Failed;
            transaction.ErrorMessage = errorMessage;
            
            // Update order payment status to failed
            if (transaction.Order != null)
            {
                transaction.Order.PaymentStatus = PaymentStatus.Failed;
                transaction.Order.UpdatedAt = DateTime.UtcNow;
                
                // Don't change order status to failed automatically - keep it as New
                // so the user can retry payment with a different method
                _logger.LogInformation("Order {OrderId} payment failed, order remains in New status for retry", 
                    transaction.Order.Id);
            }
            
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
