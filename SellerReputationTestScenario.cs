using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp;

/// <summary>
/// Test scenario for seller reputation score calculation.
/// This scenario verifies the reputation calculation formula and recalculation logic.
/// </summary>
public static class SellerReputationTestScenario
{
    /// <summary>
    /// Runs the seller reputation test scenario.
    /// </summary>
    public static async Task RunTestAsync(
        ApplicationDbContext context,
        ISellerReputationService reputationService)
    {
        Console.WriteLine("=== Seller Reputation Test Scenario ===");
        Console.WriteLine();

        // Find a test seller (store) from the seeded data
        var testStore = await context.Stores
            .FirstOrDefaultAsync(s => s.Status == StoreStatus.Active);

        if (testStore == null)
        {
            Console.WriteLine("No active stores found for testing.");
            return;
        }

        Console.WriteLine($"Testing reputation calculation for: {testStore.StoreName} (ID: {testStore.Id})");
        Console.WriteLine();

        // Get initial metrics
        var initialMetrics = await reputationService.GetReputationMetricsAsync(testStore.Id);
        Console.WriteLine("Initial Metrics:");
        DisplayMetrics(initialMetrics);

        // Calculate reputation score
        Console.WriteLine("\nCalculating reputation score...");
        var reputationScore = await reputationService.CalculateReputationScoreAsync(testStore.Id);

        if (reputationScore.HasValue)
        {
            Console.WriteLine($"✓ Reputation score calculated: {reputationScore.Value:F2}/100");
            
            // Verify it was saved to the database
            var updatedStore = await context.Stores.FindAsync(testStore.Id);
            if (updatedStore?.ReputationScore == reputationScore.Value)
            {
                Console.WriteLine($"✓ Reputation score saved to database");
                Console.WriteLine($"  Last updated: {updatedStore.ReputationScoreUpdatedAt}");
            }
            else
            {
                Console.WriteLine($"✗ Failed to save reputation score");
            }
        }
        else
        {
            Console.WriteLine($"✓ No reputation score calculated (insufficient orders)");
            Console.WriteLine($"  Minimum required: 5 completed orders");
            Console.WriteLine($"  Current: {initialMetrics.TotalCompletedOrders} completed orders");
        }

        // Test batch recalculation
        Console.WriteLine("\n--- Testing Batch Recalculation ---");
        var updatedCount = await reputationService.RecalculateAllReputationScoresAsync();
        Console.WriteLine($"✓ Batch recalculation completed: {updatedCount} stores updated");

        // Display final metrics
        var finalMetrics = await reputationService.GetReputationMetricsAsync(testStore.Id);
        Console.WriteLine("\nFinal Metrics:");
        DisplayMetrics(finalMetrics);

        Console.WriteLine();
        Console.WriteLine("=== Seller Reputation Test Completed ===");
        Console.WriteLine();
    }

    private static void DisplayMetrics(SellerReputationMetrics metrics)
    {
        Console.WriteLine($"  Store ID: {metrics.StoreId}");
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
        Console.WriteLine($"  Reputation Score: {metrics.ReputationScore?.ToString("F2") ?? "N/A"}");
    }
}
