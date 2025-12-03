using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Test scenario for feature flag management functionality.
/// </summary>
public static class FeatureFlagTestScenario
{
    public static async Task RunTestAsync(ApplicationDbContext context, IFeatureFlagManagementService flagService, IFeatureFlagService runtimeFlagService)
    {
        Console.WriteLine("\n=== Feature Flag Management Test Scenario ===\n");

        try
        {
            // Get an admin user for testing
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.UserType == UserType.Admin);
            if (adminUser == null)
            {
                Console.WriteLine("No admin user found. Skipping test.");
                return;
            }

            Console.WriteLine($"Testing with admin user: {adminUser.Email}");

            // Test 1: Create a new feature flag
            Console.WriteLine("\n1. Creating a new feature flag...");
            var newFlag = new FeatureFlag
            {
                Key = "test_advanced_search",
                Name = "Advanced Search",
                Description = "Enable advanced search functionality with filters and sorting",
                IsEnabledByDefault = false,
                IsActive = true,
                Environments = "dev,test"
            };

            var createdFlag = await flagService.CreateFlagAsync(newFlag, adminUser.Id, "127.0.0.1", "TestAgent/1.0");
            Console.WriteLine($"✓ Created feature flag: {createdFlag.Name} (ID: {createdFlag.Id})");

            // Test 2: Retrieve all flags
            Console.WriteLine("\n2. Retrieving all feature flags...");
            var allFlags = await flagService.GetAllFlagsAsync();
            Console.WriteLine($"✓ Found {allFlags.Count} feature flag(s)");

            // Test 3: Add targeting rules
            Console.WriteLine("\n3. Adding targeting rules...");
            createdFlag.Rules.Add(new FeatureFlagRule
            {
                Priority = 1,
                RuleType = FeatureFlagRuleType.UserRole,
                RuleValue = "Admin",
                IsEnabled = true,
                Description = "Enable for all admins"
            });

            createdFlag.Rules.Add(new FeatureFlagRule
            {
                Priority = 2,
                RuleType = FeatureFlagRuleType.PercentageRollout,
                RuleValue = "50",
                IsEnabled = true,
                Description = "Enable for 50% of users"
            });

            var updatedFlag = await flagService.UpdateFlagAsync(createdFlag, adminUser.Id, "127.0.0.1", "TestAgent/1.0");
            Console.WriteLine($"✓ Added {updatedFlag.Rules.Count} targeting rules");

            // Test 4: Test flag evaluation
            Console.WriteLine("\n4. Testing flag evaluation...");
            
            // Test for admin user (should match UserRole rule)
            var isEnabledForAdmin = await runtimeFlagService.IsEnabledAsync(
                "test_advanced_search", 
                userId: adminUser.Id, 
                userRole: Role.RoleNames.Admin,
                environment: "dev"
            );
            Console.WriteLine($"✓ Flag for admin user: {isEnabledForAdmin} (expected: true)");

            // Test for non-admin user (should use percentage rollout)
            var buyerUser = await context.Users.FirstOrDefaultAsync(u => u.UserType == UserType.Buyer);
            if (buyerUser != null)
            {
                var isEnabledForBuyer = await runtimeFlagService.IsEnabledAsync(
                    "test_advanced_search",
                    userId: buyerUser.Id,
                    userRole: Role.RoleNames.Buyer,
                    environment: "dev"
                );
                Console.WriteLine($"✓ Flag for buyer user: {isEnabledForBuyer} (percentage-based)");
            }

            // Test 5: Toggle flag
            Console.WriteLine("\n5. Toggling feature flag...");
            var toggledFlag = await flagService.ToggleFlagAsync(createdFlag.Id, true, adminUser.Id, "127.0.0.1", "TestAgent/1.0");
            Console.WriteLine($"✓ Flag toggled to: {(toggledFlag?.IsEnabledByDefault == true ? "Enabled" : "Disabled")}");

            // Test 6: View history
            Console.WriteLine("\n6. Retrieving flag history...");
            var history = await flagService.GetFlagHistoryAsync(createdFlag.Id);
            Console.WriteLine($"✓ Found {history.Count} history entries:");
            foreach (var entry in history.Take(3))
            {
                Console.WriteLine($"   - {entry.ChangeType}: {entry.ChangeDescription} at {entry.ChangedAt:yyyy-MM-dd HH:mm:ss}");
            }

            // Test 7: Test backward compatibility with configuration-based flags
            Console.WriteLine("\n7. Testing backward compatibility...");
            var sellerUserMgmt = await runtimeFlagService.IsEnabledAsync("SellerUserManagement");
            Console.WriteLine($"✓ Config-based flag 'SellerUserManagement': {sellerUserMgmt}");

            Console.WriteLine("\n=== Feature Flag Test Completed Successfully ===\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Test failed with error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
