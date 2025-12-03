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

    /// <summary>
    /// Sends a new order notification email to the seller.
    /// </summary>
    /// <param name="subOrder">The seller sub-order that was created.</param>
    /// <param name="parentOrder">The parent order.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendNewOrderNotificationToSellerAsync(SellerSubOrder subOrder, Order parentOrder);

    /// <summary>
    /// Sends a return request notification email to the seller.
    /// </summary>
    /// <param name="returnRequest">The return request that was created.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendReturnRequestNotificationToSellerAsync(ReturnRequest returnRequest);

    /// <summary>
    /// Sends a payout notification email to the seller.
    /// </summary>
    /// <param name="payout">The payout that was processed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendPayoutNotificationToSellerAsync(Payout payout);

    /// <summary>
    /// Sends a product moderation notification email to the seller.
    /// </summary>
    /// <param name="product">The product that was moderated.</param>
    /// <param name="newStatus">The new moderation status.</param>
    /// <param name="reason">The reason for the moderation decision.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendProductModerationNotificationToSellerAsync(Product product, ProductModerationStatus newStatus, string? reason);

    /// <summary>
    /// Sends a photo removal notification email to the seller.
    /// </summary>
    /// <param name="photo">The product image that was removed.</param>
    /// <param name="reason">The reason for the removal.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendPhotoRemovedNotificationToSellerAsync(ProductImage photo, string reason);

    /// <summary>
    /// Sends a newsletter email to users who have consented to receive newsletters.
    /// Automatically checks for active newsletter consent before sending.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="subject">The newsletter subject.</param>
    /// <param name="body">The newsletter body content.</param>
    /// <returns>True if email was sent, false if user doesn't have consent.</returns>
    Task<bool> SendNewsletterEmailAsync(int userId, string subject, string body);

    /// <summary>
    /// Sends a marketing email to users who have consented to receive marketing communications.
    /// Automatically checks for active marketing consent before sending.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="subject">The marketing email subject.</param>
    /// <param name="body">The marketing email body content.</param>
    /// <returns>True if email was sent, false if user doesn't have consent.</returns>
    Task<bool> SendMarketingEmailAsync(int userId, string subject, string body);
}

