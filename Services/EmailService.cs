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
    Task SendOrderConfirmationEmailAsync(Models.Order order);
}

/// <summary>
/// Email service implementation (stub for now, logs to console).
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendVerificationEmailAsync(string email, string verificationToken)
    {
        // In production, this would send an actual email
        // For now, just log it
        _logger.LogInformation(
            "Verification email would be sent to {Email} with token {Token}",
            email,
            verificationToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResendVerificationEmailAsync(string email, string verificationToken)
    {
        // In production, this would send an actual email
        // For now, just log it
        _logger.LogInformation(
            "Verification email resent to {Email} with token {Token}",
            email,
            verificationToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        // In production, this would send an actual email
        // For now, just log it
        _logger.LogInformation(
            "Password reset email would be sent to {Email} with token {Token}",
            email,
            resetToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendStoreInvitationEmailAsync(string email, string storeName, string invitedByName, string roleName, string invitationToken)
    {
        // In production, this would send an actual email
        // For now, just log it
        _logger.LogInformation(
            "Store invitation email would be sent to {Email} for store '{StoreName}' by {InvitedBy} with role '{Role}' and token {Token}",
            email,
            storeName,
            invitedByName,
            roleName,
            invitationToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendOrderConfirmationEmailAsync(Models.Order order)
    {
        // In production, this would send an actual email with order details
        // For now, just log it
        var recipientEmail = order.GuestEmail ?? order.User?.Email ?? "unknown";
        
        _logger.LogInformation(
            "Order confirmation email would be sent to {Email} for order {OrderNumber} with total {TotalAmount:C}. " +
            "Items: {ItemCount}, Delivery Address: {Address}",
            recipientEmail,
            order.OrderNumber,
            order.TotalAmount,
            order.Items?.Count ?? 0,
            order.DeliveryAddress?.AddressLine1 ?? "N/A");

        return Task.CompletedTask;
    }
}
