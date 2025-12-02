using MercatoApp.Authorization;
using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class RevenueReportModel : PageModel
{
    private readonly ISellerRevenueReportService _revenueReportService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RevenueReportModel> _logger;

    public RevenueReportModel(
        ISellerRevenueReportService revenueReportService,
        ApplicationDbContext context,
        ILogger<RevenueReportModel> logger)
    {
        _revenueReportService = revenueReportService;
        _context = context;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public List<OrderStatus>? SelectedStatuses { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    public List<RevenueReportItem> ReportItems { get; set; } = new();
    public RevenueReportSummary? Summary { get; set; }
    public Store? CurrentStore { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    
    public bool HasActiveFilters => 
        (SelectedStatuses != null && SelectedStatuses.Any()) || 
        FromDate.HasValue || 
        ToDate.HasValue;

    public async Task<IActionResult> OnGetAsync()
    {
        var storeId = await GetCurrentStoreIdAsync();
        if (storeId == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        CurrentStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.Id == storeId.Value);

        if (CurrentStore == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        // Load revenue report data
        try
        {
            var (items, summary) = await _revenueReportService.GetRevenueReportAsync(
                storeId.Value,
                SelectedStatuses,
                FromDate,
                ToDate);

            ReportItems = items;
            Summary = summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading revenue report for store {StoreId}", storeId);
            ErrorMessages.Add($"Error loading revenue report: {ex.Message}");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var storeId = await GetCurrentStoreIdAsync();
        if (storeId == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        try
        {
            var result = await _revenueReportService.ExportToCsvAsync(
                storeId.Value,
                SelectedStatuses,
                FromDate,
                ToDate);

            if (!result.Success || result.FileData == null || result.FileName == null || result.ContentType == null)
            {
                ErrorMessages.AddRange(result.Errors);
                
                // Reload report data for the page
                var (items, summary) = await _revenueReportService.GetRevenueReportAsync(
                    storeId.Value,
                    SelectedStatuses,
                    FromDate,
                    ToDate);
                ReportItems = items;
                Summary = summary;
                
                CurrentStore = await _context.Stores
                    .FirstOrDefaultAsync(s => s.Id == storeId.Value);
                
                return Page();
            }

            _logger.LogInformation("User exported revenue report for store {StoreId}", storeId.Value);

            // Return the file for download
            return File(result.FileData, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting revenue report for store {StoreId}", storeId);
            ErrorMessages.Add($"An error occurred while exporting: {ex.Message}");
            
            // Reload report data for the page
            var (items, summary) = await _revenueReportService.GetRevenueReportAsync(
                storeId.Value,
                SelectedStatuses,
                FromDate,
                ToDate);
            ReportItems = items;
            Summary = summary;
            
            CurrentStore = await _context.Stores
                .FirstOrDefaultAsync(s => s.Id == storeId.Value);
            
            return Page();
        }
    }

    private async Task<int?> GetCurrentStoreIdAsync()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        return store?.Id;
    }
}
