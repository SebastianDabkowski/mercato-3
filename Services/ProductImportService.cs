using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Text;

namespace MercatoApp.Services;

/// <summary>
/// Data for a single product row in an import file.
/// </summary>
public class ProductImportRowData
{
    public int RowNumber { get; set; }
    public string? Sku { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Price { get; set; }
    public string? Stock { get; set; }
    public string? Category { get; set; }
    public string? Weight { get; set; }
    public string? Length { get; set; }
    public string? Width { get; set; }
    public string? Height { get; set; }
    public string? ShippingMethods { get; set; }
}

/// <summary>
/// Result of parsing an import file.
/// </summary>
public class ParsedImportFile
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ProductImportRowData> Rows { get; set; } = new();
    public string FileType { get; set; } = string.Empty;
}

/// <summary>
/// Result of validating import data.
/// </summary>
public class ImportValidationResult
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int NewProducts { get; set; }
    public int ExistingProducts { get; set; }
    public List<ProductImportResult> ValidationResults { get; set; } = new();
}

/// <summary>
/// Result of executing an import job.
/// </summary>
public class ImportExecutionResult
{
    public bool Success { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int FailedCount { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Interface for product import service.
/// </summary>
public interface IProductImportService
{
    /// <summary>
    /// Parses an uploaded CSV or Excel file.
    /// </summary>
    Task<ParsedImportFile> ParseFileAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Validates the parsed import data and creates a preview.
    /// </summary>
    Task<ImportValidationResult> ValidateImportAsync(int storeId, List<ProductImportRowData> rows);

    /// <summary>
    /// Creates a new import job in pending status with validation results.
    /// </summary>
    Task<ProductImportJob> CreateImportJobAsync(int storeId, int userId, string fileName, string fileType, ImportValidationResult validationResult);

    /// <summary>
    /// Executes an import job, creating and updating products.
    /// </summary>
    Task<ImportExecutionResult> ExecuteImportAsync(int jobId);

    /// <summary>
    /// Gets all import jobs for a store.
    /// </summary>
    Task<List<ProductImportJob>> GetImportJobsAsync(int storeId);

    /// <summary>
    /// Gets a specific import job with results.
    /// </summary>
    Task<ProductImportJob?> GetImportJobAsync(int jobId, int storeId);

    /// <summary>
    /// Gets the error results for a job as a downloadable report.
    /// </summary>
    Task<string> GenerateErrorReportAsync(int jobId, int storeId);
}

/// <summary>
/// Service for importing product catalogs from CSV/Excel files.
/// </summary>
public class ProductImportService : IProductImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductImportService> _logger;

    // CSV column headers (case-insensitive)
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

    public ProductImportService(
        ApplicationDbContext context,
        ILogger<ProductImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ParsedImportFile> ParseFileAsync(Stream fileStream, string fileName)
    {
        var result = new ParsedImportFile();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension == ".csv")
        {
            result.FileType = "CSV";
            return await ParseCsvAsync(fileStream);
        }
        else if (extension == ".xlsx" || extension == ".xls")
        {
            result.FileType = "Excel";
            return ParseExcelAsync(fileStream);
        }
        else
        {
            result.Errors.Add("Unsupported file type. Please upload a CSV or Excel (.xlsx, .xls) file.");
            return result;
        }
    }

    private async Task<ParsedImportFile> ParseCsvAsync(Stream fileStream)
    {
        var result = new ParsedImportFile { FileType = "CSV" };

        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            var headerLine = await reader.ReadLineAsync();
            
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                result.Errors.Add("The CSV file is empty or missing a header row.");
                return result;
            }

            var headers = ParseCsvLine(headerLine);
            var columnMap = MapColumns(headers);

            if (!columnMap.ContainsKey(COL_TITLE) || !columnMap.ContainsKey(COL_PRICE) || !columnMap.ContainsKey(COL_STOCK))
            {
                result.Errors.Add("Required columns are missing. CSV must contain at least: Title, Price, Stock");
                return result;
            }

            int rowNumber = 0;
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                rowNumber++;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue; // Skip empty lines
                }

                var values = ParseCsvLine(line);
                var row = new ProductImportRowData
                {
                    RowNumber = rowNumber,
                    Sku = GetColumnValue(values, columnMap, COL_SKU),
                    Title = GetColumnValue(values, columnMap, COL_TITLE),
                    Description = GetColumnValue(values, columnMap, COL_DESCRIPTION),
                    Price = GetColumnValue(values, columnMap, COL_PRICE),
                    Stock = GetColumnValue(values, columnMap, COL_STOCK),
                    Category = GetColumnValue(values, columnMap, COL_CATEGORY),
                    Weight = GetColumnValue(values, columnMap, COL_WEIGHT),
                    Length = GetColumnValue(values, columnMap, COL_LENGTH),
                    Width = GetColumnValue(values, columnMap, COL_WIDTH),
                    Height = GetColumnValue(values, columnMap, COL_HEIGHT),
                    ShippingMethods = GetColumnValue(values, columnMap, COL_SHIPPING_METHODS)
                };

                result.Rows.Add(row);
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing CSV file");
            result.Errors.Add($"Error parsing CSV file: {ex.Message}");
        }