/// <summary>
/// Email service implementation (stub for now, logs to console and database).
/// </summary>
public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly IConsentManagementService _consentService;

    public EmailService(
        ApplicationDbContext context, 
        ILogger<EmailService> logger,
        IConsentManagementService consentService)
    {
        _context = context;
        _logger = logger;
        _consentService = consentService;
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
        int? returnRequestId = null,
        int? payoutId = null,
        int? productId = null,
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
                ReturnRequestId = returnRequestId,
                PayoutId = payoutId,
                ProductId = productId,
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

    /// <inheritdoc />
    public async Task SendNewOrderNotificationToSellerAsync(SellerSubOrder subOrder, Order parentOrder)
    {
        // Get seller email from store owner
        var store = subOrder.Store;
        var sellerEmail = store.ContactEmail ?? store.User?.Email;

        if (string.IsNullOrEmpty(sellerEmail))
        {
            _logger.LogWarning(
                "Cannot send new order notification for sub-order {SubOrderNumber}: No email address found for store {StoreId}",
                subOrder.SubOrderNumber,
                subOrder.StoreId);
            return;
        }

        var subject = $"New Order - {subOrder.SubOrderNumber}";

        // In production, this would send an actual email with order details
        // For now, just log it
        _logger.LogInformation(
            "New order notification would be sent to {Email} for sub-order {SubOrderNumber}. " +
            "Store: {StoreName}, Amount: {Amount:C}, Items: {ItemCount}, " +
            "Buyer Order: {ParentOrderNumber}, Delivery Address: {Address}",
            sellerEmail,
            subOrder.SubOrderNumber,
            store.StoreName,
            subOrder.TotalAmount,
            subOrder.Items?.Count ?? 0,
            parentOrder.OrderNumber,
            parentOrder.DeliveryAddress?.AddressLine1 ?? "N/A");

        await LogEmailAsync(
            EmailType.SellerNewOrder,
            sellerEmail,
            subject,
            EmailStatus.Sent,
            userId: store.UserId,
            orderId: parentOrder.Id,
            sellerSubOrderId: subOrder.Id);
    }

    /// <inheritdoc />
    public async Task SendReturnRequestNotificationToSellerAsync(ReturnRequest returnRequest)
    {
        // Get seller email from store owner
        var store = returnRequest.SubOrder.Store;
        var sellerEmail = store.ContactEmail ?? store.User?.Email;

        if (string.IsNullOrEmpty(sellerEmail))
        {
            _logger.LogWarning(
                "Cannot send return request notification for return {ReturnNumber}: No email address found for store {StoreId}",
                returnRequest.ReturnNumber,
                returnRequest.SubOrder.StoreId);
            return;
        }

        var requestTypeLabel = returnRequest.RequestType == ReturnRequestType.Return ? "Return Request" : "Complaint";
        var subject = $"{requestTypeLabel} - {returnRequest.ReturnNumber}";

        // In production, this would send an actual email with return details
        // For now, just log it
        _logger.LogInformation(
            "{RequestType} notification would be sent to {Email} for return {ReturnNumber}. " +
            "Store: {StoreName}, Sub-Order: {SubOrderNumber}, Reason: {Reason}, " +
            "Buyer: {BuyerName}, Requested At: {RequestedAt:g}",
            requestTypeLabel,
            sellerEmail,
            returnRequest.ReturnNumber,
            store.StoreName,
            returnRequest.SubOrder.SubOrderNumber,
            returnRequest.Reason,
            $"{returnRequest.Buyer.FirstName} {returnRequest.Buyer.LastName}",
            returnRequest.RequestedAt);

        await LogEmailAsync(
            EmailType.SellerReturnRequest,
            sellerEmail,
            subject,
            EmailStatus.Sent,
            userId: store.UserId,
            sellerSubOrderId: returnRequest.SubOrderId,
            returnRequestId: returnRequest.Id);
    }

    /// <inheritdoc />
    public async Task SendPayoutNotificationToSellerAsync(Payout payout)
    {
        // Get seller email from store owner
        var store = payout.Store;
        var sellerEmail = store.ContactEmail ?? store.User?.Email;

        if (string.IsNullOrEmpty(sellerEmail))
        {
            _logger.LogWarning(
                "Cannot send payout notification for payout {PayoutNumber}: No email address found for store {StoreId}",
                payout.PayoutNumber,
                payout.StoreId);
            return;
        }

        var subject = $"Payout Processed - {payout.PayoutNumber}";

        var statusMessage = payout.Status switch
        {
            PayoutStatus.Paid => "has been completed",
            PayoutStatus.Processing => "is being processed",
            PayoutStatus.Failed => "has failed",
            _ => $"status is {payout.Status}"
        };

        // In production, this would send an actual email with payout details
        // For now, just log it
        _logger.LogInformation(
            "Payout notification would be sent to {Email} for payout {PayoutNumber}. " +
            "Store: {StoreName}, Amount: {Amount:C} {Currency}, Status: {Status}, " +
            "Scheduled Date: {ScheduledDate:d}, Method: {PayoutMethod}",
            sellerEmail,
            payout.PayoutNumber,
            store.StoreName,
            payout.Amount,
            payout.Currency,
            statusMessage,
            payout.ScheduledDate,
            payout.PayoutMethod?.DisplayName ?? "Default");

        await LogEmailAsync(
            EmailType.SellerPayout,
            sellerEmail,
            subject,
            EmailStatus.Sent,
            userId: store.UserId,
            payoutId: payout.Id);
    }

    /// <inheritdoc />
    public async Task SendProductModerationNotificationToSellerAsync(Product product, ProductModerationStatus newStatus, string? reason)
    {
        // Get seller email from store owner
        var sellerEmail = product.Store.ContactEmail ?? product.Store.User?.Email;

        if (string.IsNullOrEmpty(sellerEmail))
        {
            _logger.LogWarning(
                "Cannot send product moderation notification for product {ProductId}: No email address found for store {StoreId}",
                product.Id,
                product.StoreId);
            return;
        }

        var subject = newStatus == ProductModerationStatus.Approved
            ? $"Product Approved: {product.Title}"
            : $"Product Rejected: {product.Title}";

        var statusMessage = newStatus == ProductModerationStatus.Approved
            ? "has been approved"
            : "has been rejected";

        // In production, this would send an actual email with moderation details
        // For now, just log it
        _logger.LogInformation(
            "Product moderation notification would be sent to {Email} for product {ProductId}. " +
            "Title: {Title}, Store: {StoreName}, Status: {Status}, Reason: {Reason}",
            sellerEmail,
            product.Id,
            product.Title,
            product.Store.StoreName,
            statusMessage,
            reason ?? "No reason provided");

        await LogEmailAsync(
            EmailType.SellerProductModeration,
            sellerEmail,
            subject,
            EmailStatus.Sent,
            userId: product.Store.UserId,
            productId: product.Id);
    }

    /// <inheritdoc />
    public async Task SendPhotoRemovedNotificationToSellerAsync(ProductImage photo, string reason)
    {
        // Get seller email from store owner
        var sellerEmail = photo.Product.Store.ContactEmail ?? photo.Product.Store.User?.Email;

        if (string.IsNullOrEmpty(sellerEmail))
        {
            _logger.LogWarning(
                "Cannot send photo removal notification for photo {PhotoId}: No email address found for store {StoreId}",
                photo.Id,
                photo.Product.StoreId);
            return;
        }

        var subject = $"Product Photo Removed: {photo.Product.Title}";

        // In production, this would send an actual email with removal details
        // For now, just log it
        _logger.LogInformation(
            "Photo removal notification would be sent to {Email} for photo {PhotoId} of product {ProductId}. " +
            "Product: {ProductTitle}, Store: {StoreName}, Reason: {Reason}",
            sellerEmail,
            photo.Id,
            photo.ProductId,
            photo.Product.Title,
            photo.Product.Store.StoreName,
            reason);

        await LogEmailAsync(
            EmailType.SellerPhotoRemoval,
            sellerEmail,
            subject,
            EmailStatus.Sent,
            userId: photo.Product.Store.UserId,
            productId: photo.ProductId);
    }

    /// <inheritdoc />
    public async Task<bool> SendNewsletterEmailAsync(int userId, string subject, string body)
    {
        // Check if user has active consent for newsletters
        var hasConsent = await _consentService.IsEligibleForCommunicationAsync(userId, ConsentType.Newsletter);
        
        if (!hasConsent)
        {
            _logger.LogInformation(
                "Newsletter email not sent to user {UserId} - no active consent",
                userId);
            return false;
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return false;
        }

        _logger.LogInformation(
            "Sending newsletter email to {Email}: {Subject}",
            user.Email,
            subject);

        await LogEmailAsync(
            EmailType.Newsletter,
            user.Email,
            subject,
            EmailStatus.Sent,
            userId: userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SendMarketingEmailAsync(int userId, string subject, string body)
    {
        // Check if user has active consent for marketing communications
        var hasConsent = await _consentService.IsEligibleForCommunicationAsync(userId, ConsentType.Marketing);
        
        if (!hasConsent)
        {
            _logger.LogInformation(
                "Marketing email not sent to user {UserId} - no active consent",
                userId);
            return false;
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return false;
        }

        _logger.LogInformation(
            "Sending marketing email to {Email}: {Subject}",
            user.Email,
            subject);

        await LogEmailAsync(
            EmailType.Marketing,
            user.Email,
            subject,
            EmailStatus.Sent,
            userId: userId);

        return true;
    }
}
