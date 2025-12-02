using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MercatoApp.Data;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class OrderExportModel : PageModel
{
    private readonly IOrderExportService _exportService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderExportModel> _logger;

    public OrderExportModel(
        IOrderExportService exportService,
        ApplicationDbContext context,
        ILogger<OrderExportModel> logger)
    {
        _exportService = exportService;
        _context = context;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public List<OrderStatus>? SelectedStatuses { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BuyerEmail { get; set; }

    [BindProperty]
    public string ExportFormat { get; set; } = "csv";

    public List<string> ErrorMessages { get; set; } = new();
    public bool HasActiveFilters => 
        (SelectedStatuses != null && SelectedStatuses.Any()) || 
        FromDate.HasValue || 
        ToDate.HasValue || 
        !string.IsNullOrWhiteSpace(BuyerEmail);

    public async Task<IActionResult> OnGetAsync()
    {
        var storeId = await GetCurrentStoreIdAsync();
        if (storeId == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        // Parse SelectedStatuses from query string if it's a comma-separated string
        ParseSelectedStatusesFromRequest(Request.Query);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var storeId = await GetCurrentStoreIdAsync();
        if (storeId == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        try
        {
            // Parse SelectedStatuses from form if it's a comma-separated string
            ParseSelectedStatusesFromRequest(Request.Form);

            // Generate the export file based on format
            OrderExportResult result;
            if (ExportFormat == "excel")
            {
                result = await _exportService.ExportToExcelAsync(
                    storeId.Value, 
                    SelectedStatuses, 
                    FromDate, 
                    ToDate, 
                    BuyerEmail);
            }
            else
            {
                result = await _exportService.ExportToCsvAsync(
                    storeId.Value, 
                    SelectedStatuses, 
                    FromDate, 
                    ToDate, 
                    BuyerEmail);
            }

            if (!result.Success || result.FileData == null || result.FileName == null || result.ContentType == null)
            {
                ErrorMessages.AddRange(result.Errors);
                return Page();
            }

            _logger.LogInformation("User exported orders for store {StoreId} in {Format} format",
                storeId.Value, ExportFormat);

            // Return the file for download
            return File(result.FileData, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting orders for store {StoreId}", storeId);
            ErrorMessages.Add($"An error occurred while exporting: {ex.Message}");
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

    private void ParseSelectedStatusesFromRequest(IEnumerable<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> requestData)
    {
        var statusesString = requestData.FirstOrDefault(kvp => kvp.Key == "SelectedStatuses").Value;
        if (!string.IsNullOrEmpty(statusesString))
        {
            var statusValues = statusesString.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var val) ? (OrderStatus)val : (OrderStatus?)null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();
            
            if (statusValues.Any())
            {
                SelectedStatuses = statusValues;
            }
        }
    }
}
