using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a seller onboarding operation.
/// </summary>
public class OnboardingResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public SellerOnboardingDraft? Draft { get; set; }
    public Store? Store { get; set; }
}

/// <summary>
/// Data for Step 1: Store Profile Basics.
/// </summary>
public class StoreProfileData
{
    public required string StoreName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// Data for Step 2: Verification Data.
/// </summary>
public class VerificationData
{
    public required string BusinessType { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? TaxId { get; set; }
}

/// <summary>
/// Data for Step 3: Payout Basics.
/// </summary>
public class PayoutData
{
    public required string BankName { get; set; }
    public required string BankAccountHolderName { get; set; }
    public required string BankAccountNumber { get; set; }
    public string? BankRoutingNumber { get; set; }
}

/// <summary>
/// Interface for seller onboarding service.
/// </summary>
public interface ISellerOnboardingService
{
    /// <summary>
    /// Gets or creates an onboarding draft for a user.
    /// </summary>
    Task<SellerOnboardingDraft> GetOrCreateDraftAsync(int userId);

    /// <summary>
    /// Saves Step 1 (Store Profile) data.
    /// </summary>
    Task<OnboardingResult> SaveStoreProfileAsync(int userId, StoreProfileData data);

    /// <summary>
    /// Saves Step 2 (Verification) data.
    /// </summary>
    Task<OnboardingResult> SaveVerificationDataAsync(int userId, VerificationData data);

    /// <summary>
    /// Saves Step 3 (Payout) data.
    /// </summary>
    Task<OnboardingResult> SavePayoutDataAsync(int userId, PayoutData data);

    /// <summary>
    /// Completes the onboarding and creates the store.
    /// </summary>
    Task<OnboardingResult> CompleteOnboardingAsync(int userId);

    /// <summary>
    /// Checks if a user has an existing store.
    /// </summary>
    Task<bool> HasExistingStoreAsync(int userId);

    /// <summary>
    /// Gets the store for a user.
    /// </summary>
    Task<Store?> GetStoreAsync(int userId);
}

/// <summary>
/// Service for managing seller onboarding wizard.
/// </summary>
public class SellerOnboardingService : ISellerOnboardingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SellerOnboardingService> _logger;

