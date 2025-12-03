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
/// Page model for editing an existing VAT rule.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly IVatRuleService _vatRuleService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IVatRuleService vatRuleService,
        ApplicationDbContext context,
        ILogger<EditModel> logger)
    {
        _vatRuleService = vatRuleService;
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public VatRule Rule { get; set; } = new();

    /// <summary>
    /// List of categories for dropdown.
    /// </summary>
    public List<SelectListItem> Categories { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var rule = await _vatRuleService.GetRuleByIdAsync(id);
        if (rule == null)
        {
            ErrorMessage = "VAT rule not found.";
            return RedirectToPage("./Index");
        }

        Rule = rule;
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

            // Update the rule (service will validate conflicts)
            var updatedRule = await _vatRuleService.UpdateRuleAsync(Rule, userId);
            
            SuccessMessage = $"VAT rule '{updatedRule.Name}' updated successfully.";
            return RedirectToPage("./Index");
        }
        catch (InvalidOperationException ex)
        {
            // Conflict or validation error
            _logger.LogWarning(ex, "Validation error updating VAT rule");
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDropdownsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating VAT rule {RuleId}", Rule.Id);
            ErrorMessage = "An error occurred while updating the VAT rule.";
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
