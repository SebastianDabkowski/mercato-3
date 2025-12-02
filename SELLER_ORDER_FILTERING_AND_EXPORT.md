# Seller Order List with Filtering and Export Feature

## Overview
This document describes the seller order list with filtering, pagination, and export feature implementation for the MercatoApp seller panel.

## Feature Status
✅ **Implemented and Ready for Use**

The seller order filtering and export feature is fully implemented and allows sellers to view, filter, paginate, and export their sub-orders efficiently.

## User Story
As a seller I want to see a list of my sub-orders with filters and export so that I can manage fulfilment and reconcile orders with my internal systems.

## Architecture

### Service Layer

#### IOrderService (`Services/IOrderService.cs`)
Extended interface with new method:

**GetSubOrdersFilteredAsync**
- Retrieves filtered and paginated seller sub-orders for a specific store
- Parameters:
  - `storeId`: The store ID
  - `statuses`: Optional list of order statuses to filter by
  - `fromDate`: Optional minimum order date
  - `toDate`: Optional maximum order date (includes entire day)
  - `buyerEmail`: Optional buyer email for partial match filtering
  - `page`: Page number (1-based, default: 1)
  - `pageSize`: Sub-orders per page (1-100, default: 10)
- Returns: Tuple of (List<SellerSubOrder>, TotalCount)

#### OrderService (`Services/OrderService.cs`)
Implementation details:

**Filtering Logic**:
- **Status Filter**: Uses `Contains` with status enum list
- **Date Range Filter**: Applies inclusive date filtering with end-of-day handling
- **Buyer Email Filter**: Uses `EF.Functions.Like` for case-insensitive partial match (optimized for database indexes)
- **Pagination**: Validates bounds (1-100) and applies Skip/Take

**Performance Optimizations**:
- Validates pageSize to prevent abuse (1-100 range)
- Uses efficient `.Include()` for related data (ParentOrder, User, Items, ShippingMethod)
- Email filter uses EF.Functions.Like instead of ToLower() for index usage
- Leverages existing database indexes on StoreId, Status, and CreatedAt

#### IOrderExportService (`Services/OrderExportService.cs`)
New service interface for exporting seller orders:

**ExportToCsvAsync**
- Exports seller sub-orders to CSV format
- Parameters: Same as GetSubOrdersFilteredAsync (filters)
- Returns: OrderExportResult with file data or errors

**ExportToExcelAsync**
- Exports seller sub-orders to Excel format with formatting
- Parameters: Same as GetSubOrdersFilteredAsync (filters)
- Returns: OrderExportResult with file data or errors

**Export Columns**:
- Sub-Order Number
- Parent Order Number
- Created Date (UTC)
- Status
- Buyer Name
- Buyer Email
- Total Amount
- Shipping Cost
- Subtotal
- Shipping Method
- Tracking Number
- Items Count

### Page Models

#### OrdersModel (`Pages/Seller/Orders.cshtml.cs`)
Properties:
- **SubOrders**: Current page of seller sub-orders
- **TotalCount**: Total matching sub-orders
- **CurrentPage**: Current page number
- **PageSize**: Sub-orders per page (default: 10)
- **TotalPages**: Calculated total pages
- **SelectedStatuses**: Filter - selected order statuses
- **FromDate**: Filter - minimum order date
- **ToDate**: Filter - maximum order date
- **BuyerEmail**: Filter - buyer email search
- **PageNumber**: Query parameter for pagination
- **CurrentStore**: The seller's store

#### OrderExportModel (`Pages/Seller/OrderExport.cshtml.cs`)
Properties:
- **SelectedStatuses**: Carried over from order list filters
- **FromDate**: Carried over from order list filters
- **ToDate**: Carried over from order list filters
- **BuyerEmail**: Carried over from order list filters
- **ExportFormat**: Selected format (csv or excel)
- **ErrorMessages**: List of validation errors
- **HasActiveFilters**: Indicates if any filters are active

### Views

