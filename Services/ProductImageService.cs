using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace MercatoApp.Services;

/// <summary>
/// Result of a product image operation.
/// </summary>
public class ProductImageResult
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of errors that occurred during the operation.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// The product image if operation was successful.
    /// </summary>
    public ProductImage? Image { get; set; }
}

/// <summary>
/// Result of a batch image upload operation.
/// </summary>
public class BatchImageUploadResult
{
    /// <summary>
    /// Indicates whether all uploads were successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of errors that occurred during the operation.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of successfully uploaded images.
    /// </summary>
    public List<ProductImage> UploadedImages { get; set; } = new();
}

/// <summary>
/// Configuration options for product image uploads.
/// </summary>
public class ProductImageOptions
{
    /// <summary>
    /// Maximum file size in bytes (default: 5 MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// Maximum number of images per product (default: 10).
    /// </summary>
    public int MaxImagesPerProduct { get; set; } = 10;

    /// <summary>
    /// Allowed content types for image uploads.
    /// </summary>
    public HashSet<string> AllowedContentTypes { get; set; } = new()
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    /// <summary>
    /// Allowed file extensions for image uploads.
    /// </summary>
    public HashSet<string> AllowedExtensions { get; set; } = new()
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    /// <summary>
    /// Thumbnail width in pixels.
    /// </summary>
    public int ThumbnailWidth { get; set; } = 150;

    /// <summary>
    /// Thumbnail height in pixels.
    /// </summary>
    public int ThumbnailHeight { get; set; } = 150;

    /// <summary>
    /// Medium image width in pixels.
    /// </summary>
    public int MediumWidth { get; set; } = 600;

    /// <summary>
    /// Medium image height in pixels.
    /// </summary>
    public int MediumHeight { get; set; } = 600;

    /// <summary>
    /// Maximum width/height for the original image.
    /// Images larger than this will be resized.
    /// </summary>
    public int MaxOriginalDimension { get; set; } = 2000;

    /// <summary>
    /// JPEG quality for compressed images (1-100).
    /// </summary>
    public int JpegQuality { get; set; } = 85;

    /// <summary>
    /// Base path for storing product images.
    /// </summary>
    public string UploadPath { get; set; } = "wwwroot/uploads/products";

    /// <summary>
    /// Base URL path for serving product images.
    /// </summary>
    public string UrlBasePath { get; set; } = "/uploads/products";
}

/// <summary>
/// Interface for product image service.
/// </summary>
public interface IProductImageService
{
    /// <summary>
    /// Uploads an image for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <param name="fileStream">The image file stream.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="fileSize">The file size in bytes.</param>
    /// <returns>The upload result with the created image.</returns>
    Task<ProductImageResult> UploadImageAsync(
        int productId,
        int storeId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize);

    /// <summary>
    /// Uploads multiple images for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <param name="files">Collection of file data to upload.</param>
    /// <returns>The batch upload result.</returns>
    Task<BatchImageUploadResult> UploadImagesAsync(
        int productId,
        int storeId,
        IEnumerable<(Stream Stream, string FileName, string ContentType, long Size)> files);

    /// <summary>
    /// Deletes a product image.
    /// </summary>
    /// <param name="imageId">The image ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <returns>The result of the operation.</returns>
    Task<ProductImageResult> DeleteImageAsync(int imageId, int storeId);

    /// <summary>
    /// Sets an image as the main image for a product.
    /// </summary>
    /// <param name="imageId">The image ID to set as main.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <returns>The result of the operation.</returns>
    Task<ProductImageResult> SetMainImageAsync(int imageId, int storeId);

    /// <summary>
    /// Updates the display order of images for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <param name="imageIds">The ordered list of image IDs.</param>
    /// <returns>The result of the operation.</returns>
    Task<ProductImageResult> ReorderImagesAsync(int productId, int storeId, List<int> imageIds);

    /// <summary>
    /// Gets all images for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">Optional store ID for ownership verification.</param>
    /// <returns>List of product images ordered by display order.</returns>
    Task<List<ProductImage>> GetProductImagesAsync(int productId, int? storeId = null);

    /// <summary>
    /// Gets the main image for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>The main product image, or null if none exists.</returns>
    Task<ProductImage?> GetMainImageAsync(int productId);

    /// <summary>
    /// Gets the main images for multiple products in a single query.
    /// </summary>
    /// <param name="productIds">The product IDs.</param>
    /// <returns>Dictionary mapping product ID to main image (null if no image).</returns>
    Task<Dictionary<int, ProductImage?>> GetMainImagesAsync(IEnumerable<int> productIds);

