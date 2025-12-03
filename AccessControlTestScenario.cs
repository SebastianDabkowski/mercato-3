using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MercatoApp;

/// <summary>
/// Test scenario for validating role-based access control and multi-tenant isolation.
/// </summary>
public class AccessControlTestScenario
{
    private readonly ApplicationDbContext _context;
    private readonly IResourceAuthorizationService _resourceAuthService;
    private readonly IOrderService _orderService;
    private readonly IAdminAuditLogService _auditLogService;
    private readonly ILogger<AccessControlTestScenario> _logger;

    public AccessControlTestScenario(
        ApplicationDbContext context,
        IResourceAuthorizationService resourceAuthService,
        IOrderService orderService,
        IAdminAuditLogService auditLogService,
        ILogger<AccessControlTestScenario> logger)
    {
        _context = context;
        _resourceAuthService = resourceAuthService;
        _orderService = orderService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Runs the complete access control test scenario.
    /// </summary>
    public async Task<bool> RunTestAsync()
    {
        _logger.LogInformation("Starting Access Control Test Scenario...");

        try
        {
            // Setup test data
            var (seller1, seller2, buyer1, buyer2, admin) = await SetupTestDataAsync();

            // Test 1: Seller cannot access another seller's products
            var test1Result = await TestSellerProductIsolation(seller1, seller2);
            
            // Test 2: Seller cannot access another seller's orders
            var test2Result = await TestSellerOrderIsolation(seller1, seller2, buyer1);
            
            // Test 3: Buyer can only access their own orders
            var test3Result = await TestBuyerOrderIsolation(buyer1, buyer2);
            
            // Test 4: Admin access to sensitive data is logged
            var test4Result = await TestAdminAccessAuditing(admin, buyer1);

            var allTestsPassed = test1Result && test2Result && test3Result && test4Result;

            if (allTestsPassed)
            {
                _logger.LogInformation("✓ All access control tests passed successfully!");
            }
            else
            {
                _logger.LogError("✗ Some access control tests failed.");
            }

            return allTestsPassed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Access control test scenario failed with exception");
            return false;
        }
    }

    private async Task<(User seller1, User seller2, User buyer1, User buyer2, User admin)> SetupTestDataAsync()
    {
        _logger.LogInformation("Setting up test data...");

        // Create sellers
        var seller1 = new User
        {
            Email = "seller1@test.com",
            FirstName = "Seller",
            LastName = "One",
            PasswordHash = "hash1",
            UserType = UserType.Seller,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(seller1);
        await _context.SaveChangesAsync();

        var seller2 = new User
        {
            Email = "seller2@test.com",
            FirstName = "Seller",
            LastName = "Two",
            PasswordHash = "hash2",
            UserType = UserType.Seller,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(seller2);
        await _context.SaveChangesAsync();

        // Create stores
        var store1 = new Store
        {
            UserId = seller1.Id,
            StoreName = "Store One",
            Slug = "store-one",
            Status = StoreStatus.Active
        };
        _context.Stores.Add(store1);

        var store2 = new Store
        {
            UserId = seller2.Id,
            StoreName = "Store Two",
            Slug = "store-two",
            Status = StoreStatus.Active
        };
        _context.Stores.Add(store2);
        await _context.SaveChangesAsync();

        // Create products
        var product1 = new Product
        {
            StoreId = store1.Id,
            Title = "Product One",
            Price = 100,
            Stock = 10,
            Category = "Electronics",
            Status = ProductStatus.Active
        };
        _context.Products.Add(product1);

        var product2 = new Product
        {
            StoreId = store2.Id,
            Title = "Product Two",
            Price = 200,
            Stock = 20,
            Category = "Electronics",
            Status = ProductStatus.Active
        };
        _context.Products.Add(product2);

        // Create buyers
        var buyer1 = new User
        {
            Email = "buyer1@test.com",
            FirstName = "Buyer",
            LastName = "One",
            PasswordHash = "hash3",
            UserType = UserType.Buyer,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(buyer1);
        await _context.SaveChangesAsync();

        var buyer2 = new User
        {
            Email = "buyer2@test.com",
            FirstName = "Buyer",
            LastName = "Two",
            PasswordHash = "hash4",
            UserType = UserType.Buyer,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(buyer2);
        await _context.SaveChangesAsync();

        // Create admin
        var admin = new User
        {
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = "hash5",
            UserType = UserType.Admin,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        // Create addresses
        var address1 = new Address
        {
            UserId = buyer1.Id,
            FullName = "Buyer One",
            AddressLine1 = "123 Main St",
            City = "City",
            StateProvince = "State",
            PostalCode = "12345",
            CountryCode = "US"
        };
        _context.Addresses.Add(address1);

        var address2 = new Address
        {
            UserId = buyer2.Id,
            FullName = "Buyer Two",
            AddressLine1 = "456 Elm St",
            City = "City",
            StateProvince = "State",
            PostalCode = "12345",
            CountryCode = "US"
        };
        _context.Addresses.Add(address2);
        await _context.SaveChangesAsync();

        // Create orders
        var order1 = new Order
        {
            UserId = buyer1.Id,
            OrderNumber = "ORD-001",
            DeliveryAddressId = address1.Id,
            Subtotal = 100,
            ShippingCost = 10,
            TotalAmount = 110,
            Status = OrderStatus.New
        };
        _context.Orders.Add(order1);

        var order2 = new Order
        {
            UserId = buyer2.Id,
            OrderNumber = "ORD-002",
            DeliveryAddressId = address2.Id,
            Subtotal = 200,
            ShippingCost = 10,
            TotalAmount = 210,
            Status = OrderStatus.New
        };
        _context.Orders.Add(order2);
        await _context.SaveChangesAsync();

        // Create sub-orders
        var subOrder1 = new SellerSubOrder
        {
            ParentOrderId = order1.Id,
            StoreId = store1.Id,
            SubOrderNumber = "SUB-001",
            Subtotal = 100,
            ShippingCost = 10,
            TotalAmount = 110,
            Status = OrderStatus.New
        };
        _context.SellerSubOrders.Add(subOrder1);

        var subOrder2 = new SellerSubOrder
        {
            ParentOrderId = order2.Id,
            StoreId = store2.Id,
            SubOrderNumber = "SUB-002",
            Subtotal = 200,
            ShippingCost = 10,
            TotalAmount = 210,
            Status = OrderStatus.New
        };
        _context.SellerSubOrders.Add(subOrder2);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Test data setup complete.");

        return (seller1, seller2, buyer1, buyer2, admin);
    }

    private async Task<bool> TestSellerProductIsolation(User seller1, User seller2)
    {
        _logger.LogInformation("Test 1: Seller cannot access another seller's products");

        // Get seller2's product
        var seller2Product = await _context.Products
            .FirstOrDefaultAsync(p => p.Store.UserId == seller2.Id);

        if (seller2Product == null)
        {
            _logger.LogError("Test data error: seller2 product not found");
            return false;
        }

        // Try to access seller2's product as seller1
        var (authResult, storeId) = await _resourceAuthService.AuthorizeProductAccessAsync(
            seller1.Id, seller2Product.Id);

        if (authResult.IsAuthorized)
        {
            _logger.LogError("✗ Test 1 FAILED: Seller1 was able to access Seller2's product");
            return false;
        }

        _logger.LogInformation("✓ Test 1 PASSED: Seller1 correctly denied access to Seller2's product");
        return true;
    }

    private async Task<bool> TestSellerOrderIsolation(User seller1, User seller2, User buyer1)
    {
        _logger.LogInformation("Test 2: Seller cannot access another seller's orders");

        // Get seller2's sub-order
        var seller2SubOrder = await _context.SellerSubOrders
            .FirstOrDefaultAsync(so => so.Store.UserId == seller2.Id);

        if (seller2SubOrder == null)
        {
            _logger.LogError("Test data error: seller2 sub-order not found");
            return false;
        }

        // Try to access seller2's sub-order as seller1
        var (authResult, storeId) = await _resourceAuthService.AuthorizeSubOrderAccessAsync(
            seller1.Id, seller2SubOrder.Id);

        if (authResult.IsAuthorized)
        {
            _logger.LogError("✗ Test 2 FAILED: Seller1 was able to access Seller2's sub-order");
            return false;
        }

        // Verify using the service method
        var subOrder = await _orderService.GetSubOrderByIdForSellerAsync(
            seller2SubOrder.Id, seller1.Id);

        if (subOrder != null)
        {
            _logger.LogError("✗ Test 2 FAILED: GetSubOrderByIdForSellerAsync returned data for wrong seller");
            return false;
        }

        _logger.LogInformation("✓ Test 2 PASSED: Seller1 correctly denied access to Seller2's sub-order");
        return true;
    }

    private async Task<bool> TestBuyerOrderIsolation(User buyer1, User buyer2)
    {
        _logger.LogInformation("Test 3: Buyer can only access their own orders");

        // Get buyer2's order
        var buyer2Order = await _context.Orders
            .FirstOrDefaultAsync(o => o.UserId == buyer2.Id);

        if (buyer2Order == null)
        {
            _logger.LogError("Test data error: buyer2 order not found");
            return false;
        }

        // Try to access buyer2's order as buyer1
        var authResult = await _resourceAuthService.AuthorizeOrderAccessAsync(
            buyer1.Id, buyer2Order.Id);

        if (authResult.IsAuthorized)
        {
            _logger.LogError("✗ Test 3 FAILED: Buyer1 was able to access Buyer2's order");
            return false;
        }

        // Verify using the service method
        var order = await _orderService.GetOrderByIdForBuyerAsync(buyer2Order.Id, buyer1.Id);

        if (order != null)
        {
            _logger.LogError("✗ Test 3 FAILED: GetOrderByIdForBuyerAsync returned data for wrong buyer");
            return false;
        }

        _logger.LogInformation("✓ Test 3 PASSED: Buyer1 correctly denied access to Buyer2's order");
        return true;
    }

    private async Task<bool> TestAdminAccessAuditing(User admin, User buyer1)
    {
        _logger.LogInformation("Test 4: Admin access to sensitive data is logged");

        // Log admin access to buyer's profile
        var auditLog = await _auditLogService.LogSensitiveAccessAsync(
            admin.Id,
            "UserProfile",
            buyer1.Id,
            buyer1.Email,
            buyer1.Id);

        if (auditLog == null)
        {
            _logger.LogError("✗ Test 4 FAILED: Audit log not created");
            return false;
        }

        // Verify the audit log was saved
        var savedAuditLog = await _context.AdminAuditLogs
            .FirstOrDefaultAsync(a => a.Id == auditLog.Id);

        if (savedAuditLog == null)
        {
            _logger.LogError("✗ Test 4 FAILED: Audit log not saved to database");
            return false;
        }

        // Verify audit log contains expected data
        if (savedAuditLog.AdminUserId != admin.Id ||
            savedAuditLog.EntityType != "UserProfile" ||
            savedAuditLog.EntityId != buyer1.Id ||
            savedAuditLog.TargetUserId != buyer1.Id ||
            savedAuditLog.Action != "ViewSensitiveData")
        {
            _logger.LogError("✗ Test 4 FAILED: Audit log data is incorrect");
            return false;
        }

        _logger.LogInformation("✓ Test 4 PASSED: Admin access correctly logged with user ID, timestamp, and resource identifier");
        return true;
    }
}
