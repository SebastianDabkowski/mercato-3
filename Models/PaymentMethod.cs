using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a payment method available on the platform.
/// Payment methods are configured centrally by the marketplace admin.
/// </summary>
public class PaymentMethod
{
    /// <summary>
    /// Gets or sets the unique identifier for the payment method.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the payment method (e.g., "Credit Card", "PayPal").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the payment method.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the payment provider identifier (e.g., "stripe", "paypal", "cash_on_delivery").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon CSS class for the payment method (e.g., "bi-credit-card").
    /// </summary>
    [MaxLength(50)]
    public string? IconClass { get; set; }

    /// <summary>
    /// Gets or sets whether this payment method is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the display order for this payment method.
    /// Lower values are displayed first.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Gets or sets the date and time when the method was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the method was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
