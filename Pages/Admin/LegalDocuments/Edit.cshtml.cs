using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Pages.Admin.LegalDocuments;

/// <summary>
/// Page model for creating or editing a legal document version.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly ILegalDocumentService _legalDocumentService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        ILegalDocumentService legalDocumentService,
        ILogger<EditModel> logger)
    {
        _legalDocumentService = legalDocumentService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    public int? DocumentId { get; set; }
    public LegalDocumentType DocumentType { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    public bool IsEdit { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [MaxLength(20, ErrorMessage = "Version number must be 20 characters or less.")]
        [Display(Name = "Version Number")]
        public string Version { get; set; } = null!;

        [Required]
        [MaxLength(200, ErrorMessage = "Title must be 200 characters or less.")]
        [Display(Name = "Document Title")]
        public string Title { get; set; } = null!;

        [Required]
        [Display(Name = "Content (HTML)")]
        public string Content { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Effective Date")]
        public DateTime EffectiveDate { get; set; }

        [Display(Name = "Activate Immediately")]
        public bool IsActive { get; set; }

        [MaxLength(1000, ErrorMessage = "Change notes must be 1000 characters or less.")]
        [Display(Name = "Change Notes")]
        public string? ChangeNotes { get; set; }

        [MaxLength(10)]
        [Display(Name = "Language Code")]
        public string LanguageCode { get; set; } = "en";
    }

    public async Task<IActionResult> OnGetAsync(int? id, int? type)
    {
        if (id.HasValue)
        {
            // Edit existing document
            var document = await _legalDocumentService.GetDocumentByIdAsync(id.Value);
            if (document == null)
            {
                ErrorMessage = "Legal document not found.";
                return RedirectToPage("./Index");
            }

            DocumentId = id.Value;
            DocumentType = document.DocumentType;
            IsEdit = true;

            Input = new InputModel
            {
                Version = document.Version,
                Title = document.Title,
                Content = document.Content,
                EffectiveDate = document.EffectiveDate,
                IsActive = document.IsActive,
                ChangeNotes = document.ChangeNotes,
                LanguageCode = document.LanguageCode
            };
        }
        else if (type.HasValue)
        {
            // Create new version
            DocumentType = (LegalDocumentType)type.Value;
            IsEdit = false;

            // Get the latest version to suggest next version number
            var history = await _legalDocumentService.GetDocumentHistoryAsync(DocumentType);
            var latestVersion = history.FirstOrDefault();

            Input = new InputModel
            {
                Version = latestVersion != null ? IncrementVersion(latestVersion.Version) : "1.0",
                Title = GetDefaultTitle(DocumentType),
                Content = string.Empty,
                EffectiveDate = DateTime.UtcNow,
                IsActive = false,
                LanguageCode = "en"
            };
        }
        else
        {
            ErrorMessage = "Invalid request. Document ID or type is required.";
            return RedirectToPage("./Index");
        }

        DocumentTypeName = IndexModel.GetDocumentTypeName(DocumentType);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id, int type)
    {
        DocumentType = (LegalDocumentType)type;
        DocumentTypeName = IndexModel.GetDocumentTypeName(DocumentType);
        IsEdit = id.HasValue;
        DocumentId = id;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                ErrorMessage = "Unable to identify current admin user.";
                return Page();
            }

            if (IsEdit)
            {
                // Update existing document
                var existingDoc = await _legalDocumentService.GetDocumentByIdAsync(id!.Value);
                if (existingDoc == null)
                {
                    ErrorMessage = "Legal document not found.";
                    return RedirectToPage("./Index");
                }

                existingDoc.Version = Input.Version;
                existingDoc.Title = Input.Title;
                existingDoc.Content = Input.Content;
                existingDoc.EffectiveDate = Input.EffectiveDate;
                existingDoc.IsActive = Input.IsActive;
                existingDoc.ChangeNotes = Input.ChangeNotes;
                existingDoc.LanguageCode = Input.LanguageCode;

                var success = await _legalDocumentService.UpdateDocumentAsync(existingDoc, userId);
                if (success)
                {
                    SuccessMessage = $"Legal document version '{Input.Version}' updated successfully.";
                    return RedirectToPage("./History", new { type = (int)DocumentType });
                }
                else
                {
                    ErrorMessage = "Failed to update legal document.";
                    return Page();
                }
            }
            else
            {
                // Create new document version
                var newDoc = new LegalDocument
                {
                    DocumentType = DocumentType,
                    Version = Input.Version,
                    Title = Input.Title,
                    Content = Input.Content,
                    EffectiveDate = Input.EffectiveDate,
                    IsActive = Input.IsActive,
                    ChangeNotes = Input.ChangeNotes,
                    LanguageCode = Input.LanguageCode
                };

                await _legalDocumentService.CreateDocumentAsync(newDoc, userId);
                SuccessMessage = $"Legal document version '{Input.Version}' created successfully.";
                return RedirectToPage("./History", new { type = (int)DocumentType });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving legal document");
            ErrorMessage = "An error occurred while saving the legal document.";
            return Page();
        }
    }

    private static string IncrementVersion(string version)
    {
        // Simple version increment logic (e.g., "1.0" -> "1.1", "1.9" -> "2.0")
        if (version.Contains('.'))
        {
            var parts = version.Split('.');
            if (parts.Length == 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
            {
                minor++;
                if (minor >= 10)
                {
                    major++;
                    minor = 0;
                }
                return $"{major}.{minor}";
            }
        }
        // If version format is not recognized, suggest a default next version
        return "2.0";
    }

    private static string GetDefaultTitle(LegalDocumentType type)
    {
        return type switch
        {
            LegalDocumentType.TermsOfService => "Terms of Service",
            LegalDocumentType.PrivacyPolicy => "Privacy Policy",
            LegalDocumentType.CookiePolicy => "Cookie Policy",
            LegalDocumentType.SellerAgreement => "Seller Agreement",
            _ => "Legal Document"
        };
    }
}
