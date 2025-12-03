using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.LegalDocuments;

/// <summary>
/// Page model for viewing all versions of a legal document type.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class HistoryModel : PageModel
{
    private readonly ILegalDocumentService _legalDocumentService;
    private readonly ILogger<HistoryModel> _logger;

    public HistoryModel(
        ILegalDocumentService legalDocumentService,
        ILogger<HistoryModel> logger)
    {
        _legalDocumentService = legalDocumentService;
        _logger = logger;
    }

    public LegalDocumentType DocumentType { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    public List<LegalDocument> Documents { get; set; } = new();
    public List<LegalDocument> FutureDocuments { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int type)
    {
        try
        {
            DocumentType = (LegalDocumentType)type;
            DocumentTypeName = IndexModel.GetDocumentTypeName(DocumentType);

            var allDocuments = await _legalDocumentService.GetDocumentHistoryAsync(DocumentType);
            var now = DateTime.UtcNow;

            Documents = allDocuments.Where(d => d.EffectiveDate <= now).ToList();
            FutureDocuments = allDocuments.Where(d => d.EffectiveDate > now).ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading legal document history for type {DocumentType}", type);
            ErrorMessage = "An error occurred while loading the document history.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, int type)
    {
        try
        {
            var document = await _legalDocumentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                ErrorMessage = "Legal document not found.";
                return RedirectToPage(new { type });
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

        return RedirectToPage(new { type });
    }

    public async Task<IActionResult> OnPostActivateAsync(int id, int type)
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
                ErrorMessage = "Cannot activate future-dated document.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating legal document {DocumentId}", id);
            ErrorMessage = "An error occurred while activating the legal document.";
        }

        return RedirectToPage(new { type });
    }
}
