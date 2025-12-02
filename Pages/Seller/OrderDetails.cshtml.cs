using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MercatoApp.Data;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = "SellerOnly")]
public class OrderDetailsModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IOrderStatusService _orderStatusService;
    private readonly IReturnRequestService _returnRequestService;
    private readonly IShippingProviderIntegrationService _shippingProviderService;
    private readonly IShippingLabelService _labelService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderDetailsModel> _logger;

    public OrderDetailsModel(
        IOrderService orderService,
        IOrderStatusService orderStatusService,
        IReturnRequestService returnRequestService,
        IShippingProviderIntegrationService shippingProviderService,
        IShippingLabelService labelService,
        ApplicationDbContext context,
        ILogger<OrderDetailsModel> logger)
    {
        _orderService = orderService;
        _orderStatusService = orderStatusService;
        _returnRequestService = returnRequestService;
        _shippingProviderService = shippingProviderService;
        _labelService = labelService;
        _context = context;
        _logger = logger;
    }

    public SellerSubOrder? SubOrder { get; set; }
    public Store? CurrentStore { get; set; }
    public List<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();
    public Shipment? Shipment { get; set; }
    public bool HasShippingLabel { get; set; }

    [BindProperty]
    public string? TrackingNumber { get; set; }

    [BindProperty]
    public string? CarrierName { get; set; }

    [BindProperty]
    public string? TrackingUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(int subOrderId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Get the seller's store
        CurrentStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (CurrentStore == null)
        {
            TempData["ErrorMessage"] = "Store not found.";
            return RedirectToPage("/Index");
        }

        // Get the sub-order
        SubOrder = await _orderService.GetSubOrderByIdAsync(subOrderId);

        if (SubOrder == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage("/Seller/Orders");
        }

        // Verify that this sub-order belongs to the current seller's store
        if (SubOrder.StoreId != CurrentStore.Id)
        {
            TempData["ErrorMessage"] = "You don't have permission to view this order.";
            return RedirectToPage("/Seller/Orders");
        }

        // Load return requests for this sub-order
        ReturnRequests = await _returnRequestService.GetReturnRequestsBySubOrderAsync(subOrderId);

        // Load shipment data if available
        Shipment = await _shippingProviderService.GetShipmentBySubOrderIdAsync(subOrderId);
        HasShippingLabel = Shipment?.LabelData != null && Shipment.LabelData.Length > 0;

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int subOrderId, string action)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Get the seller's store
        CurrentStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (CurrentStore == null)
        {
            TempData["ErrorMessage"] = "Store not found.";
            return RedirectToPage("/Index");
        }

        // Get the sub-order
        SubOrder = await _orderService.GetSubOrderByIdAsync(subOrderId);

        if (SubOrder == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage("/Seller/Orders");
        }

        // Verify that this sub-order belongs to the current seller's store
        if (SubOrder.StoreId != CurrentStore.Id)
        {
            TempData["ErrorMessage"] = "You don't have permission to update this order.";
            return RedirectToPage("/Seller/Orders");
        }

        // Execute the requested action (pass userId for audit trail)
        (bool success, string? errorMessage) = action switch
        {
            "preparing" => await _orderStatusService.UpdateSubOrderToPreparingAsync(subOrderId, userId),
            "shipped" => await _orderStatusService.UpdateSubOrderToShippedAsync(
                subOrderId, TrackingNumber, CarrierName, TrackingUrl, userId),
            "delivered" => await _orderStatusService.UpdateSubOrderToDeliveredAsync(subOrderId, userId),
            "cancel" => await _orderStatusService.CancelSubOrderAsync(subOrderId, userId),
            _ => (false, "Invalid action.")
        };

        if (success)
        {
            TempData["SuccessMessage"] = "Order status updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage ?? "Failed to update order status.";
        }

        return RedirectToPage(new { subOrderId });
    }

    public async Task<IActionResult> OnPostUpdateTrackingAsync(int subOrderId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Get the seller's store
        CurrentStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (CurrentStore == null)
        {
            TempData["ErrorMessage"] = "Store not found.";
            return RedirectToPage("/Index");
        }

        // Get the sub-order
        SubOrder = await _orderService.GetSubOrderByIdAsync(subOrderId);

        if (SubOrder == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage("/Seller/Orders");
        }

        // Verify that this sub-order belongs to the current seller's store
        if (SubOrder.StoreId != CurrentStore.Id)
        {
            TempData["ErrorMessage"] = "You don't have permission to update this order.";
            return RedirectToPage("/Seller/Orders");
        }

        // Update tracking information
        var (success, errorMessage) = await _orderStatusService.UpdateTrackingInformationAsync(
            subOrderId, TrackingNumber, CarrierName, TrackingUrl, userId);

        if (success)
        {
            TempData["SuccessMessage"] = "Tracking information updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage ?? "Failed to update tracking information.";
        }

        return RedirectToPage(new { subOrderId });
    }

    public async Task<IActionResult> OnGetDownloadLabelAsync(int subOrderId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Get the seller's store
        CurrentStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (CurrentStore == null)
        {
            TempData["ErrorMessage"] = "Store not found.";
            return RedirectToPage("/Index");
        }

        // Get the sub-order to verify ownership
        SubOrder = await _orderService.GetSubOrderByIdAsync(subOrderId);

        if (SubOrder == null || SubOrder.StoreId != CurrentStore.Id)
        {
            TempData["ErrorMessage"] = "You don't have permission to access this order.";
            return RedirectToPage("/Seller/Orders");
        }

        // Get the shipment
        var shipment = await _shippingProviderService.GetShipmentBySubOrderIdAsync(subOrderId);

        if (shipment == null)
        {
            TempData["ErrorMessage"] = "No shipment found for this order.";
            return RedirectToPage(new { subOrderId });
        }

        // Get the label
        var labelData = await _labelService.GetLabelAsync(shipment.Id);

        if (labelData == null || labelData.Data == null || labelData.Data.Length == 0)
        {
            TempData["ErrorMessage"] = "Shipping label not found.";
            return RedirectToPage(new { subOrderId });
        }

        _logger.LogInformation(
            "Shipping label downloaded for sub-order {SubOrderId} by user {UserId}",
            subOrderId, userId);

        // Return the label as a file download
        var fileName = $"shipping-label-{shipment.TrackingNumber}.{labelData.Format.ToLowerInvariant()}";
        return File(labelData.Data, labelData.ContentType, fileName);
    }

    public async Task<IActionResult> OnPostCreateShipmentAsync(int subOrderId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Get the seller's store
        CurrentStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (CurrentStore == null)
        {
            TempData["ErrorMessage"] = "Store not found.";
            return RedirectToPage("/Index");
        }

        // Get the sub-order to verify ownership
        SubOrder = await _orderService.GetSubOrderByIdAsync(subOrderId);

        if (SubOrder == null || SubOrder.StoreId != CurrentStore.Id)
        {
            TempData["ErrorMessage"] = "You don't have permission to access this order.";
            return RedirectToPage("/Seller/Orders");
        }

        // Create the shipment
        var shipment = await _shippingProviderService.CreateShipmentAsync(subOrderId, userId);

        if (shipment == null)
        {
            TempData["ErrorMessage"] = "Failed to create shipment. Please check that a shipping provider is configured and the order is ready to ship.";
            return RedirectToPage(new { subOrderId });
        }

        TempData["SuccessMessage"] = "Shipment created successfully! A shipping label has been generated.";
        
        _logger.LogInformation(
            "Shipment created for sub-order {SubOrderId} by user {UserId}",
            subOrderId, userId);

        return RedirectToPage(new { subOrderId });
    }
}
