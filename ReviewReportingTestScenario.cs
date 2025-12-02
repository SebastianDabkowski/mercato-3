using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Manual test scenario for review reporting functionality.
/// This file demonstrates how the review reporting system works and can be used for testing.
/// </summary>
public class ReviewReportingTestScenario
{
    private readonly ApplicationDbContext _context;
    private readonly IReviewModerationService _moderationService;
    private readonly IProductReviewService _reviewService;
    private readonly ILogger<ReviewReportingTestScenario> _logger;

    public ReviewReportingTestScenario(
        ApplicationDbContext context,
        IReviewModerationService moderationService,
        IProductReviewService reviewService,
        ILogger<ReviewReportingTestScenario> logger)
    {
        _context = context;
        _moderationService = moderationService;
        _reviewService = reviewService;
        _logger = logger;
    }

    /// <summary>
    /// Runs a test scenario for reporting reviews.
    /// </summary>
    public async Task RunReviewReportingTestAsync()
    {
        _logger.LogInformation("=== Starting Review Reporting Test Scenario ===");

        try
        {
            // Step 1: Find or create a review to test with
            var review = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.IsApproved);

            if (review == null)
            {
                _logger.LogWarning("No approved reviews found. Creating test review...");
                review = await CreateTestReviewAsync();
                
                if (review == null)
                {
                    _logger.LogError("Failed to create test review.");
                    return;
                }
            }

            _logger.LogInformation("Testing with review ID {ReviewId} for product '{ProductTitle}'",
                review.Id, review.Product.Title);

            // Step 2: Find or create a buyer user to report the review
            var reporter = await _context.Users
                .FirstOrDefaultAsync(u => u.UserType == UserType.Buyer && u.Id != review.UserId);

            if (reporter == null)
            {
                _logger.LogWarning("No buyer user found to act as reporter. Creating test user...");
                reporter = await CreateTestBuyerAsync();
            }

            _logger.LogInformation("Reporter: User ID {UserId} ({FirstName} {LastName})",
                reporter.Id, reporter.FirstName, reporter.LastName);

            // Step 3: Test reporting with valid reason (Abuse)
            _logger.LogInformation("\n--- Testing Report Submission (Abuse) ---");
            try
            {
                var flag = await _moderationService.FlagReviewAsync(
                    review.Id,
                    ReviewFlagReason.Abuse,
                    "This review contains offensive language.",
                    reporter.Id,
                    isAutomated: false
                );

                _logger.LogInformation("✓ Review reported successfully! Flag ID: {FlagId}", flag.Id);
                _logger.LogInformation("  Reason: {Reason}", flag.Reason);
                _logger.LogInformation("  Details: {Details}", flag.Details);
                _logger.LogInformation("  Flagged By: User {UserId}", flag.FlaggedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Failed to report review");
                return;
            }

            // Step 4: Test duplicate report prevention (same user)
            _logger.LogInformation("\n--- Testing Duplicate Report Prevention (Same User) ---");
            try
            {
                var duplicateFlag = await _moderationService.FlagReviewAsync(
                    review.Id,
                    ReviewFlagReason.Spam, // Different reason
                    "Trying to report again with different reason.",
                    reporter.Id,
                    isAutomated: false
                );

                _logger.LogWarning("✗ UNEXPECTED: Duplicate report was allowed! Flag ID: {FlagId}", duplicateFlag.Id);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInformation("✓ Duplicate report correctly prevented: {Message}", ex.Message);
            }

            // Step 5: Test reporting with different user (should succeed)
            _logger.LogInformation("\n--- Testing Report from Different User ---");
            var secondReporter = await _context.Users
                .FirstOrDefaultAsync(u => u.UserType == UserType.Buyer && u.Id != review.UserId && u.Id != reporter.Id);

            if (secondReporter == null)
            {
                _logger.LogWarning("Creating second test buyer...");
                secondReporter = await CreateTestBuyerAsync("buyer2@example.com", "Second", "Reporter");
            }

            try
            {
                var secondFlag = await _moderationService.FlagReviewAsync(
                    review.Id,
                    ReviewFlagReason.FalseInformation,
                    "This review contains incorrect product information.",
                    secondReporter.Id,
                    isAutomated: false
                );

                _logger.LogInformation("✓ Second user reported successfully! Flag ID: {FlagId}", secondFlag.Id);
                _logger.LogInformation("  Reporter: User {UserId}", secondFlag.FlaggedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Failed to submit second report");
            }

            // Step 6: Test all available report reasons
            _logger.LogInformation("\n--- Testing All Report Reasons ---");
            var testReview = await CreateTestReviewAsync();
            if (testReview != null)
            {
                var reasons = new[]
                {
                    (ReviewFlagReason.Abuse, "Contains abusive content"),
                    (ReviewFlagReason.Spam, "This is spam"),
                    (ReviewFlagReason.FalseInformation, "Contains false information"),
                    (ReviewFlagReason.Other, "Other issue")
                };

                foreach (var (reason, details) in reasons)
                {
                    var testBuyer = await CreateTestBuyerAsync($"test_{reason}@example.com", "Test", $"{reason}Reporter");
                    try
                    {
                        var flag = await _moderationService.FlagReviewAsync(
                            testReview.Id,
                            reason,
                            details,
                            testBuyer.Id,
                            isAutomated: false
                        );
                        _logger.LogInformation("✓ Reported with reason {Reason}: Flag ID {FlagId}", reason, flag.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "✗ Failed to report with reason {Reason}", reason);
                    }
                }
            }

            // Step 7: Verify flags in database
            _logger.LogInformation("\n--- Verifying Stored Flags ---");
            var allFlags = await _moderationService.GetFlagsByReviewIdAsync(review.Id, includeResolved: false);
            _logger.LogInformation("Review {ReviewId} has {Count} active flag(s)", review.Id, allFlags.Count);
            
            foreach (var flag in allFlags)
            {
                _logger.LogInformation("  - Flag ID {FlagId}: Reason={Reason}, FlaggedBy={UserId}, IsAutomated={IsAutomated}",
                    flag.Id, flag.Reason, flag.FlaggedByUserId, flag.IsAutomated);
            }

            // Step 8: Test getting all flagged reviews
            _logger.LogInformation("\n--- Testing Get All Flagged Reviews ---");
            var flaggedReviews = await _moderationService.GetFlaggedReviewsAsync(includeResolved: false);
            _logger.LogInformation("Total flagged reviews in system: {Count}", flaggedReviews.Count);

            _logger.LogInformation("\n=== Review Reporting Test Scenario Completed Successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during review reporting test scenario");
        }
    }

    /// <summary>
    /// Creates a test review for testing purposes.
    /// </summary>
    private async Task<ProductReview?> CreateTestReviewAsync()
    {
        try
        {
            // Find a product
            var product = await _context.Products.FirstOrDefaultAsync();
            if (product == null)
            {
                _logger.LogWarning("No products found to create test review.");
                return null;
            }

            // Find a buyer
            var buyer = await _context.Users
                .FirstOrDefaultAsync(u => u.UserType == UserType.Buyer);
            
            if (buyer == null)
            {
                buyer = await CreateTestBuyerAsync();
            }

            // Create a delivered order item
            var orderItem = await CreateTestOrderItemAsync(buyer.Id, product.Id);
            if (orderItem == null)
            {
                return null;
            }

            // Create the review
            var review = await _reviewService.SubmitReviewAsync(
                buyer.Id,
                orderItem.Id,
                5,
                "This is a test review for reporting functionality testing."
            );

            // Approve it manually for testing - find or create an admin
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.UserType == UserType.Admin);
            if (admin == null)
            {
                admin = new User
                {
                    Email = "testadmin@example.com",
                    FirstName = "Test",
                    LastName = "Admin",
                    PasswordHash = "dummy_hash_admin",
                    UserType = UserType.Admin,
                    Status = AccountStatus.Active,
                    AcceptedTerms = true
                };
                _context.Users.Add(admin);
                await _context.SaveChangesAsync();
            }

            var approvedReview = await _moderationService.ApproveReviewAsync(
                review.Id,
                admin.Id,
                "Auto-approved for testing"
            );

            _logger.LogInformation("✓ Test review created and approved: Review ID {ReviewId}", approvedReview.Id);
            return approvedReview;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test review");
            return null;
        }
    }

