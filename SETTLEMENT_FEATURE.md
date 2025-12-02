# Monthly Settlement Reports Implementation

## Summary
This implementation adds comprehensive monthly settlement reporting for sellers to the MercatoApp marketplace, fulfilling the requirements of Epic 7: Payments & Settlements.

## Features Implemented

### 1. Settlement Data Models
- **Settlement**: Main settlement entity with financial totals, status, and versioning
  - Tracks gross sales, refunds, commissions, adjustments, and net amounts
  - Supports versioning for audit history (Version, IsCurrentVersion, PreviousSettlementId)
  - Three status states: Draft, Finalized, Superseded
  
- **SettlementItem**: Individual order details within a settlement
  - Links to orders, sub-orders, and escrow transactions
  - Stores amounts at settlement generation time for historical accuracy
  
- **SettlementAdjustment**: Manual adjustments with prior period support
  - Multiple adjustment types: PriorPeriodAdjustment, Credit, Debit, Fee, RefundAdjustment, Other
  - Clearly marks prior period adjustments with flag and related settlement reference
  - Tracks which admin user created the adjustment
  
- **SettlementConfig**: Configuration for calendar rules
  - Generation day of month (1-28)
  - Auto-generation toggle
  - Grace period days (0-7)
  - Calendar vs rolling period mode

### 2. Settlement Service API

#### ISettlementService Interface
```csharp
// Generation
Task<SettlementResult> GenerateSettlementAsync(int storeId, DateTime periodStartDate, DateTime periodEndDate);
Task<int> GenerateMonthlySettlementsAsync(int year, int month);
Task<SettlementResult> RegenerateSettlementAsync(int settlementId);

// Querying
Task<Settlement?> GetSettlementAsync(int settlementId);
Task<List<Settlement>> GetSettlementsAsync(int storeId, bool includeSuperseded = false);
Task<SettlementSummary> GetSettlementSummaryAsync(int storeId, DateTime periodStartDate, DateTime periodEndDate);

// Modification
Task<bool> FinalizeSettlementAsync(int settlementId);
Task<SettlementAdjustment> AddAdjustmentAsync(int settlementId, SettlementAdjustmentType type, decimal amount, string description, int? relatedSettlementId = null, int? createdByUserId = null);

// Export
Task<byte[]> ExportSettlementToCsvAsync(int settlementId);

// Configuration
Task<SettlementConfig> GetOrCreateSettlementConfigAsync();
Task<SettlementConfig> UpdateSettlementConfigAsync(SettlementConfig config);
```

### 3. Admin Pages

#### Index (`/Admin/Settlements/Index`)
- Lists all settlements across all stores
- Filters by year and month
- Shows key metrics: gross sales, commission, net amount
- Displays settlement status and version
- Quick actions: view details, export CSV

#### Details (`/Admin/Settlements/Details`)
- Complete settlement summary
- Financial breakdown with totals
- List of all adjustments (with prior period flag)
- Drill-down into order details
- Actions: regenerate, add adjustment, finalize, export

#### Generate (`/Admin/Settlements/Generate`)
- Generate settlements for specific month (all stores)
- Generate custom period settlement for single store
- Validates against existing settlements

#### Export (`/Admin/Settlements/Export`)
- Exports settlement to CSV format
- Includes header information, summary, order details, and adjustments
- Download as file with settlement number in filename

#### Settings (`/Admin/Settlements/Settings`)
- Configure generation day of month
- Toggle auto-generation
- Set grace period days
- Choose calendar vs rolling periods

### 4. Key Workflows

#### Monthly Settlement Generation
1. Admin navigates to Generate page
2. Selects year and month
3. System generates settlements for all active stores
4. Each settlement includes all orders placed during that period
5. Settlements created in Draft status

#### Settlement Review and Finalization
1. Admin views settlement details
2. Reviews order breakdown and totals
3. Adds adjustments if needed (fees, credits, prior period corrections)
4. Finalizes settlement (prevents further changes)

#### Settlement Regeneration
1. Admin identifies settlement needing update
2. Clicks Regenerate button
3. System marks current settlement as Superseded
4. Creates new version with updated data
5. Copies adjustments from previous version
6. Maintains audit trail through version chain

#### Prior Period Adjustments
1. Admin finds error in previous month's settlement
2. Opens current month's settlement
3. Adds adjustment with PriorPeriodAdjustment type
4. References the related settlement
5. Adjustment is clearly marked in reports

## Database Schema

### Settlements Table
- Id (PK)
- SettlementNumber (Unique, format: STL-{StoreId:D6}-{YYYYMM}-{Random})
- StoreId (FK)
- PeriodStartDate, PeriodEndDate
- GrossSales, Refunds, Commission, Adjustments, NetAmount, TotalPayouts (all decimal(18,2))
- Currency (default: USD)
- Status (Draft/Finalized/Superseded)
- GeneratedAt, FinalizedAt
- Version, IsCurrentVersion, PreviousSettlementId (for audit history)
- Notes
- CreatedAt, UpdatedAt

