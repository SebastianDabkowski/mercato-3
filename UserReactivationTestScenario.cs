using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;

namespace MercatoApp;

/// <summary>
/// Comprehensive test scenario for user account reactivation feature.
/// This class provides test data setup and expected results for manual testing.
/// </summary>
public class UserReactivationTestScenario
{
    /// <summary>
    /// Sets up test data for user reactivation scenarios.
    /// Execute this in the application startup or via a test endpoint to populate test users.
    /// </summary>
    public static async Task SetupTestDataAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Setting up user reactivation test data...");

        // Create an admin user for testing
        var adminUser = new User
        {
            Email = "admin.reactivation@test.com",
            PasswordHash = "test-hash",
            FirstName = "Admin",
            LastName = "Reactivation",
            UserType = UserType.Admin,
            Status = AccountStatus.Active,
            AcceptedTerms = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        // Create test buyer accounts
        var blockedBuyer = new User
        {
            Email = "blocked.buyer@test.com",
            PasswordHash = "test-hash",
            FirstName = "Blocked",
            LastName = "Buyer",
            UserType = UserType.Buyer,
            Status = AccountStatus.Blocked,
            BlockedByUserId = 1, // Will be set to admin user ID
            BlockedAt = DateTime.UtcNow.AddDays(-7),
            BlockReason = BlockReason.Fraud,
            BlockNotes = "Suspected fraudulent activity on account",
            AcceptedTerms = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };

        var blockedSeller = new User
        {
            Email = "blocked.seller@test.com",
            PasswordHash = "test-hash",
            FirstName = "Blocked",
            LastName = "Seller",
            UserType = UserType.Seller,
            Status = AccountStatus.Blocked,
            BlockedByUserId = 1, // Will be set to admin user ID
            BlockedAt = DateTime.UtcNow.AddDays(-14),
            BlockReason = BlockReason.PolicyViolation,
            BlockNotes = "Multiple policy violations - selling prohibited items",
            AcceptedTerms = true,
            KycStatus = KycStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        };

        var reactivatedUser = new User
        {
            Email = "reactivated.user@test.com",
            PasswordHash = "test-hash",
            FirstName = "Reactivated",
            LastName = "User",
            UserType = UserType.Buyer,
            Status = AccountStatus.Active,
            BlockedByUserId = 1,
            BlockedAt = DateTime.UtcNow.AddDays(-30),
            BlockReason = BlockReason.Spam,
            BlockNotes = "Spam activity detected",
            RequirePasswordReset = false, // Was reactivated without password reset requirement
            AcceptedTerms = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-5)
        };

