using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing shipping labels.
/// Stores labels in the database as binary data for security and easy retrieval.
/// </summary>
public class ShippingLabelService : IShippingLabelService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShippingLabelService> _logger;

    public ShippingLabelService(
        ApplicationDbContext context,
        ILogger<ShippingLabelService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> StoreLabelAsync(int shipmentId, byte[] labelData, string labelFormat, string contentType)
    {
        if (labelData == null || labelData.Length == 0)
        {
            _logger.LogWarning("Attempted to store empty label data for shipment {ShipmentId}", shipmentId);
            return false;
        }

        var shipment = await _context.Set<Shipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found when storing label", shipmentId);
            return false;
        }

        shipment.LabelData = labelData;
        shipment.LabelFormat = labelFormat;
        shipment.LabelContentType = contentType;
        shipment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Label stored successfully for shipment {ShipmentId}, Format: {Format}, Size: {Size} bytes",
            shipmentId, labelFormat, labelData.Length);

        return true;
    }

    /// <inheritdoc />
    public async Task<ShippingLabelData?> GetLabelAsync(int shipmentId)
    {
        var shipment = await _context.Set<Shipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found when retrieving label", shipmentId);
            return null;
        }

        if (shipment.LabelData == null || shipment.LabelData.Length == 0)
        {
            _logger.LogInformation("No label data found for shipment {ShipmentId}", shipmentId);
            return null;
        }

        return new ShippingLabelData
        {
            Data = shipment.LabelData,
            Format = shipment.LabelFormat ?? "PDF",
            ContentType = shipment.LabelContentType ?? "application/pdf",
            TrackingNumber = shipment.TrackingNumber,
            CarrierService = shipment.CarrierService
        };
    }

    /// <inheritdoc />
    public async Task<bool> DeleteLabelAsync(int shipmentId)
    {
        var shipment = await _context.Set<Shipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment {ShipmentId} not found when deleting label", shipmentId);
            return false;
        }

        shipment.LabelData = null;
        shipment.LabelFormat = null;
        shipment.LabelContentType = null;
        shipment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Label deleted for shipment {ShipmentId}", shipmentId);

        return true;
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldLabelsAsync(int retentionDays = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var shipments = await _context.Set<Shipment>()
            .Where(s => s.LabelData != null && s.CreatedAt < cutoffDate)
            .ToListAsync();

        if (shipments.Count == 0)
        {
            _logger.LogInformation("No old labels found to clean up (retention: {Days} days)", retentionDays);
            return 0;
        }

        foreach (var shipment in shipments)
        {
            shipment.LabelData = null;
            shipment.LabelFormat = null;
            shipment.LabelContentType = null;
            shipment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Cleaned up {Count} old labels (retention: {Days} days)",
            shipments.Count, retentionDays);

        return shipments.Count;
    }
}
