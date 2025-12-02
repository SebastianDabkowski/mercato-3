using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a commission invoice issued to a seller for monthly platform commissions.
/// Provides legally compliant financial documents with proper numbering and tax handling.
/// </summary>
public class CommissionInvoice
{
    /// <summary>
    /// Gets or sets the unique identifier for the commission invoice.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the invoice number (unique, sequential, legally compliant).
    /// Format: INV-{Year}-{SequentialNumber:D6}
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID this invoice is issued to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the invoice period start date (inclusive).
    /// </summary>
    public DateTime PeriodStartDate { get; set; }

    /// <summary>
    /// Gets or sets the invoice period end date (inclusive).
    /// </summary>
    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// Gets or sets the invoice issue date.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the invoice due date.
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Gets or sets the subtotal amount (before tax).
    /// </summary>
    [Required]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Gets or sets the tax percentage applied.
    /// </summary>
    [Range(0, 100)]
    public decimal TaxPercentage { get; set; }

    /// <summary>
    /// Gets or sets the tax amount.
    /// </summary>
    [Required]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the total amount (subtotal + tax).
    /// </summary>
    [Required]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the currency (ISO 4217 code).
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the current status of the invoice.
    /// </summary>
    public CommissionInvoiceStatus Status { get; set; } = CommissionInvoiceStatus.Draft;

    /// <summary>
    /// Gets or sets the ID of the invoice this one corrects (for credit notes).
    /// </summary>
    public int? CorrectingInvoiceId { get; set; }

    /// <summary>
    /// Gets or sets the invoice being corrected (navigation property).
    /// </summary>
    public CommissionInvoice? CorrectingInvoice { get; set; }

    /// <summary>
    /// Gets or sets whether this is a credit note.
    /// </summary>
    public bool IsCreditNote { get; set; }

    /// <summary>
    /// Gets or sets notes about the invoice.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the invoice items (line items).
    /// </summary>
    public ICollection<CommissionInvoiceItem> Items { get; set; } = new List<CommissionInvoiceItem>();

    /// <summary>
    /// Gets or sets the date and time when the invoice was paid.
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the invoice was cancelled.
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the invoice was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the invoice was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
