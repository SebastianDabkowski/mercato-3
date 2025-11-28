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
}
