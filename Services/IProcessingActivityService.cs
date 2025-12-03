using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for managing GDPR processing activities.
/// </summary>
public interface IProcessingActivityService
{
    /// <summary>
    /// Gets all processing activities.
    /// </summary>
    /// <param name="activeOnly">Whether to return only active processing activities.</param>
    /// <returns>A list of processing activities.</returns>
    Task<List<ProcessingActivity>> GetAllAsync(bool activeOnly = false);

    /// <summary>
    /// Gets a processing activity by ID.
    /// </summary>
    /// <param name="id">The processing activity ID.</param>
    /// <returns>The processing activity, or null if not found.</returns>
    Task<ProcessingActivity?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new processing activity.
    /// </summary>
    /// <param name="activity">The processing activity to create.</param>
    /// <param name="userId">The ID of the user creating the activity.</param>
    /// <returns>The created processing activity.</returns>
    Task<ProcessingActivity> CreateAsync(ProcessingActivity activity, int userId);

    /// <summary>
    /// Updates an existing processing activity.
    /// Creates a history entry for audit purposes.
    /// </summary>
    /// <param name="activity">The processing activity to update.</param>
    /// <param name="userId">The ID of the user updating the activity.</param>
    /// <param name="changeNotes">Notes about what was changed.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    Task<bool> UpdateAsync(ProcessingActivity activity, int userId, string? changeNotes = null);

    /// <summary>
    /// Deletes a processing activity (soft delete by setting IsActive to false).
    /// </summary>
    /// <param name="id">The ID of the processing activity to delete.</param>
    /// <param name="userId">The ID of the user deleting the activity.</param>
    /// <returns>True if the deletion was successful, false otherwise.</returns>
    Task<bool> DeleteAsync(int id, int userId);

    /// <summary>
    /// Gets the history of changes for a processing activity.
    /// </summary>
    /// <param name="id">The processing activity ID.</param>
    /// <returns>A list of history entries.</returns>
    Task<List<ProcessingActivityHistory>> GetHistoryAsync(int id);

    /// <summary>
    /// Exports all processing activities to a structured format.
    /// </summary>
    /// <returns>A byte array containing the exported data.</returns>
    Task<byte[]> ExportToExcelAsync();
}
