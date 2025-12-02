# MercatoApp Logistics CSV Export - Documentation for External Partners

## Overview

This document describes the CSV export format for order and shipping data from MercatoApp. This export is designed for integration with external logistics systems, shipping partners, and warehouse management systems (WMS).

## Export Availability

The CSV export is available to all sellers through the Seller Panel at:
- **Path:** `/Seller/Orders` → Click "Export Orders"
- **Formats:** CSV (`.csv`) and Excel (`.xlsx`)
- **Authorization:** Sellers can only export their own store's orders

## CSV File Structure

### File Format Details
- **Encoding:** UTF-8
- **Delimiter:** Comma (`,`)
- **Line Ending:** CRLF (`\r\n`)
- **Header Row:** First row contains column names
- **Special Characters:** Values containing commas, quotes, or newlines are wrapped in double quotes
- **Quote Escaping:** Double quotes within values are escaped as `""`
- **Date/Time Format:** ISO 8601 format in UTC (`yyyy-MM-dd HH:mm:ss`)
- **Currency Format:** Decimal with 2 decimal places (e.g., `125.50`)

### Column Reference

The CSV export contains the following columns in order:

| Column # | Column Name | Description | Example | Required |
|----------|-------------|-------------|---------|----------|
| 1 | Sub-Order Number | Unique seller sub-order identifier | ORD-20241202-12345-1 | Yes |
| 2 | Parent Order Number | Parent order reference | ORD-20241202-12345 | Yes |
| 3 | Created Date | Order creation timestamp (UTC) | 2024-12-02 14:30:00 | Yes |
| 4 | Status | Order status | Paid, Preparing, Shipped | Yes |
| 5 | Buyer Name | Full name of recipient | John Smith | Yes |
| 6 | Buyer Email | Buyer contact email | john.smith@example.com | Yes |
| 7 | Buyer Phone | Contact phone number | +1-555-123-4567 | Yes |
| 8 | Address Line 1 | Primary street address | 123 Main Street | Yes |
| 9 | Address Line 2 | Secondary address info | Apt 4B | Optional |
| 10 | City | Delivery city | New York | Yes |
| 11 | State/Province | State or province | NY | Optional |
| 12 | Postal Code | ZIP or postal code | 10001 | Yes |
| 13 | Country Code | ISO 3166-1 alpha-2 code | US | Yes |
| 14 | Delivery Instructions | Special delivery notes | Leave at front door | Optional |
| 15 | Total Amount | Total order value | 125.50 | Yes |
| 16 | Shipping Cost | Shipping fee | 10.00 | Yes |
| 17 | Subtotal | Items subtotal | 115.50 | Yes |
| 18 | Shipping Method | Selected shipping method | Standard Shipping | Yes |
| 19 | Tracking Number | Package tracking number | 1Z999AA10123456784 | Optional |
| 20 | Carrier Name | Shipping carrier | UPS | Optional |
| 21 | Items Count | Number of distinct items | 3 | Yes |
| 22 | Items Details | Detailed item list (see format below) | Product: Widget, Qty: 2, Price: $25.00; ... | Yes |

### Items Details Format

The `Items Details` column contains a semicolon-separated list of all items in the order. Each item is formatted as:

```
Product: {ProductTitle}, Variant: {VariantDescription}, Qty: {Quantity}, Price: ${UnitPrice}
```

**Example:**
```
Product: Blue T-Shirt, Variant: Size: L, Color: Blue, Qty: 2, Price: $25.00; Product: Red Cap, Qty: 1, Price: $15.00
```

**Notes:**
- Multiple items are separated by semicolons (`;`)
- If a product has no variant, the "Variant:" part is omitted
- Prices are formatted with 2 decimal places
- Commas within product names or variants are properly escaped

### Order Status Values

The `Status` column can contain the following values:

| Status | Description | Logistics Action |
|--------|-------------|------------------|
| New | Order just placed | None yet |
| Paid | Payment confirmed | Ready to prepare |
| Preparing | Being prepared for shipment | Package items |
| Shipped | Package shipped | In transit |
| Delivered | Successfully delivered | Complete |
| Cancelled | Order cancelled | Do not ship |
| Refunded | Payment refunded | Return processed |

### Country Codes

The `Country Code` column uses ISO 3166-1 alpha-2 codes:

| Code | Country |
|------|---------|
| US | United States |
| CA | Canada |
| GB | United Kingdom |
| DE | Germany |
| FR | France |

*(Additional country codes follow ISO 3166-1 alpha-2 standard)*

## Export Filtering

Sellers can filter the export by:

- **Order Status:** Filter by one or more statuses (e.g., only "Paid" and "Preparing")
- **Date Range:** Filter by order creation date (from/to dates)
- **Buyer Email:** Search by buyer email (partial match supported)

