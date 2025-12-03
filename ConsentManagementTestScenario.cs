using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MercatoApp;

/// <summary>
/// Test scenario demonstrating user consent management functionality.
/// </summary>
public class ConsentManagementTestScenario
{
    public static async Task RunAsync()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("ConsentManagementTest")
            .ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var context = new ApplicationDbContext(options);
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var consentLogger = loggerFactory.CreateLogger<ConsentManagementService>();
        var legalDocLogger = loggerFactory.CreateLogger<LegalDocumentService>();
        
        var consentService = new ConsentManagementService(context, consentLogger);
        var legalDocService = new LegalDocumentService(context, legalDocLogger);

        Console.WriteLine("=== User Consent Management Test Scenario ===\n");

        // 1. Create test user
        var user = new User
        {
            Email = "buyer@test.com",
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "Buyer",
            UserType = UserType.Buyer,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created test user: {user.Email} (ID: {user.Id})");

        // 2. Create legal documents
        var tosDoc = new LegalDocument
        {
            DocumentType = LegalDocumentType.TermsOfService,
            Version = "1.0",
            Title = "Terms of Service",
            Content = "<p>Test terms of service content</p>",
            EffectiveDate = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LanguageCode = "en"
        };
        
        var ppDoc = new LegalDocument
        {
            DocumentType = LegalDocumentType.PrivacyPolicy,
            Version = "1.0",
            Title = "Privacy Policy",
            Content = "<p>Test privacy policy content</p>",
            EffectiveDate = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LanguageCode = "en"
        };
        
        context.LegalDocuments.AddRange(tosDoc, ppDoc);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created legal documents: ToS v{tosDoc.Version}, PP v{ppDoc.Version}\n");

        // 3. Grant consents during registration
        Console.WriteLine("--- Simulating Registration Consents ---");
        
        await consentService.RecordConsentAsync(
            user.Id,
            ConsentType.TermsOfService,
            isGranted: true,
            version: tosDoc.Version,
            consentText: "I accept the Terms of Service",
            ipAddress: "192.168.1.100",
            userAgent: "Mozilla/5.0",
            context: "registration",
            legalDocumentId: tosDoc.Id);
        Console.WriteLine("✓ Recorded ToS consent");

        await consentService.RecordConsentAsync(
            user.Id,
            ConsentType.PrivacyPolicy,
            isGranted: true,
            version: ppDoc.Version,
            consentText: "I accept the Privacy Policy",
            ipAddress: "192.168.1.100",
            userAgent: "Mozilla/5.0",
            context: "registration",
            legalDocumentId: ppDoc.Id);
        Console.WriteLine("✓ Recorded Privacy Policy consent");

        await consentService.GrantConsentAsync(
            user.Id,
            ConsentType.Newsletter,
            version: "1.0",
            consentText: "I agree to receive newsletters",
            ipAddress: "192.168.1.100",
            userAgent: "Mozilla/5.0",
            context: "registration");
        Console.WriteLine("✓ Granted Newsletter consent");

        await consentService.GrantConsentAsync(
            user.Id,
            ConsentType.Marketing,
            version: "1.0",
            consentText: "I agree to receive marketing communications",
            ipAddress: "192.168.1.100",
            userAgent: "Mozilla/5.0",
            context: "registration");
        Console.WriteLine("✓ Granted Marketing consent\n");

        // 4. Check current consents
        Console.WriteLine("--- Current Consents ---");
        var currentConsents = await consentService.GetCurrentConsentsAsync(user.Id);
        foreach (var (type, consent) in currentConsents)
        {
            Console.WriteLine($"  {type}: {(consent.IsGranted ? "GRANTED" : "WITHDRAWN")} on {consent.ConsentedAt:yyyy-MM-dd HH:mm}");
        }
        Console.WriteLine();

        // 5. Test communication eligibility
        Console.WriteLine("--- Communication Eligibility Checks ---");
        var canSendNewsletter = await consentService.IsEligibleForCommunicationAsync(user.Id, ConsentType.Newsletter);
        Console.WriteLine($"  Can send newsletter: {canSendNewsletter}");
        
        var canSendMarketing = await consentService.IsEligibleForCommunicationAsync(user.Id, ConsentType.Marketing);
        Console.WriteLine($"  Can send marketing: {canSendMarketing}\n");

        // 6. Withdraw marketing consent
        Console.WriteLine("--- Withdrawing Marketing Consent ---");
        await consentService.WithdrawConsentAsync(
            user.Id,
            ConsentType.Marketing,
            ipAddress: "192.168.1.100",
            userAgent: "Mozilla/5.0",
            context: "privacy_settings");
        Console.WriteLine("✓ Withdrawn Marketing consent\n");

        // 7. Check eligibility after withdrawal
        Console.WriteLine("--- Communication Eligibility After Withdrawal ---");
        canSendNewsletter = await consentService.IsEligibleForCommunicationAsync(user.Id, ConsentType.Newsletter);
        Console.WriteLine($"  Can send newsletter: {canSendNewsletter}");
        
        canSendMarketing = await consentService.IsEligibleForCommunicationAsync(user.Id, ConsentType.Marketing);
        Console.WriteLine($"  Can send marketing: {canSendMarketing}\n");

        // 8. View consent history
        Console.WriteLine("--- Marketing Consent History ---");
        var marketingHistory = await consentService.GetConsentHistoryAsync(user.Id, ConsentType.Marketing);
        foreach (var consent in marketingHistory)
        {
            Console.WriteLine($"  {consent.ConsentedAt:yyyy-MM-dd HH:mm}: {(consent.IsGranted ? "GRANTED" : "WITHDRAWN")} (Context: {consent.ConsentContext})");
        }
        Console.WriteLine();

        // 9. Re-grant marketing consent
        Console.WriteLine("--- Re-granting Marketing Consent ---");
        await consentService.GrantConsentAsync(
            user.Id,
            ConsentType.Marketing,
            version: "1.0",
            consentText: "I agree to receive marketing communications",
            ipAddress: "192.168.1.100",
            userAgent: "Mozilla/5.0",
            context: "privacy_settings");
        Console.WriteLine("✓ Re-granted Marketing consent\n");

        // 10. Final consent state
        Console.WriteLine("--- Final Current Consents ---");
        currentConsents = await consentService.GetCurrentConsentsAsync(user.Id);
        foreach (var (type, consent) in currentConsents)
        {
            Console.WriteLine($"  {type}: {(consent.IsGranted ? "GRANTED" : "WITHDRAWN")} on {consent.ConsentedAt:yyyy-MM-dd HH:mm}");
        }
        Console.WriteLine();

        // 11. Get all users with active newsletter consent
        Console.WriteLine("--- Users with Active Newsletter Consent ---");
        var usersWithNewsletter = await consentService.GetUsersWithActiveConsentAsync(ConsentType.Newsletter);
        Console.WriteLine($"  Found {usersWithNewsletter.Count} user(s) with active newsletter consent");

        Console.WriteLine("\n=== Test Scenario Complete ===");
    }
}
