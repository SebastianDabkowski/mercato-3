using MercatoApp.Models;
using MercatoApp.Services;
using MercatoApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Admin.VatRules;

/// <summary>
/// Page model for creating a new VAT rule.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class CreateModel : PageModel
{
    private readonly IVatRuleService _vatRuleService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IVatRuleService vatRuleService,
        ApplicationDbContext context,
        ILogger<CreateModel> logger)
    {
        _vatRuleService = vatRuleService;
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public VatRule Rule { get; set; } = new()
    {
        EffectiveStartDate = DateTime.UtcNow.Date,
        IsActive = true,
        Priority = 0,
        ApplicabilityType = VatRuleApplicability.Global
    };

    /// <summary>
    /// List of categories for dropdown.
    /// </summary>
    public List<SelectListItem> Categories { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadDropdownsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                ErrorMessage = "Unable to identify current user.";
                await LoadDropdownsAsync();
                return Page();
            }

            // Validate effective dates
            if (Rule.EffectiveEndDate.HasValue && Rule.EffectiveEndDate < Rule.EffectiveStartDate)
            {
                ModelState.AddModelError("Rule.EffectiveEndDate", 
                    "End date must be after start date.");
                await LoadDropdownsAsync();
                return Page();
            }

            // Validate country code format (ISO 3166-1 alpha-2)
            if (string.IsNullOrWhiteSpace(Rule.CountryCode) || Rule.CountryCode.Length != 2)
            {
                ModelState.AddModelError("Rule.CountryCode", 
                    "Country code must be a 2-letter ISO code (e.g., US, GB, DE).");
                await LoadDropdownsAsync();
                return Page();
            }

            Rule.CountryCode = Rule.CountryCode.ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(Rule.RegionCode))
            {
                Rule.RegionCode = Rule.RegionCode.ToUpperInvariant();
            }

            // Create the rule (service will validate conflicts)
            var createdRule = await _vatRuleService.CreateRuleAsync(Rule, userId);
            
            SuccessMessage = $"VAT rule '{createdRule.Name}' created successfully.";
            return RedirectToPage("./Index");
        }
        catch (InvalidOperationException ex)
        {
            // Conflict or validation error
            _logger.LogWarning(ex, "Validation error creating VAT rule");
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDropdownsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VAT rule");
            ErrorMessage = "An error occurred while creating the VAT rule.";
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        // Load categories for category-specific rules
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        Categories = categories
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToList();

        Categories.Insert(0, new SelectListItem { Value = "", Text = "-- Select Category --" });
    }
}
