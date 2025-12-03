using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Test scenario for Integration Management feature.
/// </summary>
public class IntegrationManagementTestScenario
{
    public static async Task RunTestAsync(ApplicationDbContext context, IIntegrationService integrationService)
    {
        Console.WriteLine("\n=== Integration Management Test Scenario ===\n");

        // Get admin user for testing
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@test.com");
        if (adminUser == null)
        {
            Console.WriteLine("ERROR: Admin user not found!");
            return;
        }

        try
        {
            // Test 1: Create Payment Integration (Production)
            Console.WriteLine("Test 1: Create Payment Integration (Production)");
            var stripeIntegration = new Integration
            {
                Name = "Stripe Payment Gateway",
                Type = IntegrationType.Payment,
                Provider = "Stripe",
                Environment = IntegrationEnvironment.Production,
                Status = IntegrationStatus.Testing,
                ApiEndpoint = "https://api.stripe.com/v1",
                ApiKey = "test_live_key_1234567890abcdefghijklmnopqrstuvwxyz",
                MerchantId = "acct_1234567890",
                CallbackUrl = "https://mercato.example.com/webhooks/stripe",
                IsEnabled = true
            };

            var createdStripe = await integrationService.CreateIntegrationAsync(stripeIntegration, adminUser.Id);
            Console.WriteLine($"  ✓ Created: {createdStripe.Name} (ID: {createdStripe.Id})");
            Console.WriteLine($"  - API Key (Masked): {integrationService.MaskApiKey(createdStripe.ApiKey)}");

            // Test 2: Create Shipping Integration (Sandbox)
            Console.WriteLine("\nTest 2: Create Shipping Integration (Sandbox)");
            var fedExIntegration = new Integration
            {
                Name = "FedEx Shipping",
                Type = IntegrationType.Shipping,
                Provider = "FedEx",
                Environment = IntegrationEnvironment.Sandbox,
                Status = IntegrationStatus.Testing,
                ApiEndpoint = "https://apis-sandbox.fedex.com",
                ApiKey = "test_fedex_api_key_sandbox_12345678",
                MerchantId = "TEST123456",
                CallbackUrl = "https://mercato.example.com/webhooks/fedex",
                IsEnabled = true
            };

            var createdFedEx = await integrationService.CreateIntegrationAsync(fedExIntegration, adminUser.Id);
            Console.WriteLine($"  ✓ Created: {createdFedEx.Name} (ID: {createdFedEx.Id})");
            Console.WriteLine($"  - Environment: {createdFedEx.Environment}");

            // Test 3: Create ERP Integration (Development)
            Console.WriteLine("\nTest 3: Create ERP Integration (Development)");
            var sapIntegration = new Integration
            {
                Name = "SAP ERP Connector",
                Type = IntegrationType.ERP,
                Provider = "SAP",
                Environment = IntegrationEnvironment.Development,
                Status = IntegrationStatus.Inactive,
                ApiEndpoint = "https://dev.sap.example.com/api",
                ApiKey = "dev_sap_key_abcdefghijklmnop",
                MerchantId = "DEV_COMPANY_001",
                IsEnabled = false
            };

            var createdSAP = await integrationService.CreateIntegrationAsync(sapIntegration, adminUser.Id);
            Console.WriteLine($"  ✓ Created: {createdSAP.Name} (ID: {createdSAP.Id})");
            Console.WriteLine($"  - Status: {createdSAP.Status}, Enabled: {createdSAP.IsEnabled}");

            // Test 4: List All Integrations
            Console.WriteLine("\nTest 4: List All Integrations");
            var allIntegrations = await integrationService.GetAllIntegrationsAsync();
            Console.WriteLine($"  Total integrations: {allIntegrations.Count}");
            foreach (var integration in allIntegrations)
            {
                Console.WriteLine($"  - {integration.Name} ({integration.Type}, {integration.Environment})");
            }

            // Test 5: Filter by Type
            Console.WriteLine("\nTest 5: Filter by Type (Payment)");
            var paymentIntegrations = await integrationService.GetAllIntegrationsAsync(IntegrationType.Payment);
            Console.WriteLine($"  Payment integrations: {paymentIntegrations.Count}");
            foreach (var integration in paymentIntegrations)
            {
                Console.WriteLine($"  - {integration.Name}");
            }

            // Test 6: Health Check
            Console.WriteLine("\nTest 6: Health Check");
            var healthCheckResult = await integrationService.PerformHealthCheckAsync(createdStripe.Id);
            Console.WriteLine($"  Stripe Health Check:");
            Console.WriteLine($"  - Success: {healthCheckResult.Success}");
            Console.WriteLine($"  - Message: {healthCheckResult.Message}");
            Console.WriteLine($"  - Details: {healthCheckResult.Details}");
            Console.WriteLine($"  - Checked At: {healthCheckResult.CheckedAt:yyyy-MM-dd HH:mm:ss}");

            // Test 7: Disable Integration
            Console.WriteLine("\nTest 7: Disable Integration");
            var disableResult = await integrationService.DisableIntegrationAsync(createdFedEx.Id, adminUser.Id);
            Console.WriteLine($"  ✓ FedEx integration disabled: {disableResult}");
            var updatedFedEx = await integrationService.GetIntegrationByIdAsync(createdFedEx.Id);
            Console.WriteLine($"  - New Status: {updatedFedEx?.Status}, Enabled: {updatedFedEx?.IsEnabled}");

            // Test 8: Enable Integration
            Console.WriteLine("\nTest 8: Enable Integration");
            var enableResult = await integrationService.EnableIntegrationAsync(createdSAP.Id, adminUser.Id);
            Console.WriteLine($"  ✓ SAP integration enabled: {enableResult}");
            var updatedSAP = await integrationService.GetIntegrationByIdAsync(createdSAP.Id);
            Console.WriteLine($"  - New Status: {updatedSAP?.Status}, Enabled: {updatedSAP?.IsEnabled}");

            // Test 9: Update Integration
            Console.WriteLine("\nTest 9: Update Integration");
            var updatedStripe = await integrationService.GetIntegrationByIdAsync(createdStripe.Id);
            if (updatedStripe != null)
            {
                updatedStripe.MerchantId = "acct_NEW_MERCHANT_ID";
                updatedStripe.CallbackUrl = "https://mercato.example.com/webhooks/stripe/v2";
                await integrationService.UpdateIntegrationAsync(updatedStripe, adminUser.Id);
                Console.WriteLine($"  ✓ Updated Stripe integration");
                Console.WriteLine($"  - New Merchant ID: {updatedStripe.MerchantId}");
                Console.WriteLine($"  - New Callback URL: {updatedStripe.CallbackUrl}");
            }

            // Test 10: API Key Masking
            Console.WriteLine("\nTest 10: API Key Masking");
            var testKeys = new[]
            {
                "test_live_key_1234567890abcdefghijklmnopqrstuvwxyz",
                "short",
                "abc",
                "",
                (string?)null
            };

            foreach (var key in testKeys)
            {
                var masked = integrationService.MaskApiKey(key);
                Console.WriteLine($"  Original: '{key ?? "(null)"}' => Masked: '{masked}'");
            }

            // Test 11: Invalid Health Check (Integration Not Found)
            Console.WriteLine("\nTest 11: Health Check on Non-Existent Integration");
            var invalidHealthCheck = await integrationService.PerformHealthCheckAsync(99999);
            Console.WriteLine($"  - Success: {invalidHealthCheck.Success}");
            Console.WriteLine($"  - Message: {invalidHealthCheck.Message}");

            Console.WriteLine("\n=== All Integration Management Tests Completed Successfully ===\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR during integration tests: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
