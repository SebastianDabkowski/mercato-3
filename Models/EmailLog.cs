namespace MercatoApp.Models;

/// <summary>
/// Type of email notification sent to users.
/// </summary>
public enum EmailType
{
    /// <summary>
    /// Email verification for new account registration.
    /// </summary>
    RegistrationVerification,

    /// <summary>
    /// Buyer registration confirmation (after verification).
    /// </summary>
    BuyerRegistrationConfirmation,

    /// <summary>
    /// Order confirmation email sent to buyer after order placement.
    /// </summary>
    OrderConfirmation,

    /// <summary>
    /// Shipping status update (preparing, shipped, delivered).
    /// </summary>
    ShippingStatusUpdate,

    /// <summary>
    /// Refund confirmation email sent to buyer.
    /// </summary>
    RefundConfirmation,

    /// <summary>
    /// Password reset email.
    /// </summary>
    PasswordReset,

    /// <summary>
    /// Store invitation email.
    /// </summary>
    StoreInvitation,

    /// <summary>
    /// Other email types.
    /// </summary>
    Other
}

/// <summary>
/// Status of an email send attempt.
/// </summary>
public enum EmailStatus
{
    /// <summary>
    /// Email has been queued for sending.
    /// </summary>
    Pending,

    /// <summary>
    /// Email was sent successfully.
    /// </summary>
    Sent,

    /// <summary>
    /// Email send attempt failed.
    /// </summary>
    Failed
}

/// <summary>
/// Log entry for email notifications sent by the system.
/// Tracks all email send attempts and results for audit and debugging.
/// </summary>
public class EmailLog
{
    /// <summary>
    /// Unique identifier for the email log entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Type of email sent.
    /// </summary>
    public EmailType EmailType { get; set; }

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public required string RecipientEmail { get; set; }

    /// <summary>
    /// User ID of the recipient (if applicable).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Order ID (if email relates to an order).
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// Refund transaction ID (if email relates to a refund).
    /// </summary>
    public int? RefundTransactionId { get; set; }

    /// <summary>
    /// Sub-order ID (if email relates to a sub-order, e.g., shipping updates).
    /// </summary>
    public int? SellerSubOrderId { get; set; }

    /// <summary>
    /// Subject line of the email.
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Status of the email send attempt.
    /// </summary>
    public EmailStatus Status { get; set; }

    /// <summary>
    /// Error message if the email send failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// External provider message ID or tracking reference.
    /// </summary>
    public string? ProviderMessageId { get; set; }

    /// <summary>
    /// Number of send attempts for this email.
    /// Currently set to 1 on initial send. Future enhancement: increment on retry attempts.
    /// </summary>
    public int AttemptCount { get; set; } = 1;

    /// <summary>
    /// When the email was created/queued.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the email was sent (if successful).
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When the email send last failed (if failed).
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Navigation property to the order.
    /// </summary>
    public Order? Order { get; set; }

    /// <summary>
    /// Navigation property to the refund transaction.
    /// </summary>
    public RefundTransaction? RefundTransaction { get; set; }

    /// <summary>
    /// Navigation property to the seller sub-order.
    /// </summary>
    public SellerSubOrder? SellerSubOrder { get; set; }
}
