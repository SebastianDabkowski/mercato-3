using MercatoApp.Models;
using MercatoApp.Data;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Interface for shopping cart operations.
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Gets the current user's cart or creates one if it doesn't exist.
    /// </summary>
    /// <param name="userId">The user ID for authenticated users, null for anonymous users.</param>
    /// <param name="sessionId">The session ID for anonymous users, null for authenticated users.</param>
    /// <returns>The cart.</returns>
    Task<Cart> GetOrCreateCartAsync(int? userId, string? sessionId);

    /// <summary>
    /// Adds a product to the cart or increases quantity if already present.
    /// </summary>
    /// <param name="userId">The user ID for authenticated users, null for anonymous users.</param>
    /// <param name="sessionId">The session ID for anonymous users, null for authenticated users.</param>
    /// <param name="productId">The product ID to add.</param>
    /// <param name="variantId">The variant ID to add (null for simple products).</param>
    /// <param name="quantity">The quantity to add.</param>
    /// <returns>The updated cart item.</returns>
    Task<CartItem> AddToCartAsync(int? userId, string? sessionId, int productId, int? variantId, int quantity = 1);

    /// <summary>
    /// Updates the quantity of an item in the cart.
    /// </summary>
    /// <param name="cartItemId">The cart item ID.</param>
    /// <param name="quantity">The new quantity.</param>
    /// <returns>The updated cart item.</returns>
    Task<CartItem> UpdateCartItemQuantityAsync(int cartItemId, int quantity);

    /// <summary>
    /// Removes an item from the cart.
    /// </summary>
    /// <param name="cartItemId">The cart item ID to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveFromCartAsync(int cartItemId);

    /// <summary>
    /// Clears all items from the cart.
    /// </summary>
    /// <param name="userId">The user ID for authenticated users, null for anonymous users.</param>
    /// <param name="sessionId">The session ID for anonymous users, null for authenticated users.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearCartAsync(int? userId, string? sessionId);

    /// <summary>
    /// Gets the cart items grouped by seller (store).
    /// </summary>
    /// <param name="userId">The user ID for authenticated users, null for anonymous users.</param>
    /// <param name="sessionId">The session ID for anonymous users, null for authenticated users.</param>
    /// <returns>A dictionary where the key is the store and value is the list of cart items for that store.</returns>
    Task<Dictionary<Store, List<CartItem>>> GetCartItemsBySellerAsync(int? userId, string? sessionId);

    /// <summary>
    /// Gets the total number of items in the cart.
    /// </summary>
    /// <param name="userId">The user ID for authenticated users, null for anonymous users.</param>
    /// <param name="sessionId">The session ID for anonymous users, null for authenticated users.</param>
    /// <returns>The total number of items.</returns>
    Task<int> GetCartItemCountAsync(int? userId, string? sessionId);

    /// <summary>
    /// Merges an anonymous cart into a user's cart when they log in.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="sessionId">The anonymous session ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MergeCartsAsync(int userId, string sessionId);
}

