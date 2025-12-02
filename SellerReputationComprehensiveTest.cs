using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Comprehensive test scenario for seller reputation score calculation with sample data.
/// This scenario creates test data and verifies the reputation calculation formula.
/// </summary>
public static class SellerReputationComprehensiveTest
{
    /// <summary>
    /// Runs the comprehensive seller reputation test scenario.
    /// </summary>
    public static async Task RunTestAsync(
        ApplicationDbContext context,
        ISellerReputationService reputationService)
    {
        Console.WriteLine("=== Comprehensive Seller Reputation Test ===");
        Console.WriteLine();

        // Create a test store
        var testUser = new User
        {
            Email = "reputation.test@example.com",
            FirstName = "Reputation",
            LastName = "Test",
            PasswordHash = "dummy_hash",
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        };
        context.Users.Add(testUser);
        await context.SaveChangesAsync();

        var testStore = new Store
        {
            UserId = testUser.Id,
            StoreName = "Reputation Test Store",
            Slug = "reputation-test-store",
            Description = "Test store for reputation calculation",
            Status = StoreStatus.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        };
        context.Stores.Add(testStore);
        await context.SaveChangesAsync();

        Console.WriteLine($"Created test store: {testStore.StoreName} (ID: {testStore.Id})");
        Console.WriteLine();

        // Create a buyer for ratings
        var testBuyer = new User
        {
            Email = "reputation.buyer@example.com",
            FirstName = "Test",
            LastName = "Buyer",
            PasswordHash = "dummy_hash",
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };
        context.Users.Add(testBuyer);
        await context.SaveChangesAsync();

        // Create test address for orders
        var testAddress = new Address
        {
            UserId = testBuyer.Id,
            FullName = "Test Buyer",
            AddressLine1 = "123 Test St",
            City = "Test City",
            StateProvince = "TC",
            PostalCode = "12345",
            CountryCode = "US",
            PhoneNumber = "1234567890",
            IsDefault = true
        };
        context.Addresses.Add(testAddress);
        await context.SaveChangesAsync();

        // Create test orders with various statuses
        var orders = new List<Order>();
        var subOrders = new List<SellerSubOrder>();

        // Scenario: 10 orders total
        // - 7 delivered (70% on-time rate)
        // - 2 shipped (not yet delivered)
        // - 1 cancelled
        // - 1 disputed (return request)

        for (int i = 1; i <= 10; i++)
        {
            var order = new Order
            {
                OrderNumber = $"TEST-{DateTime.UtcNow:yyyyMMdd}-{1000 + i}",
                UserId = testBuyer.Id,
                DeliveryAddressId = testAddress.Id,
                Status = OrderStatus.Delivered,
                Subtotal = 100m,
                ShippingCost = 10m,
                TotalAmount = 110m,
                OrderedAt = DateTime.UtcNow.AddDays(-30 + i)
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            OrderStatus subOrderStatus;
            if (i <= 7)
            {
                subOrderStatus = OrderStatus.Delivered; // 7 delivered
            }
            else if (i <= 9)
            {
                subOrderStatus = OrderStatus.Shipped; // 2 shipped
            }
            else
            {
                subOrderStatus = OrderStatus.Cancelled; // 1 cancelled
            }

            var subOrder = new SellerSubOrder
            {
                ParentOrderId = order.Id,
                StoreId = testStore.Id,
                SubOrderNumber = $"{order.OrderNumber}-1",
                Status = subOrderStatus,
                Subtotal = 100m,
                ShippingCost = 10m,
                TotalAmount = 110m,
                CreatedAt = order.OrderedAt
            };
            context.SellerSubOrders.Add(subOrder);
            await context.SaveChangesAsync();

            subOrders.Add(subOrder);
        }

        Console.WriteLine($"Created 10 test orders:");
        Console.WriteLine($"  - 7 delivered");
        Console.WriteLine($"  - 2 shipped (not yet delivered)");
        Console.WriteLine($"  - 1 cancelled");
        Console.WriteLine();

        // Add seller ratings (for the 7 delivered orders)
        // Average rating will be 4.0 (mix of 3, 4, and 5 stars)
        var ratingValues = new[] { 5, 5, 4, 4, 4, 3, 3 }; // Average = 4.0
        for (int i = 0; i < 7; i++)
        {
            var rating = new SellerRating
            {
                StoreId = testStore.Id,
                UserId = testBuyer.Id,
                SellerSubOrderId = subOrders[i].Id,
                Rating = ratingValues[i],
                CreatedAt = DateTime.UtcNow.AddDays(-25 + i)
            };
            context.SellerRatings.Add(rating);
        }
        await context.SaveChangesAsync();

        Console.WriteLine($"Added 7 seller ratings with average: {ratingValues.Average():F1} stars");
        Console.WriteLine();

        // Add one return request (dispute) for the first delivered order
        var returnRequest = new ReturnRequest
        {
            ReturnNumber = $"RTN-{DateTime.UtcNow:yyyyMMdd}-1001",
            SubOrderId = subOrders[0].Id,
            BuyerId = testBuyer.Id,
            RequestType = ReturnRequestType.Return,
            Reason = ReturnReason.Damaged,
            Status = ReturnStatus.Requested,
            Description = "Test return for reputation calculation",
            RequestedAt = DateTime.UtcNow.AddDays(-10)
        };
        context.ReturnRequests.Add(returnRequest);
        await context.SaveChangesAsync();

        Console.WriteLine($"Added 1 return request (dispute)");
        Console.WriteLine();

        // Calculate reputation score
        Console.WriteLine("--- Calculating Reputation Score ---");
        var metrics = await reputationService.GetReputationMetricsAsync(testStore.Id);
        
        Console.WriteLine("Metrics:");
        Console.WriteLine($"  Average Rating: {metrics.AverageRating?.ToString("F2") ?? "N/A"}");
        Console.WriteLine($"  Rating Count: {metrics.RatingCount}");
        Console.WriteLine($"  Total Completed Orders: {metrics.TotalCompletedOrders}");
        Console.WriteLine($"  Total Delivered Orders: {metrics.TotalDeliveredOrders}");
        Console.WriteLine($"  Total Shipped Orders: {metrics.TotalShippedOrders}");
        Console.WriteLine($"  Total Cancelled Orders: {metrics.TotalCancelledOrders}");
        Console.WriteLine($"  Total Disputed Orders: {metrics.TotalDisputedOrders}");
        Console.WriteLine($"  On-Time Shipping Rate: {metrics.OnTimeShippingRate:F2}%");
        Console.WriteLine($"  Dispute Rate: {metrics.DisputeRate:F2}%");
        Console.WriteLine($"  Cancellation Rate: {metrics.CancellationRate:F2}%");
        Console.WriteLine();

        var reputationScore = await reputationService.CalculateReputationScoreAsync(testStore.Id);
        
        if (reputationScore.HasValue)
        {
            Console.WriteLine($"✓ Reputation Score Calculated: {reputationScore.Value:F2}/100");
            Console.WriteLine();
            Console.WriteLine("Score Breakdown (expected calculation):");
            
            // Rating component: 40% weight
            // 4.0 stars = (4-1)/4 * 100 = 75% * 40% = 30 points
            var ratingComponent = ((metrics.AverageRating!.Value - 1) / 4m) * 100m * 0.40m;
            Console.WriteLine($"  Rating (40% weight): {ratingComponent:F2} points");
            Console.WriteLine($"    {metrics.AverageRating.Value:F1} stars → {((metrics.AverageRating.Value - 1) / 4m) * 100m:F2}% → {ratingComponent:F2} points");
            
            // On-time shipping: 30% weight
            // 77.78% (7/9 shipped delivered) * 30% = 23.33 points
            var onTimeComponent = metrics.OnTimeShippingRate * 0.30m;
            Console.WriteLine($"  On-Time Shipping (30% weight): {onTimeComponent:F2} points");
            Console.WriteLine($"    {metrics.OnTimeShippingRate:F2}% → {onTimeComponent:F2} points");
            
            // Dispute rate: 20% weight (inverted - lower is better)
            // 14.29% dispute rate → 100-14.29 = 85.71% * 20% = 17.14 points
            var disputeComponent = (100m - metrics.DisputeRate) * 0.20m;
            Console.WriteLine($"  Low Dispute Rate (20% weight): {disputeComponent:F2} points");
            Console.WriteLine($"    {metrics.DisputeRate:F2}% dispute → {100m - metrics.DisputeRate:F2}% score → {disputeComponent:F2} points");
            
            // Cancellation rate: 10% weight (inverted - lower is better)
            // 10% cancellation rate → 100-10 = 90% * 10% = 9.00 points
            var cancellationComponent = (100m - metrics.CancellationRate) * 0.10m;
            Console.WriteLine($"  Low Cancellation Rate (10% weight): {cancellationComponent:F2} points");
            Console.WriteLine($"    {metrics.CancellationRate:F2}% cancelled → {100m - metrics.CancellationRate:F2}% score → {cancellationComponent:F2} points");
            
            var expectedTotal = ratingComponent + onTimeComponent + disputeComponent + cancellationComponent;
            Console.WriteLine();
            Console.WriteLine($"Expected Total: {expectedTotal:F2} points");
            Console.WriteLine($"Actual Score: {reputationScore.Value:F2} points");
            
            if (Math.Abs(expectedTotal - reputationScore.Value) < 0.01m)
            {
                Console.WriteLine("✓ Calculation verified correctly!");
            }
            else
            {
                Console.WriteLine("✗ Calculation mismatch - review formula");
            }
        }
        else
        {
            Console.WriteLine("✗ No reputation score calculated");
        }

        Console.WriteLine();
        Console.WriteLine("=== Comprehensive Test Completed ===");
        Console.WriteLine();
    }
}
