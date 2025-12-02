using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Interface for email sending service.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email verification link to the user.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="verificationToken">The verification token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendVerificationEmailAsync(string email, string verificationToken);

    /// <summary>
    /// Resends an email verification link to the user.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="verificationToken">The verification token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResendVerificationEmailAsync(string email, string verificationToken);

    /// <summary>
    /// Sends a buyer registration confirmation email after successful registration.
    /// </summary>
    /// <param name="user">The registered buyer user.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendBuyerRegistrationConfirmationEmailAsync(User user);

    /// <summary>
    /// Sends a password reset link to the user.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="resetToken">The password reset token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendPasswordResetEmailAsync(string email, string resetToken);

    /// <summary>
    /// Sends a store user invitation email.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="storeName">The name of the store they're being invited to.</param>
    /// <param name="invitedByName">The name of the person who sent the invitation.</param>
    /// <param name="roleName">The role they will be assigned.</param>
    /// <param name="invitationToken">The invitation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendStoreInvitationEmailAsync(string email, string storeName, string invitedByName, string roleName, string invitationToken);

    /// <summary>
    /// Sends an order confirmation email to the buyer.
    /// </summary>
    /// <param name="order">The order to send confirmation for.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendOrderConfirmationEmailAsync(Order order);

    /// <summary>
    /// Sends a shipping status update email to the buyer.
    /// </summary>
    /// <param name="subOrder">The sub-order that was updated.</param>
    /// <param name="parentOrder">The parent order.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendShippingStatusUpdateEmailAsync(SellerSubOrder subOrder, Order parentOrder);

    /// <summary>
    /// Sends a refund confirmation email to the buyer.
    /// </summary>
    /// <param name="refundTransaction">The refund transaction.</param>
    /// <param name="order">The related order.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendRefundConfirmationEmailAsync(RefundTransaction refundTransaction, Order order);
}

