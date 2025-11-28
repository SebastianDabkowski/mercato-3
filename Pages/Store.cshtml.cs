using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages;

public class StoreModel : PageModel
{
    private readonly IStoreProfileService _storeProfileService;

    public StoreModel(IStoreProfileService storeProfileService)
    {
        _storeProfileService = storeProfileService;
    }

    public Store? Store { get; set; }

    /// <summary>
    /// Gets a value indicating whether the store is publicly viewable (Active or LimitedActive).
    /// </summary>
    public bool IsStorePubliclyViewable => Store != null && 
        (Store.Status == StoreStatus.Active || Store.Status == StoreStatus.LimitedActive);

    /// <summary>
    /// Gets the message to display when store is not accessible.
    /// </summary>
    public string? UnavailableMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        Store = await _storeProfileService.GetStoreBySlugAsync(slug);

        if (Store == null)
        {
            return NotFound();
        }

        // Handle stores that are not publicly viewable
        if (!IsStorePubliclyViewable)
        {
            UnavailableMessage = Store.Status switch
            {
                StoreStatus.Suspended => "This store is currently unavailable.",
                StoreStatus.PendingVerification => "This store is not yet available for public viewing.",
                _ => "This store is currently unavailable."
            };
        }

        return Page();
    }
}