    public SellerOnboardingService(
        ApplicationDbContext context,
        ILogger<SellerOnboardingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SellerOnboardingDraft> GetOrCreateDraftAsync(int userId)
    {
        var draft = await _context.SellerOnboardingDrafts
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (draft == null)
        {
            draft = new SellerOnboardingDraft
            {
                UserId = userId,
                CurrentStep = 1,
                LastCompletedStep = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.SellerOnboardingDrafts.Add(draft);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created new onboarding draft for user {UserId}", userId);
        }

        return draft;
    }

    /// <inheritdoc />
    public async Task<OnboardingResult> SaveStoreProfileAsync(int userId, StoreProfileData data)
    {
        var result = new OnboardingResult();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(data.StoreName))
        {
            result.Errors.Add("Store name is required.");
        }
        else if (data.StoreName.Length > 100)
        {
            result.Errors.Add("Store name must be 100 characters or less.");
        }
        else
        {
            // Check store name uniqueness
            var normalizedName = data.StoreName.Trim().ToLowerInvariant();
            var storeNameExists = await _context.Stores.AnyAsync(s => s.StoreName.ToLower() == normalizedName);
            if (storeNameExists)
            {
                result.Errors.Add("A store with this name already exists. Please choose a different name.");
            }
        }

        if (data.Description?.Length > 1000)
        {
            result.Errors.Add("Description must be 1000 characters or less.");
        }

        if (data.Category?.Length > 100)
        {
            result.Errors.Add("Category must be 100 characters or less.");
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        var draft = await GetOrCreateDraftAsync(userId);

        draft.StoreName = data.StoreName.Trim();
        draft.Description = data.Description?.Trim();
        draft.Category = data.Category?.Trim();
        draft.LastCompletedStep = Math.Max(draft.LastCompletedStep, 1);
        draft.CurrentStep = 2;
        draft.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Saved store profile for user {UserId}", userId);

        result.Success = true;
        result.Draft = draft;
        return result;
    }

    /// <inheritdoc />
    public async Task<OnboardingResult> SaveVerificationDataAsync(int userId, VerificationData data)
    {
        var result = new OnboardingResult();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(data.BusinessType))
        {
            result.Errors.Add("Business type is required.");
        }
        else if (data.BusinessType != "Individual" && data.BusinessType != "Business")
        {
            result.Errors.Add("Business type must be 'Individual' or 'Business'.");
        }

        // For business type, registration number is required
        if (data.BusinessType == "Business" && string.IsNullOrWhiteSpace(data.BusinessRegistrationNumber))
        {
            result.Errors.Add("Business registration number is required for business accounts.");
        }

        if (data.BusinessRegistrationNumber?.Length > 50)
        {
            result.Errors.Add("Business registration number must be 50 characters or less.");
        }

        if (data.TaxId?.Length > 50)
        {
            result.Errors.Add("Tax ID must be 50 characters or less.");
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        var draft = await GetOrCreateDraftAsync(userId);

        // Ensure Step 1 is completed
        if (draft.LastCompletedStep < 1)
        {
            result.Errors.Add("Please complete Step 1 (Store Profile) first.");
            return result;
        }

        draft.BusinessType = data.BusinessType.Trim();
        draft.BusinessRegistrationNumber = data.BusinessRegistrationNumber?.Trim();
        draft.TaxId = data.TaxId?.Trim();
        draft.LastCompletedStep = Math.Max(draft.LastCompletedStep, 2);
        draft.CurrentStep = 3;
        draft.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Saved verification data for user {UserId}", userId);

        result.Success = true;
        result.Draft = draft;
        return result;
    }

    /// <inheritdoc />
    public async Task<OnboardingResult> SavePayoutDataAsync(int userId, PayoutData data)
    {
        var result = new OnboardingResult();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(data.BankName))
        {
            result.Errors.Add("Bank name is required.");
        }
        else if (data.BankName.Length > 100)
        {
            result.Errors.Add("Bank name must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.BankAccountHolderName))
        {
            result.Errors.Add("Bank account holder name is required.");
        }
        else if (data.BankAccountHolderName.Length > 100)
        {
            result.Errors.Add("Bank account holder name must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.BankAccountNumber))
        {
            result.Errors.Add("Bank account number is required.");
        }
        else if (data.BankAccountNumber.Length > 100)
        {
            result.Errors.Add("Bank account number must be 100 characters or less.");
        }

        if (data.BankRoutingNumber?.Length > 50)
        {
            result.Errors.Add("Bank routing number must be 50 characters or less.");
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        var draft = await GetOrCreateDraftAsync(userId);

        // Ensure Step 2 is completed
        if (draft.LastCompletedStep < 2)
        {
            result.Errors.Add("Please complete Step 2 (Verification) first.");
            return result;
        }

        draft.BankName = data.BankName.Trim();
        draft.BankAccountHolderName = data.BankAccountHolderName.Trim();
        draft.BankAccountNumber = data.BankAccountNumber.Trim();
        draft.BankRoutingNumber = data.BankRoutingNumber?.Trim();
        draft.LastCompletedStep = Math.Max(draft.LastCompletedStep, 3);
        draft.CurrentStep = 4; // Move to completion step
        draft.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Saved payout data for user {UserId}", userId);

        result.Success = true;
        result.Draft = draft;
        return result;
    }

    /// <inheritdoc />
    public async Task<OnboardingResult> CompleteOnboardingAsync(int userId)
    {
        var result = new OnboardingResult();

        var draft = await _context.SellerOnboardingDrafts
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (draft == null)
        {
            result.Errors.Add("No onboarding draft found. Please start the onboarding wizard.");
            return result;
        }

        // Validate all steps are completed
        if (draft.LastCompletedStep < 3)
        {
            result.Errors.Add($"Please complete all onboarding steps before submission. Current progress: Step {draft.LastCompletedStep} of 3.");
            return result;
        }

        // Validate required fields one more time
        if (string.IsNullOrWhiteSpace(draft.StoreName))
        {
            result.Errors.Add("Store name is required.");
        }

        if (string.IsNullOrWhiteSpace(draft.BusinessType))
        {
            result.Errors.Add("Business type is required.");
        }

        if (string.IsNullOrWhiteSpace(draft.BankName) ||
            string.IsNullOrWhiteSpace(draft.BankAccountHolderName) ||
            string.IsNullOrWhiteSpace(draft.BankAccountNumber))
        {
            result.Errors.Add("Bank details are required.");
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        // Check if store already exists
        var existingStore = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
        if (existingStore != null)
        {
            result.Errors.Add("A store already exists for this account.");
            return result;
        }

        // Create the store
        var store = new Store
        {
            UserId = userId,
            StoreName = draft.StoreName!,
            Description = draft.Description,
            Category = draft.Category,
            BusinessType = draft.BusinessType,
            BusinessRegistrationNumber = draft.BusinessRegistrationNumber,
            BankName = draft.BankName,
            BankAccountHolderName = draft.BankAccountHolderName,
            BankAccountNumber = draft.BankAccountNumber,
            BankRoutingNumber = draft.BankRoutingNumber,
            Status = StoreStatus.PendingVerification,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Stores.Add(store);

        // Update user's TaxId if provided
        if (!string.IsNullOrWhiteSpace(draft.TaxId))
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.TaxId = draft.TaxId;
            }
        }

        // Remove the draft
        _context.SellerOnboardingDrafts.Remove(draft);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Completed onboarding for user {UserId}, created store {StoreId}", userId, store.Id);

        result.Success = true;
        result.Store = store;
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> HasExistingStoreAsync(int userId)
    {
        return await _context.Stores.AnyAsync(s => s.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<Store?> GetStoreAsync(int userId)
    {
        return await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
    }
}
