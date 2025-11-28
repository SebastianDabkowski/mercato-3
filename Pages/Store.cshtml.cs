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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Store = await _storeProfileService.GetStoreByIdAsync(id);

        if (Store == null)
        {
            return NotFound();
        }

        return Page();
    }
}