    /// <summary>
    /// Creates a test buyer user.
    /// </summary>
    private async Task<User> CreateTestBuyerAsync(
        string email = "reporter@example.com",
        string firstName = "Test",
        string lastName = "Reporter")
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            return existingUser;
        }

        var buyer = new User
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = "dummy_hash",
            UserType = UserType.Buyer,
            Status = AccountStatus.Active,
            AcceptedTerms = true
        };
        _context.Users.Add(buyer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✓ Test buyer created: {Email}", email);
        return buyer;
    }

    /// <summary>
    /// Creates a test order item for review submission.
    /// </summary>
    private async Task<OrderItem?> CreateTestOrderItemAsync(int buyerId, int productId)
    {
        try
        {
            var product = await _context.Products
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.Store == null)
            {
                _logger.LogWarning("Product or store not found.");
                return null;
            }

            // Create address
            var address = new Address
            {
                UserId = buyerId,
                FullName = "Test User",
                AddressLine1 = "123 Test Street",
                City = "Test City",
                PostalCode = "12345",
                CountryCode = "US",
                PhoneNumber = "1234567890",
                IsDefault = true
            };
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            // Create order
            var order = new Order
            {
                OrderNumber = $"ORD-TEST-{Guid.NewGuid().ToString()[..8]}",
                UserId = buyerId,
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
                StoreId = product.StoreId,
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
                StoreId = product.StoreId,
                ProductId = product.Id,
                ProductTitle = product.Title,
                Quantity = 1,
                UnitPrice = product.Price,
                Subtotal = product.Price,
                Status = OrderItemStatus.Shipped
            };
            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync();

            return orderItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test order item");
            return null;
        }
    }
}
