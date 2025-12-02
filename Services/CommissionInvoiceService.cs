using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace MercatoApp.Services;

/// <summary>
/// Service for commission invoice generation and management.
/// Provides legally compliant financial documents with proper numbering and tax handling.
/// </summary>
public class CommissionInvoiceService : ICommissionInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CommissionInvoiceService> _logger;
    private readonly ICommissionService _commissionService;

    public CommissionInvoiceService(
        ApplicationDbContext context,
        ILogger<CommissionInvoiceService> logger,
        ICommissionService commissionService)
    {
        _context = context;
        _logger = logger;
        _commissionService = commissionService;
    }

    /// <inheritdoc />
    public async Task<int> GenerateMonthlyInvoicesAsync(int year, int month)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentException("Month must be between 1 and 12", nameof(month));
        }

        var periodStartDate = new DateTime(year, month, 1);
        var periodEndDate = periodStartDate.AddMonths(1).AddDays(-1);

        _logger.LogInformation("Generating monthly commission invoices for {Year}-{Month:D2}", year, month);

        // Get all active stores
        var stores = await _context.Stores
            .Where(s => s.Status == StoreStatus.Active)
            .ToListAsync();

        int generatedCount = 0;

        foreach (var store in stores)
        {
            try
            {
                var invoice = await GenerateInvoiceAsync(store.Id, periodStartDate, periodEndDate);
                if (invoice != null)
                {
                    generatedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice for store {StoreId}", store.Id);
            }
        }

        _logger.LogInformation("Generated {Count} commission invoices for {Year}-{Month:D2}", generatedCount, year, month);
        return generatedCount;
    }

    /// <inheritdoc />
    public async Task<CommissionInvoice?> GenerateInvoiceAsync(int storeId, DateTime periodStartDate, DateTime periodEndDate)
    {
        _logger.LogInformation("Generating commission invoice for store {StoreId}, period {StartDate} to {EndDate}",
            storeId, periodStartDate, periodEndDate);

        // Check if invoice already exists for this period
        var existingInvoice = await _context.CommissionInvoices
            .Where(i => i.StoreId == storeId
                     && i.PeriodStartDate == periodStartDate
                     && i.PeriodEndDate == periodEndDate
                     && i.Status != CommissionInvoiceStatus.Cancelled
                     && i.Status != CommissionInvoiceStatus.Superseded)
            .FirstOrDefaultAsync();

        if (existingInvoice != null)
        {
            _logger.LogWarning("Invoice already exists for store {StoreId}, period {StartDate} to {EndDate}",
                storeId, periodStartDate, periodEndDate);
            return existingInvoice;
        }

        // Get commission transactions for the period
        var commissionTransactions = await _commissionService.GetCommissionTransactionsByStoreAsync(
            storeId,
            periodStartDate,
            periodEndDate.AddDays(1) // Include the entire end date
        );

        if (commissionTransactions.Count == 0)
        {
            _logger.LogInformation("No commission transactions found for store {StoreId} in period {StartDate} to {EndDate}",
                storeId, periodStartDate, periodEndDate);
            return null;
        }

        // Get configuration
        var config = await GetOrCreateConfigAsync();

        // Calculate subtotal
        var subtotal = commissionTransactions.Sum(ct => ct.CommissionAmount);

        // Calculate tax
        var taxAmount = Math.Round(subtotal * (config.DefaultTaxPercentage / 100m), 2);
        var totalAmount = subtotal + taxAmount;

        // Generate invoice number
        var invoiceNumber = await GenerateInvoiceNumberAsync(periodStartDate.Year);

        // Create invoice
        var invoice = new CommissionInvoice
        {
            InvoiceNumber = invoiceNumber,
            StoreId = storeId,
            PeriodStartDate = periodStartDate,
            PeriodEndDate = periodEndDate,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(config.InvoiceDueDays),
            Subtotal = subtotal,
            TaxPercentage = config.DefaultTaxPercentage,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            Currency = "USD",
            Status = CommissionInvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CommissionInvoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Create invoice items
        foreach (var transaction in commissionTransactions)
        {
            var description = $"Commission for {transaction.TransactionType}";
            if (transaction.Category != null)
            {
                description += $" - {transaction.Category.Name}";
            }
            description += $" (Transaction #{transaction.Id})";

            var item = new CommissionInvoiceItem
            {
                CommissionInvoiceId = invoice.Id,
                CommissionTransactionId = transaction.Id,
                Description = description,
                Amount = transaction.CommissionAmount,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommissionInvoiceItems.Add(item);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated invoice {InvoiceNumber} for store {StoreId}, total {TotalAmount:C}",
            invoiceNumber, storeId, totalAmount);

        return invoice;
    }

    /// <inheritdoc />
    public async Task<CommissionInvoice?> GetInvoiceAsync(int invoiceId)
    {
        return await _context.CommissionInvoices
            .Include(i => i.Store)
            .Include(i => i.Items)
                .ThenInclude(item => item.CommissionTransaction)
            .Include(i => i.CorrectingInvoice)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    /// <inheritdoc />
    public async Task<List<CommissionInvoice>> GetInvoicesAsync(int storeId, bool includeSuperseded = false)
    {
        var query = _context.CommissionInvoices
            .Include(i => i.Items)
            .Where(i => i.StoreId == storeId);

        if (!includeSuperseded)
        {
            query = query.Where(i => i.Status != CommissionInvoiceStatus.Superseded);
        }

        return await query
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IssueInvoiceAsync(int invoiceId)
    {
        var invoice = await _context.CommissionInvoices.FindAsync(invoiceId);
        if (invoice == null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found", invoiceId);
            return false;
        }

        if (invoice.Status != CommissionInvoiceStatus.Draft)
        {
            _logger.LogWarning("Invoice {InvoiceNumber} is not in Draft status", invoice.InvoiceNumber);
            return false;
        }

        invoice.Status = CommissionInvoiceStatus.Issued;
        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Issued invoice {InvoiceNumber}", invoice.InvoiceNumber);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkInvoiceAsPaidAsync(int invoiceId)
    {
        var invoice = await _context.CommissionInvoices.FindAsync(invoiceId);
        if (invoice == null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found", invoiceId);
            return false;
        }

        if (invoice.Status != CommissionInvoiceStatus.Issued)
        {
            _logger.LogWarning("Invoice {InvoiceNumber} is not in Issued status", invoice.InvoiceNumber);
            return false;
        }

        invoice.Status = CommissionInvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Marked invoice {InvoiceNumber} as paid", invoice.InvoiceNumber);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CancelInvoiceAsync(int invoiceId)
    {
        var invoice = await _context.CommissionInvoices.FindAsync(invoiceId);
        if (invoice == null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found", invoiceId);
            return false;
        }

        if (invoice.Status == CommissionInvoiceStatus.Paid)
        {
            _logger.LogWarning("Cannot cancel paid invoice {InvoiceNumber}", invoice.InvoiceNumber);
            return false;
        }

        invoice.Status = CommissionInvoiceStatus.Cancelled;
        invoice.CancelledAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cancelled invoice {InvoiceNumber}", invoice.InvoiceNumber);
        return true;
    }

    /// <inheritdoc />
    public async Task<CommissionInvoice> CreateCreditNoteAsync(int originalInvoiceId, string reason)
    {
        var originalInvoice = await GetInvoiceAsync(originalInvoiceId);
        if (originalInvoice == null)
        {
            throw new InvalidOperationException($"Invoice {originalInvoiceId} not found");
        }

        var config = await GetOrCreateConfigAsync();

        // Generate invoice number for credit note
        var invoiceNumber = await GenerateInvoiceNumberAsync(DateTime.UtcNow.Year);

        // Create credit note with negative amounts
        var creditNote = new CommissionInvoice
        {
            InvoiceNumber = invoiceNumber,
            StoreId = originalInvoice.StoreId,
            PeriodStartDate = originalInvoice.PeriodStartDate,
            PeriodEndDate = originalInvoice.PeriodEndDate,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(config.InvoiceDueDays),
            Subtotal = -originalInvoice.Subtotal,
            TaxPercentage = originalInvoice.TaxPercentage,
            TaxAmount = -originalInvoice.TaxAmount,
            TotalAmount = -originalInvoice.TotalAmount,
            Currency = originalInvoice.Currency,
            Status = CommissionInvoiceStatus.Draft,
            IsCreditNote = true,
            CorrectingInvoiceId = originalInvoiceId,
            Notes = $"Credit note for invoice {originalInvoice.InvoiceNumber}. Reason: {reason}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CommissionInvoices.Add(creditNote);
        await _context.SaveChangesAsync();

        // Copy items with negative amounts
        foreach (var originalItem in originalInvoice.Items)
        {
            var creditNoteItem = new CommissionInvoiceItem
            {
                CommissionInvoiceId = creditNote.Id,
                CommissionTransactionId = originalItem.CommissionTransactionId,
                Description = $"Credit: {originalItem.Description}",
                Amount = -originalItem.Amount,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommissionInvoiceItems.Add(creditNoteItem);
        }

        await _context.SaveChangesAsync();

        // Mark original invoice as superseded
        originalInvoice.Status = CommissionInvoiceStatus.Superseded;
        originalInvoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created credit note {InvoiceNumber} for invoice {OriginalInvoiceNumber}",
            invoiceNumber, originalInvoice.InvoiceNumber);

        return creditNote;
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateInvoicePdfAsync(int invoiceId)
    {
        var invoice = await GetInvoiceAsync(invoiceId);
        if (invoice == null)
        {
            throw new InvalidOperationException($"Invoice {invoiceId} not found");
        }

        var config = await GetOrCreateConfigAsync();

        // Generate simple PDF content (HTML-based for simplicity)
        // In production, use a proper PDF library like QuestPDF or iTextSharp
        var html = GenerateInvoiceHtml(invoice, config);
        
        // For now, return as UTF-8 bytes (would use PDF library in production)
        return Encoding.UTF8.GetBytes(html);
    }

    /// <inheritdoc />
    public async Task<CommissionInvoiceConfig> GetOrCreateConfigAsync()
    {
        var config = await _context.CommissionInvoiceConfigs.FirstOrDefaultAsync();
        
        if (config == null)
        {
            config = new CommissionInvoiceConfig
            {
                DefaultTaxPercentage = 0m,
                InvoiceDueDays = 30,
                GenerationDayOfMonth = 1,
                AutoGenerateEnabled = false,
                CompanyName = "MercatoApp Platform",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CommissionInvoiceConfigs.Add(config);
            await _context.SaveChangesAsync();
        }

        return config;
    }

    /// <inheritdoc />
    public async Task<CommissionInvoiceConfig> UpdateConfigAsync(CommissionInvoiceConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        _context.CommissionInvoiceConfigs.Update(config);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated commission invoice configuration");
        return config;
    }

    /// <summary>
    /// Generates a unique, sequential invoice number.
    /// </summary>
    private async Task<string> GenerateInvoiceNumberAsync(int year)
    {
        // Get the highest invoice number for the year
        var yearPrefix = $"INV-{year}-";
        var lastInvoice = await _context.CommissionInvoices
            .Where(i => i.InvoiceNumber.StartsWith(yearPrefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastInvoice != null)
        {
            // Extract the number from the last invoice
            var lastNumberStr = lastInvoice.InvoiceNumber.Substring(yearPrefix.Length);
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{yearPrefix}{nextNumber:D6}";
    }

    /// <summary>
    /// Generates HTML for the invoice (used for PDF generation).
    /// </summary>
    private string GenerateInvoiceHtml(CommissionInvoice invoice, CommissionInvoiceConfig config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
        sb.AppendLine("h1 { color: #333; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        sb.AppendLine("th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine(".header { margin-bottom: 30px; }");
        sb.AppendLine(".totals { margin-top: 20px; text-align: right; }");
        sb.AppendLine(".credit-note { color: red; font-weight: bold; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");

        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<h1>{(invoice.IsCreditNote ? "CREDIT NOTE" : "INVOICE")}</h1>");
        if (invoice.IsCreditNote)
        {
            sb.AppendLine("<p class='credit-note'>This is a credit note</p>");
        }
        sb.AppendLine($"<p><strong>{config.CompanyName}</strong></p>");
        if (!string.IsNullOrEmpty(config.CompanyAddress))
        {
            sb.AppendLine($"<p>{config.CompanyAddress}</p>");
        }
        if (!string.IsNullOrEmpty(config.CompanyTaxId))
        {
            sb.AppendLine($"<p>Tax ID: {config.CompanyTaxId}</p>");
        }
        sb.AppendLine("</div>");

        // Invoice details
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Invoice Number:</th><td>" + invoice.InvoiceNumber + "</td></tr>");
        sb.AppendLine("<tr><th>Issue Date:</th><td>" + invoice.IssueDate.ToString("yyyy-MM-dd") + "</td></tr>");
        sb.AppendLine("<tr><th>Due Date:</th><td>" + invoice.DueDate.ToString("yyyy-MM-dd") + "</td></tr>");
        sb.AppendLine("<tr><th>Period:</th><td>" + invoice.PeriodStartDate.ToString("yyyy-MM-dd") + " to " + invoice.PeriodEndDate.ToString("yyyy-MM-dd") + "</td></tr>");
        sb.AppendLine("<tr><th>Bill To:</th><td>" + invoice.Store.StoreName + "</td></tr>");
        sb.AppendLine("<tr><th>Status:</th><td>" + invoice.Status.ToString() + "</td></tr>");
        sb.AppendLine("</table>");

        // Line items
        sb.AppendLine("<h2>Items</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Description</th><th>Amount</th></tr>");
        foreach (var item in invoice.Items)
        {
            sb.AppendLine($"<tr><td>{item.Description}</td><td>{item.Amount:C}</td></tr>");
        }
        sb.AppendLine("</table>");

        // Totals
        sb.AppendLine("<div class='totals'>");
        sb.AppendLine($"<p><strong>Subtotal:</strong> {invoice.Subtotal:C}</p>");
        sb.AppendLine($"<p><strong>Tax ({invoice.TaxPercentage}%):</strong> {invoice.TaxAmount:C}</p>");
        sb.AppendLine($"<p><strong>Total Amount:</strong> {invoice.TotalAmount:C} {invoice.Currency}</p>");
        sb.AppendLine("</div>");

        if (!string.IsNullOrEmpty(invoice.Notes))
        {
            sb.AppendLine("<h2>Notes</h2>");
            sb.AppendLine($"<p>{invoice.Notes}</p>");
        }

        if (!string.IsNullOrEmpty(config.TermsAndConditions))
        {
            sb.AppendLine("<h2>Terms and Conditions</h2>");
            sb.AppendLine($"<p>{config.TermsAndConditions}</p>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
