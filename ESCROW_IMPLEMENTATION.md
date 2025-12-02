# Escrow Payment Model Implementation

## Overview
This implementation adds a comprehensive marketplace escrow payment model to MercatoApp, enabling secure multi-vendor transactions with automated fund holding, commission deduction, and controlled payout release.

## Epic Reference
**Epic 7: Payments & Settlements**

## User Story
As a marketplace owner I want all buyer payments to flow through Mercato escrow so that we can control fund release and deduct commissions.

## Acceptance Criteria Status

✅ **Payment is stored in escrow after successful confirmation**
- Escrow allocations are created automatically when payment is completed
- Each seller gets a separate escrow transaction
- Works for both immediate payments (COD) and provider-authorized payments

✅ **Escrow amounts are split per seller**
- Multi-seller orders create individual escrow allocations for each seller
- Each allocation includes gross amount, commission, and net payout amount
- Commissions are calculated based on active CommissionConfig

✅ **Cancelled orders release escrow back to the buyer**
- Full escrow amount returned to buyer when order is cancelled
- Partial refunds supported for partial returns
- Escrow status updated to ReturnedToBuyer or PartiallyRefunded

✅ **Multi-seller orders store separate escrow allocations**
- One EscrowTransaction per SellerSubOrder
- Unique index ensures one-to-one relationship
- Each seller's escrow is managed independently

## Additional Features

✅ **Escrow ledger is auditable**
- All transactions tracked with timestamps (CreatedAt, UpdatedAt, ReleasedAt, ReturnedToBuyerAt)
- Status changes logged
- Notes field for audit trail
- Commission calculations transparent

✅ **Eligibility for payout is configurable**
- Default: 7 days after delivery (configurable via constant)
- EligibleForPayoutAt timestamp tracks when funds can be released
- Automatic processing via ProcessEligiblePayoutsAsync

## Architecture

### Database Schema

#### EscrowTransaction Model
```csharp
public class EscrowTransaction
{
    public int Id { get; set; }
    public int PaymentTransactionId { get; set; }
    public int SellerSubOrderId { get; set; }
    public int StoreId { get; set; }
    public decimal GrossAmount { get; set; }           // Subtotal + shipping
    public decimal CommissionAmount { get; set; }       // Platform commission
    public decimal NetAmount { get; set; }              // Amount seller receives
    public EscrowStatus Status { get; set; }
    public DateTime? EligibleForPayoutAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime? ReturnedToBuyerAt { get; set; }
    public decimal RefundedAmount { get; set; }
    public string? Notes { get; set; }
}
```

#### EscrowStatus Enum
- **Held**: Funds held in escrow, awaiting release conditions
- **EligibleForPayout**: Funds eligible for payout based on marketplace policy
- **Released**: Funds released to seller
- **ReturnedToBuyer**: Funds returned to buyer (cancelled/refunded)
- **InDispute**: Escrow in dispute (return request pending)
- **PartiallyRefunded**: Partial refund processed

### Service Layer

#### IEscrowService Interface
Key methods:
- `CreateEscrowAllocationsAsync`: Split payment across sellers with commission deduction
- `ReleaseEscrowAsync`: Release funds to seller
- `ReturnEscrowToBuyerAsync`: Return funds to buyer (full or partial)
- `MarkEscrowEligibleForPayoutAsync`: Set eligibility date after delivery
- `CalculateCommissionAsync`: Calculate platform commission
- `ProcessEligiblePayoutsAsync`: Background job to release eligible payouts

### Integration Points

#### PaymentService
- `HandlePaymentCallbackAsync`: Creates escrow allocations after successful payment
- `InitiatePaymentAsync`: Creates escrow for immediate payments (COD)
- Non-blocking: Escrow creation failures logged but don't block payment completion

#### OrderStatusService
- `UpdateSubOrderToDeliveredAsync`: Marks escrow eligible for payout (7 days after delivery)
- `CancelSubOrderAsync`: Returns full escrow to buyer
- `RefundSubOrderAsync`: Returns partial/full escrow based on refund amount

## Payment Flow with Escrow

### 1. Payment Initiated
```
Buyer completes payment → PaymentTransaction created → Status: Pending
```

