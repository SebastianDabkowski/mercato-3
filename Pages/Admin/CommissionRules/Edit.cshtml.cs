using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MercatoApp.Data;

namespace MercatoApp.Pages.Admin.CommissionRules;

/// <summary>
/// Page model for editing an existing commission rule.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly ICommissionRuleService _ruleService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        ICommissionRuleService ruleService,
        ApplicationDbContext context,
        ILogger<EditModel> logger)
    {
        _ruleService = ruleService;
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public CommissionRule Rule { get; set; } = null!;

    /// <summary>
    /// List of categories for dropdown.
    /// </summary>
    public List<SelectListItem> Categories { get; set; } = new();

    /// <summary>
    /// List of stores for dropdown.
    /// </summary>
    public List<SelectListItem> Stores { get; set; } = new();

    /// <summary>
    /// List of seller tiers for dropdown.
    /// </summary>
    public List<SelectListItem> SellerTiers { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var rule = await _ruleService.GetRuleByIdAsync(id);
        if (rule == null)
        {
            ErrorMessage = "Commission rule not found.";
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

            // Update the rule (service will validate conflicts)
            var updatedRule = await _ruleService.UpdateRuleAsync(Rule, userId);
            
            SuccessMessage = $"Commission rule '{updatedRule.Name}' updated successfully.";
            return RedirectToPage("./Index");
        }
        catch (InvalidOperationException ex)
        {
            // Conflict or validation error
            _logger.LogWarning(ex, "Validation error updating commission rule");
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDropdownsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating commission rule");
            ErrorMessage = "An error occurred while updating the commission rule.";
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        // Load categories
        Categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();

        // Load stores
        Stores = await _context.Stores
            .OrderBy(s => s.StoreName)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.StoreName
            })
            .ToListAsync();

        // Load seller tiers
        SellerTiers = new List<SelectListItem>
        {
            new() { Value = Models.SellerTiers.Bronze, Text = "Bronze" },
            new() { Value = Models.SellerTiers.Silver, Text = "Silver" },
            new() { Value = Models.SellerTiers.Gold, Text = "Gold" },
            new() { Value = Models.SellerTiers.Platinum, Text = "Platinum" }
        };
    }
}
