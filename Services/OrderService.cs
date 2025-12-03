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
    private readonly IShippingMethodService _shippingMethodService;
    private readonly IAddressService _addressService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IAnalyticsEventService _analyticsService;
    private readonly IResourceAuthorizationService _resourceAuthService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ApplicationDbContext context,
        ICartService cartService,
        IShippingMethodService shippingMethodService,
        IAddressService addressService,
        IEmailService emailService,
        INotificationService notificationService,
        IAnalyticsEventService analyticsService,
        IResourceAuthorizationService resourceAuthService,
        ILogger<OrderService> logger)
    {
        _context = context;
        _cartService = cartService;
        _shippingMethodService = shippingMethodService;
        _addressService = addressService;
        _emailService = emailService;
        _notificationService = notificationService;
        _analyticsService = analyticsService;
        _resourceAuthService = resourceAuthService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Order> CreateOrderFromCartAsync(
        int? userId, 
        string? sessionId, 
        int addressId, 
        Dictionary<int, int> selectedShippingMethods,
        int paymentMethodId,
        string? guestEmail)
    {
        // Use a transaction to ensure atomicity (skip for in-memory database)
        var isInMemory = _context.Database.IsInMemory();
        var transaction = isInMemory ? null : await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Validate stock and prices before creating the order
            var validationResult = await ValidateCartForOrderAsync(userId, sessionId);
            if (!validationResult.IsValid)
            {
                // Build detailed error message
                var errorMessages = new List<string>();
                
                if (!string.IsNullOrEmpty(validationResult.GeneralError))
                {
                    errorMessages.Add(validationResult.GeneralError);
                }
                
                if (validationResult.StockIssues.Any())
                {
                    errorMessages.Add("Stock issues:");
                    foreach (var issue in validationResult.StockIssues)
                    {
                        errorMessages.Add($"• {issue.Message}");
                    }
                }
                
                if (validationResult.PriceIssues.Any())
                {
                    errorMessages.Add("Price changes:");
                    foreach (var issue in validationResult.PriceIssues)
                    {
                        errorMessages.Add($"• {issue.Message}");
                    }
                }
                
                throw new InvalidOperationException(string.Join(" ", errorMessages));
            }

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
            decimal itemsSubtotal = 0;
            decimal totalShipping = 0;

            // Collect all product IDs for batch loading
            var allProductIds = itemsBySeller.SelectMany(s => s.Value).Select(i => i.ProductId).Distinct().ToList();
            var productsDict = await _context.Products
                .Include(p => p.Variants)
                .Where(p => allProductIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var sellerGroup in itemsBySeller)
            {
                var store = sellerGroup.Key;
                var items = sellerGroup.Value;

                // Calculate items subtotal
                itemsSubtotal += items.Sum(i => i.PriceAtAdd * i.Quantity);

                // Calculate shipping cost for this seller
                if (selectedShippingMethods.ContainsKey(store.Id))
                {
                    var shippingMethodId = selectedShippingMethods[store.Id];
                    var shippingCost = await _shippingMethodService.CalculateShippingCostAsync(shippingMethodId, items);
                    totalShipping += shippingCost;
                }
                else
                {
                    throw new InvalidOperationException($"Shipping method not selected for store {store.StoreName}.");
                }
            }

            decimal totalAmount = itemsSubtotal + totalShipping;

            // Generate order number
            var orderNumber = await GenerateOrderNumberAsync();

            // Create order with snapshot of prices from cart
            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = userId,
                GuestEmail = guestEmail,
                DeliveryAddressId = addressId,
                Status = OrderStatus.New,
                Subtotal = itemsSubtotal,
                ShippingCost = totalShipping,
                TaxAmount = 0, // Tax calculation can be added later
                TotalAmount = totalAmount,
                PaymentMethodId = paymentMethodId,
                PaymentStatus = PaymentStatus.Pending,
                OrderedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create seller sub-orders and order items from cart items
            int sellerSequence = 1;
            foreach (var sellerGroup in itemsBySeller)
            {
                var store = sellerGroup.Key;
                var items = sellerGroup.Value;

                // Calculate totals for this seller's sub-order
                decimal sellerSubtotal = items.Sum(i => i.PriceAtAdd * i.Quantity);
                decimal sellerShippingCost = 0;
                int? shippingMethodId = null;

                // Get shipping cost for this seller
                if (selectedShippingMethods.ContainsKey(store.Id))
                {
                    shippingMethodId = selectedShippingMethods[store.Id];
                    sellerShippingCost = await _shippingMethodService.CalculateShippingCostAsync(shippingMethodId.Value, items);
                    
                    // Create order shipping method record (for backward compatibility)
                    var orderShippingMethod = new OrderShippingMethod
                    {
                        OrderId = order.Id,
                        StoreId = store.Id,
                        ShippingMethodId = shippingMethodId.Value,
                        ShippingCost = sellerShippingCost
                    };

                    _context.OrderShippingMethods.Add(orderShippingMethod);
                }

                // Create seller sub-order
                var subOrderNumber = $"{orderNumber}-{sellerSequence}";
                var sellerSubOrder = new SellerSubOrder
                {
                    ParentOrderId = order.Id,
                    StoreId = store.Id,
                    SubOrderNumber = subOrderNumber,
                    Status = OrderStatus.New,
                    Subtotal = sellerSubtotal,
                    ShippingCost = sellerShippingCost,
                    TotalAmount = sellerSubtotal + sellerShippingCost,
                    ShippingMethodId = shippingMethodId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SellerSubOrders.Add(sellerSubOrder);
                await _context.SaveChangesAsync(); // Save to get the sub-order ID

                // Create order items and link to sub-order
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
                        SellerSubOrderId = sellerSubOrder.Id,
                        StoreId = store.Id,
                        ProductId = cartItem.ProductId,
                        ProductVariantId = cartItem.ProductVariantId,
                        ProductTitle = cartItem.Product.Title,
                        VariantDescription = variantDescription,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.PriceAtAdd, // Use price from cart for stable snapshot
                        Subtotal = cartItem.PriceAtAdd * cartItem.Quantity
                    };

                    _context.OrderItems.Add(orderItem);
                    
                    // Deduct stock from inventory using cached products
                    if (productsDict.TryGetValue(cartItem.ProductId, out var product))
                    {
                        if (cartItem.ProductVariantId.HasValue)
                        {
                            var variant = product.Variants.FirstOrDefault(v => v.Id == cartItem.ProductVariantId.Value);
                            if (variant != null)
                            {
                                // Ensure stock doesn't go negative (defensive check)
                                variant.Stock = Math.Max(0, variant.Stock - cartItem.Quantity);
                                variant.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                        else
                        {
                            // Ensure stock doesn't go negative (defensive check)
                            product.Stock = Math.Max(0, product.Stock - cartItem.Quantity);
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                sellerSequence++;
            }

            await _context.SaveChangesAsync();

            // Clear the cart
            await _cartService.ClearCartAsync(userId, sessionId);

            // Commit the transaction (if using a real database)
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            _logger.LogInformation("Created order {OrderNumber} for user {UserId}", orderNumber, userId ?? 0);

            // Send email notifications to sellers for each sub-order
            // Do this after transaction commit to ensure data is persisted
            try
            {
                // Reload sub-orders with full navigation properties for email
                var subOrdersWithDetails = await _context.SellerSubOrders
                    .Include(so => so.Store)
                        .ThenInclude(s => s.User)
                    .Include(so => so.Items)
                    .Include(so => so.ParentOrder)
                        .ThenInclude(o => o.DeliveryAddress)
                    .Where(so => so.ParentOrderId == order.Id)
                    .ToListAsync();

                foreach (var subOrder in subOrdersWithDetails)
                {
                    await _emailService.SendNewOrderNotificationToSellerAsync(subOrder, order);
                    
                    // Create notification for seller
                    try
                    {
                        var store = await _context.Stores.FindAsync(subOrder.StoreId);
                        if (store != null)
                        {
                            await _notificationService.CreateNotificationAsync(
                                store.UserId,
                                NotificationType.OrderPlaced,
                                "New Order Received",
                                $"You have received a new order #{subOrder.SubOrderNumber} with {subOrder.Items.Count} item(s).",
                                $"/Seller/Orders/Details/{subOrder.Id}",
                                subOrder.Id,
                                "SellerSubOrder");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create notification for seller sub-order {SubOrderNumber}", subOrder.SubOrderNumber);
                        // Don't fail order creation if notification fails
                    }
                }
                
                // Create notification for buyer if they have an account
                if (userId.HasValue)
                {
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            userId.Value,
                            NotificationType.OrderPlaced,
                            "Order Confirmed",
                            $"Your order #{orderNumber} has been confirmed and is being processed.",
                            $"/Account/Orders/Details/{order.Id}",
                            order.Id,
                            "Order");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create notification for order {OrderNumber}", orderNumber);
                        // Don't fail order creation if notification fails
                    }
                }
            }
            catch (Exception ex)
            {
                // Don't fail the order creation if email notification fails
                _logger.LogError(ex, "Failed to send seller notifications for order {OrderNumber}", orderNumber);
            }

            // Track order completion event (fire-and-forget)
            _ = TrackOrderCompletionEventAsync(userId, sessionId, order);

            return order;
        }
        catch
        {
            // Rollback transaction on any error (if using a real database)
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
        finally
        {
            // Dispose transaction if it was created
            transaction?.Dispose();
        }
    }

    private async Task TrackOrderCompletionEventAsync(int? userId, string? sessionId, Order order)
    {
        try
        {
            await _analyticsService.TrackEventAsync(new AnalyticsEventData
            {
                EventType = AnalyticsEventType.OrderComplete,
                UserId = userId,
                SessionId = sessionId,
                OrderId = order.Id,
                Value = order.TotalAmount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking order completion event");
        }
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
            .Include(o => o.PaymentMethod)
            .Include(o => o.PaymentTransactions)
                .ThenInclude(t => t.PaymentMethod)
            .Include(o => o.ShippingMethods)
                .ThenInclude(sm => sm.ShippingMethod)
            .Include(o => o.ShippingMethods)
                .ThenInclude(sm => sm.Store)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.Store)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.ShippingMethod)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    /// <inheritdoc />
    public async Task<Order?> GetOrderByIdForBuyerAsync(int orderId, int userId)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.DeliveryAddress)
            .Include(o => o.PaymentMethod)
            .Include(o => o.PaymentTransactions)
                .ThenInclude(t => t.PaymentMethod)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.Store)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.ShippingMethod)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.Items)
                    .ThenInclude(i => i.Product)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.Items)
                    .ThenInclude(i => i.ProductVariant)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        // Authorization check - only return order if it belongs to the requesting user
        if (order == null || order.UserId != userId)
        {
            return null;
        }

        return order;
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
    public async Task<(List<Order> Orders, int TotalCount)> GetUserOrdersFilteredAsync(
        int userId, 
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? sellerId = null,
        int page = 1,
        int pageSize = 10)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            page = 1;
        }
        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = 10; // Default to safe value
        }

        // Build the base query
        var query = _context.Orders
            .Include(o => o.DeliveryAddress)
            .Include(o => o.SubOrders)
                .ThenInclude(so => so.Store)
            .Where(o => o.UserId == userId);

        // Apply status filter
        if (statuses != null && statuses.Any())
        {
            query = query.Where(o => statuses.Contains(o.Status));
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            query = query.Where(o => o.OrderedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            // Include the entire day for toDate
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.OrderedAt <= endOfDay);
        }

        // Apply seller filter - optimized to avoid subquery in Any()
        if (sellerId.HasValue)
        {
            // Get order IDs that have sub-orders from this seller
            var orderIdsWithSeller = await _context.SellerSubOrders
                .Where(so => so.StoreId == sellerId.Value)
                .Select(so => so.ParentOrderId)
                .Distinct()
                .ToListAsync();
            
            query = query.Where(o => orderIdsWithSeller.Contains(o.Id));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting and pagination
        var orders = await query
            .OrderByDescending(o => o.OrderedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders, totalCount);
    }

    /// <inheritdoc />
    public async Task<List<Store>> GetUserOrderSellersAsync(int userId)
    {
        // Get unique seller IDs from user's orders efficiently using explicit join
        var sellerIds = await _context.SellerSubOrders
            .Where(so => _context.Orders.Any(o => o.Id == so.ParentOrderId && o.UserId == userId))
            .Select(so => so.StoreId)
            .Distinct()
            .ToListAsync();

        if (!sellerIds.Any())
        {
            return new List<Store>();
        }

        // Load the stores
        return await _context.Stores
            .Where(s => sellerIds.Contains(s.Id))
            .OrderBy(s => s.StoreName)
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

    /// <inheritdoc />
    public async Task<OrderValidationResult> ValidateCartForOrderAsync(int? userId, string? sessionId)
    {
        var result = new OrderValidationResult { IsValid = true };

        try
        {
            // Get cart items
            var itemsBySeller = await _cartService.GetCartItemsBySellerAsync(userId, sessionId);
            if (!itemsBySeller.Any())
            {
                result.IsValid = false;
                result.GeneralError = "Cart is empty.";
                return result;
            }

            // Collect all product IDs and fetch products in batch to avoid N+1 queries
            var allProductIds = itemsBySeller.SelectMany(s => s.Value).Select(i => i.ProductId).Distinct().ToList();
            var productsDict = await _context.Products
                .Include(p => p.Variants)
                .Where(p => allProductIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // Validate each cart item for stock and price
            foreach (var sellerGroup in itemsBySeller)
            {
                foreach (var cartItem in sellerGroup.Value)
                {
                    // Get fresh product data from batch-loaded dictionary
                    if (!productsDict.TryGetValue(cartItem.ProductId, out var product))
                    {
                        result.IsValid = false;
                        result.GeneralError = $"Product ID {cartItem.ProductId} is no longer available.";
                        continue;
                    }

                    // Determine variant description for error messages
                    string? variantDescription = null;
                    if (cartItem.ProductVariant != null)
                    {
                        var options = cartItem.ProductVariant.Options
                            .Select(o => $"{o.AttributeValue.VariantAttribute.Name}: {o.AttributeValue.Value}");
                        variantDescription = string.Join(", ", options);
                    }

                    // Validate stock
                    int availableStock;
                    if (cartItem.ProductVariantId.HasValue)
                    {
                        var variant = product.Variants.FirstOrDefault(v => v.Id == cartItem.ProductVariantId.Value);
                        if (variant == null || !variant.IsEnabled)
                        {
                            result.IsValid = false;
                            result.StockIssues.Add(new StockValidationIssue
                            {
                                CartItemId = cartItem.Id,
                                ProductId = cartItem.ProductId,
                                ProductVariantId = cartItem.ProductVariantId,
                                ProductTitle = product.Title,
                                VariantDescription = variantDescription,
                                RequestedQuantity = cartItem.Quantity,
                                AvailableStock = 0,
                                Message = $"{product.Title} ({variantDescription}) is no longer available."
                            });
                            continue;
                        }
                        availableStock = variant.Stock;
                    }
                    else
                    {
                        availableStock = product.Stock;
                    }

                    if (cartItem.Quantity > availableStock)
                    {
                        result.IsValid = false;
                        var itemName = cartItem.ProductVariantId.HasValue 
                            ? $"{product.Title} ({variantDescription})" 
                            : product.Title;
                        result.StockIssues.Add(new StockValidationIssue
                        {
                            CartItemId = cartItem.Id,
                            ProductId = cartItem.ProductId,
                            ProductVariantId = cartItem.ProductVariantId,
                            ProductTitle = product.Title,
                            VariantDescription = variantDescription,
                            RequestedQuantity = cartItem.Quantity,
                            AvailableStock = availableStock,
                            Message = availableStock == 0 
                                ? $"{itemName} is out of stock." 
                                : $"{itemName}: Only {availableStock} available, but you requested {cartItem.Quantity}."
                        });
                    }

                    // Validate price
                    decimal currentPrice;
                    if (cartItem.ProductVariantId.HasValue)
                    {
                        var variant = product.Variants.FirstOrDefault(v => v.Id == cartItem.ProductVariantId.Value);
                        currentPrice = variant?.PriceOverride ?? product.Price;
                    }
                    else
                    {
                        currentPrice = product.Price;
                    }

                    if (cartItem.PriceAtAdd != currentPrice)
                    {
                        result.IsValid = false;
                        var itemName = cartItem.ProductVariantId.HasValue 
                            ? $"{product.Title} ({variantDescription})" 
                            : product.Title;
                        var priceChange = currentPrice > cartItem.PriceAtAdd ? "increased" : "decreased";
                        result.PriceIssues.Add(new PriceValidationIssue
                        {
                            CartItemId = cartItem.Id,
                            ProductId = cartItem.ProductId,
                            ProductVariantId = cartItem.ProductVariantId,
                            ProductTitle = product.Title,
                            VariantDescription = variantDescription,
                            PriceInCart = cartItem.PriceAtAdd,
                            CurrentPrice = currentPrice,
                            Message = $"{itemName}: Price has {priceChange} from {cartItem.PriceAtAdd:C} to {currentPrice:C}."
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cart for order");
            result.IsValid = false;
            result.GeneralError = "An error occurred while validating your order. Please try again.";
        }

        return result;
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

    /// <inheritdoc />
    public async Task<List<SellerSubOrder>> GetSubOrdersByParentOrderIdAsync(int parentOrderId)
    {
        return await _context.SellerSubOrders
            .Include(so => so.Store)
            .Include(so => so.Items)
                .ThenInclude(i => i.Product)
            .Include(so => so.Items)
                .ThenInclude(i => i.ProductVariant)
            .Include(so => so.ShippingMethod)
            .Where(so => so.ParentOrderId == parentOrderId)
            .OrderBy(so => so.SubOrderNumber)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<SellerSubOrder>> GetSubOrdersByStoreIdAsync(int storeId)
    {
        return await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.DeliveryAddress)
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.User)
            .Include(so => so.Store)
            .Include(so => so.Items)
                .ThenInclude(i => i.Product)
            .Include(so => so.Items)
                .ThenInclude(i => i.ProductVariant)
            .Include(so => so.ShippingMethod)
            .Where(so => so.StoreId == storeId)
            .OrderByDescending(so => so.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SellerSubOrder?> GetSubOrderByIdAsync(int subOrderId)
    {
        return await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.DeliveryAddress)
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.User)
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.PaymentTransactions)
            .Include(so => so.Store)
            .Include(so => so.Items)
                .ThenInclude(i => i.Product)
            .Include(so => so.Items)
                .ThenInclude(i => i.ProductVariant)
            .Include(so => so.ShippingMethod)
            .Include(so => so.StatusHistory.OrderByDescending(h => h.ChangedAt))
            .FirstOrDefaultAsync(so => so.Id == subOrderId);
    }

    /// <inheritdoc />
    public async Task<SellerSubOrder?> GetSubOrderByIdForSellerAsync(int subOrderId, int sellerUserId)
    {
        // First check authorization
        var (authResult, storeId) = await _resourceAuthService.AuthorizeSubOrderAccessAsync(sellerUserId, subOrderId);
        
        if (!authResult.IsAuthorized)
        {
            _logger.LogWarning(
                "Unauthorized sub-order access attempt - User {UserId} tried to access sub-order {SubOrderId}: {Reason}",
                sellerUserId, subOrderId, authResult.FailureReason);
            return null;
        }

        // If authorized, return the full sub-order details
        return await GetSubOrderByIdAsync(subOrderId);
    }

    /// <inheritdoc />
    public async Task<(List<SellerSubOrder> SubOrders, int TotalCount)> GetSubOrdersFilteredAsync(
        int storeId,
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? buyerEmail = null,
        int page = 1,
        int pageSize = 10)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            page = 1;
        }
        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = 10; // Default to safe value
        }

        // Build the base query
        var query = _context.SellerSubOrders
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.DeliveryAddress)
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.User)
            .Include(so => so.Store)
            .Include(so => so.Items)
                .ThenInclude(i => i.Product)
            .Include(so => so.Items)
                .ThenInclude(i => i.ProductVariant)
            .Include(so => so.ShippingMethod)
            .Where(so => so.StoreId == storeId);

        // Apply status filter
        if (statuses != null && statuses.Any())
        {
            query = query.Where(so => statuses.Contains(so.Status));
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            query = query.Where(so => so.CreatedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            // Include the entire day for toDate
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(so => so.CreatedAt <= endOfDay);
        }

        // Apply buyer email filter (partial match, case-insensitive)
        if (!string.IsNullOrWhiteSpace(buyerEmail))
        {
            query = query.Where(so => 
                (so.ParentOrder.User != null && EF.Functions.Like(so.ParentOrder.User.Email, $"%{buyerEmail}%")) ||
                (so.ParentOrder.GuestEmail != null && EF.Functions.Like(so.ParentOrder.GuestEmail, $"%{buyerEmail}%")));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting and pagination
        var subOrders = await query
            .OrderByDescending(so => so.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (subOrders, totalCount);
    }
}