### 2. Payment Confirmed
```
Payment provider confirms → PaymentTransaction: Completed
↓
PaymentService.HandlePaymentCallbackAsync()
↓
Order status: New → Paid
↓
EscrowService.CreateEscrowAllocationsAsync()
↓
For each SellerSubOrder:
  - GrossAmount = SubOrder.TotalAmount
  - CommissionAmount = Calculate from CommissionConfig
  - NetAmount = GrossAmount - CommissionAmount
  - Status = Held
```

### 3. Order Fulfilled
```
Seller ships → SubOrder status: Shipped
↓
Delivery confirmed → SubOrder status: Delivered
↓
OrderStatusService.UpdateSubOrderToDeliveredAsync()
↓
EscrowService.MarkEscrowEligibleForPayoutAsync()
↓
EligibleForPayoutAt = DateTime.UtcNow + 7 days
Status = EligibleForPayout
```

### 4. Payout Released (Background Job)
```
Daily background job: ProcessEligiblePayoutsAsync()
↓
For each escrow where:
  - Status = EligibleForPayout
  - EligibleForPayoutAt <= Now
  - SubOrder.Status = Delivered
↓
ReleaseEscrowAsync()
↓
Status = Released
ReleasedAt = DateTime.UtcNow
[Integration point: Trigger actual payout to seller's account]
```

### 5. Cancellation/Refund Flow
```
Order cancelled/refunded
↓
OrderStatusService.CancelSubOrderAsync() or RefundSubOrderAsync()
↓
EscrowService.ReturnEscrowToBuyerAsync(refundAmount)
↓
RefundedAmount += refundAmount
If full refund:
  Status = ReturnedToBuyer
  ReturnedToBuyerAt = DateTime.UtcNow
Else:
  Status = PartiallyRefunded
```

## Commission Calculation

Commissions are calculated using the active CommissionConfig:

```csharp
CommissionAmount = (GrossAmount × CommissionPercentage / 100) + FixedCommissionAmount
NetAmount = GrossAmount - CommissionAmount
```

Example:
- GrossAmount: $100.00
- CommissionPercentage: 10%
- FixedCommissionAmount: $0.50
- CommissionAmount: ($100 × 10 / 100) + $0.50 = $10.50
- NetAmount: $100.00 - $10.50 = $89.50

## Security & Safety Features

### Idempotency
- Escrow creation checks for existing allocations (prevents duplicates)
- Payment callback handling is idempotent
- Refund operations validate amounts against available escrow

### Data Integrity
- Unique index on SellerSubOrderId ensures one escrow per sub-order
- Foreign key constraints prevent orphaned records
- Decimal precision configured to 18,2 for currency values
- Tolerance-based decimal comparisons avoid floating-point issues

### Validation
- Status transition validation prevents invalid state changes
- Amount validation prevents negative or excessive refunds
- Sub-order status checks before releasing funds
- Commission config validation (defaults to 0% if missing)

### Auditability
- All state changes timestamped
- Notes field for audit trail
- Status history maintained
- Cannot release funds already returned to buyer

## Code Quality

### Security Scan Results
✅ **0 vulnerabilities** detected by CodeQL scanner

### Code Review Improvements
- Extracted magic numbers to named constants
- Improved decimal comparison with tolerance
- Added comprehensive documentation
- Documented design decisions

### Constants
```csharp
private const decimal PercentageDivisor = 100m;
private const int DefaultPayoutEligibilityDays = 7;
private const decimal CurrencyTolerance = 0.01m;
```

## Configuration

### Payout Eligibility
The default eligibility period is 7 days after delivery. This can be configured by modifying `DefaultPayoutEligibilityDays` constant in EscrowService.

To change globally:
```csharp
// In EscrowService.cs
private const int DefaultPayoutEligibilityDays = 14; // Change to 14 days
```

To change per call:
```csharp
await escrowService.MarkEscrowEligibleForPayoutAsync(subOrderId, daysUntilEligible: 14);
```

### Commission Configuration
Commissions are managed via the CommissionConfig table. Only one config should be active at a time.

Example configuration:
```csharp
var commissionConfig = new CommissionConfig
{
    CommissionPercentage = 10.5m,  // 10.5%
    FixedCommissionAmount = 0.50m, // $0.50 per transaction
    IsActive = true
};
```

## Testing Scenarios

### Manual Testing

#### Test 1: Single Seller Order
1. Create order with items from one seller
2. Complete payment
3. Verify one escrow created with correct amounts
4. Mark order as delivered
5. Verify escrow marked as EligibleForPayout
6. Run ProcessEligiblePayoutsAsync after eligibility date
7. Verify escrow released

