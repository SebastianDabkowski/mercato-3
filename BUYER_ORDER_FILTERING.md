# Buyer Order List with Filtering Feature

## Overview
This document describes the buyer order list with filtering and pagination feature implementation for the MercatoApp buyer portal.

## Feature Status
✅ **Implemented and Ready for Use**

The buyer order filtering feature is fully implemented and allows buyers to view, filter, and navigate through their order history efficiently.

## User Story
As a buyer, I want to see a list of my orders with filters so that I can quickly find a specific order or check the status of recent purchases.

## Architecture

### Service Layer

#### IOrderService (`Services/IOrderService.cs`)
Extended interface with new methods:

**GetUserOrdersFilteredAsync**
- Retrieves filtered and paginated orders for a user
- Parameters:
  - `userId`: The authenticated user's ID
  - `statuses`: Optional list of order statuses to filter by
  - `fromDate`: Optional minimum order date
  - `toDate`: Optional maximum order date (includes entire day)
  - `sellerId`: Optional seller/store ID to filter by
  - `page`: Page number (1-based, default: 1)
  - `pageSize`: Orders per page (1-100, default: 10)
- Returns: Tuple of (List<Order>, TotalCount)

**GetUserOrderSellersAsync**
- Retrieves unique sellers from a user's order history
- Used to populate the seller filter dropdown
- Parameters: `userId`
- Returns: List<Store> ordered by store name

#### OrderService (`Services/OrderService.cs`)
Implementation details:

**Filtering Logic**:
- **Status Filter**: Uses `Contains` with status enum list
- **Date Range Filter**: Applies inclusive date filtering with end-of-day handling
- **Seller Filter**: Optimized query that pre-fetches order IDs containing sub-orders from the specified seller
- **Pagination**: Validates bounds (1-100) and applies Skip/Take

**Performance Optimizations**:
- Validates pageSize to prevent abuse (1-100 range)
- Uses efficient `.Include()` for related data (DeliveryAddress, SubOrders, Store)
- Seller filter uses separate query to avoid N+1 issues
- Leverages existing database indexes on UserId, Status, and OrderedAt

### Page Model

#### OrdersModel (`Pages/Account/OrdersModel.cs`)
Properties:
- **Orders**: Current page of orders
- **TotalCount**: Total matching orders
- **CurrentPage**: Current page number
- **PageSize**: Orders per page (default: 10)
- **TotalPages**: Calculated total pages
- **SelectedStatuses**: Filter - selected order statuses
- **FromDate**: Filter - minimum order date
- **ToDate**: Filter - maximum order date
- **SellerId**: Filter - selected seller ID
- **PageNumber**: Query parameter for pagination
- **AvailableSellers**: List of sellers for dropdown

### View

#### Orders.cshtml (`Pages/Account/Orders.cshtml`)
Features:
- **Filter Section**: Collapsible card with all filter options
- **Status Multi-Select**: Allows selecting multiple statuses
- **Date Range Inputs**: HTML5 date inputs for from/to dates
- **Seller Dropdown**: Shows only when user has orders from multiple sellers
- **Results Display**: Shows order cards with status badges, payment info, and seller information
- **Pagination**: Bootstrap pagination with prev/next and page numbers
- **Empty States**: Different messages for no orders vs. no matching filters

## Database Indexes

The following indexes support efficient filtering (already present in ApplicationDbContext):

```csharp
// Index on user ID for finding all orders for a user
entity.HasIndex(e => e.UserId);

// Index on status for filtering orders
entity.HasIndex(e => e.Status);

// Composite index for ordering user's orders by date
entity.HasIndex(e => new { e.UserId, e.OrderedAt });
```

These indexes ensure optimal performance when filtering by user, status, and date range.

## Usage

### Accessing the Order List
Navigate to: `/Account/Orders`
- Requires authentication
- Automatically redirects to login if not authenticated

### Filtering Orders

**By Status**:
- Select one or more statuses from the multi-select dropdown
- Hold Ctrl/Cmd to select multiple
- Available statuses: New, Paid, Preparing, Shipped, Delivered, Cancelled, Refunded

**By Date Range**:
- Set "From Date" for minimum order date
- Set "To Date" for maximum order date (includes entire day)
- Either or both can be set independently

**By Seller**:
- Select a seller from the dropdown
- Only appears when user has orders from multiple sellers
- Shows orders containing sub-orders from that seller

**Clear Filters**:
- Click "Clear Filters" button to reset all filters and return to full order list

### Pagination
- Default: 10 orders per page
- Navigation: Previous, Page Numbers, Next
- Shows current page and total pages
- Filter parameters persist across pages

## Acceptance Criteria

✅ **Given I am logged in as a buyer, when I open my orders section, then I see a paginated list of my parent orders sorted by creation date (newest first).**
- Orders page shows 10 orders per page by default
- Orders sorted by OrderedAt descending
- Pagination controls visible when more than one page

✅ **Given I have many orders in different statuses, when I filter by status, then only orders matching the selected statuses are shown.**
- Multi-select status filter implemented
- Supports all order statuses: New, Paid, Preparing, Shipped, Delivered, Cancelled, Refunded
- Results update on form submission

✅ **Given I have orders over a long period, when I filter by date range, then only orders created within that range are shown.**
- From Date and To Date inputs implemented
- Date range filtering inclusive of entire days
- Can set one or both dates

✅ **Given orders with multiple sellers, when I apply a seller filter, then only orders that contain sub-orders for that seller are shown.**
- Seller dropdown populated from user's order history
- Filters orders containing sub-orders from selected seller
- Only shown when applicable

✅ **Given I see my order list, when I click an entry, then I am taken to the order detail view for that order.**
- "View Details" button links to `/Checkout/Confirmation?orderId=X`
- Order number also links to detail page

✅ **List must be limited to the authenticated buyer's orders only.**
- All queries filtered by `UserId` from authenticated user's claims
- Authorization required via `[Authorize]` attribute

✅ **Performance considerations: add indexes on buyer id, created date and status to support filtering at scale.**
- Indexes verified in ApplicationDbContext
- Efficient query structure with `.Include()` for related data
- Pagination bounds validation (1-100)

## Testing Recommendations

### Manual Testing
1. **Create test orders** with various statuses, dates, and sellers
2. **Test each filter** individually and in combination
3. **Test pagination** with more than 10 orders
4. **Test edge cases**: no orders, single page, boundary dates
5. **Test seller filter** with single seller and multiple sellers

### Automated Testing
Consider adding integration tests for:
- Filter combinations
- Pagination edge cases
- Performance with large datasets
- Security (user isolation)

## Security Considerations

✅ **Authorization**: `[Authorize]` attribute ensures only authenticated users can access
✅ **User Isolation**: All queries filtered by authenticated user's ID
✅ **Input Validation**: PageSize bounded to 1-100 to prevent abuse
✅ **No SQL Injection**: Uses parameterized queries via Entity Framework
✅ **XSS Prevention**: Razor automatically encodes output

## Future Enhancements

Potential improvements:
- **Search**: Add text search for order number or product names
- **Export**: Allow exporting order list to CSV/PDF
- **Advanced Filters**: Filter by payment status, price range, product categories
- **Sorting Options**: Allow sorting by price, order number, status
- **Save Filters**: Remember last used filters in user preferences
- **Bulk Actions**: Select multiple orders for bulk operations
