# Commission Calculation Feature - Implementation Summary

## Overview
This implementation adds comprehensive commission calculation functionality to the MercatoApp marketplace platform, supporting automatic commission calculation at payment confirmation with category-specific and seller-specific override rules.

## Features Implemented

### 1. Commission Rule Hierarchy
The system supports a three-tier commission rule hierarchy with the following priority:

1. **Category-specific commission** (highest priority)
   - Applied when a product belongs to a category with commission overrides
   - Stored in `Category.CommissionPercentageOverride` and `Category.FixedCommissionAmountOverride`

2. **Seller-specific commission** (medium priority)
   - Applied when a seller has custom commission rates
   - Stored in `Store.CommissionPercentageOverride` and `Store.FixedCommissionAmountOverride`

3. **Global platform commission** (lowest priority/fallback)
   - Applied when no category or seller overrides exist
   - Configured via `CommissionConfig` table

### 2. Database Schema Changes

#### New Model: `CommissionTransaction`
- **Purpose**: Audit trail for all commission calculations and adjustments
- **Key Fields**:
  - `EscrowTransactionId`: Links to the escrow transaction
  - `StoreId`: The seller being charged commission
  - `CategoryId`: Optional category for category-specific commissions
  - `TransactionType`: "Initial" or "RefundAdjustment"
  - `CommissionAmount`: Calculated commission amount
  - `CommissionPercentage`: Percentage applied (stored for history)
  - `FixedCommissionAmount`: Fixed amount applied (stored for history)
  - `CommissionSource`: "Global", "Seller", or "Category"

#### Enhanced Models:
- **Store**: Added `CommissionPercentageOverride` and `FixedCommissionAmountOverride`
- **Category**: Added `CommissionPercentageOverride` and `FixedCommissionAmountOverride`
- **CommissionTransaction**: Added to `ApplicationDbContext` with proper indexes

### 3. Service Layer

#### New Service: `CommissionService`
Implements `ICommissionService` interface with the following methods:

- `CalculateCommissionAsync()`: Calculates commission based on hierarchy
- `RecordCommissionTransactionAsync()`: Creates audit trail records
- `GetCommissionTransactionsByStoreAsync()`: Retrieves commission history
- `GetCommissionTransactionsByEscrowAsync()`: Gets transactions for specific escrow
- `GetTotalCommissionAsync()`: Platform revenue reporting
- `RecalculateCommissionForRefundAsync()`: Handles partial refund adjustments

#### Enhanced Service: `EscrowService`
- Updated to use `ICommissionService` for all commission calculations
- `CreateEscrowAllocationsAsync()`: Now records commission transactions
- `ReturnEscrowToBuyerAsync()`: Recalculates commission on partial refunds

### 4. Commission Calculation Logic

#### Initial Commission Calculation
```csharp
// At payment confirmation:
1. Determine category from order items
2. Check for category-specific commission override
3. If none, check for seller-specific commission override
4. If none, use global commission configuration
5. Calculate: (grossAmount * percentage / 100) + fixedAmount
6. Store commission with escrow transaction
7. Record commission transaction for audit
```

#### Refund Adjustment
```csharp
// On partial refund:
1. Get original commission transaction
2. Calculate refund ratio: refundAmount / originalGrossAmount
3. Calculate commission adjustment: originalCommission * refundRatio
4. Record negative commission transaction
5. Update escrow net amount
```

### 5. Transactional Integrity
- All commission calculations are performed within database transactions
- Escrow creation and commission recording are atomic operations
- Partial refunds properly adjust commission amounts
- Historical values are preserved (immutable audit trail)

### 6. High Precision Storage
All monetary values use `decimal(18,2)` precision:
- `CommissionTransaction.GrossAmount`
- `CommissionTransaction.CommissionAmount`
- `Store.CommissionPercentageOverride`
- `Category.CommissionPercentageOverride`
- Percentage fields use `decimal(5,2)` (e.g., 10.50%)

## Acceptance Criteria Verification

✅ **Commission calculated at payment confirmation**
- Implemented in `EscrowService.CreateEscrowAllocationsAsync()`
- Triggered when payment status is Completed or Authorized