/// <summary>
/// Service for managing shopping cart operations.
/// </summary>
public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CartService> _logger;

    public CartService(ApplicationDbContext context, ILogger<CartService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Cart> GetOrCreateCartAsync(int? userId, string? sessionId)
    {
        Cart? cart;

        if (userId.HasValue)
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Store)
                .Include(c => c.Items)
                    .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
            {
                cart = new Cart { UserId = userId.Value };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Store)
                .Include(c => c.Items)
                    .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);

            if (cart == null)
            {
                cart = new Cart { SessionId = sessionId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            throw new InvalidOperationException("Either userId or sessionId must be provided.");
        }

        return cart;
    }

    /// <inheritdoc />
    public async Task<CartItem> AddToCartAsync(int? userId, string? sessionId, int productId, int? variantId, int quantity = 1)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);

        // Get the product to capture the price
        var product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {productId} not found.");
        }

        // Determine the price
        decimal price = product.Price;
        if (variantId.HasValue)
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId.Value);
            if (variant == null)
            {
                throw new InvalidOperationException($"Variant with ID {variantId.Value} not found.");
            }
            price = variant.PriceOverride ?? product.Price;
        }

        // Check if item already exists in cart
        var existingItem = cart.Items.FirstOrDefault(i => 
            i.ProductId == productId && 
            i.ProductVariantId == variantId);

        if (existingItem != null)
        {
            // Update quantity
            existingItem.Quantity += quantity;
            existingItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Add new item
            var newItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = productId,
                ProductVariantId = variantId,
                Quantity = quantity,
                PriceAtAdd = price
            };
            _context.CartItems.Add(newItem);
            existingItem = newItem;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(existingItem).Reference(i => i.Product).LoadAsync();
        await _context.Entry(existingItem.Product).Reference(p => p.Store).LoadAsync();
        if (existingItem.ProductVariantId.HasValue)
        {
            await _context.Entry(existingItem).Reference(i => i.ProductVariant).LoadAsync();
        }

        _logger.LogInformation("Added product {ProductId} (variant: {VariantId}) to cart for user {UserId} / session {SessionId}", 
            productId, variantId, userId, sessionId);

        return existingItem;
    }

    /// <inheritdoc />
    public async Task<CartItem> UpdateCartItemQuantityAsync(int cartItemId, int quantity)
    {
        var item = await _context.CartItems
            .Include(i => i.Cart)
            .Include(i => i.Product)
            .Include(i => i.ProductVariant)
            .FirstOrDefaultAsync(i => i.Id == cartItemId);

        if (item == null)
        {
            throw new InvalidOperationException($"Cart item with ID {cartItemId} not found.");
        }

        // If quantity is 0, remove the item instead
        if (quantity == 0)
        {
            await RemoveFromCartAsync(cartItemId);
            return item;
        }

        if (quantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
        }

        // Validate against available stock
        int availableStock;
        if (item.ProductVariantId.HasValue && item.ProductVariant != null)
        {
            availableStock = item.ProductVariant.Stock;
        }
        else
        {
            availableStock = item.Product.Stock;
        }

        if (quantity > availableStock)
        {
            throw new InvalidOperationException($"Requested quantity ({quantity}) exceeds available stock ({availableStock}).");
        }

        item.Quantity = quantity;
        item.UpdatedAt = DateTime.UtcNow;
        item.Cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return item;
    }

    /// <inheritdoc />
    public async Task RemoveFromCartAsync(int cartItemId)
    {
        var item = await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == cartItemId);

        if (item != null)
        {
            item.Cart.UpdatedAt = DateTime.UtcNow;
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed cart item {CartItemId}", cartItemId);
        }
    }

    /// <inheritdoc />
    public async Task ClearCartAsync(int? userId, string? sessionId)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);

        if (cart.Items.Any())
        {
            _context.CartItems.RemoveRange(cart.Items);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleared cart for user {UserId} / session {SessionId}", userId, sessionId);
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<Store, List<CartItem>>> GetCartItemsBySellerAsync(int? userId, string? sessionId)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);

        // Group items by store
        var itemsByStore = cart.Items
            .GroupBy(i => i.Product.Store)
            .ToDictionary(g => g.Key, g => g.ToList());

        return itemsByStore;
    }

    /// <inheritdoc />
    public async Task<int> GetCartItemCountAsync(int? userId, string? sessionId)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);
        return cart.Items.Sum(i => i.Quantity);
    }

    /// <inheritdoc />
    public async Task MergeCartsAsync(int userId, string sessionId)
    {
        var userCart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        var sessionCart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);

        if (sessionCart == null || !sessionCart.Items.Any())
        {
            return; // Nothing to merge
        }

        if (userCart == null)
        {
            // Just transfer the session cart to the user
            sessionCart.UserId = userId;
            sessionCart.SessionId = null;
            sessionCart.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Merge items from session cart into user cart
            foreach (var sessionItem in sessionCart.Items)
            {
                var existingItem = userCart.Items.FirstOrDefault(i => 
                    i.ProductId == sessionItem.ProductId && 
                    i.ProductVariantId == sessionItem.ProductVariantId);

                if (existingItem != null)
                {
                    // Increase quantity
                    existingItem.Quantity += sessionItem.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Move item to user cart
                    sessionItem.CartId = userCart.Id;
                }
            }

            userCart.UpdatedAt = DateTime.UtcNow;

            // Delete the session cart
            _context.Carts.Remove(sessionCart);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Merged session cart {SessionId} into user cart for user {UserId}", sessionId, userId);
    }
}
