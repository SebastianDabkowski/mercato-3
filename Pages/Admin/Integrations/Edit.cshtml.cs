using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Pages.Admin.Integrations;

/// <summary>
/// Page model for editing an integration.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IIntegrationService integrationService,
        ILogger<EditModel> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Integration Integration { get; set; } = null!;

    /// <summary>
    /// Gets the masked API key for display.
    /// </summary>
    public string MaskedApiKey { get; set; } = string.Empty;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

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
        [Display(Name = "API Key (leave blank to keep existing)")]
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
    /// Handles GET request to display the edit form.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <returns>Page result or NotFound.</returns>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var integration = await _integrationService.GetIntegrationByIdAsync(id.Value);
        if (integration == null)
        {
            return NotFound();
        }
        
        Integration = integration;

        MaskedApiKey = _integrationService.MaskApiKey(Integration.ApiKey);

        Input = new InputModel
        {
            Id = Integration.Id,
            Name = Integration.Name,
            Type = Integration.Type,
            Provider = Integration.Provider,
            Environment = Integration.Environment,
            Status = Integration.Status,
            ApiEndpoint = Integration.ApiEndpoint,
            MerchantId = Integration.MerchantId,
            CallbackUrl = Integration.CallbackUrl,
            AdditionalConfig = Integration.AdditionalConfig,
            IsEnabled = Integration.IsEnabled
        };

        return Page();
    }

    /// <summary>
    /// Handles POST request to update an integration.
    /// </summary>
    /// <returns>Redirect to index on success, page on error.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var integration = await _integrationService.GetIntegrationByIdAsync(Input.Id);
            if (integration == null)
            {
                return NotFound();
            }
            Integration = integration;
            MaskedApiKey = _integrationService.MaskApiKey(Integration.ApiKey);
            return Page();
        }

        try
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var integration = await _integrationService.GetIntegrationByIdAsync(Input.Id);
            if (integration == null)
            {
                ErrorMessage = "Integration not found.";
                return RedirectToPage("Index");
            }

            integration.Name = Input.Name;
            integration.Type = Input.Type;
            integration.Provider = Input.Provider;
            integration.Environment = Input.Environment;
            integration.Status = Input.Status;
            integration.ApiEndpoint = Input.ApiEndpoint;
            integration.MerchantId = Input.MerchantId;
            integration.CallbackUrl = Input.CallbackUrl;
            integration.AdditionalConfig = Input.AdditionalConfig;
            integration.IsEnabled = Input.IsEnabled;

            // Only update API key if a new value is provided
            if (!string.IsNullOrWhiteSpace(Input.ApiKey))
            {
                integration.ApiKey = Input.ApiKey;
            }

            await _integrationService.UpdateIntegrationAsync(integration, userId);

            SuccessMessage = $"Integration {integration.Name} updated successfully.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating integration {Id}", Input.Id);
            ErrorMessage = $"Error updating integration: {ex.Message}";
            
            var integration = await _integrationService.GetIntegrationByIdAsync(Input.Id);
            if (integration != null)
            {
                Integration = integration;
                MaskedApiKey = _integrationService.MaskApiKey(Integration.ApiKey);
            }
            return Page();
        }
    }
}
