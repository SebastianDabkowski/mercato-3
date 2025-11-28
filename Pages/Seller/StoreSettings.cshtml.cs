using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class StoreSettingsModel : PageModel
{
    private readonly IStoreProfileService _storeProfileService;
    private readonly IWebHostEnvironment _environment;
    private readonly IFeatureFlagService _featureFlagService;

    public StoreSettingsModel(
        IStoreProfileService storeProfileService,
        IWebHostEnvironment environment,
        IFeatureFlagService featureFlagService)
    {
        _storeProfileService = storeProfileService;
        _environment = environment;
        _featureFlagService = featureFlagService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Store? Store { get; set; }

    public bool IsUserManagementEnabled => _featureFlagService.IsSellerUserManagementEnabled;

    [TempData]
    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Store name is required.")]
        [MaxLength(100, ErrorMessage = "Store name must be 100 characters or less.")]
        [Display(Name = "Store Name")]
        public string StoreName { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Description must be 1000 characters or less.")]
        [Display(Name = "Store Description")]
        public string? Description { get; set; }

        [MaxLength(100, ErrorMessage = "Category must be 100 characters or less.")]
        [Display(Name = "Store Category")]
        public string? Category { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(256, ErrorMessage = "Email must be 256 characters or less.")]
        [Display(Name = "Contact Email")]
        public string? ContactEmail { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [MaxLength(20, ErrorMessage = "Phone number must be 20 characters or less.")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL (e.g., https://example.com).")]
        [MaxLength(500, ErrorMessage = "Website URL must be 500 characters or less.")]
        [Display(Name = "Website URL")]
        public string? WebsiteUrl { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        // Populate form with current store data
        Input.StoreName = Store.StoreName;
        Input.Description = Store.Description;
        Input.Category = Store.Category;
        Input.ContactEmail = Store.ContactEmail;
        Input.PhoneNumber = Store.PhoneNumber;
        Input.WebsiteUrl = Store.WebsiteUrl;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var data = new UpdateStoreProfileData
        {
            StoreName = Input.StoreName,
            Description = Input.Description,
            Category = Input.Category,
            ContactEmail = Input.ContactEmail,
            PhoneNumber = Input.PhoneNumber,
            WebsiteUrl = Input.WebsiteUrl
        };

        var result = await _storeProfileService.UpdateStoreProfileAsync(userId.Value, data);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        SuccessMessage = "Store profile updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadLogoAsync(IFormFile logoFile)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        // Populate form with current store data for display
        Input.StoreName = Store.StoreName;
        Input.Description = Store.Description;
        Input.Category = Store.Category;
        Input.ContactEmail = Store.ContactEmail;
        Input.PhoneNumber = Store.PhoneNumber;
        Input.WebsiteUrl = Store.WebsiteUrl;

        if (logoFile == null || logoFile.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a file to upload.");
            return Page();
        }

        // Validate file size (max 5 MB)
        if (!StoreProfileService.IsAllowedFileSize(logoFile.Length))
        {
            ModelState.AddModelError(string.Empty, "Logo file must be 5 MB or less.");
            return Page();
        }

        // Validate file extension
        if (!StoreProfileService.IsAllowedImageExtension(logoFile.FileName))
        {
            ModelState.AddModelError(string.Empty, "Only image files (JPG, PNG, GIF, WebP) are allowed.");
            return Page();
        }

        try
        {
            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "logos");
            Directory.CreateDirectory(uploadsPath);

            // Generate unique filename
            var fileExtension = Path.GetExtension(logoFile.FileName);
            var fileName = $"store_{Store.Id}_{Guid.NewGuid():N}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            // Update store logo URL
            var logoUrl = $"/uploads/logos/{fileName}";
            var result = await _storeProfileService.UpdateStoreLogoAsync(userId.Value, logoUrl);

            if (!result.Success)
            {
                // Clean up the uploaded file
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                return Page();
            }

            // Delete old logo file if it exists
            if (!string.IsNullOrEmpty(Store.LogoUrl) && Store.LogoUrl != logoUrl)
            {
                var oldLogoPath = Path.Combine(_environment.WebRootPath, Store.LogoUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldLogoPath))
                {
                    System.IO.File.Delete(oldLogoPath);
                }
            }

            SuccessMessage = "Store logo updated successfully.";
            return RedirectToPage();
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while uploading the logo. Please try again.");
            return Page();
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
