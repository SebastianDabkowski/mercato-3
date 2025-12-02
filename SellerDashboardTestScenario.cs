using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Test scenario to verify the seller sales dashboard functionality.
/// This creates test data and validates that the dashboard shows correct metrics.
/// </summary>
public class SellerDashboardTestScenario
{
    private readonly ApplicationDbContext _context;
    private readonly ISellerDashboardService _dashboardService;

    public SellerDashboardTestScenario(
        ApplicationDbContext context,
        ISellerDashboardService dashboardService)
    {
        _context = context;
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Runs the comprehensive test scenario for seller dashboard.
    /// </summary>
    public async Task RunAsync()
    {
        Console.WriteLine("\n=== Seller Dashboard Test Scenario ===\n");

        try
        {
            // Step 1: Create test seller and store
            var seller = await CreateSellerAsync();
            var store = await CreateStoreAsync(seller.Id);
            Console.WriteLine($"✓ Created test seller (ID: {seller.Id}) and store (ID: {store.Id})");

            // Step 2: Create categories and products
            var category1 = await CreateCategoryAsync("Electronics");
            var category2 = await CreateCategoryAsync("Clothing");
            
            var product1 = await CreateProductAsync(store.Id, category1.Id, "Laptop", 999.99m);
            var product2 = await CreateProductAsync(store.Id, category1.Id, "Mouse", 29.99m);
            var product3 = await CreateProductAsync(store.Id, category2.Id, "T-Shirt", 19.99m);
            Console.WriteLine($"✓ Created test products");

            // Step 3: Create test buyer
            var buyer = await CreateBuyerAsync();
            var address = await CreateAddressAsync(buyer.Id);
            Console.WriteLine($"✓ Created test buyer (ID: {buyer.Id})");

            // Step 4: Create orders over time
            var now = DateTime.UtcNow;
            
            // Week 1 orders (7 days ago)
            await CreateOrderWithItemsAsync(buyer.Id, address.Id, store.Id, product1.Id, 1, now.AddDays(-7));
            await CreateOrderWithItemsAsync(buyer.Id, address.Id, store.Id, product2.Id, 2, now.AddDays(-7));
            
            // Week 1 middle (5 days ago)
            await CreateOrderWithItemsAsync(buyer.Id, address.Id, store.Id, product3.Id, 3, now.AddDays(-5));
            
            // Week 2 (3 days ago)
            await CreateOrderWithItemsAsync(buyer.Id, address.Id, store.Id, product1.Id, 1, now.AddDays(-3));
            await CreateOrderWithItemsAsync(buyer.Id, address.Id, store.Id, product2.Id, 1, now.AddDays(-3));
            
            // Today
            await CreateOrderWithItemsAsync(buyer.Id, address.Id, store.Id, product3.Id, 2, now);
            
            Console.WriteLine($"✓ Created 6 test orders spread over 7 days");

            // Step 5: Test dashboard metrics with different filters
            Console.WriteLine("\n--- Testing Dashboard Metrics ---");

            // Test 1: Last 7 days, daily granularity, no filters
            var metrics1 = await _dashboardService.GetMetricsAsync(
                store.Id,
                now.AddDays(-6).Date,
                now.Date,
                TimeGranularity.Day);

            Console.WriteLine($"\nTest 1: Last 7 Days (Daily)");
            Console.WriteLine($"  Total GMV: ${metrics1.TotalGMV:F2}");
            Console.WriteLine($"  Total Orders: {metrics1.TotalOrders}");
            Console.WriteLine($"  Average Order Value: ${metrics1.AverageOrderValue:F2}");
            Console.WriteLine($"  Total Items Sold: {metrics1.TotalItemsSold}");
            Console.WriteLine($"  Time Series Points: {metrics1.TimeSeriesData.Count}");

            // Expected: 6 orders, GMV = 999.99 + (2*29.99) + (3*19.99) + 999.99 + 29.99 + (2*19.99)
            var expectedGMV = 999.99m + (2 * 29.99m) + (3 * 19.99m) + 999.99m + 29.99m + (2 * 19.99m);
            var expectedItems = 1 + 2 + 3 + 1 + 1 + 2;
            
            if (Math.Abs(metrics1.TotalGMV - expectedGMV) < 0.01m && metrics1.TotalOrders == 6 && metrics1.TotalItemsSold == expectedItems)
            {
                Console.WriteLine("  ✓ Metrics match expected values!");
            }
            else
            {
                Console.WriteLine($"  ✗ Metrics mismatch! Expected GMV: ${expectedGMV:F2}, Orders: 6, Items: {expectedItems}");
            }

            // Test 2: Last 7 days, weekly granularity
            var metrics2 = await _dashboardService.GetMetricsAsync(
                store.Id,
                now.AddDays(-6).Date,
                now.Date,
                TimeGranularity.Week);

            Console.WriteLine($"\nTest 2: Last 7 Days (Weekly)");
            Console.WriteLine($"  Total GMV: ${metrics2.TotalGMV:F2}");
            Console.WriteLine($"  Total Orders: {metrics2.TotalOrders}");
            Console.WriteLine($"  Time Series Points: {metrics2.TimeSeriesData.Count}");

            // Test 3: Filter by product (Electronics category products only)
            var metrics3 = await _dashboardService.GetMetricsAsync(
                store.Id,
                now.AddDays(-6).Date,
                now.Date,
                TimeGranularity.Day,
                categoryId: category1.Id);

            Console.WriteLine($"\nTest 3: Electronics Category Filter");
            Console.WriteLine($"  Total GMV: ${metrics3.TotalGMV:F2}");
            Console.WriteLine($"  Total Orders: {metrics3.TotalOrders}"); // Should count unique orders, not items
            Console.WriteLine($"  Total Items Sold: {metrics3.TotalItemsSold}");
            
            // Expected GMV for electronics: 999.99 + (2*29.99) + 999.99 + 29.99
            var expectedElectronicsGMV = 999.99m + (2 * 29.99m) + 999.99m + 29.99m;
            
            if (Math.Abs(metrics3.TotalGMV - expectedElectronicsGMV) < 0.01m)
            {
                Console.WriteLine("  ✓ Electronics filter working correctly!");
            }
            else
            {
                Console.WriteLine($"  ✗ Filter mismatch! Expected GMV: ${expectedElectronicsGMV:F2}");
            }

            // Test 4: Filter by specific product
            var metrics4 = await _dashboardService.GetMetricsAsync(
                store.Id,
                now.AddDays(-6).Date,
                now.Date,
                TimeGranularity.Day,
                productId: product1.Id);

            Console.WriteLine($"\nTest 4: Laptop Product Filter");
            Console.WriteLine($"  Total GMV: ${metrics4.TotalGMV:F2}");
            Console.WriteLine($"  Total Orders: {metrics4.TotalOrders}");
            Console.WriteLine($"  Total Items Sold: {metrics4.TotalItemsSold}");
            
            // Expected: 2 laptops sold (1+1), GMV = 1999.98
            if (Math.Abs(metrics4.TotalGMV - 1999.98m) < 0.01m && metrics4.TotalItemsSold == 2)
            {
                Console.WriteLine("  ✓ Product filter working correctly!");
            }
            else
            {
                Console.WriteLine($"  ✗ Product filter mismatch!");
            }

            // Test 5: Verify seller isolation (create another store and verify no cross-contamination)
            var otherSeller = await CreateSellerAsync();
            var otherStore = await CreateStoreAsync(otherSeller.Id);
            var otherProduct = await CreateProductAsync(otherStore.Id, category1.Id, "Other Laptop", 1500m);
            await CreateOrderWithItemsAsync(buyer.Id, address.Id, otherStore.Id, otherProduct.Id, 5, now);

            var metrics5 = await _dashboardService.GetMetricsAsync(
                store.Id,
                now.AddDays(-6).Date,
                now.Date,
                TimeGranularity.Day);

            Console.WriteLine($"\nTest 5: Seller Isolation");
            Console.WriteLine($"  Store 1 Total Orders: {metrics5.TotalOrders}");
            
            if (metrics5.TotalOrders == 6)
            {
                Console.WriteLine("  ✓ Seller data isolation working correctly! Other store's data not included.");
            }
            else
            {
                Console.WriteLine($"  ✗ Seller isolation failed! Expected 6 orders, got {metrics5.TotalOrders}");
            }

            // Test 6: Empty state (future date range with no data)
            var metrics6 = await _dashboardService.GetMetricsAsync(
                store.Id,
                now.AddDays(10).Date,
                now.AddDays(20).Date,
                TimeGranularity.Day);

            Console.WriteLine($"\nTest 6: Empty State (Future Date Range)");
            Console.WriteLine($"  Total GMV: ${metrics6.TotalGMV:F2}");
            Console.WriteLine($"  Total Orders: {metrics6.TotalOrders}");
            
            if (metrics6.TotalOrders == 0 && metrics6.TotalGMV == 0)
            {
                Console.WriteLine("  ✓ Empty state handled correctly!");
            }
            else
            {
                Console.WriteLine($"  ✗ Empty state issue!");
            }

            Console.WriteLine("\n=== All Dashboard Tests Completed ===\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Test failed with error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task<User> CreateSellerAsync()
    {
        var user = new User
        {
            Email = $"seller{Guid.NewGuid():N}@test.com",
            PasswordHash = "test_hash",
            FirstName = "Test",
            LastName = "Seller",
            UserType = UserType.Seller,
            Status = AccountStatus.Active,
            AcceptedTerms = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<User> CreateBuyerAsync()
    {
        var user = new User
        {
            Email = $"buyer{Guid.NewGuid():N}@test.com",
            PasswordHash = "test_hash",
            FirstName = "Test",
            LastName = "Buyer",
            UserType = UserType.Buyer,
            Status = AccountStatus.Active,
            AcceptedTerms = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Store> CreateStoreAsync(int userId)
    {
        var store = new Store
        {
            UserId = userId,
            StoreName = $"Test Store {Guid.NewGuid():N}",
            Slug = $"test-store-{Guid.NewGuid():N}",
            Description = "Test store for dashboard testing",
            Status = StoreStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Stores.Add(store);
        await _context.SaveChangesAsync();
        return store;
    }

    private async Task<Category> CreateCategoryAsync(string name)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (category != null)
            return category;

        category = new Category
        {
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    private async Task<Product> CreateProductAsync(int storeId, int categoryId, string title, decimal price)
    {
        var product = new Product
        {
            StoreId = storeId,
            CategoryId = categoryId,
            Title = title,
            Category = title, // For backward compatibility
            Description = $"Test product {title}",
            Price = price,
            Status = ProductStatus.Active,
            Stock = 100,
            Condition = ProductCondition.New,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    private async Task<Address> CreateAddressAsync(int userId)
    {
        var address = new Address
        {
            UserId = userId,
            FullName = "Test Buyer",
            PhoneNumber = "555-1234",
            AddressLine1 = "123 Test St",
            City = "Test City",
            StateProvince = "TS",
            PostalCode = "12345",
            CountryCode = "US",
            IsDefault = true
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();
        return address;
    }

    private async Task<Order> CreateOrderWithItemsAsync(
        int buyerId, 
        int addressId, 
        int storeId, 
        int productId, 
        int quantity,
        DateTime orderedAt)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            throw new InvalidOperationException($"Product {productId} not found");

        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 50);
        
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = buyerId,
            DeliveryAddressId = addressId,
            Status = OrderStatus.New,
            PaymentStatus = PaymentStatus.Completed,
            Subtotal = product.Price * quantity,
            ShippingCost = 10m,
            TotalAmount = (product.Price * quantity) + 10m,
            OrderedAt = orderedAt,
            UpdatedAt = orderedAt
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            StoreId = storeId,
            ProductId = productId,
            ProductTitle = product.Title,
            Quantity = quantity,
            UnitPrice = product.Price,
            Subtotal = product.Price * quantity,
            Status = OrderItemStatus.New
        };

        _context.OrderItems.Add(orderItem);
        await _context.SaveChangesAsync();

        return order;
    }
}