    /// <summary>
    /// Validates an image file without uploading it.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="fileSize">The file size in bytes.</param>
    /// <returns>List of validation error messages.</returns>
    List<string> ValidateImage(string fileName, string contentType, long fileSize);
}

/// <summary>
/// Service for managing product images including upload, optimization, and storage.
/// </summary>
public class ProductImageService : IProductImageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductImageService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ProductImageOptions _options;

    public ProductImageService(
        ApplicationDbContext context,
        ILogger<ProductImageService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
        _options = new ProductImageOptions();
    }

    /// <inheritdoc />
    public List<string> ValidateImage(string fileName, string contentType, long fileSize)
    {
        var errors = new List<string>();

        // Validate file size
        if (fileSize > _options.MaxFileSizeBytes)
        {
            var maxSizeMb = _options.MaxFileSizeBytes / (1024 * 1024);
            errors.Add($"File size exceeds the maximum allowed size of {maxSizeMb} MB.");
        }

        if (fileSize == 0)
        {
            errors.Add("File is empty.");
        }

        // Validate content type
        if (!_options.AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            var allowedTypes = string.Join(", ", _options.AllowedContentTypes.Select(t => t.Replace("image/", "").ToUpperInvariant()));
            errors.Add($"Unsupported file format. Allowed formats are: {allowedTypes}.");
        }

        // Validate file extension
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_options.AllowedExtensions.Contains(extension))
        {
            var allowedExt = string.Join(", ", _options.AllowedExtensions.Select(e => e.ToUpperInvariant()));
            errors.Add($"Unsupported file extension. Allowed extensions are: {allowedExt}.");
        }

        return errors;
    }

    /// <inheritdoc />
    public async Task<ProductImageResult> UploadImageAsync(
        int productId,
        int storeId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize)
    {
        var result = new ProductImageResult();

        // Validate the file
        var validationErrors = ValidateImage(fileName, contentType, fileSize);
        if (validationErrors.Count > 0)
        {
            result.Errors.AddRange(validationErrors);
            return result;
        }

        // Verify product ownership
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId);

        if (product == null)
        {
            result.Errors.Add("Product not found or you do not have permission to upload images.");
            return result;
        }

        // Check image count limit
        var existingImageCount = await _context.ProductImages
            .CountAsync(i => i.ProductId == productId);

        if (existingImageCount >= _options.MaxImagesPerProduct)
        {
            result.Errors.Add($"Maximum number of images ({_options.MaxImagesPerProduct}) reached for this product.");
            return result;
        }

        try
        {
            // Generate unique file name
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var safeFileName = SanitizeFileName(fileName);
            var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
            var storedFileName = $"{productId}_{uniqueId}{extension}";

            // Create directory structure
            var productUploadPath = Path.Combine(_environment.ContentRootPath, _options.UploadPath, productId.ToString());
            EnsureDirectoryExists(productUploadPath);

            // Read image into memory for processing
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Process and save images
            var (width, height) = await ProcessAndSaveImagesAsync(
                memoryStream,
                productUploadPath,
                storedFileName);

            // Determine URLs
            var urlBase = $"{_options.UrlBasePath}/{productId}";
            var imageUrl = $"{urlBase}/{storedFileName}";
            var thumbnailUrl = $"{urlBase}/thumb_{storedFileName}";
            var mediumUrl = $"{urlBase}/medium_{storedFileName}";

            // Determine if this is the first image (will be main by default)
            var isMain = existingImageCount == 0;

            // Get next display order
            var maxDisplayOrder = await _context.ProductImages
                .Where(i => i.ProductId == productId)
                .MaxAsync(i => (int?)i.DisplayOrder) ?? -1;

            // Create database record
            var productImage = new ProductImage
            {
                ProductId = productId,
                OriginalFileName = fileName,
                StoredFileName = storedFileName,
                ImageUrl = imageUrl,
                ThumbnailUrl = thumbnailUrl,
                MediumUrl = mediumUrl,
                ContentType = contentType,
                FileSize = fileSize,
                Width = width,
                Height = height,
                IsMain = isMain,
                DisplayOrder = maxDisplayOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductImages.Add(productImage);

            // Update the product's ImageUrls field for backward compatibility
            await UpdateProductImageUrlsAsync(productId);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Uploaded image {ImageId} for product {ProductId}. Original: {FileName}, Size: {FileSize} bytes",
                productImage.Id,
                productId,
                fileName,
                fileSize);

            result.Success = true;
            result.Image = productImage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for product {ProductId}", productId);
            result.Errors.Add("An error occurred while uploading the image. Please try again.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<BatchImageUploadResult> UploadImagesAsync(
        int productId,
        int storeId,
        IEnumerable<(Stream Stream, string FileName, string ContentType, long Size)> files)
    {
        var result = new BatchImageUploadResult { Success = true };

        foreach (var file in files)
        {
            var uploadResult = await UploadImageAsync(
                productId,
                storeId,
                file.Stream,
                file.FileName,
                file.ContentType,
                file.Size);

            if (uploadResult.Success && uploadResult.Image != null)
            {
                result.UploadedImages.Add(uploadResult.Image);
            }
            else
            {
                result.Errors.AddRange(uploadResult.Errors.Select(e => $"{file.FileName}: {e}"));
                result.Success = false;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ProductImageResult> DeleteImageAsync(int imageId, int storeId)
    {
        var result = new ProductImageResult();

        var image = await _context.ProductImages
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.Product.StoreId == storeId);

        if (image == null)
        {
            result.Errors.Add("Image not found or you do not have permission to delete it.");
            return result;
        }

        try
        {
            var productId = image.ProductId;
            var wasMain = image.IsMain;

            // Delete physical files
            DeleteImageFiles(image);

            // Remove from database
            _context.ProductImages.Remove(image);

            // If the deleted image was the main image, set another as main
            if (wasMain)
            {
                var nextImage = await _context.ProductImages
                    .Where(i => i.ProductId == productId && i.Id != imageId)
                    .OrderBy(i => i.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (nextImage != null)
                {
                    nextImage.IsMain = true;
                }
            }

            // Update the product's ImageUrls field for backward compatibility
            await UpdateProductImageUrlsAsync(productId);

            // Save all changes in a single transaction
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted image {ImageId} for product {ProductId}", imageId, productId);

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId}", imageId);
            result.Errors.Add("An error occurred while deleting the image. Please try again.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ProductImageResult> SetMainImageAsync(int imageId, int storeId)
    {
        var result = new ProductImageResult();

        var image = await _context.ProductImages
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.Product.StoreId == storeId);

        if (image == null)
        {
            result.Errors.Add("Image not found or you do not have permission to modify it.");
            return result;
        }

        try
        {
            // Clear main flag from all other images for this product
            var otherImages = await _context.ProductImages
                .Where(i => i.ProductId == image.ProductId && i.IsMain)
                .ToListAsync();

            foreach (var otherImage in otherImages)
            {
                otherImage.IsMain = false;
            }

            // Set this image as main
            image.IsMain = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Set image {ImageId} as main for product {ProductId}",
                imageId,
                image.ProductId);

            result.Success = true;
            result.Image = image;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting main image {ImageId}", imageId);
            result.Errors.Add("An error occurred while updating the main image. Please try again.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ProductImageResult> ReorderImagesAsync(int productId, int storeId, List<int> imageIds)
    {
        var result = new ProductImageResult();

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId);

        if (product == null)
        {
            result.Errors.Add("Product not found or you do not have permission to modify it.");
            return result;
        }

        var images = await _context.ProductImages
            .Where(i => i.ProductId == productId)
            .ToListAsync();

        if (images.Count != imageIds.Count || !images.All(i => imageIds.Contains(i.Id)))
        {
            result.Errors.Add("Invalid image list provided.");
            return result;
        }

        try
        {
            for (int i = 0; i < imageIds.Count; i++)
            {
                var image = images.First(img => img.Id == imageIds[i]);
                image.DisplayOrder = i;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Reordered images for product {ProductId}",
                productId);

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering images for product {ProductId}", productId);
            result.Errors.Add("An error occurred while reordering images. Please try again.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<ProductImage>> GetProductImagesAsync(int productId, int? storeId = null)
    {
        var query = _context.ProductImages.AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(i => i.Product.StoreId == storeId.Value);
        }

        return await query
            .Where(i => i.ProductId == productId && !i.IsRemoved)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProductImage?> GetMainImageAsync(int productId)
    {
        return await _context.ProductImages
            .Where(i => i.ProductId == productId && i.IsMain && !i.IsRemoved)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, ProductImage?>> GetMainImagesAsync(IEnumerable<int> productIds)
    {
        var productIdList = productIds.ToList();
        var mainImages = await _context.ProductImages
            .Where(i => productIdList.Contains(i.ProductId) && i.IsMain && !i.IsRemoved)
            .ToListAsync();

        var result = new Dictionary<int, ProductImage?>();
        foreach (var productId in productIdList)
        {
            result[productId] = mainImages.FirstOrDefault(i => i.ProductId == productId);
        }

        return result;
    }

    /// <summary>
    /// Processes and saves the original, thumbnail, and medium versions of an image.
    /// </summary>
    private async Task<(int Width, int Height)> ProcessAndSaveImagesAsync(
        MemoryStream sourceStream,
        string uploadPath,
        string storedFileName)
    {
        using var originalBitmap = SKBitmap.Decode(sourceStream);
        if (originalBitmap == null)
        {
            throw new InvalidOperationException("Unable to decode image file.");
        }

        var originalWidth = originalBitmap.Width;
        var originalHeight = originalBitmap.Height;

        // Save original (possibly resized if too large)
        var (resizedBitmap, finalWidth, finalHeight) = ResizeIfNeeded(
            originalBitmap,
            _options.MaxOriginalDimension,
            _options.MaxOriginalDimension);

        var originalPath = Path.Combine(uploadPath, storedFileName);
        await SaveBitmapAsync(resizedBitmap ?? originalBitmap, originalPath, _options.JpegQuality);

        // Save thumbnail
        var (thumbnailBitmap, _, _) = ResizeToFit(
            originalBitmap,
            _options.ThumbnailWidth,
            _options.ThumbnailHeight);

        var thumbnailPath = Path.Combine(uploadPath, $"thumb_{storedFileName}");
        await SaveBitmapAsync(thumbnailBitmap, thumbnailPath, _options.JpegQuality);
        thumbnailBitmap.Dispose();

        // Save medium
        var (mediumBitmap, _, _) = ResizeToFit(
            originalBitmap,
            _options.MediumWidth,
            _options.MediumHeight);

        var mediumPath = Path.Combine(uploadPath, $"medium_{storedFileName}");
        await SaveBitmapAsync(mediumBitmap, mediumPath, _options.JpegQuality);
        mediumBitmap.Dispose();

        resizedBitmap?.Dispose();

        return (finalWidth > 0 ? finalWidth : originalWidth, finalHeight > 0 ? finalHeight : originalHeight);
    }

    /// <summary>
    /// Resizes a bitmap if it exceeds maximum dimensions.
    /// </summary>
    private static (SKBitmap? ResizedBitmap, int Width, int Height) ResizeIfNeeded(
        SKBitmap bitmap,
        int maxWidth,
        int maxHeight)
    {
        if (bitmap.Width <= maxWidth && bitmap.Height <= maxHeight)
        {
            return (null, bitmap.Width, bitmap.Height);
        }

        return ResizeToFit(bitmap, maxWidth, maxHeight);
    }

    /// <summary>
    /// Resizes a bitmap to fit within the specified dimensions while maintaining aspect ratio.
    /// </summary>
    private static (SKBitmap ResizedBitmap, int Width, int Height) ResizeToFit(
        SKBitmap bitmap,
        int maxWidth,
        int maxHeight)
    {
        var ratioX = (double)maxWidth / bitmap.Width;
        var ratioY = (double)maxHeight / bitmap.Height;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(bitmap.Width * ratio);
        var newHeight = (int)(bitmap.Height * ratio);

        var resizedBitmap = bitmap.Resize(new SKSizeI(newWidth, newHeight), SKSamplingOptions.Default);
        if (resizedBitmap == null)
        {
            throw new InvalidOperationException("Unable to resize image.");
        }

        return (resizedBitmap, newWidth, newHeight);
    }

    /// <summary>
    /// Saves a bitmap to disk as JPEG.
    /// </summary>
    private static async Task SaveBitmapAsync(SKBitmap bitmap, string path, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        await using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }

    /// <summary>
    /// Deletes all physical files associated with an image.
    /// </summary>
    private void DeleteImageFiles(ProductImage image)
    {
        var basePath = Path.Combine(_environment.ContentRootPath, _options.UploadPath, image.ProductId.ToString());

        try
        {
            var originalPath = Path.Combine(basePath, image.StoredFileName);
            if (File.Exists(originalPath))
            {
                File.Delete(originalPath);
            }

            var thumbnailPath = Path.Combine(basePath, $"thumb_{image.StoredFileName}");
            if (File.Exists(thumbnailPath))
            {
                File.Delete(thumbnailPath);
            }

            var mediumPath = Path.Combine(basePath, $"medium_{image.StoredFileName}");
            if (File.Exists(mediumPath))
            {
                File.Delete(mediumPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deleting image files for {StoredFileName}", image.StoredFileName);
        }
    }

    /// <summary>
    /// Updates the product's ImageUrls field for backward compatibility.
    /// </summary>
    private async Task UpdateProductImageUrlsAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return;

        var images = await _context.ProductImages
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.DisplayOrder)
            .Select(i => i.ImageUrl)
            .ToListAsync();

        product.ImageUrls = images.Count > 0 ? string.Join(",", images) : null;
    }

    /// <summary>
    /// Sanitizes a file name to remove invalid characters.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