/// <summary>
/// Email service implementation (stub for now, logs to console and database).
/// </summary>
public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailService> _logger;

    public EmailService(ApplicationDbContext context, ILogger<EmailService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendVerificationEmailAsync(string email, string verificationToken)
    {
        var subject = "Verify your MercatoApp account";
        
        // In production, this would send an actual email
        // For now, just log it
        _logger.LogInformation(
            "Verification email would be sent to {Email} with token {Token}",
            email,
            verificationToken);

        await LogEmailAsync(
            EmailType.RegistrationVerification,
            email,
            subject,
            EmailStatus.Sent,
            userId: null,
            orderId: null);
    }

    /// <inheritdoc />
    public async Task ResendVerificationEmailAsync(string email, string verificationToken)
    {
        var subject = "Verify your MercatoApp account";
        
        // In production, this would send an actual email
        // For now, just log it
        _logger.LogInformation(
            "Verification email resent to {Email} with token {Token}",
            email,
            verificationToken);

        await LogEmailAsync(
            EmailType.RegistrationVerification,
            email,
            subject,
            EmailStatus.Sent,
            userId: null,
            orderId: null);
    }

    /// <inheritdoc />
    public async Task SendBuyerRegistrationConfirmationEmailAsync(User user)
    {
        var subject = "Welcome to MercatoApp - Registration Successful";
        
        // In production, this would send an actual email with welcome message
        // For now, just log it
        _logger.LogInformation(
            "Buyer registration confirmation email would be sent to {Email} for user {UserName}",
            user.Email,
            $"{user.FirstName} {user.LastName}");

        await LogEmailAsync(
            EmailType.BuyerRegistrationConfirmation,
            user.Email,
            subject,
            EmailStatus.Sent,
            userId: user.Id,
            orderId: null);
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        var subject = "Reset your MercatoApp password";
        
        // In production, this would send an actual email
        // For now, just log it
        _logger.LogInformation(
            "Password reset email would be sent to {Email} with token {Token}",
            email,
            resetToken);

        await LogEmailAsync(
            EmailType.PasswordReset,
            email,
            subject,
            EmailStatus.Sent,
            userId: null,
            orderId: null);
    }

    /// <inheritdoc />
    public async Task SendStoreInvitationEmailAsync(string email, string storeName, string invitedByName, string roleName, string invitationToken)
    {
        var subject = $"You've been invited to join {storeName} on MercatoApp";
        
        // In production, this would send an actual email
        // For now, just log it
        _logger.LogInformation(
            "Store invitation email would be sent to {Email} for store '{StoreName}' by {InvitedBy} with role '{Role}' and token {Token}",
            email,
            storeName,
            invitedByName,
            roleName,
            invitationToken);

        await LogEmailAsync(
            EmailType.StoreInvitation,
            email,
            subject,
            EmailStatus.Sent,
            userId: null,
            orderId: null);
    }

    /// <inheritdoc />
    public async Task SendOrderConfirmationEmailAsync(Order order)
    {
        var recipientEmail = order.GuestEmail ?? order.User?.Email ?? "unknown";
        var subject = $"Order Confirmation - {order.OrderNumber}";
        
        // In production, this would send an actual email with order details
        // For now, just log it
        _logger.LogInformation(
            "Order confirmation email would be sent to {Email} for order {OrderNumber} with total {TotalAmount:C}. " +
            "Items: {ItemCount}, Delivery Address: {Address}",
            recipientEmail,
            order.OrderNumber,
            order.TotalAmount,
            order.Items?.Count ?? 0,
            order.DeliveryAddress?.AddressLine1 ?? "N/A");

        await LogEmailAsync(
            EmailType.OrderConfirmation,
            recipientEmail,
            subject,
            EmailStatus.Sent,
            userId: order.UserId,
            orderId: order.Id);
    }

    /// <inheritdoc />
    public async Task SendShippingStatusUpdateEmailAsync(SellerSubOrder subOrder, Order parentOrder)
    {
        var recipientEmail = parentOrder.GuestEmail ?? parentOrder.User?.Email ?? "unknown";
        
        var statusMessage = subOrder.Status switch
        {
            OrderStatus.Preparing => "is being prepared",
            OrderStatus.Shipped => "has been shipped",
            OrderStatus.Delivered => "has been delivered",
            _ => $"status has been updated to {subOrder.Status}"
        };

        var subject = $"Shipping Update - Order {parentOrder.OrderNumber}";
        
        var trackingInfo = !string.IsNullOrEmpty(subOrder.TrackingNumber)
            ? $"Tracking Number: {subOrder.TrackingNumber}" +
              (!string.IsNullOrEmpty(subOrder.CarrierName) ? $" via {subOrder.CarrierName}" : "") +
              (!string.IsNullOrEmpty(subOrder.TrackingUrl) ? $", Tracking URL: {subOrder.TrackingUrl}" : "")
            : "No tracking information available";
        
        // In production, this would send an actual email with shipping status and tracking info
        // For now, just log it
        _logger.LogInformation(
            "Shipping status update email would be sent to {Email} for order {OrderNumber}, sub-order {SubOrderNumber}. " +
            "Status: {Status}. Store: {StoreName}. {TrackingInfo}",
            recipientEmail,
            parentOrder.OrderNumber,
            subOrder.SubOrderNumber,
            statusMessage,
            subOrder.Store?.StoreName ?? "Unknown Store",
            trackingInfo);

        await LogEmailAsync(
            EmailType.ShippingStatusUpdate,
            recipientEmail,
            subject,
            EmailStatus.Sent,
            userId: parentOrder.UserId,
            orderId: parentOrder.Id,
            sellerSubOrderId: subOrder.Id);
    }

    /// <inheritdoc />
    public async Task SendRefundConfirmationEmailAsync(RefundTransaction refundTransaction, Order order)
    {
        var recipientEmail = order.GuestEmail ?? order.User?.Email ?? "unknown";
        var subject = $"Refund Confirmation - {refundTransaction.RefundNumber}";
        
        var refundTypeMessage = refundTransaction.RefundType switch
        {
            RefundType.Full => "full refund",
            RefundType.Partial => "partial refund",
            _ => "refund"
        };

        // In production, this would send an actual email with refund details
        // For now, just log it
        _logger.LogInformation(
            "Refund confirmation email would be sent to {Email} for order {OrderNumber}. " +
            "Refund: {RefundNumber}, Type: {RefundType}, Amount: {Amount:C}, Status: {Status}, Reason: {Reason}",
            recipientEmail,
            order.OrderNumber,
            refundTransaction.RefundNumber,
            refundTypeMessage,
            refundTransaction.RefundAmount,
            refundTransaction.Status,
            refundTransaction.Reason);

        await LogEmailAsync(
            EmailType.RefundConfirmation,
            recipientEmail,
            subject,
            EmailStatus.Sent,
            userId: order.UserId,
            orderId: order.Id,
            refundTransactionId: refundTransaction.Id);
    }

    /// <summary>
    /// Logs an email send attempt to the database.
    /// </summary>
    private async Task LogEmailAsync(
        EmailType emailType,
        string recipientEmail,
        string subject,
        EmailStatus status,
        int? userId = null,
        int? orderId = null,
        int? refundTransactionId = null,
        int? sellerSubOrderId = null,
        string? errorMessage = null,
        string? providerMessageId = null)
    {
        try
        {
            var emailLog = new EmailLog
            {
                EmailType = emailType,
                RecipientEmail = recipientEmail,
                UserId = userId,
                OrderId = orderId,
                RefundTransactionId = refundTransactionId,
                SellerSubOrderId = sellerSubOrderId,
                Subject = subject,
                Status = status,
                ErrorMessage = errorMessage,
                ProviderMessageId = providerMessageId,
                CreatedAt = DateTime.UtcNow,
                SentAt = status == EmailStatus.Sent ? DateTime.UtcNow : null,
                FailedAt = status == EmailStatus.Failed ? DateTime.UtcNow : null
            };

            _context.EmailLogs.Add(emailLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Don't let email logging failures break the application
            _logger.LogError(ex, "Failed to log email send attempt for {EmailType} to {Email}", emailType, recipientEmail);
        }
    }
}
