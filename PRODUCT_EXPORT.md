# Product Export Feature

## Overview

The product export feature allows sellers to download their product catalog in CSV or Excel format for external analysis, reporting, or bulk editing. This feature complements the existing product import functionality and supports round-trip data editing.

## How to Use

### Step 1: Access the Export Function

1. Log in as a seller
2. Navigate to **Products** from the seller dashboard
3. Click the **Export** button in the top-right corner

### Step 2: Select Export Options

On the export page, you can choose:

#### Export Format
- **CSV (Comma-Separated Values)**: Compatible with Excel, Google Sheets, and most spreadsheet applications
- **Excel (.xlsx)**: Microsoft Excel native format with formatting

#### Products to Export
If you selected products on the Products page before clicking Export:
- **Selected Products Only**: Export only the products you selected
- **All Products**: Export all active and draft products (excluding archived)

If no products were selected:
- All active and draft products will be exported automatically

### Step 3: Download the Export

1. Click **Download Export**
2. Your browser will download the file with a name like:
   - `products_export_20231201_143025_utc.csv` (CSV format)
   - `products_export_20231201_143025_utc.xlsx` (Excel format)

## Exported Data

The export file includes the following columns (matching the import format):

| Column | Description | Example |
|--------|-------------|---------|
| SKU | Stock Keeping Unit | "PROD-001" |
| Title | Product title | "Blue Cotton T-Shirt" |
| Description | Product description | "Comfortable cotton..." |
| Price | Product price in USD | 19.99 |
| Stock | Stock quantity | 100 |
| Category | Product category | "Clothing" |
| Weight | Weight in kilograms | 0.25 |
| Length | Length in centimeters | 30 |
| Width | Width in centimeters | 20 |
| Height | Height in centimeters | 2 |
| ShippingMethods | Available shipping methods | "Standard;Express" |

## Export Behavior

### Included Products
- ✅ Products with status: **Active** or **Draft**
- ❌ Products with status: **Archived** (excluded)

### Data Accuracy
- Export reflects the **current state** of your catalog at the time of export
- All product fields are included with their latest values
- Numeric values are formatted consistently:
  - Prices: 2 decimal places (e.g., 19.99)
  - Dimensions: 2 decimal places (e.g., 10.50)

### Seller Boundaries
- You can **only export your own products**
- Products from other stores are never included
- The system enforces this automatically for security

## Round-Trip Editing

The export format is **100% compatible** with the import format, enabling round-trip editing:

1. **Export** your products to CSV or Excel
2. **Edit** the file in your preferred spreadsheet application
3. **Import** the modified file back to MercatoApp
4. Products with matching SKUs will be updated; new rows will create new products

This workflow is ideal for:
- Bulk price updates
- Stock adjustments across many products
- Updating product descriptions or categories
- Adding new products in batches

## CSV Special Characters

When exporting to CSV, the system automatically handles special characters:
- **Commas** in descriptions are preserved (text is quoted)
- **Quotes** in text are escaped properly
- **Newlines** in descriptions are maintained
- No risk of CSV injection attacks

Example:
```csv
SKU,Title,Description,Price,Stock,Category
PROD-001,"Sample Product","This has a comma, quote"", and newline
in it",19.99,100,Electronics
```

## Excel Format Features

When exporting to Excel (.xlsx):
- Header row is **bold** with a gray background
- Columns are **auto-fitted** for easy reading
- Numeric values maintain proper data types
- File is ready to open in Excel, Numbers, or Google Sheets

## Use Cases

### Reporting and Analysis
- Download your catalog to create custom reports
- Analyze product performance in Excel or BI tools
- Share product listings with partners or marketplaces

### Inventory Management
- Export current stock levels
- Update stock in bulk using spreadsheet formulas
- Re-import to sync inventory

### Catalog Backup
- Create periodic backups of your product catalog
- Maintain historical snapshots
- Restore products if needed

### Bulk Editing
- Export products for a category
- Update prices or descriptions in bulk
- Re-import to apply changes

## Performance Considerations

- **No limits** on the number of products exported
- Exports are generated **immediately** (no background processing)
- Large catalogs (1000+ products) export in seconds
- Downloaded files are compressed efficiently

## Security and Privacy

- ✅ Only **your products** are exported
- ✅ No sensitive customer data is included
- ✅ Secure download via HTTPS
- ✅ Authorization enforced on every export
- ✅ All exports are logged for audit purposes

## Troubleshooting

### "No products found to export"
- You may have no active or draft products
- All your products might be archived
- Try creating or restoring products first

### File won't open in Excel
- Ensure you selected the correct format (CSV vs Excel)
- Try downloading the file again
- Check that your browser didn't block the download

### Missing products in export
- Archived products are **intentionally excluded**
- Check product status on the Products page
- Restore archived products if you need to export them

## API Details

For developers integrating with the export feature:

### Service Interface
```csharp
public interface IProductExportService
{
    Task<ProductExportResult> ExportToCsvAsync(int storeId, List<int>? productIds = null);
    Task<ProductExportResult> ExportToExcelAsync(int storeId, List<int>? productIds = null);
}
```

### Export Result
```csharp
public class ProductExportResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; }
    public byte[]? FileData { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}
```

## Related Features

- **[Product Import](PRODUCT_IMPORT.md)**: Import products from CSV/Excel files
- **Bulk Update**: Update multiple products at once using the UI
- **Product Management**: Create, edit, and manage individual products

## Support

If you encounter issues with the export feature:
1. Check the troubleshooting section above
2. Review the Products page for correct product status
3. Contact support with your export timestamp for assistance
