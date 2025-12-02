# Seller Reputation Score - Implementation Summary

## Overview
This implementation adds a comprehensive seller reputation scoring system to the MercatoApp marketplace. The reputation score is an aggregated metric (0-100) that reflects seller performance across multiple dimensions, helping buyers identify trusted sellers.

## Epic
**Reviews, Ratings & Reputation (Phase 2)**

## User Story
As a system I want to calculate an aggregated reputation score so that the marketplace can highlight trusted sellers.

## Acceptance Criteria
✅ **Given a seller has activity history, when the system runs reputation calculation, then it updates the seller's reputation score based on defined formula.**
- Reputation score is calculated using a weighted formula
- Score ranges from 0-100
- Requires minimum 5 completed orders for calculation
- Score is stored in the database with timestamp

✅ **Given the updated score, when displayed, then buyers see simplified reputation metrics.**
- Reputation score displayed on Store page
- Visual indicators (badges) for high-reputation sellers
- Progress bar visualization in Store Info sidebar
- Explanation of score components

✅ **Reputation formula includes: seller ratings, dispute rate, on-time shipping rate, cancelled orders.**
- Seller ratings: 40% weight
- On-time shipping rate: 30% weight
- Dispute rate: 20% weight (inverted)
- Cancellation rate: 10% weight (inverted)

✅ **Requires periodic recalculation (batch or event-driven).**
- `RecalculateAllReputationScoresAsync()` method for batch processing
- Can be called from scheduled jobs or admin actions
- Event-driven updates possible via individual `CalculateReputationScoreAsync()`

## Implementation Details

### 1. Data Model Changes

**File**: `Models/Store.cs`

Added two new fields to the `Store` model:
```csharp
/// <summary>
/// Gets or sets the aggregated reputation score for this seller (0-100).
/// </summary>
[Range(0, 100)]
public decimal? ReputationScore { get; set; }

/// <summary>
/// Gets or sets the date and time when the reputation score was last calculated.
/// </summary>
public DateTime? ReputationScoreUpdatedAt { get; set; }
```

### 2. Service Layer

**Files**:
- `Services/ISellerReputationService.cs` (interface)
- `Services/SellerReputationService.cs` (implementation)

**Key Methods**:

#### CalculateReputationScoreAsync(int storeId)
Calculates and updates the reputation score for a specific seller based on the weighted formula.

**Returns**: `decimal?` - The calculated reputation score (0-100), or `null` if insufficient data.

**Requirements**:
- Minimum 5 completed orders required
- Score is automatically saved to the database

#### RecalculateAllReputationScoresAsync()
Batch recalculates reputation scores for all active sellers in the marketplace.

**Returns**: `int` - The number of stores updated.

**Use Cases**:
- Scheduled background job (daily/weekly)
- Admin-triggered bulk update
- Post-deployment data migration

#### GetReputationMetricsAsync(int storeId)
Retrieves detailed metrics breakdown for a seller's reputation calculation.

**Returns**: `SellerReputationMetrics` object containing:
- Average rating and rating count
- Order statistics (delivered, shipped, cancelled, disputed)
- Calculated rates (on-time shipping, dispute, cancellation)
- Current reputation score

### 3. Reputation Calculation Formula

The reputation score is calculated using a weighted formula with four components:

```
Reputation Score = (Rating Component × 40%) 
                 + (On-Time Shipping Component × 30%)
                 + (Dispute Component × 20%)
                 + (Cancellation Component × 10%)
```

#### Component Details:

**1. Rating Component (40% weight)**
```csharp
ratingScore = ((averageRating - 1) / 4) * 100
ratingComponent = ratingScore * 0.40
```
- Converts 1-5 star rating to 0-100 scale
- Example: 4.0 stars → 75% → 30 points

**2. On-Time Shipping Component (30% weight)**
```csharp
onTimeRate = (totalDeliveredOrders / totalShippedOrders) * 100
onTimeComponent = onTimeRate * 0.30
```
- Measures delivery success rate
- Example: 7 delivered / 9 shipped → 77.78% → 23.33 points

**3. Dispute Component (20% weight, inverted)**
```csharp
disputeRate = (totalDisputedOrders / totalCompletedOrders) * 100
disputeScore = 100 - disputeRate
disputeComponent = disputeScore * 0.20
```
- Lower dispute rate is better
- Example: 1 dispute / 7 completed → 14.29% → 85.71% score → 17.14 points

