using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MercatoApp.Tests;

/// <summary>
/// Test scenario for account deletion with anonymization feature.
/// This demonstrates how to test the account deletion functionality.
/// 
/// To run this test manually:
/// 1. Start the application
/// 2. Register test users (buyer and seller)
/// 3. Create some orders and return requests
/// 4. Navigate to Account > Delete Account
/// 5. Verify blocking conditions are enforced
/// 6. Verify deletion impact is displayed correctly
/// 7. Complete account deletion
/// 8. Verify user data is anonymized
/// 9. Verify transactional data is retained but anonymized
/// 10. Verify deletion is logged in audit trail
/// </summary>
public class AccountDeletionTestScenario
{
    /// <summary>
    /// Setup test data for account deletion scenarios
    /// </summary>
    public async Task SetupTestDataAsync(ApplicationDbContext context)
    {
        // Create buyer user with complete profile
        var buyerUser = new User
        {
            Email = "testbuyer@example.com",
            FirstName = "John",
            LastName = "Buyer",
            PhoneNumber = "+1234567890",
            Address = "123 Test Street",
            City = "Test City",
            PostalCode = "12345",
            Country = "US",
            UserType = UserType.Buyer,
            Status = AccountStatus.Active,
            PasswordHash = "test_hash_buyer",
            CreatedAt = DateTime.UtcNow,
            AcceptedTerms = true
        };

        var sellerUser = new User
        {
            Email = "testseller@example.com",
            FirstName = "Jane",
            LastName = "Seller",
            PhoneNumber = "+9876543210",
            Address = "456 Seller Ave",
            City = "Seller City",
            PostalCode = "54321",
            Country = "CA",
            UserType = UserType.Seller,
            Status = AccountStatus.Active,
            PasswordHash = "test_hash_seller",
            CreatedAt = DateTime.UtcNow,
            AcceptedTerms = true,
            KycStatus = KycStatus.Approved
        };

        context.Users.AddRange(buyerUser, sellerUser);
        await context.SaveChangesAsync();

        // Create store for seller
        var store = new Store
        {
            UserId = sellerUser.Id,
            StoreName = "Test Seller Store",
            Slug = "test-seller-store",
            Status = StoreStatus.Active,
            Description = "Test store for deletion scenario"
        };

        context.Stores.Add(store);
        await context.SaveChangesAsync();

        // Create addresses for buyer
        var address1 = new Address
        {
            UserId = buyerUser.Id,
            FullName = "John Buyer",
            PhoneNumber = "+1234567890",
            AddressLine1 = "123 Test Street",
            City = "Test City",
            PostalCode = "12345",
            CountryCode = "US",
            IsDefault = true
        };

        var address2 = new Address
        {
            UserId = buyerUser.Id,
            FullName = "John Buyer",
            PhoneNumber = "+1234567890",
            AddressLine1 = "789 Work Ave",
            City = "Test City",
            PostalCode = "12345",
            CountryCode = "US",
            IsDefault = false
        };

        context.Addresses.AddRange(address1, address2);
        await context.SaveChangesAsync();

        // Create product for seller
        var product = new Product
        {
            StoreId = store.Id,
            Title = "Test Product",
            Description = "A test product",
            Price = 99.99m,
            Status = ProductStatus.Active,
            Stock = 10,
            Category = "Test Category",
            Condition = ProductCondition.New,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Create completed order (safe to delete)
        var completedOrder = new Order
        {
            OrderNumber = "ORD-TEST-001",
            UserId = buyerUser.Id,
            DeliveryAddressId = address1.Id,
            Status = OrderStatus.Delivered,
            Subtotal = 99.99m,
            ShippingCost = 10.00m,
            TaxAmount = 5.50m,
            TotalAmount = 115.49m,
            PaymentStatus = PaymentStatus.Completed,
            OrderedAt = DateTime.UtcNow.AddDays(-30)
        };

        context.Orders.Add(completedOrder);
        await context.SaveChangesAsync();

        var orderItem = new OrderItem
        {
            OrderId = completedOrder.Id,
            ProductId = product.Id,
            StoreId = store.Id,
            ProductTitle = product.Title,
            Quantity = 1,
            UnitPrice = 99.99m,
            Subtotal = 99.99m,
            Status = OrderItemStatus.Shipped
        };

        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        // Create seller sub-order
        var subOrder = new SellerSubOrder
        {
            ParentOrderId = completedOrder.Id,
            StoreId = store.Id,
            SubOrderNumber = "SUB-TEST-001",
            Status = OrderStatus.Delivered,
            Subtotal = 99.99m,
            ShippingCost = 10.00m,
            TotalAmount = 109.99m,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        context.SellerSubOrders.Add(subOrder);
        await context.SaveChangesAsync();

        orderItem.SellerSubOrderId = subOrder.Id;
        await context.SaveChangesAsync();

        // Create completed return request (safe to delete)
        var returnRequest = new ReturnRequest
        {
            ReturnNumber = "RTN-TEST-001",
            SubOrderId = subOrder.Id,
            BuyerId = buyerUser.Id,
            RequestType = ReturnRequestType.Return,
            Reason = ReturnReason.NotAsDescribed,
            Status = ReturnStatus.Completed,
            Description = "Product not as described",
            RequestedAt = DateTime.UtcNow.AddDays(-20),
            UpdatedAt = DateTime.UtcNow.AddDays(-15)
        };

        context.ReturnRequests.Add(returnRequest);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Setup data for blocking conditions test
    /// </summary>
    public async Task SetupBlockingConditionsAsync(ApplicationDbContext context)
    {
        var buyerUser = await context.Users.FirstAsync(u => u.Email == "testbuyer@example.com");
        var sellerUser = await context.Users.FirstAsync(u => u.Email == "testseller@example.com");
        var store = await context.Stores.FirstAsync(s => s.UserId == sellerUser.Id);
        var address = await context.Addresses.FirstAsync(a => a.UserId == buyerUser.Id && a.IsDefault);
        var product = await context.Products.FirstAsync(p => p.StoreId == store.Id);

        // Create pending order (blocking condition)
        var pendingOrder = new Order
        {
            OrderNumber = "ORD-PENDING-001",
            UserId = buyerUser.Id,
            DeliveryAddressId = address.Id,
            Status = OrderStatus.New,
            Subtotal = 99.99m,
            ShippingCost = 10.00m,
            TaxAmount = 5.50m,
            TotalAmount = 115.49m,
            PaymentStatus = PaymentStatus.Pending,
            OrderedAt = DateTime.UtcNow
        };

        context.Orders.Add(pendingOrder);
        await context.SaveChangesAsync();

        var orderItem = new OrderItem
        {
            OrderId = pendingOrder.Id,
            ProductId = product.Id,
            StoreId = store.Id,
            ProductTitle = product.Title,
            Quantity = 1,
            UnitPrice = 99.99m,
            Subtotal = 99.99m,
            Status = OrderItemStatus.New
        };

        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        var subOrder = new SellerSubOrder
        {
            ParentOrderId = pendingOrder.Id,
            StoreId = store.Id,
            SubOrderNumber = "SUB-PENDING-001",
            Status = OrderStatus.New,
            Subtotal = 99.99m,
            ShippingCost = 10.00m,
            TotalAmount = 109.99m,
            CreatedAt = DateTime.UtcNow
        };

        context.SellerSubOrders.Add(subOrder);
        await context.SaveChangesAsync();

        orderItem.SellerSubOrderId = subOrder.Id;
        await context.SaveChangesAsync();

        // Create unresolved return request (blocking condition)
        var unresolvedReturn = new ReturnRequest
        {
            ReturnNumber = "RTN-PENDING-001",
            SubOrderId = subOrder.Id,
            BuyerId = buyerUser.Id,
            RequestType = ReturnRequestType.Return,
            Reason = ReturnReason.Damaged,
            Status = ReturnStatus.Requested,
            Description = "Item is defective",
            RequestedAt = DateTime.UtcNow
        };

        context.ReturnRequests.Add(unresolvedReturn);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Test account deletion validation - should pass
    /// </summary>
    public async Task TestValidation_ShouldAllow(IAccountDeletionService deletionService)
    {
        Console.WriteLine("=== Testing Account Deletion Validation (Should Allow) ===");
        
        // Use buyer with only completed orders and returns
        var buyerId = 1; // Assuming this is the buyer user ID

        var validation = await deletionService.ValidateAccountDeletionAsync(buyerId);

        Console.WriteLine($"Can Delete: {validation.CanDelete}");
        Console.WriteLine($"Blocking Reasons Count: {validation.BlockingReasons.Count}");
        
        foreach (var reason in validation.BlockingReasons)
        {
            Console.WriteLine($"  - {reason}");
        }

        if (validation.CanDelete)
        {
            Console.WriteLine("✓ Validation passed - account can be deleted");
        }
        else
        {
            Console.WriteLine("✗ Validation failed - account cannot be deleted");
        }
    }

    /// <summary>
    /// Test account deletion validation - should block
    /// </summary>
    public async Task TestValidation_ShouldBlock(IAccountDeletionService deletionService)
    {
        Console.WriteLine("\n=== Testing Account Deletion Validation (Should Block) ===");
        
        // After adding blocking conditions
        var buyerId = 1; // Assuming this is the buyer user ID

        var validation = await deletionService.ValidateAccountDeletionAsync(buyerId);

        Console.WriteLine($"Can Delete: {validation.CanDelete}");
        Console.WriteLine($"Blocking Reasons Count: {validation.BlockingReasons.Count}");
        
        foreach (var reason in validation.BlockingReasons)
        {
            Console.WriteLine($"  - {reason}");
        }

        if (!validation.CanDelete && validation.BlockingReasons.Count > 0)
        {
            Console.WriteLine("✓ Validation correctly blocked deletion");
        }
        else
        {
            Console.WriteLine("✗ Validation should have blocked deletion");
        }
    }

    /// <summary>
    /// Test getting deletion impact
    /// </summary>
    public async Task TestGetDeletionImpact(IAccountDeletionService deletionService)
    {
        Console.WriteLine("\n=== Testing Get Deletion Impact ===");
        
        var buyerId = 1; // Assuming this is the buyer user ID

        var impact = await deletionService.GetDeletionImpactAsync(buyerId);

        Console.WriteLine($"Order Count: {impact.OrderCount}");
        Console.WriteLine($"Return Request Count: {impact.ReturnRequestCount}");
        Console.WriteLine($"Address Count: {impact.AddressCount}");
        Console.WriteLine($"Has Store: {impact.HasStore}");
        if (impact.HasStore)
        {
            Console.WriteLine($"Store Name: {impact.StoreName}");
        }

        Console.WriteLine("✓ Deletion impact retrieved successfully");
    }

    /// <summary>
    /// Test successful account deletion
    /// </summary>
    public async Task TestAccountDeletion(IAccountDeletionService deletionService, ApplicationDbContext context)
    {
        Console.WriteLine("\n=== Testing Account Deletion ===");
        
        var buyerId = 1; // Assuming this is the buyer user ID
        var ipAddress = "192.168.1.1";
        var reason = "Testing account deletion feature";

        // Get user before deletion
        var userBefore = await context.Users.FindAsync(buyerId);
        if (userBefore == null)
        {
            Console.WriteLine("✗ User not found");
            return;
        }

        Console.WriteLine($"Before Deletion:");
        Console.WriteLine($"  Email: {userBefore.Email}");
        Console.WriteLine($"  Name: {userBefore.FirstName} {userBefore.LastName}");
        Console.WriteLine($"  Status: {userBefore.Status}");

        // Perform deletion
        var result = await deletionService.DeleteAccountAsync(buyerId, ipAddress, reason);

        if (result.Success)
        {
            Console.WriteLine($"\n✓ Account deleted successfully");
            Console.WriteLine($"  Anonymized Email: {result.AnonymizedEmail}");
            Console.WriteLine($"  Deletion Log ID: {result.DeletionLogId}");

            // Verify anonymization
            var userAfter = await context.Users.FindAsync(buyerId);
            if (userAfter != null)
            {
                Console.WriteLine($"\nAfter Deletion:");
                Console.WriteLine($"  Email: {userAfter.Email}");
                Console.WriteLine($"  Name: {userAfter.FirstName} {userAfter.LastName}");
                Console.WriteLine($"  Status: {userAfter.Status}");
                Console.WriteLine($"  Phone: {userAfter.PhoneNumber ?? "null"}");
                Console.WriteLine($"  Address: {userAfter.Address ?? "null"}");

                // Verify deletion log
                var deletionLog = await context.AccountDeletionLogs.FindAsync(result.DeletionLogId);
                if (deletionLog != null)
                {
                    Console.WriteLine($"\nDeletion Log:");
                    Console.WriteLine($"  User ID: {deletionLog.UserId}");
                    Console.WriteLine($"  Anonymized Email: {deletionLog.AnonymizedEmail}");
                    Console.WriteLine($"  User Type: {deletionLog.UserType}");
                    Console.WriteLine($"  Order Count: {deletionLog.OrderCount}");
                    Console.WriteLine($"  Return Request Count: {deletionLog.ReturnRequestCount}");
                    Console.WriteLine($"  IP Address: {deletionLog.RequestIpAddress}");
                    Console.WriteLine($"  Metadata: {deletionLog.Metadata}");
                }

                // Verify addresses are anonymized
                var addresses = await context.Addresses.Where(a => a.UserId == buyerId).ToListAsync();
                Console.WriteLine($"\nAnonymized Addresses: {addresses.Count}");
                foreach (var address in addresses)
                {
                    Console.WriteLine($"  - {address.FullName}, {address.AddressLine1}, {address.City}");
                }

                // Verify orders are preserved but contact info anonymized
                var orders = await context.Orders.Where(o => o.UserId == buyerId).ToListAsync();
                Console.WriteLine($"\nPreserved Orders: {orders.Count}");
                foreach (var order in orders)
                {
                    Console.WriteLine($"  - {order.OrderNumber}, Amount: ${order.TotalAmount}, Status: {order.Status}");
                }
            }
        }
        else
        {
            Console.WriteLine($"\n✗ Account deletion failed");
            Console.WriteLine($"  Error: {result.ErrorMessage}");
        }
    }

    /// <summary>
    /// Run all tests in sequence
    /// </summary>
    public async Task RunAllTestsAsync(
        ApplicationDbContext context,
        IAccountDeletionService deletionService)
    {
        Console.WriteLine("=== Account Deletion Test Scenario ===\n");

        try
        {
            // Setup initial test data
            Console.WriteLine("Setting up test data...");
            await SetupTestDataAsync(context);
            Console.WriteLine("✓ Test data created\n");

            // Test deletion impact
            await TestGetDeletionImpact(deletionService);

            // Test validation without blocking conditions
            await TestValidation_ShouldAllow(deletionService);

            // Add blocking conditions
            Console.WriteLine("\nAdding blocking conditions...");
            await SetupBlockingConditionsAsync(context);
            Console.WriteLine("✓ Blocking conditions added\n");

            // Test validation with blocking conditions
            await TestValidation_ShouldBlock(deletionService);

            // Remove blocking conditions for successful deletion
            Console.WriteLine("\nRemoving blocking conditions...");
            var pendingOrders = await context.Orders.Where(o => o.OrderNumber.StartsWith("ORD-PENDING")).ToListAsync();
            context.Orders.RemoveRange(pendingOrders);
            var unresolvedReturns = await context.ReturnRequests.Where(r => r.ReturnNumber.StartsWith("RTN-PENDING")).ToListAsync();
            context.ReturnRequests.RemoveRange(unresolvedReturns);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Blocking conditions removed\n");

            // Test successful deletion
            await TestAccountDeletion(deletionService, context);

            Console.WriteLine("\n=== All Tests Completed ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Test failed with exception:");
            Console.WriteLine($"  {ex.Message}");
            Console.WriteLine($"  {ex.StackTrace}");
        }
    }
}
