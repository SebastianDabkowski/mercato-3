using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MercatoApp;

/// <summary>
/// Test scenario for seller email notifications.
/// Validates that sellers receive email alerts for new orders, returns, and payouts.
/// </summary>
public class SellerEmailNotificationTestScenario
{
    public static async Task RunTestScenarioAsync()
    {
        Console.WriteLine("=== Seller Email Notification Test Scenario ===\n");

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "SellerEmailNotificationTestDb")
            .Options;

        using var context = new ApplicationDbContext(options);

        // Setup services
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var emailLogger = loggerFactory.CreateLogger<EmailService>();
        var orderLogger = loggerFactory.CreateLogger<OrderService>();
        var returnLogger = loggerFactory.CreateLogger<ReturnRequestService>();
        var payoutLogger = loggerFactory.CreateLogger<PayoutService>();

        var emailService = new EmailService(context, emailLogger);
        
        // Setup test data
        var (seller, store, buyer, product) = await SetupTestDataAsync(context);

        Console.WriteLine($"✓ Test data created:");
        Console.WriteLine($"  - Seller: {seller.FirstName} {seller.LastName} ({seller.Email})");
        Console.WriteLine($"  - Store: {store.StoreName} (Contact: {store.ContactEmail})");
        Console.WriteLine($"  - Buyer: {buyer.FirstName} {buyer.LastName} ({buyer.Email})");
        Console.WriteLine($"  - Product: {product.Title} (${product.Price})\n");

        // Test 1: New Order Notification
        Console.WriteLine("--- Test 1: New Order Notification ---");
        await TestNewOrderNotificationAsync(context, emailService, seller, store, buyer, product);

        // Test 2: Return Request Notification
        Console.WriteLine("\n--- Test 2: Return Request Notification ---");
        await TestReturnRequestNotificationAsync(context, emailService, seller, store, buyer);

        // Test 3: Payout Notification
        Console.WriteLine("\n--- Test 3: Payout Notification ---");
        await TestPayoutNotificationAsync(context, emailService, store);

        // Verify all emails were logged
        Console.WriteLine("\n--- Email Log Summary ---");
        await VerifyEmailLogsAsync(context);

