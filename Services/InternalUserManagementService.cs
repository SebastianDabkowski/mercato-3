using System.Security.Cryptography;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of an internal user management operation.
/// </summary>
public class InternalUserResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = [];

    public static InternalUserResult Success() => new() { IsSuccess = true };
    public static InternalUserResult Fail(string message) => new() { IsSuccess = false, ErrorMessage = message, Errors = [message] };
}

/// <summary>
/// Result of an invitation creation operation.
/// </summary>
public class InvitationResult : InternalUserResult
{
    public StoreUserInvitation? Invitation { get; set; }

    public static new InvitationResult Success() => new() { IsSuccess = true };
    public static InvitationResult Success(StoreUserInvitation invitation) => new() { IsSuccess = true, Invitation = invitation };
    public static new InvitationResult Fail(string message) => new() { IsSuccess = false, ErrorMessage = message, Errors = [message] };
}

/// <summary>
/// Represents an internal user with their role in a store.
/// </summary>
public class StoreInternalUser
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public StoreRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
    public string RoleDisplayName => StoreRoleNames.GetDisplayName(Role);
}

/// <summary>
/// Data for inviting a new internal user.
/// </summary>
public class InviteUserData
{
    public required string Email { get; set; }
    public required StoreRole Role { get; set; }
}

/// <summary>
/// Interface for internal user management service.
/// </summary>
public interface IInternalUserManagementService
{
    /// <summary>
    /// Gets all internal users for a store.
    /// </summary>
    Task<List<StoreInternalUser>> GetStoreUsersAsync(int storeId);

    /// <summary>
    /// Gets pending invitations for a store.
    /// </summary>
    Task<List<StoreUserInvitation>> GetPendingInvitationsAsync(int storeId);

    /// <summary>
    /// Invites a new user to the store.
    /// </summary>
    Task<InvitationResult> InviteUserAsync(int storeId, int invitedByUserId, InviteUserData data);

    /// <summary>
    /// Accepts an invitation and creates the user role assignment.
    /// </summary>
    Task<InternalUserResult> AcceptInvitationAsync(string invitationToken, int acceptingUserId);

    /// <summary>
    /// Changes the role of an existing internal user.
    /// </summary>
    Task<InternalUserResult> ChangeUserRoleAsync(int storeId, int userId, StoreRole newRole, int changedByUserId);

    /// <summary>
    /// Deactivates an internal user (revokes access to the store).
    /// </summary>
    Task<InternalUserResult> DeactivateUserAsync(int storeId, int userId, int deactivatedByUserId);

    /// <summary>
    /// Reactivates a previously deactivated internal user.
    /// </summary>
    Task<InternalUserResult> ReactivateUserAsync(int storeId, int userId, int reactivatedByUserId);

    /// <summary>
    /// Revokes a pending invitation.
    /// </summary>
    Task<InternalUserResult> RevokeInvitationAsync(int invitationId, int revokedByUserId);

    /// <summary>
    /// Gets an invitation by its token.
    /// </summary>
    Task<StoreUserInvitation?> GetInvitationByTokenAsync(string token);

    /// <summary>
    /// Gets a user's role in a specific store.
    /// </summary>
    Task<StoreUserRole?> GetUserStoreRoleAsync(int storeId, int userId);

    /// <summary>
    /// Checks if a user has store owner role for a specific store.
    /// </summary>
    Task<bool> IsStoreOwnerAsync(int storeId, int userId);

    /// <summary>
    /// Gets the store for a user based on their store user role.
    /// </summary>
    Task<Store?> GetStoreForUserAsync(int userId);
}

/// <summary>
/// Service for managing internal users within a store.
/// </summary>
public class InternalUserManagementService : IInternalUserManagementService
{
    private const int TokenSizeBytes = 32;
    private static readonly TimeSpan InvitationExpiry = TimeSpan.FromDays(7);

    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<InternalUserManagementService> _logger;

