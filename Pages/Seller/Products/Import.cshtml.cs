using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller.Products;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class ImportModel : PageModel
{
    private readonly IProductImportService _importService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<ImportModel> _logger;

    public ImportModel(
        IProductImportService importService,
        IStoreProfileService storeProfileService,
        ILogger<ImportModel> logger)
    {
        _importService = importService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    [BindProperty]
    public int JobId { get; set; }

    public ImportValidationResult? ValidationResult { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public string? FileName { get; set; }
    public string? FileType { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (store == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (store == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        if (UploadedFile == null || UploadedFile.Length == 0)
        {
            ErrorMessages.Add("Please select a file to upload.");
            return Page();
        }

        // Validate file size (10 MB max)
        const long maxFileSize = 10 * 1024 * 1024;
        if (UploadedFile.Length > maxFileSize)
        {
            ErrorMessages.Add("File size exceeds the maximum allowed size of 10 MB.");
            return Page();
        }

        // Validate file extension
        var extension = Path.GetExtension(UploadedFile.FileName).ToLowerInvariant();
        if (extension != ".csv" && extension != ".xlsx" && extension != ".xls")
        {
            ErrorMessages.Add("Invalid file type. Please upload a CSV or Excel file (.csv, .xlsx, .xls).");
            return Page();
        }

        try
        {
            // Parse the file
            using var stream = UploadedFile.OpenReadStream();
            var parseResult = await _importService.ParseFileAsync(stream, UploadedFile.FileName);

            if (!parseResult.Success || parseResult.Errors.Count > 0)
            {
                ErrorMessages.AddRange(parseResult.Errors);
                return Page();
            }

            if (parseResult.Rows.Count == 0)
            {
                ErrorMessages.Add("The uploaded file contains no data rows.");
                return Page();
            }

            // Validate the data
            ValidationResult = await _importService.ValidateImportAsync(store.Id, parseResult.Rows);
            FileName = UploadedFile.FileName;
            FileType = parseResult.FileType;

            // Create import job in pending status
            var job = await _importService.CreateImportJobAsync(
                store.Id,
                userId.Value,
                UploadedFile.FileName,
                parseResult.FileType,
                ValidationResult);

            JobId = job.Id;

            _logger.LogInformation("User {UserId} uploaded import file {FileName} for store {StoreId}, job {JobId}",
                userId.Value, UploadedFile.FileName, store.Id, job.Id);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing upload for user {UserId}", userId);
            ErrorMessages.Add($"An error occurred while processing the file: {ex.Message}");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (store == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        try
        {
            // Execute the import
            var result = await _importService.ExecuteImportAsync(JobId);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Import failed.";
                return RedirectToPage("ImportHistory");
            }

            _logger.LogInformation("Import job {JobId} executed: {Created} created, {Updated} updated, {Failed} failed",
                JobId, result.CreatedCount, result.UpdatedCount, result.FailedCount);

            if (result.FailedCount > 0)
            {
                TempData["SuccessMessage"] = $"Import completed with {result.CreatedCount} products created, {result.UpdatedCount} updated, and {result.FailedCount} failed.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Import completed successfully! {result.CreatedCount} products created and {result.UpdatedCount} updated.";
            }

            return RedirectToPage("ImportHistory");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing import job {JobId}", JobId);
            TempData["ErrorMessage"] = "An error occurred while importing products.";
            return RedirectToPage("ImportHistory");
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
