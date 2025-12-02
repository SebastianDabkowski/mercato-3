using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Configuration for commission invoice generation and tax handling.
/// </summary>
public class CommissionInvoiceConfig
{
    /// <summary>
    /// Gets or sets the unique identifier for the configuration.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the default tax percentage for commission invoices.
    /// </summary>
    [Required]
    [Range(0, 100)]
    public decimal DefaultTaxPercentage { get; set; }

    /// <summary>
    /// Gets or sets the invoice due days (days after issue date).
    /// </summary>
    [Required]
    [Range(1, 365)]
    public int InvoiceDueDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the day of month to generate invoices (1-28).
    /// </summary>
    [Required]
    [Range(1, 28)]
    public int GenerationDayOfMonth { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether auto-generation is enabled.
    /// </summary>
    public bool AutoGenerateEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the platform/company name for invoices.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the platform/company address.
    /// </summary>
    [MaxLength(500)]
    public string? CompanyAddress { get; set; }

    /// <summary>
    /// Gets or sets the platform/company tax ID.
    /// </summary>
    [MaxLength(100)]
    public string? CompanyTaxId { get; set; }

    /// <summary>
    /// Gets or sets additional terms and conditions for invoices.
    /// </summary>
    [MaxLength(2000)]
    public string? TermsAndConditions { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the configuration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
