using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a line item in a commission invoice.
/// Links to commission transactions for audit trail.
/// </summary>
public class CommissionInvoiceItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the invoice item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the commission invoice ID this item belongs to.
    /// </summary>
    public int CommissionInvoiceId { get; set; }

    /// <summary>
    /// Gets or sets the commission invoice (navigation property).
    /// </summary>
    public CommissionInvoice CommissionInvoice { get; set; } = null!;

    /// <summary>
    /// Gets or sets the commission transaction ID this item refers to.
    /// </summary>
    public int CommissionTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the commission transaction (navigation property).
    /// </summary>
    public CommissionTransaction CommissionTransaction { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description of the line item.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount for this line item.
    /// </summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the item was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
