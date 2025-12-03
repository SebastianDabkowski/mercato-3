using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Pages.Admin.Integrations;

/// <summary>
/// Page model for creating a new integration.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class CreateModel : PageModel
{
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IIntegrationService integrationService,
        ILogger<CreateModel> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [MaxLength(200)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Type")]
        public IntegrationType Type { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Provider")]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Environment")]
        public IntegrationEnvironment Environment { get; set; }

        [Required]
        [Display(Name = "Status")]
        public IntegrationStatus Status { get; set; }

        [MaxLength(500)]
        [Display(Name = "API Endpoint")]
        [Url]
        public string? ApiEndpoint { get; set; }

        [MaxLength(500)]
        [Display(Name = "API Key")]
        public string? ApiKey { get; set; }

        [MaxLength(100)]
        [Display(Name = "Merchant ID")]
        public string? MerchantId { get; set; }

        [MaxLength(500)]
        [Display(Name = "Callback URL")]
        [Url]
        public string? CallbackUrl { get; set; }

        [Display(Name = "Additional Configuration (JSON)")]
        public string? AdditionalConfig { get; set; }

        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Handles GET request to display the create form.
    /// </summary>
    /// <returns>Page result.</returns>
    public IActionResult OnGet()
    {
        return Page();
    }

    /// <summary>
    /// Handles POST request to create a new integration.
    /// </summary>
    /// <returns>Redirect to index on success, page on error.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var integration = new Integration
            {
                Name = Input.Name,
                Type = Input.Type,
                Provider = Input.Provider,
                Environment = Input.Environment,
                Status = Input.Status,
                ApiEndpoint = Input.ApiEndpoint,
                ApiKey = Input.ApiKey,
                MerchantId = Input.MerchantId,
                CallbackUrl = Input.CallbackUrl,
                AdditionalConfig = Input.AdditionalConfig,
                IsEnabled = Input.IsEnabled
            };

            await _integrationService.CreateIntegrationAsync(integration, userId);

            SuccessMessage = $"Integration {integration.Name} created successfully.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating integration");
            ErrorMessage = $"Error creating integration: {ex.Message}";
            return Page();
        }
    }
}