        var reactivatedRequiresPasswordReset = new User
        {
            Email = "reactivated.passwordreset@test.com",
            PasswordHash = "test-hash",
            FirstName = "Reactivated",
            LastName = "PasswordReset",
            UserType = UserType.Buyer,
            Status = AccountStatus.Active,
            BlockedByUserId = 1,
            BlockedAt = DateTime.UtcNow.AddDays(-15),
            BlockReason = BlockReason.AbusiveBehavior,
            BlockNotes = "Account compromised - required security reset",
            RequirePasswordReset = true, // Reactivated with password reset requirement
            AcceptedTerms = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-8)
        };

        context.Users.AddRange(adminUser, blockedBuyer, blockedSeller, reactivatedUser, reactivatedRequiresPasswordReset);
        await context.SaveChangesAsync();

        // Create audit log entries
        var auditLogs = new List<AdminAuditLog>
        {
            new()
            {
                AdminUserId = adminUser.Id,
                TargetUserId = blockedBuyer.Id,
                Action = "BlockUser",
                Reason = "Fraud: Suspected fraudulent activity on account",
                ActionTimestamp = DateTime.UtcNow.AddDays(-7),
                Metadata = "Previous status: Active, Reason: Fraud"
            },
            new()
            {
                AdminUserId = adminUser.Id,
                TargetUserId = blockedSeller.Id,
                Action = "BlockUser",
                Reason = "PolicyViolation: Multiple policy violations - selling prohibited items",
                ActionTimestamp = DateTime.UtcNow.AddDays(-14),
                Metadata = "Previous status: Active, Reason: PolicyViolation"
            },
            new()
            {
                AdminUserId = adminUser.Id,
                TargetUserId = reactivatedUser.Id,
                Action = "BlockUser",
                Reason = "Spam: Spam activity detected",
                ActionTimestamp = DateTime.UtcNow.AddDays(-30),
                Metadata = "Previous status: Active, Reason: Spam"
            },
            new()
            {
                AdminUserId = adminUser.Id,
                TargetUserId = reactivatedUser.Id,
                Action = "UnblockUser",
                Reason = "Issue resolved after user verification",
                ActionTimestamp = DateTime.UtcNow.AddDays(-25),
                Metadata = "Previous status: Blocked, New status: Active"
            },
            new()
            {
                AdminUserId = adminUser.Id,
                TargetUserId = reactivatedRequiresPasswordReset.Id,
                Action = "BlockUser",
                Reason = "AbusiveBehavior: Account compromised - required security reset",
                ActionTimestamp = DateTime.UtcNow.AddDays(-15),
                Metadata = "Previous status: Active, Reason: AbusiveBehavior"
            },
            new()
            {
                AdminUserId = adminUser.Id,
                TargetUserId = reactivatedRequiresPasswordReset.Id,
                Action = "UnblockUser",
                Reason = "Account verified and ownership confirmed",
                ActionTimestamp = DateTime.UtcNow.AddDays(-10),
                Metadata = "Previous status: Blocked, New status: Active, Password reset required"
            }
        };

        context.AdminAuditLogs.AddRange(auditLogs);
        await context.SaveChangesAsync();

        logger.LogInformation("User reactivation test data setup complete");
    }

    /// <summary>
    /// Test Case 1: Admin reactivates a blocked buyer account without password reset.
    /// </summary>
    public static class TestCase1_ReactivateWithoutPasswordReset
    {
        public const string Title = "Reactivate Blocked Buyer Without Password Reset";
        
        public static readonly string[] Steps =
        {
            "1. Log in as admin (admin.reactivation@test.com)",
            "2. Navigate to Admin > Users",
            "3. Search for 'blocked.buyer@test.com'",
            "4. Click on the user to view details",
            "5. Verify the account status shows 'Blocked'",
            "6. Verify blocking information is displayed (who, when, reason, notes)",
            "7. Click 'Reactivate Account' button",
            "8. On the reactivation page, verify current block information is shown",
            "9. Add optional notes: 'User verified identity via support ticket'",
            "10. Leave 'Require password reset on next login' unchecked",
            "11. Click 'Confirm and Reactivate Account'",
            "12. Verify redirect to user details page",
            "13. Verify success message is displayed",
            "14. Verify account status now shows 'Active'",
            "15. Verify block history is preserved in audit log",
            "16. Verify new audit log entry shows reactivation with admin name and notes"
        };

        public static readonly string[] ExpectedResults =
        {
            "✓ User status changes from 'Blocked' to 'Active'",
            "✓ Block history is preserved (BlockedByUserId, BlockedAt, BlockReason, BlockNotes)",
            "✓ RequirePasswordReset remains false",
            "✓ Audit log contains 'UnblockUser' entry with timestamp, admin info, and notes",
            "✓ User can log in immediately without password reset",
            "✓ No 'Password Reset Required' warning shown on details page"
        };
    }

    /// <summary>
    /// Test Case 2: Admin reactivates a blocked seller account with password reset required.
    /// </summary>
    public static class TestCase2_ReactivateWithPasswordReset
    {
        public const string Title = "Reactivate Blocked Seller With Password Reset Required";
        
        public static readonly string[] Steps =
        {
            "1. Log in as admin (admin.reactivation@test.com)",
            "2. Navigate to Admin > Users",
            "3. Search for 'blocked.seller@test.com'",
            "4. Click on the user to view details",
            "5. Verify the account status shows 'Blocked'",
            "6. Click 'Reactivate Account' button",
            "7. Add notes: 'Account compromised - security incident resolved'",
            "8. Check the box 'Require password reset on next login'",
            "9. Verify the helper text about recommended use for security concerns",
            "10. Click 'Confirm and Reactivate Account'",
            "11. Verify redirect to user details page",
            "12. Verify success message is displayed",
            "13. Verify account status now shows 'Active'",
            "14. Verify warning alert shows 'Password Reset Required'",
            "15. Verify audit log entry includes 'Password reset required' in metadata"
        };

        public static readonly string[] ExpectedResults =
        {
            "✓ User status changes from 'Blocked' to 'Active'",
            "✓ RequirePasswordReset flag is set to true",
            "✓ Audit log contains 'UnblockUser' entry with 'Password reset required' in metadata",
            "✓ User details page shows warning: 'Password Reset Required'",
            "✓ User cannot complete login without resetting password"
        };
    }

    /// <summary>
    /// Test Case 3: Reactivated user with password reset requirement attempts to log in.
    /// </summary>
    public static class TestCase3_LoginWithPasswordResetRequired
    {
        public const string Title = "Login With Password Reset Requirement";
        
        public static readonly string[] Steps =
        {
            "1. Log out if currently logged in",
            "2. Navigate to login page",
            "3. Enter email: blocked.seller@test.com (from TestCase2)",
            "4. Enter correct password",
            "5. Click 'Sign In'",
            "6. Verify redirect to Forced Password Reset page",
            "7. Verify info message about security requirement",
            "8. Verify account email is displayed",
            "9. Enter current password",
            "10. Enter new password (meeting requirements)",
            "11. Confirm new password",
            "12. Click 'Reset Password and Continue'",
            "13. Verify redirect to login page",
            "14. Verify success message about password change",
            "15. Log in with new password",
            "16. Verify successful login and normal access"
        };

        public static readonly string[] ExpectedResults =
        {
            "✓ Login redirects to ForcedPasswordReset page instead of home",
            "✓ User cannot bypass password reset",
            "✓ After successful password reset, RequirePasswordReset flag is cleared",
            "✓ User sessions are invalidated during password change",
            "✓ User can log in normally after password reset",
            "✓ No more password reset requirement on subsequent logins"
        };
    }

    /// <summary>
    /// Test Case 4: View full audit log history for a user with multiple block/reactivate cycles.
    /// </summary>
    public static class TestCase4_ViewAuditLogHistory
    {
        public const string Title = "View Complete Block/Reactivate History";
        
        public static readonly string[] Steps =
        {
            "1. Log in as admin",
            "2. Navigate to Admin > Users",
            "3. Search for 'reactivated.passwordreset@test.com'",
            "4. Click on the user to view details",
            "5. Scroll to 'Admin Audit Log' section",
            "6. Verify all historical actions are listed",
            "7. Verify each entry shows: Date/Time, Action (with badge), Admin name, Reason",
            "8. Verify most recent action appears first (descending order)",
            "9. Verify block history is preserved even though account is now Active",
            "10. Check that block metadata (who blocked, when, reason) is still visible"
        };

        public static readonly string[] ExpectedResults =
        {
            "✓ Audit log shows both 'Block User' and 'Reactivate User' entries",
            "✓ Entries are in chronological order (newest first)",
            "✓ Each entry includes timestamp, admin name, and reason/notes",
            "✓ Badge colors: Block User = red/danger, Reactivate User = green/success",
            "✓ Historical block information is preserved for compliance",
            "✓ Metadata indicates if password reset was required during reactivation"
        };
    }

    /// <summary>
    /// Test Case 5: Reactivated seller's store becomes visible again.
    /// </summary>
    public static class TestCase5_SellerStoreVisibility
    {
        public const string Title = "Seller Store Becomes Visible After Reactivation";
        
        public static readonly string[] Steps =
        {
            "1. Log in as admin",
            "2. Block a seller account using the Block Account feature",
            "3. Log out and visit the seller's store page as anonymous user",
            "4. Verify store shows 'This store is currently unavailable' message",
            "5. Log in as admin again",
            "6. Reactivate the seller account (without password reset)",
            "7. Log out and visit the seller's store page again",
            "8. Verify store and product listings are now visible",
            "9. Verify buyers can browse products and add to cart"
        };

        public static readonly string[] ExpectedResults =
        {
            "✓ Blocked seller's store is hidden from public view",
            "✓ After reactivation, store immediately becomes visible",
            "✓ Product listings are accessible to buyers",
            "✓ Buyers can interact with store normally (view, cart, purchase)"
        };
    }

    /// <summary>
    /// Test Case 6: Edge case - Attempt to reactivate an already active account.
    /// </summary>
    public static class TestCase6_ReactivateActiveAccount
    {
        public const string Title = "Attempt to Reactivate Already Active Account";
        
        public static readonly string[] Steps =
        {
            "1. Log in as admin",
            "2. Navigate to user details for an Active user",
            "3. Verify 'Reactivate Account' button is NOT displayed",
            "4. Verify 'Block Account' button IS displayed",
            "5. Try to manually navigate to /Admin/Users/Unblock?id={userId}",
            "6. Verify error message or redirect",
            "7. Confirm no changes are made to the user account"
        };

        public static readonly string[] ExpectedResults =
        {
            "✓ Reactivate option only available for Blocked accounts",
            "✓ Direct URL access to unblock page for active user shows error",
            "✓ Service validates account is actually blocked before unblocking",
            "✓ No duplicate audit log entries created"
        };
    }

    /// <summary>
    /// Summary of all acceptance criteria validations.
    /// </summary>
    public static class AcceptanceCriteria
    {
        public static readonly string[] Criteria =
        {
            "✅ AC1: Given a user account is blocked, when I select the option to reactivate and confirm, then the user status changes back to Active and this change is recorded in the audit log.",
            "  - Validated by: TestCase1, TestCase2, TestCase4",
            "",
            "✅ AC2: Given a user account was reactivated, when the user next attempts to log in, then they can log in normally subject to standard security checks.",
            "  - Validated by: TestCase1 (no password reset), TestCase3 (with password reset)",
            "",
            "✅ AC3: Given I am viewing user details, when I open the account history, then I see the full block/reactivate history with timestamps, admin names and reasons.",
            "  - Validated by: TestCase4",
            "",
            "✅ FEATURE: Optional password reset requirement for high-risk cases",
            "  - Validated by: TestCase2, TestCase3",
            "",
            "✅ FEATURE: Reactivation does not reset passwords or security data by default",
            "  - Validated by: TestCase1 (block history preserved)"
        };
    }
}
