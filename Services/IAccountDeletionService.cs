using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for account deletion service.
/// </summary>
public interface IAccountDeletionService
{
    /// <summary>
    /// Checks if a user account can be deleted.
    /// Validates that there are no blocking conditions such as unresolved disputes or open chargebacks.
    /// </summary>
    /// <param name="userId">The ID of the user to check.</param>
    /// <returns>A result indicating whether deletion is allowed and any blocking reasons.</returns>
    Task<AccountDeletionValidationResult> ValidateAccountDeletionAsync(int userId);

    /// <summary>
    /// Deletes and anonymizes a user account.
    /// This operation is irreversible and removes all personal identifiable information.
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <param name="ipAddress">The IP address from which the deletion was requested.</param>
    /// <param name="reason">Optional reason provided by the user.</param>
    /// <returns>A result indicating success or failure with details.</returns>
    Task<AccountDeletionResult> DeleteAccountAsync(int userId, string? ipAddress, string? reason);

    /// <summary>
    /// Gets the deletion impact summary for a user account.
    /// Provides information about what data will be anonymized and what will be retained.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A summary of the deletion impact.</returns>
    Task<AccountDeletionImpact> GetDeletionImpactAsync(int userId);
}

/// <summary>
/// Represents the result of account deletion validation.
/// </summary>
public class AccountDeletionValidationResult
{
    /// <summary>
    /// Gets or sets whether the account can be deleted.
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Gets or sets the list of blocking reasons preventing deletion.
    /// </summary>
    public List<string> BlockingReasons { get; set; } = new List<string>();
}

/// <summary>
/// Represents the result of an account deletion operation.
/// </summary>
public class AccountDeletionResult
{
    /// <summary>
    /// Gets or sets whether the deletion was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if deletion failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the anonymized email after successful deletion.
    /// </summary>
    public string? AnonymizedEmail { get; set; }

    /// <summary>
    /// Gets or sets the deletion log ID for audit trail.
    /// </summary>
    public int? DeletionLogId { get; set; }
}

/// <summary>
/// Represents the impact summary of account deletion.
/// </summary>
public class AccountDeletionImpact
{
    /// <summary>
    /// Gets or sets the number of orders associated with the account.
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// Gets or sets the number of return requests associated with the account.
    /// </summary>
    public int ReturnRequestCount { get; set; }

    /// <summary>
    /// Gets or sets the number of addresses associated with the account.
    /// </summary>
    public int AddressCount { get; set; }

    /// <summary>
    /// Gets or sets whether the user has a store (for sellers).
    /// </summary>
    public bool HasStore { get; set; }

    /// <summary>
    /// Gets or sets the store name if the user is a seller.
    /// </summary>
    public string? StoreName { get; set; }
}
