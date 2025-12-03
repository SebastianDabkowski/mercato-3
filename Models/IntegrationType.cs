namespace MercatoApp.Models;

/// <summary>
/// Types of external integrations supported by the platform.
/// </summary>
public enum IntegrationType
{
    /// <summary>
    /// Payment gateway integration (e.g., Stripe, PayPal).
    /// </summary>
    Payment = 1,

    /// <summary>
    /// Shipping/logistics provider integration.
    /// </summary>
    Shipping = 2,

    /// <summary>
    /// Enterprise Resource Planning (ERP) system integration.
    /// </summary>
    ERP = 3,

    /// <summary>
    /// E-commerce platform connector.
    /// </summary>
    ECommerce = 4,

    /// <summary>
    /// Email service provider integration.
    /// </summary>
    EmailService = 5,

    /// <summary>
    /// SMS/messaging service integration.
    /// </summary>
    MessagingService = 6,

    /// <summary>
    /// Analytics and reporting service.
    /// </summary>
    Analytics = 7,

    /// <summary>
    /// Other custom integrations.
    /// </summary>
    Other = 99
}