        Console.WriteLine("\n=== All Tests Passed ===");
    }

    private static async Task<(User seller, Store store, User buyer, Product product)> SetupTestDataAsync(
        ApplicationDbContext context)
    {
        // Create seller user
        var seller = new User
        {
            Email = "seller@example.com",
            FirstName = "Jane",
            LastName = "Seller",
            UserType = UserType.Seller,
            PasswordHash = "dummy_hash",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(seller);
        await context.SaveChangesAsync();

        // Create store for seller
        var store = new Store
        {
            UserId = seller.Id,
            StoreName = "Jane's Marketplace",
            Slug = "janes-marketplace",
            ContactEmail = "store@example.com",
            Status = StoreStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Stores.Add(store);

        // Create buyer user
        var buyer = new User
        {
            Email = "buyer@example.com",
            FirstName = "John",
            LastName = "Buyer",
            UserType = UserType.Buyer,
            PasswordHash = "dummy_hash",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(buyer);
        await context.SaveChangesAsync();

        // Create buyer address
        var address = new Address
        {
            UserId = buyer.Id,
            FullName = "John Buyer",
            PhoneNumber = "555-0123",
            AddressLine1 = "123 Main St",
            City = "Test City",
            StateProvince = "TS",
            PostalCode = "12345",
            CountryCode = "US",
            IsDefault = true
        };
        context.Addresses.Add(address);

        // Create product
        var product = new Product
        {
            StoreId = store.Id,
            Title = "Test Product",
            Description = "A test product",
            Price = 50.00m,
            Stock = 100,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);

        await context.SaveChangesAsync();

        return (seller, store, buyer, product);
    }

    private static async Task TestNewOrderNotificationAsync(
        ApplicationDbContext context,
        IEmailService emailService,
        User seller,
        Store store,
        User buyer,
        Product product)
    {
        // Create an order
        var order = new Order
        {
            OrderNumber = "ORD-TEST-001",
            UserId = buyer.Id,
            DeliveryAddressId = context.Addresses.First(a => a.UserId == buyer.Id).Id,
            Status = OrderStatus.New,
            Subtotal = 50.00m,
            ShippingCost = 10.00m,
            TotalAmount = 60.00m,
            PaymentMethodId = 1,
            PaymentStatus = PaymentStatus.Pending,
            OrderedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Create sub-order for seller
        var subOrder = new SellerSubOrder
        {
            ParentOrderId = order.Id,
            StoreId = store.Id,
            SubOrderNumber = "ORD-TEST-001-1",
            Status = OrderStatus.New,
            Subtotal = 50.00m,
            ShippingCost = 10.00m,
            TotalAmount = 60.00m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.SellerSubOrders.Add(subOrder);

        // Create order item
        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            SellerSubOrderId = subOrder.Id,
            StoreId = store.Id,
            ProductId = product.Id,
            ProductTitle = product.Title,
            Quantity = 1,
            UnitPrice = 50.00m,
            Subtotal = 50.00m
        };
        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        // Reload sub-order with navigation properties
        var subOrderWithDetails = await context.SellerSubOrders
            .Include(so => so.Store)
                .ThenInclude(s => s.User)
            .Include(so => so.Items)
            .Include(so => so.ParentOrder)
                .ThenInclude(o => o.DeliveryAddress)
            .FirstAsync(so => so.Id == subOrder.Id);

        var orderWithAddress = await context.Orders
            .Include(o => o.DeliveryAddress)
            .FirstAsync(o => o.Id == order.Id);

        // Send notification
        await emailService.SendNewOrderNotificationToSellerAsync(subOrderWithDetails, orderWithAddress);

        // Verify email log
        var emailLog = await context.EmailLogs
            .Where(e => e.EmailType == EmailType.SellerNewOrder && e.SellerSubOrderId == subOrder.Id)
            .FirstOrDefaultAsync();

        if (emailLog == null)
        {
            throw new Exception("❌ New order notification email was not logged");
        }

        Console.WriteLine($"✓ New order notification sent to: {emailLog.RecipientEmail}");
        Console.WriteLine($"  Subject: {emailLog.Subject}");
        Console.WriteLine($"  Status: {emailLog.Status}");
        Console.WriteLine($"  Sub-Order: {subOrder.SubOrderNumber}");
    }

    private static async Task TestReturnRequestNotificationAsync(
        ApplicationDbContext context,
        IEmailService emailService,
        User seller,
        Store store,
        User buyer)
    {
        // Get the existing sub-order
        var subOrder = await context.SellerSubOrders
            .FirstAsync(so => so.StoreId == store.Id);

        // Mark sub-order as delivered so return is eligible
        subOrder.Status = OrderStatus.Delivered;
        await context.SaveChangesAsync();

        // Create return request
        var returnRequest = new ReturnRequest
        {
            ReturnNumber = "RTN-TEST-001",
            SubOrderId = subOrder.Id,
            BuyerId = buyer.Id,
            RequestType = ReturnRequestType.Return,
            Reason = ReturnReason.Damaged,
            Description = "Product arrived damaged",
            Status = ReturnStatus.Requested,
            RefundAmount = 50.00m,
            IsFullReturn = true,
            RequestedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FirstResponseDeadline = DateTime.UtcNow.AddDays(2),
            ResolutionDeadline = DateTime.UtcNow.AddDays(7)
        };
        context.ReturnRequests.Add(returnRequest);
        await context.SaveChangesAsync();

        // Reload return request with navigation properties
        var returnRequestWithDetails = await context.ReturnRequests
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.Store)
                    .ThenInclude(s => s.User)
            .Include(rr => rr.Buyer)
            .FirstAsync(rr => rr.Id == returnRequest.Id);

        // Send notification
        await emailService.SendReturnRequestNotificationToSellerAsync(returnRequestWithDetails);

        // Verify email log
        var emailLog = await context.EmailLogs
            .Where(e => e.EmailType == EmailType.SellerReturnRequest && e.ReturnRequestId == returnRequest.Id)
            .FirstOrDefaultAsync();

        if (emailLog == null)
        {
            throw new Exception("❌ Return request notification email was not logged");
        }

        Console.WriteLine($"✓ Return request notification sent to: {emailLog.RecipientEmail}");
        Console.WriteLine($"  Subject: {emailLog.Subject}");
        Console.WriteLine($"  Status: {emailLog.Status}");
        Console.WriteLine($"  Return Number: {returnRequest.ReturnNumber}");
        Console.WriteLine($"  Reason: {returnRequest.Reason}");
    }

    private static async Task TestPayoutNotificationAsync(
        ApplicationDbContext context,
        IEmailService emailService,
        Store store)
    {
        // Create payout
        var payout = new Payout
        {
            PayoutNumber = "PAY-TEST-001",
            StoreId = store.Id,
            Amount = 500.00m,
            Currency = "USD",
            Status = PayoutStatus.Paid,
            ScheduledDate = DateTime.UtcNow,
            InitiatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Payouts.Add(payout);
        await context.SaveChangesAsync();

        // Reload payout with navigation properties
        var payoutWithDetails = await context.Payouts
            .Include(p => p.Store)
                .ThenInclude(s => s.User)
            .Include(p => p.PayoutMethod)
            .FirstAsync(p => p.Id == payout.Id);

        // Send notification
        await emailService.SendPayoutNotificationToSellerAsync(payoutWithDetails);

        // Verify email log
        var emailLog = await context.EmailLogs
            .Where(e => e.EmailType == EmailType.SellerPayout && e.PayoutId == payout.Id)
            .FirstOrDefaultAsync();

        if (emailLog == null)
        {
            throw new Exception("❌ Payout notification email was not logged");
        }

        Console.WriteLine($"✓ Payout notification sent to: {emailLog.RecipientEmail}");
        Console.WriteLine($"  Subject: {emailLog.Subject}");
        Console.WriteLine($"  Status: {emailLog.Status}");
        Console.WriteLine($"  Payout Number: {payout.PayoutNumber}");
        Console.WriteLine($"  Amount: ${payout.Amount:F2} {payout.Currency}");
    }

    private static async Task VerifyEmailLogsAsync(ApplicationDbContext context)
    {
        var sellerEmails = await context.EmailLogs
            .Where(e => e.EmailType == EmailType.SellerNewOrder
                     || e.EmailType == EmailType.SellerReturnRequest
                     || e.EmailType == EmailType.SellerPayout)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        Console.WriteLine($"Total seller notification emails logged: {sellerEmails.Count}");
        
        foreach (var email in sellerEmails)
        {
            Console.WriteLine($"  - {email.EmailType}: {email.Subject} ({email.Status})");
        }

        if (sellerEmails.Count != 3)
        {
            throw new Exception($"❌ Expected 3 seller notification emails, but found {sellerEmails.Count}");
        }

        // Verify all emails were sent successfully
        var failedEmails = sellerEmails.Where(e => e.Status != EmailStatus.Sent).ToList();
        if (failedEmails.Any())
        {
            throw new Exception($"❌ {failedEmails.Count} emails failed to send");
        }

        Console.WriteLine("✓ All seller notification emails were logged and sent successfully");
    }
}
