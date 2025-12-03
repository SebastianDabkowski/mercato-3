using System.ComponentModel.DataAnnotations;
using MercatoApp.Models;
using MercatoApp.Services;
using MercatoApp.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly IUserRegistrationService _registrationService;
    private readonly IPasswordValidationService _passwordValidation;
    private readonly IConfiguration _configuration;
    private readonly IConsentManagementService _consentService;
    private readonly ILegalDocumentService _legalDocumentService;

    public RegisterModel(
        IUserRegistrationService registrationService,
        IPasswordValidationService passwordValidation,
        IConfiguration configuration,
        IConsentManagementService consentService,
        ILegalDocumentService legalDocumentService)
    {
        _registrationService = registrationService;
        _passwordValidation = passwordValidation;
        _configuration = configuration;
        _consentService = consentService;
        _legalDocumentService = legalDocumentService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<string> ExternalProviders { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Please select whether you want to register as a buyer or seller.")]
        [Display(Name = "Account Type")]
        public UserType UserType { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Address")]
        [MaxLength(200)]
        public string? Address { get; set; }

        [Display(Name = "City")]
        [MaxLength(100)]
        public string? City { get; set; }

        [Display(Name = "Postal Code")]
        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [Display(Name = "Country")]
        [MaxLength(100)]
        public string? Country { get; set; }

        [Display(Name = "Tax ID / VAT Number")]
        [MaxLength(50)]
        public string? TaxId { get; set; }

        [MustBeTrue(ErrorMessage = "You must accept the terms and conditions.")]
        [Display(Name = "I accept the terms and conditions")]
        public bool AcceptTerms { get; set; }

        [Display(Name = "I want to receive newsletters")]
        public bool AcceptNewsletter { get; set; }

        [Display(Name = "I want to receive marketing communications")]
        public bool AcceptMarketing { get; set; }

        [Display(Name = "I consent to personalization and profiling")]
        public bool AcceptProfiling { get; set; }
    }

    public void OnGet()
    {
        LoadExternalProviders();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadExternalProviders();

        // Validate password with our custom service
        var passwordValidation = _passwordValidation.Validate(Input.Password);
        if (!passwordValidation.IsValid)
        {
            foreach (var error in passwordValidation.Errors)
            {
                ModelState.AddModelError("Input.Password", error);
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var registrationData = new RegistrationData
        {
            Email = Input.Email,
            Password = Input.Password,
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            PhoneNumber = Input.PhoneNumber,
            Address = Input.Address,
            City = Input.City,
            PostalCode = Input.PostalCode,
            Country = Input.Country,
            TaxId = Input.TaxId,
            UserType = Input.UserType,
            AcceptedTerms = Input.AcceptTerms
        };

        var result = await _registrationService.RegisterAsync(registrationData);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        // Record consents for the newly registered user
        if (result.User != null)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            // Record required consents (Terms of Service and Privacy Policy)
            var tosDocument = await _legalDocumentService.GetActiveDocumentAsync(LegalDocumentType.TermsOfService);
            if (tosDocument != null)
            {
                await _consentService.RecordConsentAsync(
                    result.User.Id,
                    ConsentType.TermsOfService,
                    isGranted: true,
                    version: tosDocument.Version,
                    consentText: "I accept the Terms of Service",
                    ipAddress,
                    userAgent,
                    context: "registration",
                    legalDocumentId: tosDocument.Id);
            }

            var ppDocument = await _legalDocumentService.GetActiveDocumentAsync(LegalDocumentType.PrivacyPolicy);
            if (ppDocument != null)
            {
                await _consentService.RecordConsentAsync(
                    result.User.Id,
                    ConsentType.PrivacyPolicy,
                    isGranted: true,
                    version: ppDocument.Version,
                    consentText: "I accept the Privacy Policy",
                    ipAddress,
                    userAgent,
                    context: "registration",
                    legalDocumentId: ppDocument.Id);
            }

            // Record optional consents
            if (Input.AcceptNewsletter)
            {
                await _consentService.GrantConsentAsync(
                    result.User.Id,
                    ConsentType.Newsletter,
                    version: "1.0",
                    consentText: "I agree to receive newsletters and general updates from Mercato.",
                    ipAddress,
                    userAgent,
                    context: "registration");
            }

            if (Input.AcceptMarketing)
            {
                await _consentService.GrantConsentAsync(
                    result.User.Id,
                    ConsentType.Marketing,
                    version: "1.0",
                    consentText: "I agree to receive marketing communications and promotional offers from Mercato.",
                    ipAddress,
                    userAgent,
                    context: "registration");
            }

            if (Input.AcceptProfiling)
            {
                await _consentService.GrantConsentAsync(
                    result.User.Id,
                    ConsentType.Profiling,
                    version: "1.0",
                    consentText: "I agree to allow Mercato to analyze my behavior and provide personalized recommendations.",
                    ipAddress,
                    userAgent,
                    context: "registration");
            }
        }

        return RedirectToPage("RegisterConfirmation");
    }

    private void LoadExternalProviders()
    {
        // Check if Google is configured
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
        {
            ExternalProviders.Add("Google");
        }

        // Check if Facebook is configured
        var facebookAppId = _configuration["Authentication:Facebook:AppId"];
        var facebookAppSecret = _configuration["Authentication:Facebook:AppSecret"];
        if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
        {
            ExternalProviders.Add("Facebook");
        }
    }
}