**4. Cancellation Component (10% weight, inverted)**
```csharp
cancellationRate = (totalCancelledOrders / totalOrders) * 100
cancellationScore = 100 - cancellationRate
cancellationComponent = cancellationScore * 0.10
```
- Lower cancellation rate is better
- Example: 1 cancelled / 8 total → 12.50% → 87.50% score → 8.75 points

**Example Calculation**:
- Rating: 4.0 stars → 30.00 points
- On-Time: 77.78% → 23.33 points
- Low Disputes: 85.71% → 17.14 points
- Low Cancellations: 87.50% → 8.75 points
- **Total: 79.23/100**

### 4. User Interface

#### Store Page Enhancement
**Files**: 
- `Pages/Store.cshtml`
- `Pages/Store.cshtml.cs`

**Changes**:
1. Added reputation score display with visual badges:
   - Score ≥ 90: "Trusted Seller" badge (green)
   - Score ≥ 75: "Verified Seller" badge (blue)
   
2. Added progress bar visualization in Store Info sidebar:
   - Color-coded by score range
   - Shows score breakdown explanation
   - Displays last update timestamp

### 5. Testing

**Files**:
- `SellerReputationTestScenario.cs` - Basic test scenario
- `SellerReputationComprehensiveTest.cs` - Comprehensive test with sample data

**Comprehensive Test Results**:
```
Created test store with:
  - 7 delivered orders with ratings (avg 4.0 stars)
  - 2 shipped orders (not yet delivered)
  - 1 cancelled order
  - 1 disputed order (return request)

Calculated Score: 79.23/100
  ✓ Rating (40%): 30.00 points
  ✓ On-Time Shipping (30%): 23.33 points
  ✓ Low Dispute Rate (20%): 17.14 points
  ✓ Low Cancellation Rate (10%): 8.75 points
  ✓ Calculation verified correctly!
```

## Configuration

### Minimum Orders Threshold
```csharp
private const int MINIMUM_ORDERS_FOR_REPUTATION = 5;
```
- Prevents scores from being calculated with insufficient data
- Can be adjusted based on marketplace needs

### Weight Distribution
```csharp
private const decimal RATING_WEIGHT = 40m;           // 40%
private const decimal ON_TIME_WEIGHT = 30m;          // 30%
private const decimal DISPUTE_WEIGHT = 20m;          // 20%
private const decimal CANCELLATION_WEIGHT = 10m;     // 10%
```
- Weights are tunable based on marketplace priorities
- Total must equal 100

## Future Enhancements

### Periodic Recalculation
The service is ready for integration with:
1. **Background Jobs**: Use Hangfire, Quartz.NET, or similar
   ```csharp
   RecurringJob.AddOrUpdate(
       "recalculate-reputation-scores",
       () => reputationService.RecalculateAllReputationScoresAsync(),
       Cron.Daily
   );
   ```

2. **Admin Panel**: Add UI for manual batch recalculation

3. **Event-Driven**: Trigger updates after:
   - Order delivery
   - Seller rating submission
   - Return request creation
   - Order cancellation

### Additional Metrics
Future versions could include:
- Response time to customer inquiries
- Product quality (average product ratings)
- Shipping speed (actual vs promised)
- Refund frequency

## Security Considerations

1. **Data Integrity**: Reputation scores are calculated from verified data sources only
2. **Audit Trail**: Last update timestamp tracked for transparency
3. **Input Validation**: All calculations use validated database data
4. **No User Manipulation**: Scores cannot be manually set by sellers or buyers

## Performance Considerations

1. **Database Efficiency**: 
   - Uses database aggregation (not in-memory)
   - Indexed queries on StoreId, Status
   
2. **Batch Processing**:
   - Batch recalculation processes stores individually
   - Continues on errors (logs and moves to next store)
   
3. **Caching Opportunity**:
   - Scores are persisted in database
   - No need to recalculate on every page view
   - Recommended recalculation frequency: Daily or weekly

## Dependencies

- ASP.NET Core 10
- Entity Framework Core (In-Memory Database for development)
- Existing models: Store, SellerSubOrder, SellerRating, ReturnRequest
- Existing services: None (self-contained)

## Breaking Changes

None. This is a new feature that adds to existing functionality without modifying existing behavior.

## Database Migration Notes

For production deployment with SQL database:
1. Add migration for new Store columns:
   ```bash
   dotnet ef migrations add AddReputationScoreToStore
   ```
2. Update database:
   ```bash
   dotnet ef database update
   ```
3. Run initial reputation calculation:
   ```csharp
   await reputationService.RecalculateAllReputationScoresAsync();
   ```
