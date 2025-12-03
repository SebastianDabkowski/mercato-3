using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

/// <summary>
/// Page model for account deletion.
/// </summary>
[Authorize]
public class DeleteAccountModel : PageModel
{
    private readonly IAccountDeletionService _accountDeletionService;
    private readonly ILogger<DeleteAccountModel> _logger;

    public DeleteAccountModel(
        IAccountDeletionService accountDeletionService,
        ILogger<DeleteAccountModel> logger)
    {
        _accountDeletionService = accountDeletionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the deletion impact information.
    /// </summary>
    public AccountDeletionImpact? DeletionImpact { get; set; }

    /// <summary>
    /// Gets or sets the validation result.
    /// </summary>
    public AccountDeletionValidationResult? ValidationResult { get; set; }

    /// <summary>
    /// Gets or sets whether the user has confirmed they understand the consequences.
    /// </summary>
    [BindProperty]
    public bool ConfirmUnderstanding { get; set; }

    /// <summary>
    /// Gets or sets the optional reason for deletion.
    /// </summary>
    [BindProperty]
    public string? DeletionReason { get; set; }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Handles GET request to display the deletion page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Get deletion impact
        DeletionImpact = await _accountDeletionService.GetDeletionImpactAsync(userId);

        // Validate if account can be deleted
        ValidationResult = await _accountDeletionService.ValidateAccountDeletionAsync(userId);

        return Page();
    }

    /// <summary>
    /// Handles POST request to delete the account.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Validate confirmation checkbox
        if (!ConfirmUnderstanding)
        {
            StatusMessage = "Error: You must confirm that you understand the consequences of account deletion.";
            
            // Reload data
            DeletionImpact = await _accountDeletionService.GetDeletionImpactAsync(userId);
            ValidationResult = await _accountDeletionService.ValidateAccountDeletionAsync(userId);
            
            return Page();
        }

        // Get IP address
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Attempt to delete the account
        var result = await _accountDeletionService.DeleteAccountAsync(userId, ipAddress, DeletionReason);

        if (result.Success)
        {
            _logger.LogInformation("User {UserId} successfully deleted their account", userId);
            
            // Sign out the user
            return RedirectToPage("/Account/Logout", new { area = "", deletionSuccess = true });
        }
        else
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
            
            // Reload data
            DeletionImpact = await _accountDeletionService.GetDeletionImpactAsync(userId);
            ValidationResult = await _accountDeletionService.ValidateAccountDeletionAsync(userId);
            
            return Page();
        }
    }
}
