using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a seller verification operation.
/// </summary>
public class VerificationResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public SellerVerification? Verification { get; set; }
}

/// <summary>
/// Data for company seller verification form.
/// </summary>
public class CompanyVerificationData
{
    public required string CompanyName { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string TaxId { get; set; }
    public required string RegisteredAddress { get; set; }
    public required string ContactPersonName { get; set; }
    public required string ContactPersonEmail { get; set; }
    public required string ContactPersonPhone { get; set; }
}

/// <summary>
/// Data for individual seller verification form.
/// </summary>
public class IndividualVerificationData
{
    public required string FullName { get; set; }
    public required string PersonalIdNumber { get; set; }
    public required string Address { get; set; }
    public required string ContactEmail { get; set; }
    public required string ContactPhone { get; set; }
}

/// <summary>
/// Interface for seller verification service.
/// </summary>
public interface ISellerVerificationService
{
    /// <summary>
    /// Gets the current verification for a seller.
    /// </summary>
    Task<SellerVerification?> GetVerificationAsync(int userId);

    /// <summary>
    /// Submits a company verification form.
    /// </summary>
    Task<VerificationResult> SubmitCompanyVerificationAsync(int userId, CompanyVerificationData data);

    /// <summary>
    /// Submits an individual verification form.
    /// </summary>
    Task<VerificationResult> SubmitIndividualVerificationAsync(int userId, IndividualVerificationData data);

    /// <summary>
    /// Checks if a seller can submit a verification form.
    /// </summary>
    Task<bool> CanSubmitVerificationAsync(int userId);

    /// <summary>
    /// Gets the seller type from the store (Company or Individual).
    /// </summary>
    Task<string?> GetSellerTypeAsync(int userId);

    /// <summary>
    /// Gets the current KYC status for a user.
    /// </summary>
    Task<KycStatus> GetKycStatusAsync(int userId);
}

/// <summary>
/// Service for managing seller verification forms.
/// </summary>
public class SellerVerificationService : ISellerVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SellerVerificationService> _logger;