### SettlementItems Table
- Id (PK)
- SettlementId (FK)
- OrderId (FK), SellerSubOrderId (FK), EscrowTransactionId (FK)
- OrderNumber, OrderDate
- GrossAmount, RefundAmount, CommissionAmount, NetAmount (all decimal(18,2))
- CreatedAt

### SettlementAdjustments Table
- Id (PK)
- SettlementId (FK)
- Type (enum)
- Amount (decimal(18,2))
- Description
- RelatedSettlementId (FK, nullable - for prior period adjustments)
- IsPriorPeriodAdjustment (bool)
- CreatedAt, CreatedByUserId (FK, nullable)

### SettlementConfigs Table
- Id (PK)
- GenerationDayOfMonth (1-28)
- AutoGenerateEnabled (bool)
- GracePeriodDays (0-7)
- UseCalendarMonth (bool)
- CreatedAt, UpdatedAt

## Integration Points

### Relationship with Payouts
- Settlements are **independent** from payouts
- Both reference the same escrow transactions
- Settlement's TotalPayouts field shows what was paid during the period
- Settlements are for accounting/reconciliation
- Payouts are for actual money transfers

### Relationship with Orders
- Settlements aggregate order data from escrow transactions
- Each SettlementItem links to an order and its escrow transaction
- Amounts are captured at settlement generation time for historical accuracy

### Relationship with Escrow
- Settlements read from EscrowTransactions but don't modify them
- Uses GrossAmount, CommissionAmount, RefundedAmount from escrow
- Links SettlementItem to EscrowTransaction for drill-down

## Configuration

Settings in `appsettings.json` (if needed):
```json
{
  "Settlement": {
    "DefaultGenerationDay": 1,
    "DefaultGracePeriodDays": 0
  }
}
```

All configuration is stored in database via SettlementConfig model and managed through Settings page.

## Background Job Recommendation

For production deployment, schedule a background job:

**Monthly on {GenerationDayOfMonth} at 2 AM**: `GenerateMonthlySettlementsAsync(year, month)`
- Generates settlements for previous month for all active stores

Recommended implementation: Hangfire, Quartz.NET, or Azure Functions

## CSV Export Format

```
Settlement Report
Settlement Number,{number}
Store,{storeName}
Period,{startDate} to {endDate}
Generated,{timestamp}
Version,{version}
Status,{status}

Summary
Gross Sales,{amount}
Refunds,{amount}
Commission,{amount}
Adjustments,{amount}
Net Amount,{amount}
Total Payouts,{amount}

Order Details
Order Number,Order Date,Gross Amount,Refund Amount,Commission Amount,Net Amount
{order data...}

Adjustments
Type,Amount,Description,Prior Period
{adjustment data...}
```

## Acceptance Criteria Verification

| Criterion | Status | Implementation |
|-----------|--------|----------------|
| System generates settlement per seller at month end | ✅ | `GenerateMonthlySettlementsAsync()` |
| Admin can view totals and drill into orders | ✅ | Details page with order breakdown |
| Adjustments for previous months marked clearly | ✅ | `IsPriorPeriodAdjustment` flag with UI badge |
| Settlements can be exported or downloaded | ✅ | CSV export functionality |
| Calendar rules must be configurable | ✅ | Settings page with all configuration options |
| Settlements align with payout history but remain independent | ✅ | TotalPayouts field, separate from payout processing |
| Regeneration should keep audit history | ✅ | Versioning system with PreviousSettlementId |

## Security Considerations

- All settlement pages require AdminOnly authorization
- Financial amounts use decimal(18,2) precision
- All operations logged for audit trail
- Settlement numbers include random component to prevent enumeration
- Finalized settlements cannot be modified (immutable)
- CodeQL security scan: 0 vulnerabilities

## Future Enhancements

Potential improvements for future iterations:
1. Email notifications when settlements are finalized
2. Automated reconciliation with accounting systems
3. Multi-currency settlement support
4. Seller-facing settlement view (read-only)
5. PDF export in addition to CSV
6. Settlement approval workflow
7. Batch finalization of multiple settlements
8. Settlement comparison reports (month-over-month)

## Testing Recommendations

Manual testing should cover:
- ✅ Generate settlement for single month
- ✅ Generate settlement for custom period
- ✅ Regenerate existing settlement
- ✅ Add various adjustment types
- ✅ Finalize settlement and verify immutability
- ✅ Export to CSV and verify format
- ✅ View settlement with prior period adjustments
- ✅ Filter settlements by year/month
- ✅ Verify version chain navigation

## Code Quality

- Full XML documentation on all public APIs
- Comprehensive error handling
- Follows ASP.NET Core conventions
- Dependency injection for all services
- Configuration-driven behavior
- Entity Framework relationships properly configured
- ICollection<T> used for navigation properties (EF best practice)
