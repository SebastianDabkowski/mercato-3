namespace MercatoApp.Services;

/// <summary>
/// Interface for user data export service.
/// Supports GDPR Right of Access compliance.
/// </summary>
public interface IDataExportService
{
    /// <summary>
    /// Generates a complete export of all personal data for a user.
    /// </summary>
    /// <param name="userId">The user ID requesting the export.</param>
    /// <param name="ipAddress">The IP address from which the request originated.</param>
    /// <param name="userAgent">The user agent string from the request.</param>
    /// <returns>A byte array containing the ZIP file with exported data.</returns>
    Task<byte[]> GenerateUserDataExportAsync(int userId, string? ipAddress, string? userAgent);

    /// <summary>
    /// Gets the export history for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <returns>A list of data export log entries.</returns>
    Task<List<Models.DataExportLog>> GetExportHistoryAsync(int userId, int limit = 10);

    /// <summary>
    /// Gets all export logs for admin review.
    /// </summary>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated list of export logs.</returns>
    Task<Models.PaginatedList<Models.DataExportLog>> GetAllExportLogsAsync(int page = 1, int pageSize = 50);
}
