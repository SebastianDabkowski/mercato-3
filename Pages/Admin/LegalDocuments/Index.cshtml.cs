using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.LegalDocuments;

/// <summary>
/// Page model for listing and managing legal documents.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly ILegalDocumentService _legalDocumentService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ILegalDocumentService legalDocumentService,
        ILogger<IndexModel> logger)
    {
        _legalDocumentService = legalDocumentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the dictionary of active documents by type.
    /// </summary>
    public Dictionary<LegalDocumentType, LegalDocument?> ActiveDocuments { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            ActiveDocuments = await _legalDocumentService.GetAllActiveDocumentsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading legal documents");
            ErrorMessage = "An error occurred while loading legal documents.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var document = await _legalDocumentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                ErrorMessage = "Legal document not found.";
                return RedirectToPage();
            }

            var success = await _legalDocumentService.DeleteDocumentAsync(id);
            if (success)
            {
                SuccessMessage = $"Legal document version '{document.Version}' deleted successfully.";
            }
            else
            {
                ErrorMessage = "Cannot delete active document or document with associated consents.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting legal document {DocumentId}", id);
            ErrorMessage = "An error occurred while deleting the legal document.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(int id)
    {
        try
        {
            var success = await _legalDocumentService.ActivateDocumentAsync(id);
            if (success)
            {
                SuccessMessage = "Legal document activated successfully.";
            }
            else
            {
                ErrorMessage = "Cannot activate future-dated document or document not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating legal document {DocumentId}", id);
            ErrorMessage = "An error occurred while activating the legal document.";
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Gets a friendly display name for a document type.
    /// </summary>
    public static string GetDocumentTypeName(LegalDocumentType type)
    {
        return type switch
        {
            LegalDocumentType.TermsOfService => "Terms of Service",
            LegalDocumentType.PrivacyPolicy => "Privacy Policy",
            LegalDocumentType.CookiePolicy => "Cookie Policy",
            LegalDocumentType.SellerAgreement => "Seller Agreement",
            _ => type.ToString()
        };
    }
}
