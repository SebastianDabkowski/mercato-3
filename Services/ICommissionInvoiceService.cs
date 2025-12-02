using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for commission invoice generation and management service.
/// </summary>
public interface ICommissionInvoiceService
{
    /// <summary>
    /// Generates commission invoices for all stores for a given month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <returns>The number of invoices generated.</returns>
    Task<int> GenerateMonthlyInvoicesAsync(int year, int month);

    /// <summary>
    /// Generates a commission invoice for a specific store and period.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="periodStartDate">The period start date.</param>
    /// <param name="periodEndDate">The period end date.</param>
    /// <returns>The generated invoice or null if no commissions found.</returns>
    Task<CommissionInvoice?> GenerateInvoiceAsync(int storeId, DateTime periodStartDate, DateTime periodEndDate);

    /// <summary>
    /// Gets an invoice by ID.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <returns>The invoice or null if not found.</returns>
    Task<CommissionInvoice?> GetInvoiceAsync(int invoiceId);

    /// <summary>
    /// Gets all invoices for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="includeSuperseded">Whether to include superseded invoices.</param>
    /// <returns>A list of invoices.</returns>
    Task<List<CommissionInvoice>> GetInvoicesAsync(int storeId, bool includeSuperseded = false);

    /// <summary>
    /// Issues an invoice (changes status from Draft to Issued).
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> IssueInvoiceAsync(int invoiceId);

    /// <summary>
    /// Marks an invoice as paid.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> MarkInvoiceAsPaidAsync(int invoiceId);

    /// <summary>
    /// Cancels an invoice.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> CancelInvoiceAsync(int invoiceId);

    /// <summary>
    /// Creates a credit note (correcting invoice) for an existing invoice.
    /// </summary>
    /// <param name="originalInvoiceId">The invoice to correct.</param>
    /// <param name="reason">The reason for the credit note.</param>
    /// <returns>The created credit note invoice.</returns>
    Task<CommissionInvoice> CreateCreditNoteAsync(int originalInvoiceId, string reason);

    /// <summary>
    /// Generates a PDF for an invoice.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <returns>The PDF as a byte array.</returns>
    Task<byte[]> GenerateInvoicePdfAsync(int invoiceId);

    /// <summary>
    /// Gets or creates the invoice configuration.
    /// </summary>
    /// <returns>The invoice configuration.</returns>
    Task<CommissionInvoiceConfig> GetOrCreateConfigAsync();

    /// <summary>
    /// Updates the invoice configuration.
    /// </summary>
    /// <param name="config">The configuration to update.</param>
    /// <returns>The updated configuration.</returns>
    Task<CommissionInvoiceConfig> UpdateConfigAsync(CommissionInvoiceConfig config);
}