#### Orders.cshtml (`Pages/Seller/Orders.cshtml`)
Features:
- **Header Section**: Store name, Export Orders button
- **Filter Section**: Collapsible card with all filter options
- **Status Multi-Select**: Allows selecting multiple statuses (Ctrl/Cmd)
- **Date Range Inputs**: HTML5 date inputs for from/to dates
- **Buyer Email Input**: Text search with partial match support
- **Results Display**: Table showing order details with status badges
- **Pagination**: Bootstrap pagination with prev/next and page numbers
- **Empty States**: Different messages for no orders vs. no matching filters
- **Filter Persistence**: All filter parameters preserved across pagination

#### OrderExport.cshtml (`Pages/Seller/OrderExport.cshtml`)
Features:
- **Format Selection**: Radio buttons for CSV or Excel
- **Active Filters Display**: Shows which filters will be applied to export
- **Export Information**: Details about what data is included
- **Download Button**: Triggers export generation and download
- **Back Navigation**: Return to orders list

## Database Considerations

The following indexes support efficient filtering (already present in ApplicationDbContext):

```csharp
// Index on store ID for finding all sub-orders for a store
entity.HasIndex(e => e.StoreId);

// Index on status for filtering sub-orders
entity.HasIndex(e => e.Status);

// Composite index for ordering store's sub-orders by date
entity.HasIndex(e => new { e.StoreId, e.CreatedAt });
```

These indexes ensure optimal performance when filtering by store, status, and date range.

## Usage

### Accessing the Order List
Navigate to: `/Seller/Orders`
- Requires authentication and SellerOnly policy
- Automatically loads seller's store
- Shows paginated list of sub-orders

### Filtering Orders

**By Status**:
- Select one or more statuses from the multi-select dropdown
- Hold Ctrl/Cmd to select multiple
- Available statuses: New, Paid, Preparing, Shipped, Delivered, Cancelled, Refunded

**By Date Range**:
- Set "From Date" for minimum order date
- Set "To Date" for maximum order date (includes entire day)
- Either or both can be set independently

**By Buyer Email**:
- Enter full or partial buyer email
- Search is case-insensitive
- Searches both registered user emails and guest emails

**Clear Filters**:
- Click "Clear Filters" button to reset all filters and return to full order list

### Pagination
- Default: 10 sub-orders per page
- Navigation: Previous, Page Numbers, Next
- Shows current page and total pages
- Filter parameters persist across pages

### Exporting Orders

**From Order List**:
1. Apply desired filters (optional)
2. Click "Export Orders" button
3. Filters are automatically carried to export page

**Export Configuration**:
1. Select format (CSV or Excel)
2. Review active filters
3. Click "Download Export"
4. File downloads immediately

**Export Formats**:
- **CSV**: Compatible with Excel, Google Sheets, all spreadsheet apps
- **Excel**: Native .xlsx with formatting, currency columns, auto-fit

## Acceptance Criteria

✅ **Given I am logged in as a seller, when I open my orders section, then I see a paginated list of sub-orders that belong only to my store.**
- Orders page shows sub-orders filtered by seller's store ID
- Pagination controls visible when more than 10 sub-orders
- Sub-orders sorted by CreatedAt descending (newest first)

✅ **Given I have many sub-orders, when I filter by status, date range or buyer, then the list updates to show only matching sub-orders.**
- Status multi-select filter implemented
- Date range filter implemented with full-day inclusion
- Buyer email partial match filter implemented
- Results update on form submission

✅ **Given I need to process shipments in bulk, when I filter sub-orders by 'paid' or 'preparing', then I can focus on orders that require action.**
- Status filter supports selecting "Paid" and "Preparing" statuses
- Results show only matching sub-orders
- Clear filtering allows quick focus on actionable orders

✅ **Given I want to export my orders, when I choose export with a selected filter set, then the system generates an export file (e.g. CSV/XLS) containing at least order id, creation date, status, buyer, total amount and shipping method.**
- Export button available on orders page
- Filters automatically carry to export page
- Both CSV and Excel formats supported
- All required columns included

✅ **Given an export is generated, when the file is ready, then I can download it from the UI.**
- Export generates synchronously
- File downloads immediately via browser
- Filename includes timestamp for organization

