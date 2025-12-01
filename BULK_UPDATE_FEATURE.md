# Bulk Update Price and Stock Feature

## Overview
This feature allows sellers to update prices or stock levels for multiple products simultaneously, saving time and improving catalog management efficiency.

## Features

### 1. Product Selection
- Sellers can select multiple products from their product catalog using checkboxes
- A "Select All" checkbox allows selecting all products at once
- The selected count is displayed in real-time
- Only non-archived products can be selected for bulk updates

### 2. Bulk Update Operations

#### Price Updates
- **Set to Value**: Set all selected products to a specific price
- **Increase by Amount**: Add a fixed amount to current prices
- **Decrease by Amount**: Subtract a fixed amount from current prices
- **Increase by Percentage**: Increase prices by a percentage (e.g., 10% for a 10% price increase)
- **Decrease by Percentage**: Decrease prices by a percentage (e.g., 25% for a 25% discount)

#### Stock Updates
- **Set to Value**: Set all selected products to a specific stock quantity
- **Increase by Amount**: Add a fixed quantity to current stock
- **Decrease by Amount**: Subtract a fixed quantity from current stock
- **Increase by Percentage**: Increase stock by a percentage
- **Decrease by Percentage**: Decrease stock by a percentage

### 3. Preview Functionality
Before executing a bulk update, sellers can preview the changes:
- Shows current value and new value for each product
- Highlights invalid updates that would result in negative prices or stock
- Displays error messages for products that cannot be updated
- Shows a summary of valid vs. invalid updates

### 4. Validation
The system validates all updates to prevent invalid data:
- **Price Validation**:
  - Prices must be greater than zero
  - Prices cannot exceed the maximum allowed value ($999,999.99)
- **Stock Validation**:
  - Stock levels cannot be negative
  - Stock values must be within valid integer range

### 5. Safety Features
- **Store Ownership Verification**: Only products belonging to the seller's store can be updated
- **Archived Product Protection**: Archived products are excluded from bulk updates
- **Transaction Safety**: All updates are executed in a single database transaction
- **Audit Logging**: Every bulk update operation is logged with:
  - User ID performing the operation
  - Store ID
  - Update type (Price or Stock)
  - Operation performed
  - Number of successful and failed updates
  - Detailed list of all changes

## User Workflow

### Step 1: Select Products
1. Navigate to the Products page in the Seller Panel
2. Use checkboxes to select products to update
3. Click the "Bulk Update" button (enabled when products are selected)

### Step 2: Configure Update
1. Choose update type: Price or Stock
2. Select operation: SetValue, IncreaseBy, DecreaseBy, IncreaseByPercent, or DecreaseByPercent
3. Enter the value for the operation
4. Click "Preview Changes"

### Step 3: Preview and Confirm
1. Review the preview table showing:
   - Product names
   - Current values
   - New values after update
   - Validation status for each product
2. Check the summary showing number of valid and invalid updates
3. Click "Confirm and Update" to execute the changes (only enabled if at least one valid update exists)

### Step 4: View Results
1. After successful update, you'll see a success message with the count of updated products
2. If some products failed validation, error messages are displayed
3. Return to the Products page to see the updated values

## Technical Implementation

### Models
- `BulkUpdateType`: Enum for Price/Stock updates
- `BulkUpdateOperation`: Enum for operation types
- `BulkUpdateRequest`: Request model containing product IDs, update type, operation, and value
- `BulkUpdateResult`: Result model with success count, failure count, and errors
- `BulkUpdatePreviewItem`: Preview model showing before/after values and validation status
- `ProductBulkUpdateError`: Error model for individual product failures

### Service Layer
- `IBulkProductUpdateService`: Interface defining preview and execute methods
- `BulkProductUpdateService`: Implementation with:
  - Preview generation
  - Validation logic
  - Bulk update execution
  - Audit logging

### UI Components
- **Products/Index.cshtml**: Enhanced with checkboxes and bulk update button
- **Products/BulkUpdate.cshtml**: Dedicated page for configuring and executing bulk updates
- JavaScript for managing checkbox selections and navigation

## Security Considerations
- All operations require seller authentication
- Store ownership is verified for all products
- Input validation prevents SQL injection
- URL encoding prevents XSS attacks
- Anti-forgery tokens protect against CSRF attacks

## Performance Optimizations
- HashSet used for efficient product ID lookups in large datasets
- Single database query for fetching products
- Batch updates executed in a single transaction
- Efficient LINQ queries with minimal database roundtrips

## Examples

### Example 1: Increase all product prices by 10%
1. Select products
2. Choose "Price" as update type
3. Select "Increase by percentage"
4. Enter value: 10
5. Preview shows: Product A: $100.00 → $110.00, Product B: $50.00 → $55.00
6. Confirm to execute

### Example 2: Set stock to 100 for low-stock items
1. Select products with low stock
2. Choose "Stock" as update type
3. Select "Set to value"
4. Enter value: 100
5. Preview shows: Product A: 5 → 100, Product B: 12 → 100
6. Confirm to execute

### Example 3: Decrease prices by $5 (with validation error)
1. Select products including one priced at $3
2. Choose "Price" as update type
3. Select "Decrease by amount"
4. Enter value: 5
5. Preview shows:
   - Product A: $20.00 → $15.00 (Valid)
   - Product B: $3.00 → $-2.00 (Invalid - Price must be greater than zero)
6. Only valid products will be updated when confirmed

## Benefits
- **Time Savings**: Update hundreds of products in seconds instead of editing each individually
- **Error Prevention**: Preview and validation catch mistakes before they're committed
- **Audit Trail**: Complete logging for accountability and potential rollback analysis
- **Flexibility**: Multiple operation types support various business scenarios
- **Safety**: Validation ensures data integrity is maintained

## Future Enhancements (Not in this PR)
- Background job processing for very large product sets (1000+ products)
- Scheduled bulk updates (e.g., automatic price adjustments at specific times)
- Rollback functionality to undo bulk updates
- Export/import bulk update configurations
- Filtering products before bulk update (by category, status, price range, etc.)
