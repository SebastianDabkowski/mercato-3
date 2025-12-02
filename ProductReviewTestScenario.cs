using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Manual test scenario for product review functionality.
/// This file demonstrates how the product review system works and can be used for testing.
/// </summary>
public class ProductReviewTestScenario
{
    private readonly ApplicationDbContext _context;
    private readonly IProductReviewService _reviewService;
    private readonly IOrderService _orderService;
    private readonly ILogger<ProductReviewTestScenario> _logger;

    public ProductReviewTestScenario(
        ApplicationDbContext context,
        IProductReviewService reviewService,
        IOrderService orderService,
        ILogger<ProductReviewTestScenario> logger)
    {
        _context = context;
        _reviewService = reviewService;
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Runs a test scenario for submitting product reviews.
    /// </summary>
    public async Task RunReviewSubmissionTestAsync()
    {
        _logger.LogInformation("=== Starting Product Review Test Scenario ===");

        try
        {
            // Step 1: Find a delivered order with items
            var order = await _context.Orders
                .Include(o => o.SubOrders)
                    .ThenInclude(so => so.Items)
                        .ThenInclude(i => i.Product)
                .Include(o => o.SubOrders)
                    .ThenInclude(so => so.Store)
                .Where(o => o.UserId != null && o.SubOrders.Any(so => so.Status == OrderStatus.Delivered))
                .FirstOrDefaultAsync();

            if (order == null)
            {
                _logger.LogWarning("No delivered orders found for testing. Creating test data...");
                await CreateTestDeliveredOrderAsync();
                order = await _context.Orders
                    .Include(o => o.SubOrders)
                        .ThenInclude(so => so.Items)
                            .ThenInclude(i => i.Product)
                    .Include(o => o.SubOrders)
                        .ThenInclude(so => so.Store)
                    .Where(o => o.UserId != null && o.SubOrders.Any(so => so.Status == OrderStatus.Delivered))
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    _logger.LogError("Failed to create test data.");
                    return;
                }
            }

            _logger.LogInformation("Found delivered order {OrderNumber} for user {UserId}",
                order.OrderNumber, order.UserId);

            // Step 2: Get the first delivered sub-order and its first item
            var deliveredSubOrder = order.SubOrders.First(so => so.Status == OrderStatus.Delivered);
            var orderItem = deliveredSubOrder.Items.FirstOrDefault();

            if (orderItem == null)
            {
                _logger.LogWarning("No order items found in delivered sub-order.");
                return;
            }

            _logger.LogInformation("Testing review for product: {ProductTitle} (Item ID: {ItemId})",
                orderItem.ProductTitle, orderItem.Id);

            // Step 3: Check if user has already reviewed this item
            var hasReview = await _reviewService.HasUserReviewedOrderItemAsync(order.UserId!.Value, orderItem.Id);
            _logger.LogInformation("User has already reviewed this item: {HasReview}", hasReview);

            if (hasReview)
            {
                _logger.LogInformation("User has already submitted a review for this item. Skipping submission test.");
            }
            else
            {
                // Step 4: Submit a review
                _logger.LogInformation("Submitting 5-star review...");
                var review = await _reviewService.SubmitReviewAsync(
                    order.UserId!.Value,
                    orderItem.Id,
                    5,
                    "Excellent product! Very satisfied with my purchase. Fast delivery and great quality.");

                _logger.LogInformation("✓ Review submitted successfully! Review ID: {ReviewId}", review.Id);
                _logger.LogInformation("  Rating: {Rating} stars", review.Rating);
                _logger.LogInformation("  Approved: {IsApproved}", review.IsApproved);
            }

            // Step 5: Test retrieving reviews for the product
            _logger.LogInformation("Retrieving all approved reviews for product {ProductId}...", orderItem.ProductId);
            var reviews = await _reviewService.GetApprovedReviewsForProductAsync(orderItem.ProductId);
            _logger.LogInformation("Found {Count} approved review(s)", reviews.Count);

            foreach (var r in reviews)
            {
                _logger.LogInformation("  - Review by User {UserId}: {Rating} stars, Created: {CreatedAt}",
                    r.UserId, r.Rating, r.CreatedAt);
            }

            // Step 6: Get average rating
            var avgRating = await _reviewService.GetAverageRatingAsync(orderItem.ProductId);
            if (avgRating.HasValue)
            {
                _logger.LogInformation("Average rating for product: {AvgRating:0.0} stars", avgRating.Value);
            }
            else
            {
                _logger.LogInformation("No reviews yet for this product.");
            }

            // Step 7: Test rate limiting - try to submit multiple reviews
            _logger.LogInformation("\n--- Testing Rate Limiting ---");
            var rateLimitTestItem = deliveredSubOrder.Items.Skip(1).FirstOrDefault();
            if (rateLimitTestItem != null)
            {
                try
                {
                    await _reviewService.SubmitReviewAsync(
                        order.UserId!.Value,
                        rateLimitTestItem.Id,
                        4,
                        "Another great product!");
                    _logger.LogInformation("✓ Second review submitted successfully.");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogInformation("✗ Rate limiting or validation prevented submission: {Message}", ex.Message);
                }
            }

            // Step 8: Test duplicate review prevention
            _logger.LogInformation("\n--- Testing Duplicate Review Prevention ---");
            try
            {
                await _reviewService.SubmitReviewAsync(
                    order.UserId!.Value,
                    orderItem.Id,
                    3,
                    "Trying to review again...");
                _logger.LogWarning("✗ UNEXPECTED: Duplicate review was allowed!");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInformation("✓ Duplicate review correctly prevented: {Message}", ex.Message);
            }

            _logger.LogInformation("\n=== Product Review Test Scenario Completed Successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during product review test scenario");
        }
    }

