namespace MercatoApp.Services;

/// <summary>
/// Interface for shipping label management.
/// Handles storage, retrieval, and cleanup of shipping labels.
/// </summary>
public interface IShippingLabelService
{
    /// <summary>
    /// Stores a shipping label for a shipment.
    /// </summary>
    /// <param name="shipmentId">The shipment ID.</param>
    /// <param name="labelData">The label data (PDF, PNG, etc.).</param>
    /// <param name="labelFormat">The label format (e.g., "PDF", "PNG").</param>
    /// <param name="contentType">The MIME content type (e.g., "application/pdf").</param>
    /// <returns>True if label was stored successfully, false otherwise.</returns>
    Task<bool> StoreLabelAsync(int shipmentId, byte[] labelData, string labelFormat, string contentType);

    /// <summary>
    /// Retrieves a shipping label for a shipment.
    /// </summary>
    /// <param name="shipmentId">The shipment ID.</param>
    /// <returns>Label data if found, null otherwise.</returns>
    Task<ShippingLabelData?> GetLabelAsync(int shipmentId);

    /// <summary>
    /// Deletes a shipping label for a shipment.
    /// </summary>
    /// <param name="shipmentId">The shipment ID.</param>
    /// <returns>True if label was deleted successfully, false otherwise.</returns>
    Task<bool> DeleteLabelAsync(int shipmentId);

    /// <summary>
    /// Cleans up old labels based on data retention policy.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain labels. Default is 90 days.</param>
    /// <returns>Number of labels deleted.</returns>
    Task<int> CleanupOldLabelsAsync(int retentionDays = 90);
}

/// <summary>
/// Represents shipping label data.
/// </summary>
public class ShippingLabelData
{
    /// <summary>
    /// Gets or sets the label data (binary).
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the label format (e.g., "PDF", "PNG").
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME content type.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tracking number associated with this label.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the carrier service name.
    /// </summary>
    public string? CarrierService { get; set; }
}