    public InternalUserManagementService(
        ApplicationDbContext context,
        IEmailService emailService,
        ISessionService sessionService,
        ILogger<InternalUserManagementService> logger)
    {
        _context = context;
        _emailService = emailService;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<StoreInternalUser>> GetStoreUsersAsync(int storeId)
    {
        return await _context.StoreUserRoles
            .Where(sur => sur.StoreId == storeId)
            .Include(sur => sur.User)
            .Select(sur => new StoreInternalUser
            {
                UserId = sur.UserId,
                Email = sur.User.Email,
                FirstName = sur.User.FirstName,
                LastName = sur.User.LastName,
                Role = sur.Role,
                IsActive = sur.IsActive,
                JoinedAt = sur.CreatedAt
            })
            .OrderBy(u => u.Role)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<StoreUserInvitation>> GetPendingInvitationsAsync(int storeId)
    {
        // Clean up expired invitations first
        await CleanupExpiredInvitationsAsync(storeId);

        return await _context.StoreUserInvitations
            .Where(i => i.StoreId == storeId && i.Status == InvitationStatus.Pending)
            .Include(i => i.InvitedByUser)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<InvitationResult> InviteUserAsync(int storeId, int invitedByUserId, InviteUserData data)
    {
        try
        {
            // Validate the store exists
            var store = await _context.Stores
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == storeId);

            if (store == null)
            {
                return InvitationResult.Fail("Store not found.");
            }

            // Validate the inviting user has permission (must be store owner)
            var inviterRole = await GetUserStoreRoleAsync(storeId, invitedByUserId);
            if (inviterRole == null || inviterRole.Role != StoreRole.StoreOwner)
            {
                // Also check if this is the original store owner
                if (store.UserId != invitedByUserId)
                {
                    return InvitationResult.Fail("Only store owners can invite new users.");
                }
            }

            // Check if user is already a member
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == data.Email);
            if (existingUser != null)
            {
                var existingRole = await GetUserStoreRoleAsync(storeId, existingUser.Id);
                if (existingRole != null && existingRole.IsActive)
                {
                    return InvitationResult.Fail("This user is already a member of your store.");
                }
            }

            // Check if there's already a pending invitation
            var existingInvitation = await _context.StoreUserInvitations
                .FirstOrDefaultAsync(i => i.StoreId == storeId &&
                                          i.Email == data.Email &&
                                          i.Status == InvitationStatus.Pending &&
                                          i.ExpiresAt > DateTime.UtcNow);

            if (existingInvitation != null)
            {
                return InvitationResult.Fail("An invitation has already been sent to this email address.");
            }

            // Get the inviting user's name
            var invitingUser = await _context.Users.FindAsync(invitedByUserId);
            var invitedByName = invitingUser != null
                ? $"{invitingUser.FirstName} {invitingUser.LastName}".Trim()
                : "Store Owner";

            // Create the invitation
            var invitation = new StoreUserInvitation
            {
                StoreId = storeId,
                Email = data.Email,
                Role = data.Role,
                InvitationToken = GenerateSecureToken(),
                Status = InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(InvitationExpiry),
                InvitedByUserId = invitedByUserId
            };

            _context.StoreUserInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            // Send invitation email
            await _emailService.SendStoreInvitationEmailAsync(
                data.Email,
                store.StoreName,
                invitedByName,
                StoreRoleNames.GetDisplayName(data.Role),
                invitation.InvitationToken);

            _logger.LogInformation(
                "Invitation sent to {Email} for store {StoreId} with role {Role}",
                data.Email,
                storeId,
                data.Role);

            return InvitationResult.Success(invitation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invite user {Email} to store {StoreId}", data.Email, storeId);
            return InvitationResult.Fail("An error occurred while sending the invitation.");
        }
    }

    /// <inheritdoc />
    public async Task<InternalUserResult> AcceptInvitationAsync(string invitationToken, int acceptingUserId)
    {
        try
        {
            var invitation = await GetInvitationByTokenAsync(invitationToken);
            if (invitation == null)
            {
                return InternalUserResult.Fail("Invitation not found or has expired.");
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                return InternalUserResult.Fail("This invitation has already been used or revoked.");
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                await _context.SaveChangesAsync();
                return InternalUserResult.Fail("This invitation has expired.");
            }

            // Verify the accepting user's email matches the invitation
            var user = await _context.Users.FindAsync(acceptingUserId);
            if (user == null)
            {
                return InternalUserResult.Fail("User not found.");
            }

            if (!string.Equals(user.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
            {
                return InternalUserResult.Fail("This invitation was sent to a different email address.");
            }

            // Check if user already has a role in this store
            var existingRole = await GetUserStoreRoleAsync(invitation.StoreId, acceptingUserId);
            if (existingRole != null)
            {
                if (existingRole.IsActive)
                {
                    return InternalUserResult.Fail("You are already a member of this store.");
                }

                // Reactivate with the new role
                existingRole.IsActive = true;
                existingRole.Role = invitation.Role;
                existingRole.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create the store user role
                var storeUserRole = new StoreUserRole
                {
                    StoreId = invitation.StoreId,
                    UserId = acceptingUserId,
                    Role = invitation.Role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AssignedByUserId = invitation.InvitedByUserId
                };

                _context.StoreUserRoles.Add(storeUserRole);
            }

            // Update the invitation
            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.AcceptedByUserId = acceptingUserId;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} accepted invitation to store {StoreId} with role {Role}",
                acceptingUserId,
                invitation.StoreId,
                invitation.Role);

            return InternalUserResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept invitation with token {Token}", invitationToken);
            return InternalUserResult.Fail("An error occurred while accepting the invitation.");
        }
    }

    /// <inheritdoc />
    public async Task<InternalUserResult> ChangeUserRoleAsync(int storeId, int userId, StoreRole newRole, int changedByUserId)
    {
        try
        {
            // Get the store to verify original owner
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null)
            {
                return InternalUserResult.Fail("Store not found.");
            }

            // Validate the changing user has permission (must be store owner)
            if (!await IsStoreOwnerAsync(storeId, changedByUserId) && store.UserId != changedByUserId)
            {
                return InternalUserResult.Fail("Only store owners can change user roles.");
            }

            // Prevent changing own role (must have at least one store owner)
            if (userId == changedByUserId)
            {
                return InternalUserResult.Fail("You cannot change your own role.");
            }

            // Prevent changing original store owner's role
            if (userId == store.UserId)
            {
                return InternalUserResult.Fail("Cannot change the role of the original store owner.");
            }

            var userRole = await GetUserStoreRoleAsync(storeId, userId);
            if (userRole == null)
            {
                return InternalUserResult.Fail("User is not a member of this store.");
            }

            userRole.Role = newRole;
            userRole.UpdatedAt = DateTime.UtcNow;
            userRole.AssignedByUserId = changedByUserId;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} role changed to {Role} in store {StoreId} by {ChangedBy}",
                userId,
                newRole,
                storeId,
                changedByUserId);

            return InternalUserResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change role for user {UserId} in store {StoreId}", userId, storeId);
            return InternalUserResult.Fail("An error occurred while changing the user role.");
        }
    }

    /// <inheritdoc />
    public async Task<InternalUserResult> DeactivateUserAsync(int storeId, int userId, int deactivatedByUserId)
    {
        try
        {
            // Get the store to verify original owner
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null)
            {
                return InternalUserResult.Fail("Store not found.");
            }

            // Validate the deactivating user has permission (must be store owner)
            if (!await IsStoreOwnerAsync(storeId, deactivatedByUserId) && store.UserId != deactivatedByUserId)
            {
                return InternalUserResult.Fail("Only store owners can deactivate users.");
            }

            // Prevent deactivating self
            if (userId == deactivatedByUserId)
            {
                return InternalUserResult.Fail("You cannot deactivate yourself.");
            }

            // Prevent deactivating original store owner
            if (userId == store.UserId)
            {
                return InternalUserResult.Fail("Cannot deactivate the original store owner.");
            }

            var userRole = await GetUserStoreRoleAsync(storeId, userId);
            if (userRole == null)
            {
                return InternalUserResult.Fail("User is not a member of this store.");
            }

            if (!userRole.IsActive)
            {
                return InternalUserResult.Fail("User is already deactivated.");
            }

            userRole.IsActive = false;
            userRole.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Invalidate all sessions for this user (to immediately revoke access)
            await _sessionService.InvalidateAllUserSessionsAsync(userId);

            _logger.LogInformation(
                "User {UserId} deactivated from store {StoreId} by {DeactivatedBy}",
                userId,
                storeId,
                deactivatedByUserId);

            return InternalUserResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate user {UserId} from store {StoreId}", userId, storeId);
            return InternalUserResult.Fail("An error occurred while deactivating the user.");
        }
    }

    /// <inheritdoc />
    public async Task<InternalUserResult> ReactivateUserAsync(int storeId, int userId, int reactivatedByUserId)
    {
        try
        {
            // Get the store to verify original owner
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null)
            {
                return InternalUserResult.Fail("Store not found.");
            }

            // Validate the reactivating user has permission (must be store owner)
            if (!await IsStoreOwnerAsync(storeId, reactivatedByUserId) && store.UserId != reactivatedByUserId)
            {
                return InternalUserResult.Fail("Only store owners can reactivate users.");
            }

            var userRole = await GetUserStoreRoleAsync(storeId, userId);
            if (userRole == null)
            {
                return InternalUserResult.Fail("User is not a member of this store.");
            }

            if (userRole.IsActive)
            {
                return InternalUserResult.Fail("User is already active.");
            }

            userRole.IsActive = true;
            userRole.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} reactivated in store {StoreId} by {ReactivatedBy}",
                userId,
                storeId,
                reactivatedByUserId);

            return InternalUserResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reactivate user {UserId} in store {StoreId}", userId, storeId);
            return InternalUserResult.Fail("An error occurred while reactivating the user.");
        }
    }

    /// <inheritdoc />
    public async Task<InternalUserResult> RevokeInvitationAsync(int invitationId, int revokedByUserId)
    {
        try
        {
            var invitation = await _context.StoreUserInvitations
                .Include(i => i.Store)
                .FirstOrDefaultAsync(i => i.Id == invitationId);

            if (invitation == null)
            {
                return InternalUserResult.Fail("Invitation not found.");
            }

            // Validate the revoking user has permission (must be store owner)
            if (!await IsStoreOwnerAsync(invitation.StoreId, revokedByUserId) &&
                invitation.Store.UserId != revokedByUserId)
            {
                return InternalUserResult.Fail("Only store owners can revoke invitations.");
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                return InternalUserResult.Fail("Only pending invitations can be revoked.");
            }

            invitation.Status = InvitationStatus.Revoked;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Invitation {InvitationId} revoked by {RevokedBy}",
                invitationId,
                revokedByUserId);

            return InternalUserResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke invitation {InvitationId}", invitationId);
            return InternalUserResult.Fail("An error occurred while revoking the invitation.");
        }
    }

    /// <inheritdoc />
    public async Task<StoreUserInvitation?> GetInvitationByTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return await _context.StoreUserInvitations
            .Include(i => i.Store)
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.InvitationToken == token);
    }

    /// <inheritdoc />
    public async Task<StoreUserRole?> GetUserStoreRoleAsync(int storeId, int userId)
    {
        return await _context.StoreUserRoles
            .FirstOrDefaultAsync(sur => sur.StoreId == storeId && sur.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<bool> IsStoreOwnerAsync(int storeId, int userId)
    {
        // First check if this is the original store owner
        var store = await _context.Stores.FindAsync(storeId);
        if (store?.UserId == userId)
        {
            return true;
        }

        // Then check for StoreOwner role in StoreUserRoles
        var role = await GetUserStoreRoleAsync(storeId, userId);
        return role != null && role.IsActive && role.Role == StoreRole.StoreOwner;
    }

    /// <inheritdoc />
    public async Task<Store?> GetStoreForUserAsync(int userId)
    {
        // First check if user is the original store owner
        var ownedStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (ownedStore != null)
        {
            return ownedStore;
        }

        // Then check if user has any active store role
        var storeUserRole = await _context.StoreUserRoles
            .Where(sur => sur.UserId == userId && sur.IsActive)
            .Include(sur => sur.Store)
            .FirstOrDefaultAsync();

        return storeUserRole?.Store;
    }

    private async Task CleanupExpiredInvitationsAsync(int storeId)
    {
        var expiredInvitations = await _context.StoreUserInvitations
            .Where(i => i.StoreId == storeId &&
                       i.Status == InvitationStatus.Pending &&
                       i.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var invitation in expiredInvitations)
        {
            invitation.Status = InvitationStatus.Expired;
        }

        if (expiredInvitations.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    private static string GenerateSecureToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(TokenSizeBytes);
        return Convert.ToBase64String(tokenBytes);
    }
}