✅ **Export format should be compatible with common spreadsheet tools and potential ERP/WMS imports.**
- CSV uses proper escaping for commas, quotes, newlines
- Excel uses native .xlsx format
- Currency columns formatted in Excel
- UTC timestamps for consistency

✅ **Access control: sellers must not see orders or buyer data for other sellers.**
- All queries filtered by authenticated seller's store ID
- Authorization via SellerOnly policy
- No cross-store data leakage

✅ **Large exports may require background processing and a download link rather than synchronous generation.**
- Current implementation is synchronous (suitable for moderate data volumes)
- Future enhancement: Add async processing for large exports if needed

## Security Considerations

✅ **Authorization**: `[Authorize(Policy = PolicyNames.SellerOnly)]` ensures only sellers can access
✅ **User Isolation**: All queries filtered by authenticated seller's store ID
✅ **Input Validation**: PageSize bounded to 1-100 to prevent abuse
✅ **No SQL Injection**: Uses parameterized queries via Entity Framework Core
✅ **XSS Prevention**: Razor automatically encodes output
✅ **Database Optimization**: Uses EF.Functions.Like for index-friendly filtering
✅ **CodeQL Scan**: 0 vulnerabilities detected

## Performance Characteristics

- **Query Optimization**: Uses database indexes for filtering
- **Email Search**: EF.Functions.Like enables index usage
- **Pagination**: Limits result set size to prevent memory issues
- **Eager Loading**: Strategic use of .Include() to avoid N+1 queries
- **Export Performance**: Loads all matching records for export (consider async for very large datasets)

## Testing Recommendations

### Manual Testing
1. **Create test sub-orders** with various statuses, dates, and buyers
2. **Test each filter** individually and in combination
3. **Test pagination** with more than 10 sub-orders
4. **Test export** in both CSV and Excel formats
5. **Test filter persistence** across pagination
6. **Test empty states** (no orders, no matching filters)
7. **Test security** (ensure sellers can't access other stores' orders)

### Automated Testing
Consider adding integration tests for:
- Filter combinations
- Pagination edge cases
- Export file generation
- Security (store isolation)
- Performance with large datasets

## Future Enhancements

Potential improvements:
- **Async Export**: Background processing for very large exports with email notification
- **Additional Filters**: Filter by product, price range, delivery address
- **Search**: Full-text search across order numbers and product names
- **Sorting Options**: Allow sorting by different columns
- **Save Filters**: Remember last used filters in user preferences
- **Bulk Actions**: Select multiple orders for status updates or label printing
- **Export Templates**: Custom export column selection
- **Scheduled Exports**: Automatic daily/weekly exports via email

## Code Examples

### Using the Filtering Service
```csharp
// Get filtered sub-orders
var (subOrders, totalCount) = await _orderService.GetSubOrdersFilteredAsync(
    storeId: myStoreId,
    statuses: new List<OrderStatus> { OrderStatus.Paid, OrderStatus.Preparing },
    fromDate: DateTime.UtcNow.AddDays(-30),
    toDate: DateTime.UtcNow,
    buyerEmail: "john",
    page: 1,
    pageSize: 10
);
```

### Using the Export Service
```csharp
// Export to CSV
var result = await _exportService.ExportToCsvAsync(
    storeId: myStoreId,
    statuses: new List<OrderStatus> { OrderStatus.Paid },
    fromDate: DateTime.UtcNow.AddDays(-7),
    toDate: null,
    buyerEmail: null
);

if (result.Success)
{
    return File(result.FileData, result.ContentType, result.FileName);
}
```

## Related Files

**Services:**
- `/Services/IOrderService.cs`
- `/Services/OrderService.cs`
- `/Services/OrderExportService.cs`

**Pages:**
- `/Pages/Seller/Orders.cshtml`
- `/Pages/Seller/Orders.cshtml.cs`
- `/Pages/Seller/OrderExport.cshtml`
- `/Pages/Seller/OrderExport.cshtml.cs`

**Models:**
- `/Models/SellerSubOrder.cs`
- `/Models/Order.cs`
- `/Models/OrderStatus.cs`

**Configuration:**
- `/Program.cs` (service registration)
