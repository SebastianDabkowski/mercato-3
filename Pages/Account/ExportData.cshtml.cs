using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

[Authorize]
public class ExportDataModel : PageModel
{
    private readonly IDataExportService _dataExportService;
    private readonly ILogger<ExportDataModel> _logger;

    public ExportDataModel(
        IDataExportService dataExportService,
        ILogger<ExportDataModel> logger)
    {
        _dataExportService = dataExportService;
        _logger = logger;
    }

    public List<DataExportLog> ExportHistory { get; set; } = new();
    public bool IsGenerating { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Load export history
        ExportHistory = await _dataExportService.GetExportHistoryAsync(userId, limit: 10);

        return Page();
    }

    public async Task<IActionResult> OnPostGenerateExportAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            IsGenerating = true;

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            _logger.LogInformation("User {UserId} requested data export from IP {IpAddress}", userId, ipAddress);

            // Generate the export
            var exportData = await _dataExportService.GenerateUserDataExportAsync(userId, ipAddress, userAgent);

            // Return the file as a download
            var fileName = $"mercato_data_export_{userId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
            
            _logger.LogInformation(
                "Successfully generated data export for user {UserId}. File size: {Size} bytes",
                userId,
                exportData.Length);

            return File(exportData, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating data export for user {UserId}", userId);
            ErrorMessage = "An error occurred while generating your data export. Please try again later.";
            
            // Reload the page with error message
            ExportHistory = await _dataExportService.GetExportHistoryAsync(userId, limit: 10);
            return Page();
        }
    }
}
