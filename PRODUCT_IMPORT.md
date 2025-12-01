# Product Import Feature

## Overview

The product import feature allows sellers to quickly onboard or update many products at once by uploading CSV or Excel files. This feature supports:

- **File Formats**: CSV (.csv), Excel (.xlsx, .xls)
- **Operations**: Create new products, update existing products by SKU
- **Validation**: Real-time validation of required fields, data types, and business rules
- **Error Reporting**: Detailed error messages per row with downloadable reports
- **Import History**: Track all import jobs with statistics and status

## How to Use

### Step 1: Prepare Your Import File

Download the template file from the import page or create a file with the following columns:

#### Required Columns

| Column | Type | Description | Example |
|--------|------|-------------|---------|
| Title | Text (max 200 chars) | Product title | "Blue Cotton T-Shirt" |
| Price | Decimal | Product price in USD | 19.99 |
| Stock | Integer | Stock quantity | 100 |

#### Optional Columns

| Column | Type | Description | Example |
|--------|------|-------------|---------|
| SKU | Text (max 100 chars) | Stock Keeping Unit - used for updates | "PROD-001" |
| Description | Text (max 2000 chars) | Product description | "Comfortable cotton..." |
| Category | Text (max 100 chars) | Product category | "Clothing" |
| Weight | Decimal | Weight in kilograms | 0.25 |
| Length | Decimal | Length in centimeters | 30 |
| Width | Decimal | Width in centimeters | 20 |
| Height | Decimal | Height in centimeters | 2 |
| ShippingMethods | Text | Comma or semicolon separated | "Standard;Express" |

### Step 2: Upload and Preview

1. Navigate to **Products > Import** from the seller dashboard
2. Click **Choose File** and select your CSV or Excel file
3. Click **Upload and Validate**
4. Review the preview showing:
   - Total rows to be imported
   - Number of new products vs. updates
   - Any validation errors

### Step 3: Confirm and Import

If validation passes:
1. Review the summary statistics
2. Click **Confirm and Import** to proceed
3. The import will process in the background
4. View the **Import History** to track progress

If validation fails:
1. Review the error messages for each failed row
2. Fix the errors in your file
3. Upload the corrected file again

### Step 4: Review Results

After import completes:
1. Go to **Products > Import History**
2. View the import job details:
   - Total products processed
   - Number created/updated/failed
   - Download error report if needed
3. View individual imported products

## Import Behavior

### Creating Products

Products **without** a SKU or with a SKU that doesn't exist in your store will be created as **new products** with status "Draft".

### Updating Products

Products **with** a SKU that matches an existing product in your store will **update** that product with the new data from the file.

### SKU Uniqueness

- SKUs must be unique within your store
- Duplicate SKUs within the same import file will be rejected
- Products without SKUs can be imported (but cannot be updated via import later)

### Data Validation

All data is validated before import:
- Required fields must be present
- Numeric fields must be valid numbers
- Text fields must not exceed maximum lengths
- Prices must be positive
- Stock cannot be negative
- Dimensions must be within allowed ranges

## File Format Examples

### CSV Example

```csv
SKU,Title,Description,Price,Stock,Category,Weight,Length,Width,Height,ShippingMethods
SHIRT-001,Blue T-Shirt,Comfortable cotton shirt,19.99,100,Clothing,0.25,30,20,2,Standard;Express
SHOES-002,Red Sneakers,Athletic running shoes,89.99,50,Footwear,0.8,35,25,15,Standard
MOUSE-003,Wireless Mouse,Ergonomic wireless mouse,29.99,75,Electronics,0.15,12,7,5,Standard;Express;Overnight
```

### Excel Example

Same column structure as CSV, but in Excel format (.xlsx or .xls).

## Performance Considerations

- **File Size Limit**: 10 MB maximum
- **Batch Processing**: Products are saved in batches of 100 for optimal performance
- **Large Imports**: Imports run in the background, allowing you to continue working
- **Recommended**: Split very large catalogs into multiple files of 1000-2000 products each

## Error Handling

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| "Title is required" | Missing or empty title | Add a title for the product |
| "Price must be a valid number" | Non-numeric price value | Use decimal format (e.g., 19.99) |
| "Stock must be a valid integer" | Non-integer stock value | Use whole numbers only |
| "Duplicate SKU found" | Same SKU appears multiple times | Ensure each SKU is unique |
| "Price exceeds maximum" | Price too high | Reduce price to below $1,000,000 |

### Error Reports

When an import has errors:
1. Download the error report from Import History
2. The report is a CSV file with:
   - Row number
   - SKU
   - Title  
   - Error message
3. Fix the errors and re-import

## Best Practices

1. **Start Small**: Test with a small file (5-10 products) first
2. **Use SKUs**: Always include SKUs for products you might update later
3. **Validate Data**: Check your data in Excel/CSV before uploading
4. **Backup**: Keep a copy of your original import file
5. **Review Results**: Always check import results in Import History
6. **Incremental Updates**: For updates, include only products that changed

## API Details

The import feature uses the following service:

- **Service**: `IProductImportService`
- **Methods**:
  - `ParseFileAsync`: Parses CSV/Excel files
  - `ValidateImportAsync`: Validates import data
  - `CreateImportJobAsync`: Creates import job in pending status
  - `ExecuteImportAsync`: Executes the import
  - `GetImportJobsAsync`: Gets import history
  - `GetImportJobAsync`: Gets specific job details
  - `GenerateErrorReportAsync`: Generates error report CSV

## Database Schema

### ProductImportJob

Tracks import jobs with status and statistics.

### ProductImportResult

Tracks individual row results (success/failure, errors, created product ID).

### Product

Updated to include SKU field for unique identification within a store.

## Security Considerations

- File uploads are limited to 10 MB
- Only CSV and Excel files are accepted
- All user input is validated and sanitized
- Import jobs are scoped to the authenticated seller's store
- No script execution from uploaded files
