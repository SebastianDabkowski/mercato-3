# Seller Revenue Report Feature

## Overview
This document describes the seller revenue report with CSV export feature implementation for the MercatoApp seller panel.

## Feature Status
✅ **Implemented and Ready for Use**

The seller revenue report feature is fully implemented and allows sellers to view their order and revenue data with commission calculations and export to CSV format.

## User Story
As a seller I want to generate order and revenue reports with CSV export so that I can reconcile my sales and import data into my own systems.

## Architecture

### Service Layer

#### ISellerRevenueReportService (`Services/ISellerRevenueReportService.cs`)
New service interface for generating seller revenue reports:

**GetRevenueReportAsync**
- Retrieves revenue report data for a seller's store with optional filters
- Parameters:
  - `storeId`: The store ID to generate report for
  - `statuses`: Optional list of order statuses to filter by
  - `fromDate`: Optional minimum order date
  - `toDate`: Optional maximum order date (includes entire day)
- Returns: Tuple of (List<RevenueReportItem>, RevenueReportSummary)

**ExportToCsvAsync**
- Exports revenue report to CSV format
- Parameters: Same as GetRevenueReportAsync
- Returns: RevenueReportExportResult with file data or errors

#### SellerRevenueReportService (`Services/SellerRevenueReportService.cs`)
Implementation details:

**Revenue Report Data Models**:
- **RevenueReportItem**: Individual order line item with financial details
  - Sub-Order Number
  - Parent Order Number
  - Created Date
  - Status
  - Buyer Name
  - Buyer Email
  - Order Value (subtotal + shipping)
  - Commission Charged (from escrow transaction)
  - Net Amount to Seller (order value - commission)
  - Refunded Amount

- **RevenueReportSummary**: Aggregated totals
  - Total Orders
  - Total Order Value
  - Total Commission Charged
  - Total Net Amount to Seller
  - Total Refunded Amount

**Commission Calculation**:
- Commission amounts are retrieved from EscrowTransaction records
- Each seller sub-order has an associated escrow transaction with the commission amount
- Net amount = Order value - Commission charged
- Calculations are consistent with the payments and settlements module

**Filtering Logic**:
- **Status Filter**: Filters by order status enum
- **Date Range Filter**: Applies inclusive date filtering with end-of-day handling
- **Performance**: Uses efficient database queries with Include for related data

**CSV Export Format**:
- Headers: Sub-Order Number, Parent Order Number, Created Date, Status, Buyer Name, Buyer Email, Order Value, Commission Charged, Net Amount to Seller, Refunded Amount
- Data rows: One row per seller sub-order
- Summary section: Totals for verification and reconciliation
- Proper CSV escaping for special characters
- UTF-8 encoding
- Timestamp in filename: `revenue_report_YYYYMMDD_HHmmss_utc.csv`

**Empty Data Handling**:
- When no orders match filters, generates CSV with headers only
- Returns error message indicating no data available
- Allows sellers to verify the export ran successfully even with no data

### Page Models

#### RevenueReportModel (`Pages/Seller/RevenueReport.cshtml.cs`)
Properties:
- **SelectedStatuses**: Filter - selected order statuses
- **FromDate**: Filter - minimum order date
- **ToDate**: Filter - maximum order date
- **ReportItems**: List of revenue report items
- **Summary**: Revenue report summary totals
- **CurrentStore**: The seller's store
- **ErrorMessages**: List of error messages
- **HasActiveFilters**: Indicates if any filters are active

Methods:
- **OnGetAsync**: Loads revenue report data with filters
- **OnPostExportCsvAsync**: Generates and downloads CSV export

### Views

#### RevenueReport.cshtml (`Pages/Seller/RevenueReport.cshtml`)
Features:
- **Header Section**: Revenue Report title, store name, Export CSV button
- **Filter Section**: Collapsible card with filter options
  - Status Multi-Select: Allows selecting multiple order statuses
  - Date Range Inputs: From date and to date
  - Apply Filters and Clear Filters buttons
  - "Active Filters" badge when filters are applied
- **Summary Card**: Dashboard-style metrics
  - Total Orders count
  - Total Order Value
  - Commission Charged (in red)
  - Net Amount to Seller (in green, emphasized)
  - Refunded Amount (in yellow)
- **Order Details Table**: 
  - Sub-order number with link to order details
  - Parent order number
  - Date and time
  - Status badge (color-coded)
  - Buyer name and email
  - Financial columns: Order Value, Commission, Net to Seller, Refunded
- **Empty States**: 
  - Different messages for no orders vs. no matching filters
  - Clear Filters button when filters are active
- **Navigation**: Back to Dashboard link

## Database Considerations

The revenue report queries:
- **SellerSubOrders** table for order data
- **EscrowTransactions** table for commission amounts
- Filters by `StoreId` to ensure seller isolation
- Uses existing indexes for efficient filtering
- Eager loads related entities (ParentOrder, User) to avoid N+1 queries

## Security & Data Isolation

✅ **Authorization**: `[Authorize(Policy = PolicyNames.SellerOnly)]` ensures only sellers can access
✅ **User Isolation**: All queries filtered by authenticated seller's store ID
✅ **No Cross-Store Data Leakage**: Sellers only see orders belonging to their store
✅ **Commission Consistency**: Uses same escrow transaction data as settlement module
✅ **Input Validation**: Date range validation in service layer
✅ **No SQL Injection**: Uses parameterized queries via Entity Framework Core
✅ **XSS Prevention**: Razor automatically encodes output

## Usage

### Accessing the Revenue Report
Navigate to: `/Seller/RevenueReport`
- Available in the Seller Panel dropdown menu
- Requires authentication and SellerOnly policy
- Automatically loads seller's store revenue data