    public SellerVerificationService(
        ApplicationDbContext context,
        ILogger<SellerVerificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SellerVerification?> GetVerificationAsync(int userId)
    {
        return await _context.SellerVerifications
            .FirstOrDefaultAsync(v => v.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<bool> CanSubmitVerificationAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.UserType != UserType.Seller)
        {
            return false;
        }

        // Cannot submit if already pending review
        if (user.KycStatus == KycStatus.Pending)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<string?> GetSellerTypeAsync(int userId)
    {
        var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
        return store?.BusinessType;
    }

    /// <inheritdoc />
    public async Task<KycStatus> GetKycStatusAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.KycStatus ?? KycStatus.NotStarted;
    }

    /// <inheritdoc />
    public async Task<VerificationResult> SubmitCompanyVerificationAsync(int userId, CompanyVerificationData data)
    {
        var result = new VerificationResult();

        // Validate user can submit
        if (!await CanSubmitVerificationAsync(userId))
        {
            result.Errors.Add("You cannot submit a verification form at this time.");
            return result;
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(data.CompanyName))
        {
            result.Errors.Add("Company name is required.");
        }
        else if (data.CompanyName.Length > 200)
        {
            result.Errors.Add("Company name must be 200 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.RegistrationNumber))
        {
            result.Errors.Add("Registration number is required.");
        }
        else if (data.RegistrationNumber.Length > 50)
        {
            result.Errors.Add("Registration number must be 50 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.TaxId))
        {
            result.Errors.Add("Tax ID is required.");
        }
        else if (data.TaxId.Length > 50)
        {
            result.Errors.Add("Tax ID must be 50 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.RegisteredAddress))
        {
            result.Errors.Add("Registered address is required.");
        }
        else if (data.RegisteredAddress.Length > 500)
        {
            result.Errors.Add("Registered address must be 500 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.ContactPersonName))
        {
            result.Errors.Add("Contact person name is required.");
        }
        else if (data.ContactPersonName.Length > 200)
        {
            result.Errors.Add("Contact person name must be 200 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.ContactPersonEmail))
        {
            result.Errors.Add("Contact person email is required.");
        }
        else if (data.ContactPersonEmail.Length > 256)
        {
            result.Errors.Add("Contact person email must be 256 characters or less.");
        }
        else if (!IsValidEmail(data.ContactPersonEmail))
        {
            result.Errors.Add("Contact person email is not a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(data.ContactPersonPhone))
        {
            result.Errors.Add("Contact person phone is required.");
        }
        else if (data.ContactPersonPhone.Length > 20)
        {
            result.Errors.Add("Contact person phone must be 20 characters or less.");
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        // Check for existing verification and update or create
        var verification = await _context.SellerVerifications.FirstOrDefaultAsync(v => v.UserId == userId);
        if (verification == null)
        {
            verification = new SellerVerification
            {
                UserId = userId,
                SellerType = "Company",
                SubmittedAt = DateTime.UtcNow
            };
            _context.SellerVerifications.Add(verification);
        }

        verification.SellerType = "Company";
        verification.CompanyName = data.CompanyName.Trim();
        verification.RegistrationNumber = data.RegistrationNumber.Trim();
        verification.TaxId = data.TaxId.Trim();
        verification.RegisteredAddress = data.RegisteredAddress.Trim();
        verification.ContactPersonName = data.ContactPersonName.Trim();
        verification.ContactPersonEmail = data.ContactPersonEmail.Trim();
        verification.ContactPersonPhone = data.ContactPersonPhone.Trim();
        verification.UpdatedAt = DateTime.UtcNow;

        // Clear individual-specific fields
        verification.FullName = null;
        verification.PersonalIdNumber = null;
        verification.Address = null;
        verification.ContactEmail = null;
        verification.ContactPhone = null;

        // Update user KYC status to Pending
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.KycStatus = KycStatus.Pending;
            user.KycSubmittedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Company verification submitted for user {UserId}", userId);

        result.Success = true;
        result.Verification = verification;
        return result;
    }

    /// <inheritdoc />
    public async Task<VerificationResult> SubmitIndividualVerificationAsync(int userId, IndividualVerificationData data)
    {
        var result = new VerificationResult();

        // Validate user can submit
        if (!await CanSubmitVerificationAsync(userId))
        {
            result.Errors.Add("You cannot submit a verification form at this time.");
            return result;
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(data.FullName))
        {
            result.Errors.Add("Full name is required.");
        }
        else if (data.FullName.Length > 200)
        {
            result.Errors.Add("Full name must be 200 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.PersonalIdNumber))
        {
            result.Errors.Add("Personal ID number is required.");
        }
        else if (data.PersonalIdNumber.Length > 50)
        {
            result.Errors.Add("Personal ID number must be 50 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.Address))
        {
            result.Errors.Add("Address is required.");
        }
        else if (data.Address.Length > 500)
        {
            result.Errors.Add("Address must be 500 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.ContactEmail))
        {
            result.Errors.Add("Contact email is required.");
        }
        else if (data.ContactEmail.Length > 256)
        {
            result.Errors.Add("Contact email must be 256 characters or less.");
        }
        else if (!IsValidEmail(data.ContactEmail))
        {
            result.Errors.Add("Contact email is not a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(data.ContactPhone))
        {
            result.Errors.Add("Contact phone is required.");
        }
        else if (data.ContactPhone.Length > 20)
        {
            result.Errors.Add("Contact phone must be 20 characters or less.");
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        // Check for existing verification and update or create
        var verification = await _context.SellerVerifications.FirstOrDefaultAsync(v => v.UserId == userId);
        if (verification == null)
        {
            verification = new SellerVerification
            {
                UserId = userId,
                SellerType = "Individual",
                SubmittedAt = DateTime.UtcNow
            };
            _context.SellerVerifications.Add(verification);
        }

        verification.SellerType = "Individual";
        verification.FullName = data.FullName.Trim();
        verification.PersonalIdNumber = data.PersonalIdNumber.Trim();
        verification.Address = data.Address.Trim();
        verification.ContactEmail = data.ContactEmail.Trim();
        verification.ContactPhone = data.ContactPhone.Trim();
        verification.UpdatedAt = DateTime.UtcNow;

        // Clear company-specific fields
        verification.CompanyName = null;
        verification.RegistrationNumber = null;
        verification.TaxId = null;
        verification.RegisteredAddress = null;
        verification.ContactPersonName = null;
        verification.ContactPersonEmail = null;
        verification.ContactPersonPhone = null;

        // Update user KYC status to Pending
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.KycStatus = KycStatus.Pending;
            user.KycSubmittedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Individual verification submitted for user {UserId}", userId);

        result.Success = true;
        result.Verification = verification;
        return result;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
