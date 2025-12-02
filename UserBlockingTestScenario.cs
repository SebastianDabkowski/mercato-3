using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Tests;

/// <summary>
/// Manual test scenario for admin user blocking feature.
/// This demonstrates how to test the user blocking functionality.
/// 
/// To run this test manually:
/// 1. Start the application
/// 2. Register as an admin user
/// 3. Register a test buyer and/or seller user
/// 4. Navigate to Admin > Users
/// 5. Click on a user to view details
/// 6. Click "Block Account" button
/// 7. Select a reason and provide notes
/// 8. Verify the user is blocked
/// 9. Attempt to login as the blocked user (should fail)
/// 10. If seller, verify their store is no longer visible
/// 11. Navigate back to the user details page
/// 12. Click "Unblock Account"
/// 13. Verify the user is unblocked
/// 14. Attempt to login as the unblocked user (should succeed)
/// </summary>
public class UserBlockingTestScenario
{
    /// <summary>
    /// Example test data setup for user blocking feature
    /// </summary>
    public async Task SetupTestDataExample(ApplicationDbContext context, IUserManagementService userManagementService)
    {
        // Create test users
        var buyerUser = new User
        {
            Email = "testbuyer@example.com",
            FirstName = "Test",
            LastName = "Buyer",
            UserType = UserType.Buyer,
            Status = AccountStatus.Active,
            PasswordHash = "test_hash", // In production, use proper password hashing
            CreatedAt = DateTime.UtcNow,
            AcceptedTerms = true
        };

        var sellerUser = new User
        {
            Email = "testseller@example.com",
            FirstName = "Test",
            LastName = "Seller",
            UserType = UserType.Seller,
            Status = AccountStatus.Active,
            PasswordHash = "test_hash", // In production, use proper password hashing
            CreatedAt = DateTime.UtcNow,
            AcceptedTerms = true,
            KycStatus = KycStatus.Approved
        };

        var adminUser = new User
        {
            Email = "testadmin@example.com",
            FirstName = "Test",
            LastName = "Admin",
            UserType = UserType.Admin,
            Status = AccountStatus.Active,
            PasswordHash = "test_hash", // In production, use proper password hashing
            CreatedAt = DateTime.UtcNow,
            AcceptedTerms = true
        };

        context.Users.AddRange(buyerUser, sellerUser, adminUser);
        await context.SaveChangesAsync();

        // Create a store for the seller
        var store = new Store
        {
            UserId = sellerUser.Id,
            StoreName = "Test Seller Store",
            Slug = "test-seller-store",
            Status = StoreStatus.Active,
            Description = "A test store for blocking scenario"
        };

        context.Stores.Add(store);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Test blocking a user account
    /// </summary>
    public async Task TestBlockUser(IUserManagementService userManagementService, int targetUserId, int adminUserId)
    {
        // Block the user
        var blockResult = await userManagementService.BlockUserAsync(
            targetUserId, 
            adminUserId, 
            BlockReason.PolicyViolation,
            "Test blocking - policy violation example"
        );

        if (!blockResult)
        {
            throw new Exception("Failed to block user");
        }

        // Verify user is blocked
        var blockedUser = await userManagementService.GetUserDetailsAsync(targetUserId);
        if (blockedUser?.Status != AccountStatus.Blocked)
        {
            throw new Exception("User status is not Blocked");
        }

        if (!blockedUser.BlockedByUserId.HasValue || blockedUser.BlockedByUserId.Value != adminUserId)
        {
            throw new Exception("BlockedByUserId is not set correctly");
        }

        if (!blockedUser.BlockedAt.HasValue)
        {
            throw new Exception("BlockedAt timestamp is not set");
        }

        if (blockedUser.BlockReason != BlockReason.PolicyViolation)
        {
            throw new Exception("BlockReason is not set correctly");
        }

        // Verify audit log entry was created
        var auditLogs = await userManagementService.GetUserAuditLogAsync(targetUserId, 10);
        var blockLog = auditLogs.FirstOrDefault(log => log.Action == "BlockUser");
        
        if (blockLog == null)
        {
            throw new Exception("Audit log entry for BlockUser was not created");
        }

        if (blockLog.AdminUserId != adminUserId)
        {
            throw new Exception("Audit log admin user ID is incorrect");
        }
    }

    /// <summary>
    /// Test unblocking a user account
    /// </summary>
    public async Task TestUnblockUser(IUserManagementService userManagementService, int targetUserId, int adminUserId)
    {
        // Unblock the user
        var unblockResult = await userManagementService.UnblockUserAsync(
            targetUserId,
            adminUserId,
            "Test unblocking - resolved issue"
        );

        if (!unblockResult)
        {
            throw new Exception("Failed to unblock user");
        }

        // Verify user is unblocked (status should be Active)
        var unblockedUser = await userManagementService.GetUserDetailsAsync(targetUserId);
        if (unblockedUser?.Status != AccountStatus.Active)
        {
            throw new Exception("User status is not Active after unblocking");
        }

        // Note: BlockedByUserId, BlockedAt, etc. are kept for audit purposes

        // Verify audit log entry was created
        var auditLogs = await userManagementService.GetUserAuditLogAsync(targetUserId, 10);
        var unblockLog = auditLogs.FirstOrDefault(log => log.Action == "UnblockUser");
        
        if (unblockLog == null)
        {
            throw new Exception("Audit log entry for UnblockUser was not created");
        }

        if (unblockLog.AdminUserId != adminUserId)
        {
            throw new Exception("Audit log admin user ID is incorrect");
        }
    }

    /// <summary>
    /// Test that blocked users cannot log in
    /// </summary>
    public async Task TestBlockedUserLoginRejection(IUserAuthenticationService authService, string email, string password)
    {
        var loginResult = await authService.AuthenticateAsync(new LoginData
        {
            Email = email,
            Password = password
        });

        if (loginResult.Success)
        {
            throw new Exception("Blocked user was able to log in - this should not happen!");
        }

        if (!loginResult.ErrorMessage.Contains("blocked", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Error message does not mention that the account is blocked");
        }
    }

    /// <summary>
    /// Expected test results
    /// </summary>
    public void PrintExpectedTestResults()
    {
        Console.WriteLine("Expected Test Results:");
        Console.WriteLine("1. User can be blocked successfully with a reason");
        Console.WriteLine("2. Blocked user status is set to Blocked");
        Console.WriteLine("3. BlockedBy, BlockedAt, BlockReason, and BlockNotes are set correctly");
        Console.WriteLine("4. Admin audit log entry is created for blocking action");
        Console.WriteLine("5. Blocked user cannot log in");
        Console.WriteLine("6. Login error message indicates account is blocked");
        Console.WriteLine("7. If seller is blocked, their store is not publicly viewable");
        Console.WriteLine("8. User can be unblocked successfully");
        Console.WriteLine("9. Unblocked user status is set to Active");
        Console.WriteLine("10. Admin audit log entry is created for unblocking action");
        Console.WriteLine("11. Unblocked user can log in successfully");
        Console.WriteLine("12. If seller is unblocked, their store becomes publicly viewable again");
    }
}