✅ **Different commission rules applied per category or seller**
- Three-tier hierarchy: Category > Seller > Global
- Implemented in `CommissionService.CalculateCommissionAsync()`

✅ **Partial refunds recalculate commission**
- Implemented in `CommissionService.RecalculateCommissionForRefundAsync()`
- Proportional adjustment based on refund ratio
- Negative commission transaction recorded

✅ **Historical orders keep original commission values**
- Commission details stored in `CommissionTransaction` table
- Immutable audit trail with transaction type and timestamp
- Original commission preserved even if config changes

✅ **Support global and per-seller overrides**
- Global: `CommissionConfig` table
- Per-seller: `Store.CommissionPercentageOverride` and `FixedCommissionAmountOverride`
- Per-category: `Category.CommissionPercentageOverride` and `FixedCommissionAmountOverride`

✅ **Store values with high precision**
- All amounts use `decimal(18,2)` precision
- Percentages use `decimal(5,2)` precision
- Proper rounding applied (2 decimal places)

✅ **Commission calculation must be transactional**
- All operations within database transaction scope
- Atomic escrow creation + commission recording
- Rollback on failure

## API Usage Examples

### Setting Seller-Specific Commission
```csharp
var store = await context.Stores.FindAsync(storeId);
store.CommissionPercentageOverride = 12.5m; // 12.5%
store.FixedCommissionAmountOverride = 0.99m; // $0.99 per transaction
await context.SaveChangesAsync();
```

### Setting Category-Specific Commission
```csharp
var category = await context.Categories.FindAsync(categoryId);
category.CommissionPercentageOverride = 15.0m; // 15%
category.FixedCommissionAmountOverride = 0.0m; // No fixed fee
await context.SaveChangesAsync();
```

### Retrieving Commission History
```csharp
var commissions = await commissionService.GetCommissionTransactionsByStoreAsync(
    storeId,
    fromDate: DateTime.UtcNow.AddMonths(-1),
    toDate: DateTime.UtcNow
);

var totalCommission = commissions.Sum(c => c.CommissionAmount);
```

### Getting Platform Revenue
```csharp
var monthlyRevenue = await commissionService.GetTotalCommissionAsync(
    fromDate: new DateTime(2024, 12, 1),
    toDate: new DateTime(2024, 12, 31)
);
```

## Database Indexes
Optimized queries with the following indexes:
- `CommissionTransactions(EscrowTransactionId)`
- `CommissionTransactions(StoreId)`
- `CommissionTransactions(CategoryId)`
- `CommissionTransactions(TransactionType)`
- `CommissionTransactions(StoreId, CreatedAt)` - For date-range queries

## Migration Notes
For existing deployments:
1. Database schema will be updated automatically (in-memory database)
2. Existing stores/categories have `NULL` for commission overrides
3. Existing escrow transactions have commission calculated with global config
4. New `CommissionTransactions` table will be created
5. No data migration needed - backward compatible

## Testing Recommendations

### Unit Tests
1. Test commission calculation hierarchy (category > seller > global)
2. Test partial refund commission adjustment
3. Test commission recording for audit trail
4. Test precision and rounding

### Integration Tests
1. Test end-to-end order flow with commission calculation
2. Test multiple sellers with different commission rates
3. Test category-based commission with multiple products
4. Test partial refund scenarios

### Manual Testing
1. Create order with payment → verify commission recorded
2. Process partial refund → verify commission adjusted
3. Check commission history → verify audit trail
4. Set seller override → verify it takes precedence
5. Set category override → verify it takes highest precedence

## Security Considerations
- Commission configuration restricted to admin users only
- All commission calculations logged for audit
- Immutable transaction history
- No sensitive data in commission records
- Proper authorization required for commission reports

## Performance Considerations
- Commission calculation is O(1) - single query per calculation
- Indexes optimize commission history queries
- No N+1 query issues in escrow allocation
- Efficient date-range queries for reporting

## Future Enhancements
- Per-product commission overrides
- Time-based commission rules (promotional periods)
- Volume-based commission tiers
- Commission reporting dashboard
- Automated commission payout integration