#### Test 2: Multi-Seller Order
1. Create order with items from 3 different sellers
2. Complete payment
3. Verify 3 escrow transactions created (one per seller)
4. Verify commission calculations are correct
5. Verify total of all escrows equals payment amount

#### Test 3: Order Cancellation
1. Create and pay for order
2. Verify escrow created and held
3. Cancel order before shipment
4. Verify full escrow returned to buyer
5. Verify escrow status = ReturnedToBuyer

#### Test 4: Partial Refund
1. Create and complete order
2. Ship and deliver order
3. Process partial return (50% of items)
4. Verify partial refund processed
5. Verify escrow status = PartiallyRefunded
6. Verify remaining escrow still eligible for release

#### Test 5: Commission Deduction
1. Set CommissionConfig: 10% + $1.00 fixed
2. Create order with subtotal $50 + shipping $10 = $60
3. Verify escrow created:
   - GrossAmount = $60.00
   - CommissionAmount = ($60 × 0.10) + $1.00 = $7.00
   - NetAmount = $60.00 - $7.00 = $53.00

### Integration Testing
```csharp
// Test escrow creation on payment
var payment = await paymentService.HandlePaymentCallbackAsync(transactionId, true, "provider-123", null);
var escrows = await escrowService.GetEscrowTransactionsByPaymentAsync(transactionId);
Assert.NotEmpty(escrows);
Assert.All(escrows, e => Assert.Equal(EscrowStatus.Held, e.Status));

// Test escrow release on delivery
await orderStatusService.UpdateSubOrderToDeliveredAsync(subOrderId);
var escrow = await escrowService.GetEscrowTransactionBySubOrderAsync(subOrderId);
Assert.Equal(EscrowStatus.EligibleForPayout, escrow.Status);
Assert.NotNull(escrow.EligibleForPayoutAt);

// Test escrow return on cancellation
await orderStatusService.CancelSubOrderAsync(subOrderId);
escrow = await escrowService.GetEscrowTransactionBySubOrderAsync(subOrderId);
Assert.Equal(EscrowStatus.ReturnedToBuyer, escrow.Status);
Assert.Equal(escrow.GrossAmount, escrow.RefundedAmount);
```

## Database Indexes

For optimal query performance, the following indexes are configured:

```csharp
// Find all escrows from a payment
entity.HasIndex(e => e.PaymentTransactionId);

// Find escrow by sub-order (unique, one-to-one)
entity.HasIndex(e => e.SellerSubOrderId).IsUnique();

// Find all escrows for a seller
entity.HasIndex(e => e.StoreId);

// Filter by status
entity.HasIndex(e => e.Status);

// Find eligible payouts
entity.HasIndex(e => new { e.Status, e.EligibleForPayoutAt });
```

## Background Jobs (Future Enhancement)

The escrow system is designed to support automated payout processing via background jobs:

```csharp
// Run daily to process eligible payouts
public class EscrowPayoutJob : IHostedService
{
    public async Task ProcessAsync()
    {
        var releasedCount = await escrowService.ProcessEligiblePayoutsAsync();
        logger.LogInformation("Released {Count} eligible escrow payouts", releasedCount);
    }
}
```

## API Examples

### Get Escrow for Sub-Order
```csharp
var escrow = await escrowService.GetEscrowTransactionBySubOrderAsync(subOrderId);
if (escrow != null)
{
    Console.WriteLine($"Gross: {escrow.GrossAmount:C}");
    Console.WriteLine($"Commission: {escrow.CommissionAmount:C}");
    Console.WriteLine($"Net to Seller: {escrow.NetAmount:C}");
    Console.WriteLine($"Status: {escrow.Status}");
}
```

### Get All Escrows for Seller
```csharp
// Get all held escrows for a seller
var heldEscrows = await escrowService.GetEscrowTransactionsByStoreAsync(
    storeId, 
    EscrowStatus.Held);

// Get all escrows (any status)
var allEscrows = await escrowService.GetEscrowTransactionsByStoreAsync(storeId);
```

### Calculate Commission Preview
```csharp
var orderTotal = 100.00m;
var commission = await escrowService.CalculateCommissionAsync(orderTotal);
var sellerReceives = orderTotal - commission;
```

## Limitations & Known Issues

