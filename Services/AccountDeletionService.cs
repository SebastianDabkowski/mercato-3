using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for handling account deletion and anonymization.
/// </summary>
public class AccountDeletionService : IAccountDeletionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountDeletionService> _logger;
    private readonly IAdminAuditLogService _auditLogService;
    private readonly AuditHelper _auditHelper;

    // Constants for anonymized values
    private const string AnonymizedEmailFormat = "deleted-user-{0}@anonymized.local";
    private const string AnonymizedFirstName = "Deleted";
    private const string AnonymizedLastName = "User";
    private const string AnonymizedAddressLine = "[Anonymized]";
    private const string AnonymizedCity = "[Anonymized]";
    private const string AnonymizedPostalCode = "00000";
    private const string AnonymizedCountryCode = "XX";
    private const string InvalidPasswordHash = "[DELETED_ACCOUNT_NO_ACCESS]";

    public AccountDeletionService(
        ApplicationDbContext context,
        ILogger<AccountDeletionService> logger,
        IAdminAuditLogService auditLogService,
        AuditHelper auditHelper)
    {
        _context = context;
        _logger = logger;
        _auditLogService = auditLogService;
        _auditHelper = auditHelper;
    }

    /// <inheritdoc />
    public async Task<AccountDeletionValidationResult> ValidateAccountDeletionAsync(int userId)
    {
        try
        {
            var result = new AccountDeletionValidationResult
            {
                CanDelete = true,
                BlockingReasons = new List<string>()
            };

            // Check for user existence
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                result.CanDelete = false;
                result.BlockingReasons.Add("User account not found.");
                return result;
            }

            // Check if account is already deleted
            if (user.Status == AccountStatus.Deleted)
            {
                result.CanDelete = false;
                result.BlockingReasons.Add("Account is already deleted.");
                return result;
            }

            // Check for unresolved return requests
            var unresolvedReturns = await _context.ReturnRequests
                .Where(r => r.BuyerId == userId && 
                       (r.Status == ReturnStatus.Requested || 
                        r.Status == ReturnStatus.Approved || 
                        r.Status == ReturnStatus.UnderAdminReview))
                .CountAsync();

            if (unresolvedReturns > 0)
            {
                result.CanDelete = false;
                result.BlockingReasons.Add($"You have {unresolvedReturns} unresolved return request(s). Please resolve them before deleting your account.");
            }

            // Check for seller-specific blocking conditions
            if (user.UserType == UserType.Seller)
            {
                var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
                if (store != null)
                {
                    // Check for pending seller sub-orders
                    var pendingSubOrders = await _context.SellerSubOrders
                        .Where(so => so.StoreId == store.Id && 
                               (so.Status == OrderStatus.New || 
                                so.Status == OrderStatus.Paid || 
                                so.Status == OrderStatus.Preparing || 
                                so.Status == OrderStatus.Shipped))
                        .CountAsync();

                    if (pendingSubOrders > 0)
                    {
                        result.CanDelete = false;
                        result.BlockingReasons.Add($"Your store has {pendingSubOrders} pending order(s). Please complete or cancel them before deleting your account.");
                    }

                    // Check for unresolved return requests as seller
                    var sellerUnresolvedReturns = await _context.ReturnRequests
                        .Join(_context.SellerSubOrders,
                              r => r.SubOrderId,
                              so => so.Id,
                              (r, so) => new { r, so })
                        .Where(x => x.so.StoreId == store.Id && 
                               (x.r.Status == ReturnStatus.Requested || 
                                x.r.Status == ReturnStatus.Approved || 
                                x.r.Status == ReturnStatus.UnderAdminReview))
                        .CountAsync();

                    if (sellerUnresolvedReturns > 0)
                    {
                        result.CanDelete = false;
                        result.BlockingReasons.Add($"Your store has {sellerUnresolvedReturns} unresolved return request(s). Please resolve them before deleting your account.");
                    }

                    // Check for pending payouts
                    var pendingPayouts = await _context.Payouts
                        .Where(p => p.StoreId == store.Id && 
                               (p.Status == PayoutStatus.Scheduled || 
                                p.Status == PayoutStatus.Processing))
                        .CountAsync();

                    if (pendingPayouts > 0)
                    {
                        result.CanDelete = false;
                        result.BlockingReasons.Add($"Your store has {pendingPayouts} pending payout(s). Please wait for them to complete before deleting your account.");
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating account deletion for user {UserId}", userId);
            return new AccountDeletionValidationResult
            {
                CanDelete = false,
                BlockingReasons = new List<string> { "An error occurred while validating account deletion. Please try again later." }
            };
        }
    }

    /// <inheritdoc />
    public async Task<AccountDeletionResult> DeleteAccountAsync(int userId, string? ipAddress, string? reason)
    {
        try
        {
            // Validate deletion is allowed
            var validation = await ValidateAccountDeletionAsync(userId);
            if (!validation.CanDelete)
            {
                return new AccountDeletionResult
                {
                    Success = false,
                    ErrorMessage = string.Join(" ", validation.BlockingReasons)
                };
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new AccountDeletionResult
                {
                    Success = false,
                    ErrorMessage = "User not found."
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var originalEmail = user.Email;
                var anonymizedEmail = string.Format(AnonymizedEmailFormat, userId);

                // Count associated records before anonymization
                var orderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
                var returnRequestCount = await _context.ReturnRequests.CountAsync(r => r.BuyerId == userId);

                // Anonymize user personal data
                user.Email = anonymizedEmail;
                user.FirstName = AnonymizedFirstName;
                user.LastName = AnonymizedLastName;
                user.PhoneNumber = null;
                user.Address = null;
                user.City = null;
                user.PostalCode = null;
                user.Country = null;
                user.TaxId = null;
                user.PasswordHash = InvalidPasswordHash; // Set to invalid value to prevent any access
                user.EmailVerificationToken = null;
                user.PasswordResetToken = null;
                user.SecurityStamp = null;
                user.TwoFactorSecretKey = null;
                user.TwoFactorRecoveryCodes = null;
                user.ExternalProvider = null;
                user.ExternalProviderId = null;
                user.Status = AccountStatus.Deleted;

                // Anonymize addresses
                var addresses = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                foreach (var address in addresses)
                {
                    address.FullName = AnonymizedFirstName + " " + AnonymizedLastName;
                    address.PhoneNumber = string.Empty;
                    address.AddressLine1 = AnonymizedAddressLine;
                    address.AddressLine2 = null;
                    address.City = AnonymizedCity;
                    address.PostalCode = AnonymizedPostalCode;
                    address.CountryCode = AnonymizedCountryCode;
                }

                // Anonymize order contact information while preserving transactional data
                var orders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .ToListAsync();

                foreach (var order in orders)
                {
                    // Preserve financial and product data, anonymize contact info
                    order.GuestEmail = anonymizedEmail;
                    // Note: DeliveryAddress will be anonymized separately via the addresses table
                }

                // Anonymize return request messages
                var returnMessages = await _context.ReturnRequestMessages
                    .Where(m => m.SenderId == userId)
                    .ToListAsync();

                foreach (var message in returnMessages)
                {
                    // Keep the message structure but mark sender as deleted
                    // The message content may contain business-relevant information so we preserve it
                    // but the sender identity is anonymized via the user record
                }

                // Anonymize product reviews (keep review content for transparency, anonymize author)
                var reviews = await _context.ProductReviews
                    .Where(r => r.UserId == userId)
                    .ToListAsync();

                foreach (var review in reviews)
                {
                    // The UserId relationship will show as deleted user
                    // Review content is preserved for product authenticity
                }

                // Invalidate all user sessions
                var sessions = await _context.UserSessions
                    .Where(s => s.UserId == userId)
                    .ToListAsync();

                foreach (var session in sessions)
                {
                    session.IsValid = false;
                }

                // Delete consents (no longer relevant for deleted account)
                var consents = await _context.UserConsents
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                _context.UserConsents.RemoveRange(consents);

                // Delete push subscriptions
                var pushSubscriptions = await _context.PushSubscriptions
                    .Where(ps => ps.UserId == userId)
                    .ToListAsync();

                _context.PushSubscriptions.RemoveRange(pushSubscriptions);

                // Delete notifications
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .ToListAsync();

                _context.Notifications.RemoveRange(notifications);

                // Create deletion log entry
                var deletionLog = new AccountDeletionLog
                {
                    UserId = userId,
                    AnonymizedEmail = anonymizedEmail,
                    UserType = user.UserType,
                    RequestedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    RequestIpAddress = ipAddress,
                    Metadata = reason,
                    OrderCount = orderCount,
                    ReturnRequestCount = returnRequestCount
                };

                _context.AccountDeletionLogs.Add(deletionLog);

                // Save all changes
                await _context.SaveChangesAsync();

                // Log audit event (using system audit as user is self-deleting)
                await _auditLogService.LogActionAsync(
                    adminUserId: userId,
                    entityType: "User",
                    entityId: userId,
                    entityDisplayName: $"User: {originalEmail}",
                    action: "DeleteAccount",
                    reason: $"User requested account deletion. Reason: {reason ?? "Not provided"}",
                    metadata: $"Anonymized to: {anonymizedEmail}"
                );

                // Log to comprehensive audit log
                await _auditHelper.LogAccountDeletionAsync(
                    initiatorUserId: userId,
                    targetUserId: userId,
                    targetEmail: originalEmail,
                    userType: user.UserType.ToString(),
                    reason: reason);

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Account deleted and anonymized for user {UserId}. Email anonymized from {OriginalEmail} to {AnonymizedEmail}",
                    userId, originalEmail, anonymizedEmail);

                return new AccountDeletionResult
                {
                    Success = true,
                    AnonymizedEmail = anonymizedEmail,
                    DeletionLogId = deletionLog.Id
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account for user {UserId}", userId);
            return new AccountDeletionResult
            {
                Success = false,
                ErrorMessage = "An error occurred while deleting your account. Please try again later."
            };
        }
    }

    /// <inheritdoc />
    public async Task<AccountDeletionImpact> GetDeletionImpactAsync(int userId)
    {
        try
        {
            var impact = new AccountDeletionImpact();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return impact;
            }

            impact.OrderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
            impact.ReturnRequestCount = await _context.ReturnRequests.CountAsync(r => r.BuyerId == userId);
            impact.AddressCount = await _context.Addresses.CountAsync(a => a.UserId == userId);

            if (user.UserType == UserType.Seller)
            {
                var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
                if (store != null)
                {
                    impact.HasStore = true;
                    impact.StoreName = store.StoreName;
                }
            }

            return impact;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deletion impact for user {UserId}", userId);
            return new AccountDeletionImpact();
        }
    }
}
