using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Manual test scenario for Commission Invoice functionality.
/// </summary>
public class CommissionInvoiceTestScenario
{
    public static async Task RunTestAsync(ApplicationDbContext context, ICommissionInvoiceService invoiceService, ICommissionService commissionService)
    {
        Console.WriteLine("=== Commission Invoice Test Scenario ===");
        Console.WriteLine();

        // 1. Get or create test store
        var testStore = await context.Stores.FirstOrDefaultAsync(s => s.StoreName.Contains("Electronics"));
        if (testStore == null)
        {
            Console.WriteLine("ERROR: No test store found. Please ensure test data is seeded.");
            return;
        }

        Console.WriteLine($"✓ Found test store: {testStore.StoreName} (ID: {testStore.Id})");
        Console.WriteLine();

        // 2. Create some test commission transactions
        Console.WriteLine("Creating test commission transactions...");
        
        // Get or create an escrow transaction
        var escrow = await context.EscrowTransactions.FirstOrDefaultAsync();
        if (escrow == null)
        {
            Console.WriteLine("WARNING: No escrow transaction found. Skipping commission transaction creation.");
        }
        else
        {
            // Record a commission transaction
            var commission = await commissionService.RecordCommissionTransactionAsync(
                escrowTransactionId: escrow.Id,
                storeId: testStore.Id,
                categoryId: null,
                transactionType: CommissionTransactionType.Initial,
                grossAmount: 100.00m,
                commissionAmount: 10.00m,
                percentage: 10.0m,
                fixedAmount: 0m,
                source: CommissionSource.Global,
                notes: "Test commission transaction"
            );

            Console.WriteLine($"✓ Created test commission transaction: {commission.Id} - ${commission.CommissionAmount:F2}");
        }
        Console.WriteLine();

        // 3. Generate invoice for current month
        Console.WriteLine("Generating invoice for current month...");
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var invoice = await invoiceService.GenerateInvoiceAsync(testStore.Id, periodStart, periodEnd);
        
        if (invoice == null)
        {
            Console.WriteLine("INFO: No commission transactions found for the current month.");
            Console.WriteLine("Attempting to generate for last month...");
            
            periodStart = periodStart.AddMonths(-1);
            periodEnd = periodStart.AddMonths(1).AddDays(-1);
            invoice = await invoiceService.GenerateInvoiceAsync(testStore.Id, periodStart, periodEnd);
        }

        if (invoice != null)
        {
            Console.WriteLine($"✓ Generated invoice: {invoice.InvoiceNumber}");
            Console.WriteLine($"  Period: {invoice.PeriodStartDate:yyyy-MM-dd} to {invoice.PeriodEndDate:yyyy-MM-dd}");
            Console.WriteLine($"  Status: {invoice.Status}");
            Console.WriteLine($"  Subtotal: ${invoice.Subtotal:F2}");
            Console.WriteLine($"  Tax ({invoice.TaxPercentage}%): ${invoice.TaxAmount:F2}");
            Console.WriteLine($"  Total: ${invoice.TotalAmount:F2}");
            Console.WriteLine($"  Line Items: {invoice.Items.Count}");
            Console.WriteLine();

            // 4. Test invoice status changes
            Console.WriteLine("Testing invoice status changes...");
            
            // Issue the invoice
            var issued = await invoiceService.IssueInvoiceAsync(invoice.Id);
            Console.WriteLine($"✓ Issue invoice: {(issued ? "Success" : "Failed")}");

            // Reload to check status
            invoice = await invoiceService.GetInvoiceAsync(invoice.Id);
            Console.WriteLine($"  Current Status: {invoice?.Status}");
            Console.WriteLine();

            // Mark as paid
            if (invoice != null)
            {
                var paid = await invoiceService.MarkInvoiceAsPaidAsync(invoice.Id);
                Console.WriteLine($"✓ Mark as paid: {(paid ? "Success" : "Failed")}");

                invoice = await invoiceService.GetInvoiceAsync(invoice.Id);
                Console.WriteLine($"  Current Status: {invoice?.Status}");
                Console.WriteLine($"  Paid At: {invoice?.PaidAt:yyyy-MM-dd HH:mm:ss}");
            }
            Console.WriteLine();

            // 5. Test PDF generation
            Console.WriteLine("Testing PDF generation...");
            if (invoice != null)
            {
                var pdf = await invoiceService.GenerateInvoicePdfAsync(invoice.Id);
                Console.WriteLine($"✓ Generated PDF: {pdf.Length} bytes");
                Console.WriteLine($"  (Note: Currently HTML format, would be PDF in production)");
            }
            Console.WriteLine();

            // 6. Get all invoices for store
            Console.WriteLine("Retrieving all invoices for store...");
            var allInvoices = await invoiceService.GetInvoicesAsync(testStore.Id);
            Console.WriteLine($"✓ Found {allInvoices.Count} invoice(s) for store {testStore.StoreName}");
            
            foreach (var inv in allInvoices)
            {
                Console.WriteLine($"  - {inv.InvoiceNumber}: {inv.Status} - ${inv.TotalAmount:F2}");
            }
            Console.WriteLine();

            // 7. Test configuration
            Console.WriteLine("Testing invoice configuration...");
            var config = await invoiceService.GetOrCreateConfigAsync();
            Console.WriteLine($"✓ Configuration:");
            Console.WriteLine($"  Company Name: {config.CompanyName}");
            Console.WriteLine($"  Default Tax: {config.DefaultTaxPercentage}%");
            Console.WriteLine($"  Invoice Due Days: {config.InvoiceDueDays}");
            Console.WriteLine($"  Auto-Generate: {config.AutoGenerateEnabled}");
            Console.WriteLine($"  Generation Day: {config.GenerationDayOfMonth}");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("WARNING: Could not generate invoice (no commission transactions found).");
            Console.WriteLine();
        }

        Console.WriteLine("=== Test Scenario Complete ===");
        Console.WriteLine();
    }
}
