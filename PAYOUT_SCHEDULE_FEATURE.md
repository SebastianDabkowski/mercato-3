# Seller Payout Schedule Implementation

## Summary
This implementation adds a comprehensive seller payout schedule system to the MercatoApp marketplace, fulfilling the requirements of Epic 7: Payments & Settlements.

## Features Implemented

### 1. Payout Scheduling
- **PayoutSchedule** model for store-specific payout configuration
- Configurable payout frequency:
  - Weekly (every 7 days)
  - Bi-weekly (every 14 days)
  - Monthly (specific day of month)
- Minimum payout threshold to prevent small payouts
- Next payout date calculation

### 2. Payout Status Tracking
Sellers can see four payout statuses:
- **Scheduled**: Payout is scheduled for future processing
- **Processing**: Payout is currently being processed
- **Paid**: Payout has been successfully completed
- **Failed**: Payout failed and may require attention

### 3. Failed Payout Handling
- Error message storage for troubleshooting
- Error reference code from payment provider
- Retry count tracking
- Next retry date scheduling
- Configurable maximum retry attempts (default: 3)
- Exponential backoff for retry delays

### 4. Below-Threshold Rollover
- Balances below the minimum threshold are NOT paid out
- Eligible escrow transactions remain available for the next payout cycle
- Funds automatically roll over until threshold is met

### 5. Batch Processing
Three background job methods for automated payout management:
- **GenerateScheduledPayoutsAsync**: Creates payouts for all due schedules
- **ProcessScheduledPayoutsAsync**: Processes all payouts that are due
- **RetryFailedPayoutsAsync**: Retries failed payouts that are eligible

## API Overview

### IPayoutService Interface

#### Payout Schedule Management
```csharp
Task<PayoutSchedule> CreateOrUpdatePayoutScheduleAsync(
    int storeId,
    PayoutFrequency frequency,
    decimal minimumThreshold,
    int? dayOfWeek = null,
    int? dayOfMonth = null);

Task<PayoutSchedule?> GetPayoutScheduleAsync(int storeId);
```

#### Balance Queries
```csharp
Task<PayoutBalanceSummary> GetEligibleBalanceSummaryAsync(int storeId);
```

#### Payout Operations
```csharp
Task<PayoutResult> CreatePayoutAsync(int storeId, DateTime scheduledDate);
Task<PayoutResult> ProcessPayoutAsync(int payoutId);
Task<List<Payout>> GetPayoutsAsync(int storeId, PayoutStatus? status = null);
Task<Payout?> GetPayoutAsync(int payoutId);
```

#### Background Job Methods
```csharp
Task<int> GenerateScheduledPayoutsAsync();
Task<int> ProcessScheduledPayoutsAsync();
Task<int> RetryFailedPayoutsAsync();
```

## Configuration

Settings are in `appsettings.json` under the `Payout` section:

```json
{
  "Payout": {
    "DefaultMinimumThreshold": 50.00,
    "DefaultFrequency": "Weekly",
    "DefaultDayOfWeek": 1,
    "MaxRetryAttempts": 3,
    "RetryDelayHours": 24,
    "ProcessingEnabled": true
  }
}
```

## Database Schema

### PayoutSchedule Table
- Id (PK)
- StoreId (FK, Unique) - One schedule per store
- Frequency (enum)
- MinimumPayoutThreshold
- DayOfWeek (nullable)
- DayOfMonth (nullable)
- NextPayoutDate
- IsEnabled
- CreatedAt, UpdatedAt

### Payout Table
- Id (PK)
- PayoutNumber (Unique)
- StoreId (FK)
- PayoutMethodId (FK, nullable)
- PayoutScheduleId (FK, nullable)
- Amount
- Currency
- Status (enum)
- ScheduledDate
- InitiatedAt, CompletedAt, FailedAt (nullable)
- ErrorMessage, ErrorReference (nullable)
- RetryCount, MaxRetryAttempts
- NextRetryDate (nullable)
- ExternalTransactionId (nullable)
- Notes (nullable)
- CreatedAt, UpdatedAt

### EscrowTransaction Update
- Added PayoutId (FK, nullable) to link escrow transactions to payouts

## Integration Points

### Existing Services
- **IPayoutSettingsService**: Used to verify payout method configuration
- **EscrowTransaction**: Source of eligible balances for payouts

### Payment Provider Integration
The implementation includes a simulated payment provider interface. In production, this would integrate with:
- Stripe Connect
- PayPal Payouts
- Banking APIs (ACH, SEPA, Wire transfers)

## Background Job Recommendation

For production deployment, schedule these background jobs:

1. **Daily at 2 AM**: `GenerateScheduledPayoutsAsync()`
   - Creates payouts for schedules that are due
   
2. **Daily at 3 AM**: `ProcessScheduledPayoutsAsync()`
   - Processes all scheduled payouts
   
3. **Every 6 hours**: `RetryFailedPayoutsAsync()`
   - Retries failed payouts

Recommended implementation: Hangfire, Quartz.NET, or Azure Functions

## Testing

Manual test suite included in `PayoutServiceManualTest.cs` covering:
- ✅ Payout schedule creation
- ✅ Eligible balance aggregation
- ✅ Payout creation with threshold validation
- ✅ Payout processing simulation
- ✅ Failed payout retry logic
- ✅ Escrow transaction linkage

All tests pass successfully with 0 security vulnerabilities (verified by CodeQL).

## Acceptance Criteria Verification

| Criterion | Status | Implementation |
|-----------|--------|----------------|
| Eligible balances are aggregated by schedule | ✅ | `GetEligibleBalanceSummaryAsync()` |
| Sellers see payout status: scheduled, processing, paid, failed | ✅ | `PayoutStatus` enum with all 4 states |
| Failed payouts store error reference | ✅ | `ErrorMessage` and `ErrorReference` fields |
| Below-threshold balances roll over | ✅ | Threshold check in `CreatePayoutAsync()` |
| Support at least weekly payouts | ✅ | Weekly, Bi-weekly, and Monthly supported |
| Retry logic required | ✅ | Configurable retry with exponential backoff |
| Payout batching recommended | ✅ | Three batch processing methods implemented |

## Future Enhancements

Potential improvements for future iterations:
1. Add seller-facing UI pages for payout schedule management
2. Integrate with real payment providers
3. Add email notifications for payout status changes
4. Implement payout transaction history export
5. Add analytics dashboard for payout trends
6. Support multiple currencies per payout method
7. Add payout fee calculations
8. Implement instant payout options (with fees)

## Security Considerations

- All financial operations logged for audit trail
- Error messages sanitized to prevent sensitive data exposure
- Payout methods verified before processing
- Integration with existing PayoutMethod verification system
- Configuration-driven retry limits prevent infinite loops
- CodeQL security scan: 0 vulnerabilities

## Code Quality

- Full XML documentation on all public APIs
- Comprehensive error handling
- Follows ASP.NET Core conventions
- Dependency injection for all services
- Configuration-driven behavior
- Testable architecture with interface abstraction
