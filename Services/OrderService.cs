using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing orders.
/// </summary>
public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ICartService _cartService;
    private readonly ICartTotalsService _cartTotalsService;
    private readonly IAddressService _addressService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ApplicationDbContext context,
        ICartService cartService,
        ICartTotalsService cartTotalsService,
        IAddressService addressService,
        ILogger<OrderService> logger)
    {
        _context = context;
        _cartService = cartService;
        _cartTotalsService = cartTotalsService;
        _addressService = addressService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Order> CreateOrderFromCartAsync(int? userId, string? sessionId, int addressId, string? guestEmail)
    {
        // Validate address
        var address = await _addressService.GetAddressByIdAsync(addressId);
        if (address == null)
        {
            throw new InvalidOperationException("Delivery address not found.");
        }

        // Validate shipping for this address
        var (isValid, errorMessage) = await ValidateShippingForCartAsync(userId, sessionId, address.CountryCode);
        if (!isValid)
        {
            throw new InvalidOperationException(errorMessage ?? "Shipping validation failed.");
        }

        // Get cart items
        var itemsBySeller = await _cartService.GetCartItemsBySellerAsync(userId, sessionId);
        if (!itemsBySeller.Any())
        {
            throw new InvalidOperationException("Cart is empty.");
        }

        // Calculate totals
        var cartTotals = await _cartTotalsService.CalculateCartTotalsAsync(userId, sessionId);

        // Generate order number
        var orderNumber = await GenerateOrderNumberAsync();

        // Create order
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            GuestEmail = guestEmail,
            DeliveryAddressId = addressId,
            Status = OrderStatus.Pending,
            Subtotal = cartTotals.ItemsSubtotal,
            ShippingCost = cartTotals.TotalShipping,
            TaxAmount = 0, // Tax calculation can be added later
            TotalAmount = cartTotals.TotalAmount,
            OrderedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Create order items from cart items
        foreach (var sellerGroup in itemsBySeller)
        {
            var store = sellerGroup.Key;
            var items = sellerGroup.Value;

            foreach (var cartItem in items)
            {
                var variantDescription = string.Empty;
                if (cartItem.ProductVariant != null)
                {
                    var options = cartItem.ProductVariant.Options
                        .Select(o => $"{o.AttributeValue.VariantAttribute.Name}: {o.AttributeValue.Value}");
                    variantDescription = string.Join(", ", options);
                }

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    StoreId = store.Id,
                    ProductId = cartItem.ProductId,
                    ProductVariantId = cartItem.ProductVariantId,
                    ProductTitle = cartItem.Product.Title,
                    VariantDescription = variantDescription,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.PriceAtAdd,
                    Subtotal = cartItem.PriceAtAdd * cartItem.Quantity
                };

                _context.OrderItems.Add(orderItem);
            }
        }

        await _context.SaveChangesAsync();

        // Clear the cart
        await _cartService.ClearCartAsync(userId, sessionId);

        _logger.LogInformation("Created order {OrderNumber} for user {UserId}", orderNumber, userId ?? 0);

        return order;
    }

    /// <inheritdoc />
    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.DeliveryAddress)
            .Include(o => o.Items)
                .ThenInclude(i => i.Store)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariant)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    /// <inheritdoc />
    public async Task<List<Order>> GetUserOrdersAsync(int userId)
    {
        return await _context.Orders
            .Include(o => o.DeliveryAddress)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<(bool IsValid, string? ErrorMessage)> ValidateShippingForCartAsync(int? userId, string? sessionId, string countryCode)
    {
        // Check if shipping is allowed to this country
        if (!await _addressService.IsShippingAllowedToCountryAsync(countryCode))
        {
            return (false, $"We currently do not ship to {countryCode}. Please select a different delivery address.");
        }

        // Get cart items to validate against shipping rules
        var itemsBySeller = await _cartService.GetCartItemsBySellerAsync(userId, sessionId);
        
        // Additional validation could be added here:
        // - Check if specific products/stores ship to the country
        // - Validate restricted items for certain regions
        // For now, we only check if the country is in our allowed list

        return (true, null);
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        // Generate a unique order number in format: ORD-YYYYMMDD-XXXXX
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        
        string orderNumber;
        bool isUnique;
        
        do
        {
            // Use cryptographically secure random number generation
            var randomBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(4);
            var randomNumber = BitConverter.ToUInt32(randomBytes, 0);
            var sequence = (randomNumber % 90000) + 10000; // Generate number between 10000 and 99999
            orderNumber = $"ORD-{date}-{sequence}";
            
            // Check if this order number already exists
            isUnique = !await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber);
        }
        while (!isUnique);

        return orderNumber;
    }
}