        return result;
    }

    private ParsedImportFile ParseExcelAsync(Stream fileStream)
    {
        var result = new ParsedImportFile { FileType = "Excel" };

        try
        {
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                result.Errors.Add("The Excel file contains no worksheets.");
                return result;
            }

            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount < 2)
            {
                result.Errors.Add("The Excel file is empty or missing data rows.");
                return result;
            }

            // Read header row
            var headers = new List<string>();
            var colCount = worksheet.Dimension?.Columns ?? 0;
            for (int col = 1; col <= colCount; col++)
            {
                headers.Add(worksheet.Cells[1, col].Text?.Trim() ?? string.Empty);
            }

            var columnMap = MapColumns(headers.ToArray());

            if (!columnMap.ContainsKey(COL_TITLE) || !columnMap.ContainsKey(COL_PRICE) || !columnMap.ContainsKey(COL_STOCK))
            {
                result.Errors.Add("Required columns are missing. Excel must contain at least: Title, Price, Stock");
                return result;
            }

            // Read data rows
            for (int row = 2; row <= rowCount; row++)
            {
                var values = new List<string>();
                for (int col = 1; col <= colCount; col++)
                {
                    values.Add(worksheet.Cells[row, col].Text?.Trim() ?? string.Empty);
                }

                var rowData = new ProductImportRowData
                {
                    RowNumber = row - 1, // Exclude header
                    Sku = GetColumnValue(values.ToArray(), columnMap, COL_SKU),
                    Title = GetColumnValue(values.ToArray(), columnMap, COL_TITLE),
                    Description = GetColumnValue(values.ToArray(), columnMap, COL_DESCRIPTION),
                    Price = GetColumnValue(values.ToArray(), columnMap, COL_PRICE),
                    Stock = GetColumnValue(values.ToArray(), columnMap, COL_STOCK),
                    Category = GetColumnValue(values.ToArray(), columnMap, COL_CATEGORY),
                    Weight = GetColumnValue(values.ToArray(), columnMap, COL_WEIGHT),
                    Length = GetColumnValue(values.ToArray(), columnMap, COL_LENGTH),
                    Width = GetColumnValue(values.ToArray(), columnMap, COL_WIDTH),
                    Height = GetColumnValue(values.ToArray(), columnMap, COL_HEIGHT),
                    ShippingMethods = GetColumnValue(values.ToArray(), columnMap, COL_SHIPPING_METHODS)
                };

                result.Rows.Add(rowData);
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Excel file");
            result.Errors.Add($"Error parsing Excel file: {ex.Message}");
        }

        return result;
    }

    private string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString().Trim());
        return values.ToArray();
    }

    private Dictionary<string, int> MapColumns(string[] headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i].Trim();
            if (!string.IsNullOrEmpty(header))
            {
                map[header] = i;
            }
        }
        return map;
    }

    private string? GetColumnValue(string[] values, Dictionary<string, int> columnMap, string columnName)
    {
        if (columnMap.TryGetValue(columnName, out int index) && index < values.Length)
        {
            var value = values[index]?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<ImportValidationResult> ValidateImportAsync(int storeId, List<ProductImportRowData> rows)
    {
        var result = new ImportValidationResult
        {
            TotalRows = rows.Count
        };

        // Check for duplicate SKUs within the import file
        var skuGroups = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Sku))
            .GroupBy(r => r.Sku, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (skuGroups.Any())
        {
            var duplicateSkus = string.Join(", ", skuGroups.Select(g => g.Key));
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Sku) && skuGroups.Any(g => g.Key!.Equals(row.Sku, StringComparison.OrdinalIgnoreCase)))
                {
                    var validationResult = new ProductImportResult
                    {
                        RowNumber = row.RowNumber,
                        Sku = row.Sku,
                        Title = row.Title,
                        Success = false,
                        ErrorMessage = $"Duplicate SKU '{row.Sku}' found in import file. SKUs must be unique."
                    };
                    result.ValidationResults.Add(validationResult);
                }
            }
            
            // Process remaining rows that don't have duplicate SKUs
            rows = rows.Where(r => string.IsNullOrWhiteSpace(r.Sku) || 
                !skuGroups.Any(g => g.Key!.Equals(r.Sku, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        // Load existing products by SKU for this store
        var skusToCheck = rows.Where(r => !string.IsNullOrWhiteSpace(r.Sku)).Select(r => r.Sku!).Distinct().ToList();
        var existingProducts = await _context.Products
            .Where(p => p.StoreId == storeId && p.Sku != null && skusToCheck.Contains(p.Sku))
            .ToDictionaryAsync(p => p.Sku!, p => p);

        foreach (var row in rows)
        {
            var validationResult = new ProductImportResult
            {
                RowNumber = row.RowNumber,
                Sku = row.Sku,
                Title = row.Title,
                Description = row.Description
            };

            var errors = new List<string>();

            // Parse and validate required fields
            decimal parsedPrice = 0;
            int parsedStock = 0;

            // Validate required fields
            if (string.IsNullOrWhiteSpace(row.Title))
            {
                errors.Add("Title is required");
            }
            else if (row.Title.Length > ProductService.MaxTitleLength)
            {
                errors.Add($"Title exceeds maximum length of {ProductService.MaxTitleLength}");
            }

            if (string.IsNullOrWhiteSpace(row.Price))
            {
                errors.Add("Price is required");
            }
            else if (!decimal.TryParse(row.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedPrice))
            {
                errors.Add("Price must be a valid number");
            }
            else if (parsedPrice <= 0)
            {
                errors.Add("Price must be greater than zero");
            }
            else if (parsedPrice > ProductService.MaxPrice)
            {
                errors.Add($"Price exceeds maximum of {ProductService.MaxPrice:C}");
            }

            if (string.IsNullOrWhiteSpace(row.Stock))
            {
                errors.Add("Stock is required");
            }
            else if (!int.TryParse(row.Stock, out parsedStock))
            {
                errors.Add("Stock must be a valid integer");
            }
            else if (parsedStock < 0)
            {
                errors.Add("Stock cannot be negative");
            }

            // Store parsed values if valid
            if (errors.Count == 0)
            {
                validationResult.Price = parsedPrice;
                validationResult.Stock = parsedStock;
                validationResult.Category = row.Category ?? "Uncategorized";

                // Parse optional dimensions
                if (!string.IsNullOrWhiteSpace(row.Weight) && decimal.TryParse(row.Weight, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedWeight))
                {
                    validationResult.Weight = parsedWeight;
                }

                if (!string.IsNullOrWhiteSpace(row.Length) && decimal.TryParse(row.Length, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedLength))
                {
                    validationResult.Length = parsedLength;
                }

                if (!string.IsNullOrWhiteSpace(row.Width) && decimal.TryParse(row.Width, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedWidth))
                {
                    validationResult.Width = parsedWidth;
                }

                if (!string.IsNullOrWhiteSpace(row.Height) && decimal.TryParse(row.Height, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedHeight))
                {
                    validationResult.Height = parsedHeight;
                }

                validationResult.ShippingMethods = row.ShippingMethods;
            }

            // Validate optional fields
            if (!string.IsNullOrWhiteSpace(row.Description) && row.Description.Length > ProductService.MaxDescriptionLength)
            {
                errors.Add($"Description exceeds maximum length of {ProductService.MaxDescriptionLength}");
            }

            if (!string.IsNullOrWhiteSpace(row.Category) && row.Category.Length > ProductService.MaxCategoryLength)
            {
                errors.Add($"Category exceeds maximum length of {ProductService.MaxCategoryLength}");
            }

            if (!string.IsNullOrWhiteSpace(row.Sku) && row.Sku.Length > 100)
            {
                errors.Add("SKU exceeds maximum length of 100");
            }

            // Validate dimensions ranges
            if (!string.IsNullOrWhiteSpace(row.Weight) && decimal.TryParse(row.Weight, NumberStyles.Any, CultureInfo.InvariantCulture, out var weightVal))
            {
                if (weightVal < 0 || weightVal > ProductService.MaxWeight)
                {
                    errors.Add($"Weight must be between 0 and {ProductService.MaxWeight} kg");
                }
            }
            else if (!string.IsNullOrWhiteSpace(row.Weight))
            {
                errors.Add("Weight must be a valid number");
            }

            if (!string.IsNullOrWhiteSpace(row.Length) && decimal.TryParse(row.Length, NumberStyles.Any, CultureInfo.InvariantCulture, out var lengthVal))
            {
                if (lengthVal < 0 || lengthVal > ProductService.MaxDimension)
                {
                    errors.Add($"Length must be between 0 and {ProductService.MaxDimension} cm");
                }
            }
            else if (!string.IsNullOrWhiteSpace(row.Length))
            {
                errors.Add("Length must be a valid number");
            }

            if (!string.IsNullOrWhiteSpace(row.Width) && decimal.TryParse(row.Width, NumberStyles.Any, CultureInfo.InvariantCulture, out var widthVal))
            {
                if (widthVal < 0 || widthVal > ProductService.MaxDimension)
                {
                    errors.Add($"Width must be between 0 and {ProductService.MaxDimension} cm");
                }
            }
            else if (!string.IsNullOrWhiteSpace(row.Width))
            {
                errors.Add("Width must be a valid number");
            }

            if (!string.IsNullOrWhiteSpace(row.Height) && decimal.TryParse(row.Height, NumberStyles.Any, CultureInfo.InvariantCulture, out var heightVal))
            {
                if (heightVal < 0 || heightVal > ProductService.MaxDimension)
                {
                    errors.Add($"Height must be between 0 and {ProductService.MaxDimension} cm");
                }
            }
            else if (!string.IsNullOrWhiteSpace(row.Height))
            {
                errors.Add("Height must be a valid number");
            }

            // Determine if this is a create or update
            bool isCreate = true;
            if (!string.IsNullOrWhiteSpace(row.Sku) && existingProducts.ContainsKey(row.Sku))
            {
                isCreate = false;
                result.ExistingProducts++;
            }
            else
            {
                result.NewProducts++;
            }

            if (errors.Count > 0)
            {
                validationResult.Success = false;
                validationResult.ErrorMessage = string.Join("; ", errors);
            }
            else
            {
                validationResult.Success = true;
                validationResult.IsCreate = isCreate;
            }

            result.ValidationResults.Add(validationResult);
        }

        result.Success = result.ValidationResults.All(r => r.Success);
        return result;
    }

    /// <inheritdoc />
    public async Task<ProductImportJob> CreateImportJobAsync(int storeId, int userId, string fileName, string fileType, ImportValidationResult validationResult)
    {
        var job = new ProductImportJob
        {
            StoreId = storeId,
            UserId = userId,
            FileName = fileName,
            FileType = fileType,
            Status = ProductImportJobStatus.Pending,
            TotalRows = validationResult.TotalRows,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductImportJobs.Add(job);
        await _context.SaveChangesAsync();

        // Add validation results
        foreach (var validationRow in validationResult.ValidationResults)
        {
            validationRow.JobId = job.Id;
            _context.ProductImportResults.Add(validationRow);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created import job {JobId} for store {StoreId} with {TotalRows} rows", job.Id, storeId, validationResult.TotalRows);

        return job;
    }

    /// <inheritdoc />
    public async Task<ImportExecutionResult> ExecuteImportAsync(int jobId)
    {
        var result = new ImportExecutionResult();

        var job = await _context.ProductImportJobs
            .Include(j => j.Results)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
        {
            result.ErrorMessage = "Import job not found";
            return result;
        }

        if (job.Status != ProductImportJobStatus.Pending)
        {
            result.ErrorMessage = "Import job has already been processed";
            return result;
        }

        try
        {
            job.Status = ProductImportJobStatus.Processing;
            job.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Load existing products by SKU
            var skus = job.Results.Where(r => !string.IsNullOrWhiteSpace(r.Sku)).Select(r => r.Sku!).Distinct().ToList();
            var existingProducts = await _context.Products
                .Where(p => p.StoreId == job.StoreId && p.Sku != null && skus.Contains(p.Sku))
                .ToDictionaryAsync(p => p.Sku!);

            // Process only successful validation results
            var successfulResults = job.Results.Where(r => r.Success).ToList();
            int saveCounter = 0;
            const int batchSize = 100; // Save every 100 products

            foreach (var importResult in successfulResults)
            {
                try
                {
                    Product? product = null;
                    bool isCreate = true;

                    // Check if this is an update (existing SKU)
                    if (!string.IsNullOrWhiteSpace(importResult.Sku) && existingProducts.TryGetValue(importResult.Sku, out var existingProduct))
                    {
                        product = existingProduct;
                        isCreate = false;
                    }

                    if (isCreate)
                    {
                        // Create new product
                        product = new Product
                        {
                            StoreId = job.StoreId,
                            Sku = importResult.Sku,
                            Title = importResult.Title ?? string.Empty,
                            Description = importResult.Description,
                            Price = importResult.Price ?? 0,
                            Stock = importResult.Stock ?? 0,
                            Category = importResult.Category ?? "Uncategorized",
                            Weight = importResult.Weight,
                            Length = importResult.Length,
                            Width = importResult.Width,
                            Height = importResult.Height,
                            ShippingMethods = importResult.ShippingMethods,
                            Status = ProductStatus.Draft,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.Products.Add(product);
                        importResult.IsCreate = true;
                        result.CreatedCount++;

                        _logger.LogInformation("Prepared product creation from import job {JobId}, row {RowNumber}", 
                            jobId, importResult.RowNumber);
                    }
                    else if (product != null)
                    {
                        // Update existing product
                        product.Title = importResult.Title ?? product.Title;
                        product.Description = importResult.Description ?? product.Description;
                        product.Price = importResult.Price ?? product.Price;
                        product.Stock = importResult.Stock ?? product.Stock;
                        product.Category = importResult.Category ?? product.Category;
                        product.Weight = importResult.Weight ?? product.Weight;
                        product.Length = importResult.Length ?? product.Length;
                        product.Width = importResult.Width ?? product.Width;
                        product.Height = importResult.Height ?? product.Height;
                        product.ShippingMethods = importResult.ShippingMethods ?? product.ShippingMethods;
                        product.UpdatedAt = DateTime.UtcNow;

                        importResult.IsCreate = false;
                        result.UpdatedCount++;

                        _logger.LogInformation("Prepared product update from import job {JobId}, row {RowNumber}", 
                            jobId, importResult.RowNumber);
                    }

                    // Batch save for performance
                    saveCounter++;
                    if (saveCounter >= batchSize)
                    {
                        await _context.SaveChangesAsync();
                        
                        // Update ProductId for created products
                        foreach (var res in job.Results.Where(r => r.Success && r.ProductId == null && r.IsCreate == true))
                        {
                            var createdProduct = _context.Products.Local.FirstOrDefault(p => p.Sku == res.Sku && p.StoreId == job.StoreId);
                            if (createdProduct != null)
                            {
                                res.ProductId = createdProduct.Id;
                            }
                        }
                        
                        saveCounter = 0;
                        _logger.LogInformation("Saved batch of {Count} products for import job {JobId}", batchSize, jobId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing row {RowNumber} in job {JobId}", importResult.RowNumber, jobId);
                    importResult.Success = false;
                    importResult.ErrorMessage = $"Error: {ex.Message}";
                    result.FailedCount++;
                }
            }

            // Save remaining products
            if (saveCounter > 0)
            {
                await _context.SaveChangesAsync();
                
                // Update ProductId for created products
                foreach (var res in job.Results.Where(r => r.Success && r.ProductId == null && r.IsCreate == true))
                {
                    var createdProduct = _context.Products.Local.FirstOrDefault(p => p.Sku == res.Sku && p.StoreId == job.StoreId);
                    if (createdProduct != null)
                    {
                        res.ProductId = createdProduct.Id;
                    }
                }
                
                _logger.LogInformation("Saved final batch of {Count} products for import job {JobId}", saveCounter, jobId);
            }

            await _context.SaveChangesAsync();

            job.CreatedCount = result.CreatedCount;
            job.UpdatedCount = result.UpdatedCount;
            job.FailedCount = result.FailedCount;
            job.CompletedAt = DateTime.UtcNow;
            
            if (result.FailedCount == 0)
            {
                job.Status = ProductImportJobStatus.Completed;
            }
            else if (result.CreatedCount + result.UpdatedCount > 0)
            {
                job.Status = ProductImportJobStatus.CompletedWithErrors;
            }
            else
            {
                job.Status = ProductImportJobStatus.Failed;
                job.ErrorMessage = "All rows failed to import";
            }

            await _context.SaveChangesAsync();

            result.Success = job.Status == ProductImportJobStatus.Completed || job.Status == ProductImportJobStatus.CompletedWithErrors;
            _logger.LogInformation("Completed import job {JobId}: {Created} created, {Updated} updated, {Failed} failed", 
                jobId, result.CreatedCount, result.UpdatedCount, result.FailedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error executing import job {JobId}", jobId);
            job.Status = ProductImportJobStatus.Failed;
            job.ErrorMessage = $"Fatal error: {ex.Message}";
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            result.ErrorMessage = job.ErrorMessage;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<ProductImportJob>> GetImportJobsAsync(int storeId)
    {
        return await _context.ProductImportJobs
            .Where(j => j.StoreId == storeId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProductImportJob?> GetImportJobAsync(int jobId, int storeId)
    {
        return await _context.ProductImportJobs
            .Include(j => j.Results)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.StoreId == storeId);
    }

    /// <inheritdoc />
    public async Task<string> GenerateErrorReportAsync(int jobId, int storeId)
    {
        var job = await _context.ProductImportJobs
            .Include(j => j.Results)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.StoreId == storeId);

        if (job == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Row,SKU,Title,Error");

        foreach (var result in job.Results.Where(r => !r.Success).OrderBy(r => r.RowNumber))
        {
            sb.AppendLine($"{result.RowNumber},\"{result.Sku}\",\"{result.Title}\",\"{result.ErrorMessage}\"");
        }

        return sb.ToString();
    }
}