**Note:** Filters are applied server-side before export generation. Empty exports (no matching orders) will return an error message instead of generating a file.

## Sample CSV Export

```csv
Sub-Order Number,Parent Order Number,Created Date,Status,Buyer Name,Buyer Email,Buyer Phone,Address Line 1,Address Line 2,City,State/Province,Postal Code,Country Code,Delivery Instructions,Total Amount,Shipping Cost,Subtotal,Shipping Method,Tracking Number,Carrier Name,Items Count,Items Details
ORD-20241202-12345-1,ORD-20241202-12345,2024-12-02 14:30:00,Paid,John Smith,john.smith@example.com,+1-555-123-4567,123 Main Street,Apt 4B,New York,NY,10001,US,Leave at front door,125.50,10.00,115.50,Standard Shipping,1Z999AA10123456784,UPS,2,"Product: Blue T-Shirt, Variant: Size: L, Color: Blue, Qty: 2, Price: $25.00; Product: Red Cap, Qty: 1, Price: $15.00"
ORD-20241202-12346-1,ORD-20241202-12346,2024-12-02 15:45:00,Preparing,Jane Doe,jane.doe@example.com,+1-555-987-6543,456 Oak Avenue,,Los Angeles,CA,90001,US,,85.00,8.00,77.00,Express Shipping,,,1,"Product: Green Jacket, Variant: Size: M, Qty: 1, Price: $77.00"
```

## Excel Format

The Excel export (`.xlsx`) contains the same data with additional formatting:

- **Header row:** Bold text with gray background
- **Currency columns:** Formatted with currency symbol (`$#,##0.00`)
- **Auto-fit columns:** Columns automatically sized to content
- **Single worksheet:** Named "Orders"

## Data Privacy & Security

- **Access Control:** Sellers can only export orders from their own store
- **Personal Data:** Export contains buyer contact information and delivery addresses
- **GDPR/Privacy:** Handle exported data according to applicable privacy regulations
- **Secure Storage:** Store exported files securely and delete after use
- **Data Retention:** Follow your organization's data retention policies

## Performance Characteristics

- **Export Volume:** Synchronous generation suitable for up to ~10,000 orders
- **Large Exports:** For very large exports (>10,000 orders), consider using date range filters
- **Response Time:** Export generation typically completes in <10 seconds for moderate datasets
- **File Size:** Approximate file size is 1-2 KB per order row

## Integration Guidelines

### Importing into Logistics Systems

**WMS/ERP Systems:**
1. Map CSV columns to your system's import fields
2. Handle optional fields (Address Line 2, State/Province, Delivery Instructions, Tracking Number, Carrier Name)
3. Validate country codes against your supported regions
4. Parse `Items Details` column for line-item integration
5. Use `Sub-Order Number` as the unique order identifier

**Shipping Label Systems:**
1. Extract delivery address fields (columns 8-13)
2. Use `Buyer Phone` for carrier contact requirements
3. Reference `Shipping Method` for service level selection
4. Update MercatoApp with `Tracking Number` and `Carrier Name` after label generation

**Common Mapping Examples:**

| MercatoApp Column | ShipStation Field | EasyPost Field |
|-------------------|-------------------|----------------|
| Address Line 1 | address1 | street1 |
| Address Line 2 | address2 | street2 |
| City | city | city |
| State/Province | state | state |
| Postal Code | postal_code | zip |
| Country Code | country_code | country |
| Buyer Phone | phone | phone |

### Parsing Items Details

To parse the `Items Details` column:

```python
# Python example
items_details = "Product: Blue T-Shirt, Variant: Size: L, Color: Blue, Qty: 2, Price: $25.00; Product: Red Cap, Qty: 1, Price: $15.00"

# Split by semicolon
items = items_details.split("; ")

for item_str in items:
    # Parse each item
    parts = {}
    for segment in item_str.split(", "):
        key, value = segment.split(": ", 1)
        parts[key] = value
    
    product_name = parts["Product"]
    variant = parts.get("Variant", "")
    quantity = int(parts["Qty"])
    price = float(parts["Price"].replace("$", ""))
```

## API Access (Future Enhancement)

Currently, order exports are available only through the seller web interface. A future enhancement may provide:
- REST API endpoint for programmatic export
- Webhook notifications for new orders
- Scheduled automatic exports

## Support & Contact

For technical questions about the CSV export format or integration assistance:
- **Documentation:** Refer to this document
- **Support:** Contact your MercatoApp account manager
- **Feature Requests:** Submit via the platform feedback form

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0 | 2024-12-02 | Added logistics fields: buyer phone, full address, delivery instructions, carrier name, detailed items list |
| 1.0 | 2024-11-15 | Initial export with basic order and buyer information |

---

**Document Version:** 2.0  
**Last Updated:** 2024-12-02  
**Status:** Current
