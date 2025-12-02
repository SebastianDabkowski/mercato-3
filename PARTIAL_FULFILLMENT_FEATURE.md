# Partial Fulfillment Feature (Phase 2)

## Overview

The Partial Fulfillment feature enables sellers to ship or cancel individual items or quantities within a sub-order, providing fine-grained control over order fulfillment. This is a Phase 2 enhancement that builds upon the basic full-order fulfillment (Phase 1).

## Features

### Item-Level Status Tracking

Each `OrderItem` now tracks its own fulfillment status:

- **New**: Item is awaiting processing
- **Preparing**: Item is being prepared for shipment
- **Shipped**: Item has been shipped to the customer
- **Cancelled**: Item has been cancelled (before shipment)

### Partial Quantities

Sellers can:
- Ship a portion of an item's quantity while keeping the rest pending
- Cancel a portion of an item's quantity while shipping the rest
- Track shipped, cancelled, and available quantities separately

### Automatic Refund Calculation

When items are cancelled:
- Refund amounts are calculated automatically
- Includes proportional item cost and tax
- Refunds are tracked at both item and sub-order levels
- Parent order refunded amount is updated automatically

### Status Aggregation

Sub-order status is automatically determined from item statuses:
- All items shipped → Sub-order marked as Shipped
- All items cancelled → Sub-order marked as Cancelled
- Mixed states → Sub-order reflects the most advanced active status

## Models

### OrderItemStatus Enum

```csharp
public enum OrderItemStatus
{
    New,        // Item awaiting processing
    Preparing,  // Item being prepared
    Shipped,    // Item shipped
    Cancelled   // Item cancelled
}
```

### OrderItem Updates

New properties added to `OrderItem` model:

```csharp
public OrderItemStatus Status { get; set; } = OrderItemStatus.New;
public int QuantityShipped { get; set; } = 0;
public int QuantityCancelled { get; set; } = 0;
public decimal RefundedAmount { get; set; } = 0;
```

## Services

### IOrderItemFulfillmentService

Primary service for item-level fulfillment operations:

- `ShipItemQuantityAsync(itemId, quantity, userId)` - Ship a specific quantity
- `CancelItemQuantityAsync(itemId, quantity, userId)` - Cancel a specific quantity
- `MarkItemAsPreparingAsync(itemId, userId)` - Mark item as preparing
- `GetAvailableQuantityAsync(itemId)` - Get available quantity
- `ValidateItemFulfillmentAsync(subOrderId)` - Validate fulfillment permissions
- `CalculateItemRefundAmountAsync(itemId, quantity)` - Calculate refund amount
- `GetSubOrderItemsWithStatusAsync(subOrderId)` - Get items with status

## User Interface

### Seller Panel

#### Partial Fulfillment Page (`/Seller/PartialFulfillment`)

Accessible when a sub-order is in `Paid` or `Preparing` status.

**Features:**
- View all items in a sub-order with their fulfillment status
- See ordered, shipped, cancelled, and available quantities
- Enter quantities to ship or cancel for each item
- Submit ship or cancel actions in bulk
- Visual feedback on item status with color-coded badges
- Automatic refund amount display

**Access:**
- From Order Details page via "Partial Fulfillment" button
- Only available for paid/preparing orders

#### Enhanced Order Details

- Item-level status display in order items table
- Shipped and cancelled quantity badges
- Refunded amount display per item
- Link to Partial Fulfillment page

### Buyer Portal

#### Enhanced Order Detail Page

- Item-level status badges (New, Preparing, Shipped, Cancelled)
- Shipped and cancelled quantity display
- Refunded amount shown per item
- Partial fulfillment notification when applicable
- Clear visibility into which items are shipped vs pending

## Business Logic

### Validation Rules

1. **Payment Validation**: Items can only be fulfilled if payment is completed
2. **Quantity Validation**: Cannot ship/cancel more than available quantity
3. **Status Validation**: Cannot cancel already shipped items
4. **Terminal States**: Cancelled sub-orders cannot be fulfilled

### Refund Calculation

```
Refund Amount = (Unit Price + Tax Per Unit) × Cancelled Quantity

Tax Per Unit = Total Tax Amount / Total Quantity
```

### Status Transitions

**Item Level:**
- New → Preparing (manual action)
- New → Shipped (when quantity shipped)
- New → Cancelled (when all quantity cancelled)
- Preparing → Shipped (when quantity shipped)
- Preparing → Cancelled (when quantity cancelled)

**Sub-Order Level:**
- Automatically updated based on item statuses
- Prioritizes active items over cancelled items in mixed states

## API Examples

### Ship Partial Quantity

```csharp
var (success, error) = await _itemFulfillmentService
    .ShipItemQuantityAsync(orderItemId: 123, quantityToShip: 3, userId: sellerId);
```

### Cancel Partial Quantity

```csharp
var (success, error, refundAmount) = await _itemFulfillmentService
    .CancelItemQuantityAsync(orderItemId: 123, quantityToCancel: 2, userId: sellerId);
```

### Get Available Quantity

```csharp
var available = await _itemFulfillmentService.GetAvailableQuantityAsync(orderItemId: 123);
```

## Database Schema Changes

### OrderItem Table Updates

| Column | Type | Description |
|--------|------|-------------|
| Status | int (enum) | Item fulfillment status |
| QuantityShipped | int | Number of units shipped |
| QuantityCancelled | int | Number of units cancelled |
| RefundedAmount | decimal | Amount refunded for this item |

## Security Considerations

- **Authorization**: Only sellers who own the store can fulfill items
- **Validation**: All quantity operations are validated server-side
- **Audit Trail**: User IDs are logged for all fulfillment actions
- **Anti-Forgery**: CSRF protection on all forms

## Financial Reporting Impact

Partial fulfillment affects financial calculations:

1. **Revenue Recognition**: Revenue should be recognized per shipped item
2. **Refunds**: Track refunds at item level for accurate reporting
3. **Commission/Fees**: Calculate based on shipped items only
4. **Inventory**: Update stock based on shipped/cancelled quantities

## Phase 2 Benefits

- **Flexibility**: Sellers can ship available items immediately
- **Customer Satisfaction**: Faster partial shipments instead of waiting for full stock
- **Inventory Management**: Handle stockouts without cancelling entire orders
- **Transparency**: Clear communication of item status to buyers
- **Financial Accuracy**: Precise refund calculations per item

## Acceptance Criteria Met

✅ Sellers can mark selected items as 'shipped' while leaving others in 'preparing' or 'new' status  
✅ Buyers can clearly see which items have been shipped and which are pending or cancelled  
✅ Refund amounts are calculated only for cancelled items  
✅ Revenue and fees can be calculated correctly per shipped and cancelled item  
✅ Partial fulfillment is clearly communicated to both buyers and sellers  

## Future Enhancements

- Item-level tracking numbers for multi-package shipments
- Partial return support for shipped quantities
- Email notifications for partial shipments
- Bulk fulfillment actions across multiple sub-orders
- Analytics dashboard for fulfillment metrics
- Integration with shipping carriers for item-level tracking

## Notes

- This feature is Phase 2 and does not block Phase 1 basic order fulfillment
- Backward compatible with existing orders (defaults to full-order fulfillment)
- Item-level status is optional - sellers can still use full sub-order actions
- All financial calculations maintain precision to 2 decimal places
