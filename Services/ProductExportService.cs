using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Text;

namespace MercatoApp.Services;

/// <summary>
/// Result of generating a product export file.
/// </summary>
public class ProductExportResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public byte[]? FileData { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}

/// <summary>
/// Interface for product export service.
/// </summary>
public interface IProductExportService
{
    /// <summary>
    /// Exports products to CSV format.
    /// </summary>
    /// <param name="storeId">The store ID to export products from.</param>
    /// <param name="productIds">Optional list of product IDs to export. If null, exports all products.</param>
    Task<ProductExportResult> ExportToCsvAsync(int storeId, List<int>? productIds = null);

    /// <summary>
    /// Exports products to Excel format.
    /// </summary>
    /// <param name="storeId">The store ID to export products from.</param>
    /// <param name="productIds">Optional list of product IDs to export. If null, exports all products.</param>
    Task<ProductExportResult> ExportToExcelAsync(int storeId, List<int>? productIds = null);
}

/// <summary>
/// Service for exporting product catalogs to CSV/Excel files.
/// </summary>
public class ProductExportService : IProductExportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductExportService> _logger;

    // Column headers matching import format
    private const string COL_SKU = "SKU";
    private const string COL_TITLE = "Title";
    private const string COL_DESCRIPTION = "Description";
    private const string COL_PRICE = "Price";
    private const string COL_STOCK = "Stock";
    private const string COL_CATEGORY = "Category";
    private const string COL_WEIGHT = "Weight";
    private const string COL_LENGTH = "Length";
    private const string COL_WIDTH = "Width";
    private const string COL_HEIGHT = "Height";
    private const string COL_SHIPPING_METHODS = "ShippingMethods";

    public ProductExportService(
        ApplicationDbContext context,
        ILogger<ProductExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProductExportResult> ExportToCsvAsync(int storeId, List<int>? productIds = null)
    {
        var result = new ProductExportResult();

        try
        {
            var products = await GetProductsForExportAsync(storeId, productIds);

            if (products.Count == 0)
            {
                result.Errors.Add("No products found to export.");
                return result;
            }

            var csv = new StringBuilder();

            // Header row
            csv.AppendLine($"{COL_SKU},{COL_TITLE},{COL_DESCRIPTION},{COL_PRICE},{COL_STOCK},{COL_CATEGORY},{COL_WEIGHT},{COL_LENGTH},{COL_WIDTH},{COL_HEIGHT},{COL_SHIPPING_METHODS}");

            // Data rows
            foreach (var product in products)
            {
                csv.AppendLine(FormatCsvRow(
                    EscapeCsvValue(product.Sku),
                    EscapeCsvValue(product.Title),
                    EscapeCsvValue(product.Description),
                    product.Price.ToString("F2", CultureInfo.InvariantCulture),
                    product.Stock.ToString(),
                    EscapeCsvValue(product.Category),
                    product.Weight?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                    product.Length?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                    product.Width?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                    product.Height?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                    EscapeCsvValue(product.ShippingMethods)
                ));
            }

            var fileName = $"products_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            result.FileData = Encoding.UTF8.GetBytes(csv.ToString());
            result.FileName = fileName;
            result.ContentType = "text/csv";
            result.Success = true;

            _logger.LogInformation("Exported {Count} products to CSV for store {StoreId}", products.Count, storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting products to CSV for store {StoreId}", storeId);
            result.Errors.Add($"An error occurred while exporting: {ex.Message}");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ProductExportResult> ExportToExcelAsync(int storeId, List<int>? productIds = null)
    {
        var result = new ProductExportResult();

        try
        {
            var products = await GetProductsForExportAsync(storeId, productIds);

            if (products.Count == 0)
            {
                result.Errors.Add("No products found to export.");
                return result;
            }

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Products");

            // Header row
            worksheet.Cells[1, 1].Value = COL_SKU;
            worksheet.Cells[1, 2].Value = COL_TITLE;
            worksheet.Cells[1, 3].Value = COL_DESCRIPTION;
            worksheet.Cells[1, 4].Value = COL_PRICE;
            worksheet.Cells[1, 5].Value = COL_STOCK;
            worksheet.Cells[1, 6].Value = COL_CATEGORY;
            worksheet.Cells[1, 7].Value = COL_WEIGHT;
            worksheet.Cells[1, 8].Value = COL_LENGTH;
            worksheet.Cells[1, 9].Value = COL_WIDTH;
            worksheet.Cells[1, 10].Value = COL_HEIGHT;
            worksheet.Cells[1, 11].Value = COL_SHIPPING_METHODS;

            // Style header row
            using (var range = worksheet.Cells[1, 1, 1, 11])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data rows
            int row = 2;
            foreach (var product in products)
            {
                worksheet.Cells[row, 1].Value = product.Sku;
                worksheet.Cells[row, 2].Value = product.Title;
                worksheet.Cells[row, 3].Value = product.Description;
                worksheet.Cells[row, 4].Value = product.Price;
                worksheet.Cells[row, 5].Value = product.Stock;
                worksheet.Cells[row, 6].Value = product.Category;
                worksheet.Cells[row, 7].Value = product.Weight;
                worksheet.Cells[row, 8].Value = product.Length;
                worksheet.Cells[row, 9].Value = product.Width;
                worksheet.Cells[row, 10].Value = product.Height;
                worksheet.Cells[row, 11].Value = product.ShippingMethods;
                row++;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            var fileName = $"products_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            result.FileData = package.GetAsByteArray();
            result.FileName = fileName;
            result.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            result.Success = true;

            _logger.LogInformation("Exported {Count} products to Excel for store {StoreId}", products.Count, storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting products to Excel for store {StoreId}", storeId);
            result.Errors.Add($"An error occurred while exporting: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Gets products for export, optionally filtered by product IDs.
    /// </summary>
    private async Task<List<Product>> GetProductsForExportAsync(int storeId, List<int>? productIds = null)
    {
        var query = _context.Products
            .Where(p => p.StoreId == storeId && p.Status != ProductStatus.Archived)
            .AsQueryable();

        if (productIds != null && productIds.Count > 0)
        {
            query = query.Where(p => productIds.Contains(p.Id));
        }

        return await query
            .OrderBy(p => p.Title)
            .ToListAsync();
    }

    /// <summary>
    /// Escapes a CSV value by wrapping it in quotes if it contains special characters.
    /// </summary>
    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // If value contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Formats a CSV row from individual values.
    /// </summary>
    private static string FormatCsvRow(params string[] values)
    {
        return string.Join(",", values);
    }
}