### Filtering Revenue Data

**By Status**:
- Select one or more order statuses from the multi-select dropdown
- Hold Ctrl/Cmd to select multiple
- Available statuses: New, Paid, Preparing, Shipped, Delivered, Cancelled, Refunded

**By Date Range**:
- Set "From Date" for minimum order date
- Set "To Date" for maximum order date (includes entire day)
- Either or both can be set independently

**Clear Filters**:
- Click "Clear Filters" button to reset all filters and return to full revenue report

### Exporting to CSV

**From Revenue Report Page**:
1. Apply desired filters (optional)
2. Click "Export CSV" button
3. File downloads immediately with current filters applied

**CSV Contents**:
- All orders matching current filters
- Summary section at the bottom with totals
- Totals match what is displayed in the on-screen summary
- Empty file with headers if no orders match filters

**CSV Format**:
- Compatible with Excel, Google Sheets, and accounting/ERP tools
- UTF-8 encoding for international characters
- Proper escaping for commas, quotes, and special characters
- Filename includes UTC timestamp for tracking

## Acceptance Criteria

✅ **Given I am logged in as a seller, when I open my order and revenue report, then I see only orders that belong to my store.**
- Revenue report page filters by authenticated seller's store ID
- Only sub-orders belonging to the seller's store are displayed
- No data leakage from other sellers

✅ **Given I am on the seller report page, when I filter by date range and order status, then the table refreshes and shows matching orders with basic financial fields (order value, commission charged, net amount to seller).**
- Date range filter implemented with from/to dates
- Order status multi-select filter implemented
- Table displays Order Value, Commission Charged, and Net Amount to Seller
- Summary section shows totals for filtered data

✅ **Given I have filtered my report, when I click 'Export CSV', then I receive a CSV file containing only my orders that match the current filters.**
- Export CSV button available on revenue report page
- CSV export respects current filters
- Only matching orders are included in the CSV
- File downloads immediately

✅ **Given I open the CSV export in a spreadsheet tool, when I check the sums, then total amounts match what I see in the on-screen report for the same filter.**
- CSV includes summary section with totals
- Totals match on-screen summary values
- Total Order Value = sum of all Order Value rows
- Total Commission Charged = sum of all Commission rows
- Total Net Amount to Seller = sum of all Net Amount rows
- Total Refunded Amount = sum of all Refunded rows

✅ **Given there are no orders for the selected period, when I attempt a CSV export, then the system generates an empty file with headers or informs me that no data is available.**
- Empty CSV with headers is generated
- Error message displayed: "No orders found for the selected period. Generated empty file with headers."
- Allows verification that export ran successfully

## Additional Implementation Notes

### Seller Exports Must Never Contain Data of Other Sellers
- All queries include `.Where(so => so.StoreId == storeId)` filter
- Authorization policy ensures only authenticated seller can access their data
- No ability to manipulate store ID via query parameters

### Format Compatible with Accounting and ERP Tools
- Standard CSV format with comma delimiters
- Quoted values for fields containing special characters
- UTF-8 encoding for international support
- Consistent date format: `yyyy-MM-dd HH:mm:ss`
- Decimal format: `F2` (two decimal places)

### Commission and Net Amounts Calculated Consistently
- Commission amounts retrieved from `EscrowTransaction` table
- Same source of truth as payments and settlements module
- Net Amount = Order Value - Commission Charged
- No duplicate calculation logic

### Initial Version Filters
- Date range (from date, to date)
- Order status (multi-select)
- Future enhancements could add: buyer email, product name, order number search

## Testing Verification

### Manual Testing Performed
✅ Login as seller (seller@test.com / Test123!)
✅ Navigate to Revenue Report from seller menu
✅ Verify revenue data displays correctly
✅ Apply date range filter (future date) - verify no orders shown
✅ Export CSV with filters - verify empty file with headers
✅ Clear filters - verify all orders shown again
✅ Export CSV without filters - verify data matches on-screen
✅ Verify CSV totals match on-screen summary
✅ Verify seller can only see their own store's orders
✅ Verify commission amounts display correctly

### Screenshots
- Revenue Report page: ![Revenue Report Screenshot](https://github.com/user-attachments/assets/95639080-eac7-4e41-a1ea-c3f2ed19dd71)

## Related Files

**Services:**
- `/Services/ISellerRevenueReportService.cs` - Service interface and data models
- `/Services/SellerRevenueReportService.cs` - Service implementation

**Pages:**
- `/Pages/Seller/RevenueReport.cshtml` - Razor view
- `/Pages/Seller/RevenueReport.cshtml.cs` - Page model

**Configuration:**
- `/Program.cs` - Service registration
- `/Pages/Shared/_Layout.cshtml` - Navigation menu with Revenue Report link

**Models:**
- `/Models/SellerSubOrder.cs` - Sub-order model
- `/Models/EscrowTransaction.cs` - Escrow and commission data
- `/Models/Order.cs` - Parent order model
- `/Models/OrderStatus.cs` - Order status enum

## Future Enhancements

Potential improvements:
- **Additional Filters**: Buyer email search, product name, order number
- **Excel Export**: Native .xlsx format with formatting
- **Scheduled Reports**: Automatic weekly/monthly email exports
- **Custom Date Ranges**: Quick select options (Last 7 days, Last 30 days, This month, Last month)
- **Export Templates**: Custom column selection
- **Charts and Graphs**: Visual representation of revenue trends
- **Comparison Mode**: Compare current period to previous period
- **Advanced Filtering**: Multiple date ranges, buyer country, shipping method