    /// <summary>
    /// Creates a test delivered order for testing purposes.
    /// </summary>
    private async Task CreateTestDeliveredOrderAsync()
    {
        _logger.LogInformation("Creating test delivered order...");

        // Find or create a buyer user
        var buyer = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == "buyer@example.com");

        if (buyer == null)
        {
            buyer = new User
            {
                Email = "buyer@example.com",
                FirstName = "Test",
                LastName = "Buyer",
                PasswordHash = "dummy_hash",
                UserType = UserType.Buyer,
                Status = AccountStatus.Active,
                AcceptedTerms = true
            };
            _context.Users.Add(buyer);
            await _context.SaveChangesAsync();
        }

        // Find a store and product
        var store = await _context.Stores.Include(s => s.Products).FirstOrDefaultAsync();
        if (store == null || !store.Products.Any())
        {
            _logger.LogWarning("No store or products found to create test order.");
            return;
        }

        var product = store.Products.First();

        // Create a test address
        var address = new Address
        {
            UserId = buyer.Id,
            FullName = $"{buyer.FirstName} {buyer.LastName}",
            AddressLine1 = "123 Test Street",
            City = "Test City",
            PostalCode = "12345",
            CountryCode = "US",
            PhoneNumber = "1234567890",
            IsDefault = true
        };
        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        // Create a delivered order
        var order = new Order
        {
            OrderNumber = $"ORD-TEST-{DateTime.UtcNow.Ticks}",
            UserId = buyer.Id,
            DeliveryAddressId = address.Id,
            Status = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            Subtotal = product.Price,
            ShippingCost = 5.00m,
            TotalAmount = product.Price + 5.00m,
            OrderedAt = DateTime.UtcNow.AddDays(-7)
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Create sub-order
        var subOrder = new SellerSubOrder
        {
            ParentOrderId = order.Id,
            StoreId = store.Id,
            SubOrderNumber = $"{order.OrderNumber}-1",
            Status = OrderStatus.Delivered,
            Subtotal = product.Price,
            ShippingCost = 5.00m,
            TotalAmount = product.Price + 5.00m,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };
        _context.SellerSubOrders.Add(subOrder);
        await _context.SaveChangesAsync();

        // Create order item
        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            SellerSubOrderId = subOrder.Id,
            StoreId = store.Id,
            ProductId = product.Id,
            ProductTitle = product.Title,
            Quantity = 1,
            UnitPrice = product.Price,
            Subtotal = product.Price,
            Status = OrderItemStatus.Shipped
        };
        _context.OrderItems.Add(orderItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✓ Test delivered order created: {OrderNumber}", order.OrderNumber);
    }
}
