using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MercatoApp.Tests;

/// <summary>
/// Manual test for verifying payout service functionality.
/// This is a simple test class to verify the core features work as expected.
/// </summary>
public static class PayoutServiceManualTest
{
    public static async Task RunTestsAsync()
    {
        Console.WriteLine("=== Payout Service Manual Tests ===\n");

        // Setup in-memory database and services
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "PayoutTestDb")
            .Options;

        using var context = new ApplicationDbContext(options);
        
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var payoutLogger = loggerFactory.CreateLogger<PayoutService>();
        var settingsLogger = loggerFactory.CreateLogger<PayoutSettingsService>();

        // Setup configuration
        var configData = new Dictionary<string, string?>
        {
            { "Payout:MaxRetryAttempts", "3" },
            { "Payout:RetryDelayHours", "24" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        
        var payoutSettingsService = new PayoutSettingsService(context, settingsLogger);
        
        // Create a mock email service for testing
        var emailLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<EmailService>();
        var emailService = new EmailService(context, emailLogger);
        
        // Create a mock notification service for testing
        var notificationLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<NotificationService>();
        var notificationService = new NotificationService(context, notificationLogger);
        
        var payoutService = new PayoutService(context, payoutLogger, payoutSettingsService, emailService, notificationService, configuration);

        // Setup test data
        await SetupTestDataAsync(context, payoutSettingsService);

        // Run tests
        await TestCreatePayoutSchedule(payoutService);
        await TestGetEligibleBalance(payoutService, context);
        await TestCreatePayout(payoutService, context);
        await TestGenerateScheduledPayouts(payoutService);
        await TestProcessPayout(payoutService, context);
        await TestRetryFailedPayouts(payoutService);

        Console.WriteLine("\n=== All Tests Completed ===");
    }

    private static async Task SetupTestDataAsync(ApplicationDbContext context, PayoutSettingsService payoutSettingsService)
    {
        Console.WriteLine("Setting up test data...");

        // Create a test user
        var user = new User
        {
            Email = "seller@test.com",
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "Seller",
            UserType = UserType.Seller,
            Status = AccountStatus.Active,
            AcceptedTerms = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Create a test store
        var store = new Store
        {
            UserId = user.Id,
            StoreName = "Test Store",
            Slug = "test-store",
            Description = "A test store",
            CreatedAt = DateTime.UtcNow
        };
        context.Stores.Add(store);
        await context.SaveChangesAsync();

        // Create a payout method
        var payoutMethodData = new BankTransferPayoutData
        {
            DisplayName = "Main Account",
            BankName = "Test Bank",
            BankAccountHolderName = "Test Seller",
            BankAccountNumber = "1234567890",
            BankRoutingNumber = "123456789",
            Currency = "USD",
            CountryCode = "US",
            IsDefault = true
        };
        await payoutSettingsService.AddBankTransferPayoutMethodAsync(store.Id, payoutMethodData);

        // Create test escrow transactions
        var now = DateTime.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            var escrow = new EscrowTransaction
            {
                PaymentTransactionId = 1,
                SellerSubOrderId = i + 1,
                StoreId = store.Id,
                GrossAmount = 100m,
                CommissionAmount = 10m,
                NetAmount = 90m,
                Status = EscrowStatus.EligibleForPayout,
                EligibleForPayoutAt = now.AddDays(-1),
                CreatedAt = now.AddDays(-7)
            };
            context.EscrowTransactions.Add(escrow);
        }
        await context.SaveChangesAsync();

        Console.WriteLine($"Created test store (ID: {store.Id}) with 5 eligible escrow transactions (total: $450)\n");
    }

    private static async Task TestCreatePayoutSchedule(IPayoutService payoutService)
    {
        Console.WriteLine("Test 1: Create Payout Schedule");
        try
        {
            var schedule = await payoutService.CreateOrUpdatePayoutScheduleAsync(
                storeId: 1,
                frequency: PayoutFrequency.Weekly,
                minimumThreshold: 50m,
                dayOfWeek: 1 // Monday
            );

            Console.WriteLine($"✓ Created payout schedule: {schedule.Frequency}, threshold ${schedule.MinimumPayoutThreshold}, next payout: {schedule.NextPayoutDate:yyyy-MM-dd}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed: {ex.Message}");
        }
        Console.WriteLine();
    }

    private static async Task TestGetEligibleBalance(IPayoutService payoutService, ApplicationDbContext context)
    {
        Console.WriteLine("Test 2: Get Eligible Balance Summary");
        try
        {
            var summary = await payoutService.GetEligibleBalanceSummaryAsync(1);

            Console.WriteLine($"✓ Eligible balance: ${summary.EligibleBalance:F2}");
            Console.WriteLine($"  - Transaction count: {summary.EligibleTransactionCount}");
            Console.WriteLine($"  - Meets threshold: {summary.MeetsThreshold}");
            Console.WriteLine($"  - Minimum threshold: ${summary.MinimumThreshold:F2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed: {ex.Message}");
        }
        Console.WriteLine();
    }

    private static async Task TestCreatePayout(IPayoutService payoutService, ApplicationDbContext context)
    {
        Console.WriteLine("Test 3: Create Payout");
        try
        {
            var result = await payoutService.CreatePayoutAsync(
                storeId: 1,
                scheduledDate: DateTime.UtcNow
            );

            if (result.Success && result.Payout != null)
            {
                Console.WriteLine($"✓ Created payout: {result.Payout.PayoutNumber}");
                Console.WriteLine($"  - Amount: ${result.Payout.Amount:F2}");
                Console.WriteLine($"  - Status: {result.Payout.Status}");
                Console.WriteLine($"  - Scheduled date: {result.Payout.ScheduledDate:yyyy-MM-dd}");

                // Verify escrow transactions are linked
                var linkedEscrows = await context.EscrowTransactions
                    .Where(e => e.PayoutId == result.Payout.Id)
                    .CountAsync();
                Console.WriteLine($"  - Linked escrow transactions: {linkedEscrows}");
            }
            else
            {
                Console.WriteLine($"✗ Failed to create payout: {string.Join(", ", result.Errors)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed: {ex.Message}");
        }
        Console.WriteLine();
    }

    private static async Task TestGenerateScheduledPayouts(IPayoutService payoutService)
    {
        Console.WriteLine("Test 4: Generate Scheduled Payouts");
        try
        {
            // This should not create new payouts since we already created one above
            var count = await payoutService.GenerateScheduledPayoutsAsync();
            Console.WriteLine($"✓ Generated {count} scheduled payouts");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed: {ex.Message}");
        }
        Console.WriteLine();
    }

    private static async Task TestProcessPayout(IPayoutService payoutService, ApplicationDbContext context)
    {
        Console.WriteLine("Test 5: Process Payout");
        try
        {
            // Get the first scheduled payout
            var payout = await context.Payouts
                .FirstOrDefaultAsync(p => p.Status == PayoutStatus.Scheduled);

            if (payout != null)
            {
                var result = await payoutService.ProcessPayoutAsync(payout.Id);

                if (result.Success && result.Payout != null)
                {
                    Console.WriteLine($"✓ Processed payout: {result.Payout.PayoutNumber}");
                    Console.WriteLine($"  - Status: {result.Payout.Status}");
                    Console.WriteLine($"  - Completed at: {result.Payout.CompletedAt:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"  - External ID: {result.Payout.ExternalTransactionId}");
                }
                else
                {
                    Console.WriteLine($"✗ Processing failed or resulted in failed status");
                    if (result.Payout != null)
                    {
                        Console.WriteLine($"  - Status: {result.Payout.Status}");
                        Console.WriteLine($"  - Error: {result.Payout.ErrorMessage}");
                        Console.WriteLine($"  - Error reference: {result.Payout.ErrorReference}");
                        Console.WriteLine($"  - Retry count: {result.Payout.RetryCount}/{result.Payout.MaxRetryAttempts}");
                        if (result.Payout.NextRetryDate.HasValue)
                        {
                            Console.WriteLine($"  - Next retry: {result.Payout.NextRetryDate:yyyy-MM-dd HH:mm:ss}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No scheduled payout found to process");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed: {ex.Message}");
        }
        Console.WriteLine();
    }

    private static async Task TestRetryFailedPayouts(IPayoutService payoutService)
    {
        Console.WriteLine("Test 6: Retry Failed Payouts");
        try
        {
            var count = await payoutService.RetryFailedPayoutsAsync();
            Console.WriteLine($"✓ Retried {count} failed payouts");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed: {ex.Message}");
        }
        Console.WriteLine();
    }
}
