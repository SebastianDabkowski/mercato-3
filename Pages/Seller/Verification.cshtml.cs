using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class VerificationModel : PageModel
{
    private readonly ISellerVerificationService _verificationService;
    private readonly ILogger<VerificationModel> _logger;

    public VerificationModel(
        ISellerVerificationService verificationService,
        ILogger<VerificationModel> logger)
    {
        _verificationService = verificationService;
        _logger = logger;
    }

    [BindProperty]
    public CompanyInputModel CompanyInput { get; set; } = new();

    [BindProperty]
    public IndividualInputModel IndividualInput { get; set; } = new();

    public string? SellerType { get; set; }
    public KycStatus CurrentKycStatus { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool CanSubmit { get; set; }
    public SellerVerification? ExistingVerification { get; set; }

    public class CompanyInputModel
    {
        [Required(ErrorMessage = "Company name is required.")]
        [MaxLength(200, ErrorMessage = "Company name must be 200 characters or less.")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Registration number is required.")]
        [MaxLength(50, ErrorMessage = "Registration number must be 50 characters or less.")]
        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tax ID is required.")]
        [MaxLength(50, ErrorMessage = "Tax ID must be 50 characters or less.")]
        [Display(Name = "Tax ID")]
        public string TaxId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Registered address is required.")]
        [MaxLength(500, ErrorMessage = "Registered address must be 500 characters or less.")]
        [Display(Name = "Registered Address")]
        public string RegisteredAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person name is required.")]
        [MaxLength(200, ErrorMessage = "Contact person name must be 200 characters or less.")]
        [Display(Name = "Contact Person Name")]
        public string ContactPersonName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person email is required.")]
        [MaxLength(256, ErrorMessage = "Contact person email must be 256 characters or less.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Contact Person Email")]
        public string ContactPersonEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person phone is required.")]
        [MaxLength(20, ErrorMessage = "Contact person phone must be 20 characters or less.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Contact Person Phone")]
        public string ContactPersonPhone { get; set; } = string.Empty;
    }

    public class IndividualInputModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(200, ErrorMessage = "Full name must be 200 characters or less.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Personal ID number is required.")]
        [MaxLength(50, ErrorMessage = "Personal ID number must be 50 characters or less.")]
        [Display(Name = "Personal ID Number")]
        public string PersonalIdNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(500, ErrorMessage = "Address must be 500 characters or less.")]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact email is required.")]
        [MaxLength(256, ErrorMessage = "Contact email must be 256 characters or less.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact phone is required.")]
        [MaxLength(20, ErrorMessage = "Contact phone must be 20 characters or less.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Contact Phone")]
        public string ContactPhone { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadPageDataAsync(userId.Value);

        // If approved, redirect to dashboard
        if (CurrentKycStatus == KycStatus.Approved)
        {
            return RedirectToPage("/Index");
        }

        // Pre-populate form with existing verification data if available
        if (ExistingVerification != null)
        {
            if (SellerType == "Business")
            {
                CompanyInput.CompanyName = ExistingVerification.CompanyName ?? string.Empty;
                CompanyInput.RegistrationNumber = ExistingVerification.RegistrationNumber ?? string.Empty;
                CompanyInput.TaxId = ExistingVerification.TaxId ?? string.Empty;
                CompanyInput.RegisteredAddress = ExistingVerification.RegisteredAddress ?? string.Empty;
                CompanyInput.ContactPersonName = ExistingVerification.ContactPersonName ?? string.Empty;
                CompanyInput.ContactPersonEmail = ExistingVerification.ContactPersonEmail ?? string.Empty;
                CompanyInput.ContactPersonPhone = ExistingVerification.ContactPersonPhone ?? string.Empty;
            }
            else if (SellerType == "Individual")
            {
                IndividualInput.FullName = ExistingVerification.FullName ?? string.Empty;
                IndividualInput.PersonalIdNumber = ExistingVerification.PersonalIdNumber ?? string.Empty;
                IndividualInput.Address = ExistingVerification.Address ?? string.Empty;
                IndividualInput.ContactEmail = ExistingVerification.ContactEmail ?? string.Empty;
                IndividualInput.ContactPhone = ExistingVerification.ContactPhone ?? string.Empty;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCompanyAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadPageDataAsync(userId.Value);

        if (!CanSubmit)
        {
            ModelState.AddModelError(string.Empty, "You cannot submit a verification form at this time.");
            return Page();
        }

        // Clear all model state and only validate company input
        ModelState.Clear();
        if (!TryValidateModel(CompanyInput, nameof(CompanyInput)))
        {
            return Page();
        }

        var data = new CompanyVerificationData
        {
            CompanyName = CompanyInput.CompanyName,
            RegistrationNumber = CompanyInput.RegistrationNumber,
            TaxId = CompanyInput.TaxId,
            RegisteredAddress = CompanyInput.RegisteredAddress,
            ContactPersonName = CompanyInput.ContactPersonName,
            ContactPersonEmail = CompanyInput.ContactPersonEmail,
            ContactPersonPhone = CompanyInput.ContactPersonPhone
        };

        var result = await _verificationService.SubmitCompanyVerificationAsync(userId.Value, data);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        _logger.LogInformation("Company verification form submitted for user {UserId}", userId.Value);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostIndividualAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadPageDataAsync(userId.Value);

        if (!CanSubmit)
        {
            ModelState.AddModelError(string.Empty, "You cannot submit a verification form at this time.");
            return Page();
        }

        // Clear all model state and only validate individual input
        ModelState.Clear();
        if (!TryValidateModel(IndividualInput, nameof(IndividualInput)))
        {
            return Page();
        }

        var data = new IndividualVerificationData
        {
            FullName = IndividualInput.FullName,
            PersonalIdNumber = IndividualInput.PersonalIdNumber,
            Address = IndividualInput.Address,
            ContactEmail = IndividualInput.ContactEmail,
            ContactPhone = IndividualInput.ContactPhone
        };

        var result = await _verificationService.SubmitIndividualVerificationAsync(userId.Value, data);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        _logger.LogInformation("Individual verification form submitted for user {UserId}", userId.Value);

        return RedirectToPage();
    }

    private async Task LoadPageDataAsync(int userId)
    {
        SellerType = await _verificationService.GetSellerTypeAsync(userId);
        CanSubmit = await _verificationService.CanSubmitVerificationAsync(userId);
        ExistingVerification = await _verificationService.GetVerificationAsync(userId);
        CurrentKycStatus = await _verificationService.GetKycStatusAsync(userId);

        if (ExistingVerification != null)
        {
            SubmittedAt = ExistingVerification.SubmittedAt;
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