### Current Limitations
1. **In-Memory Database**: Production deployment requires persistent database
2. **No Actual Payout Integration**: EscrowService marks funds as released but doesn't transfer to seller bank accounts (requires payment gateway integration)
3. **Manual Background Jobs**: ProcessEligiblePayoutsAsync must be called manually or via scheduled job
4. **No Dispute Resolution UI**: InDispute status exists but no UI for managing disputes

### Design Decisions
1. **One-to-One Escrow/SubOrder**: Each sub-order has exactly one escrow transaction (no partial payments supported)
2. **Non-Blocking Escrow Creation**: If escrow creation fails, payment still succeeds (escrow can be created in retry)
3. **7-Day Default Hold**: Funds held for 7 days after delivery before release (configurable)

## Future Enhancements

### Recommended Additions
1. **Payout Gateway Integration**: Integrate with Stripe Connect, PayPal Payouts, or similar
2. **Automated Background Jobs**: Use Hangfire or similar for ProcessEligiblePayoutsAsync
3. **Dispute Management**: UI for handling escrow disputes (returns, chargebacks)
4. **Escrow Dashboard**: Seller dashboard showing pending, eligible, and released funds
5. **Commission Override**: Per-seller commission rates (override platform default)
6. **Escrow Reserve**: Hold percentage of funds in reserve for high-risk sellers
7. **Instant Payout Option**: Allow sellers to pay fee for immediate payout
8. **Payout Scheduling**: Configure payout frequency (daily, weekly, monthly)
9. **Multi-Currency Support**: Handle currency conversion for international sellers
10. **Tax Withholding**: Automatic tax withholding based on seller location

### Production Checklist
- [ ] Migrate to persistent database (PostgreSQL, SQL Server, etc.)
- [ ] Implement actual payout gateway integration
- [ ] Set up automated background job for ProcessEligiblePayoutsAsync
- [ ] Add monitoring/alerting for escrow operations
- [ ] Implement escrow reconciliation reports
- [ ] Add seller payout history UI
- [ ] Configure commission rates per marketplace requirements
- [ ] Test with real payment provider
- [ ] Set up error handling for failed payouts
- [ ] Implement payout retry logic
- [ ] Add dispute resolution workflow
- [ ] Configure database backup strategy
- [ ] Set up audit log archival

## Support & Troubleshooting

### Common Issues

**Issue: Escrow not created after payment**
- Check logs for escrow creation errors
- Verify CommissionConfig exists and is active
- Manually call CreateEscrowAllocationsAsync with payment transaction ID

**Issue: Escrow not released after delivery**
- Verify sub-order status is Delivered
- Check EligibleForPayoutAt date has passed
- Verify escrow status is EligibleForPayout
- Run ProcessEligiblePayoutsAsync manually

**Issue: Refund amount exceeds available escrow**
- Verify refund amount <= (GrossAmount - RefundedAmount)
- Check if escrow was already released to seller

### Monitoring Queries

```sql
-- Check escrow balances by status
SELECT Status, COUNT(*), SUM(GrossAmount) as TotalGross, SUM(NetAmount) as TotalNet
FROM EscrowTransactions
GROUP BY Status;

-- Find stuck escrows (eligible but not released)
SELECT *
FROM EscrowTransactions
WHERE Status = 'EligibleForPayout'
  AND EligibleForPayoutAt < GETDATE()
ORDER BY EligibleForPayoutAt;

-- Seller payout summary
SELECT StoreId, 
       COUNT(*) as TotalOrders,
       SUM(GrossAmount) as TotalSales,
       SUM(CommissionAmount) as TotalCommissions,
       SUM(NetAmount) as TotalPayouts
FROM EscrowTransactions
WHERE Status = 'Released'
GROUP BY StoreId;
```

## Migration Notes

### Database Migration
When deploying to production:
1. Run database migrations to create EscrowTransactions table
2. Ensure indexes are created
3. Verify foreign key constraints
4. Add CommissionConfig if not exists

### Existing Orders
Existing paid orders (before escrow implementation) will not have escrow records. Options:
1. Create escrow retroactively for recent orders
2. Only apply escrow to new orders
3. Use transition period with optional escrow

---

**Implementation Date**: December 2, 2025
**Status**: ✅ Complete
**Security Scan**: ✅ Passed (0 vulnerabilities)
**Code Review**: ✅ Passed with improvements applied
