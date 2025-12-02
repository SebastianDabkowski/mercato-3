using MercatoApp.Services;

namespace MercatoApp.Helpers;

/// <summary>
/// Background service that periodically checks for SLA breaches on pending return/complaint cases.
/// </summary>
public class SLAMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SLAMonitoringService> _logger;
    private readonly TimeSpan _checkInterval;

    public SLAMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<SLAMonitoringService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Default to checking every 30 minutes
        var intervalMinutes = configuration.GetValue<int?>("SLA:CheckIntervalMinutes") ?? 30;
        _checkInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Monitoring Service started. Check interval: {Interval} minutes", _checkInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSLABreachesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing SLA breaches");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("SLA Monitoring Service stopped");
    }

    private async Task ProcessSLABreachesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var slaService = scope.ServiceProvider.GetRequiredService<ISLAService>();

        try
        {
            var breachCount = await slaService.ProcessSLABreachesAsync();
            
            if (breachCount > 0)
            {
                _logger.LogInformation("SLA breach check completed. {BreachCount} cases flagged", breachCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SLA breaches");
        }
    }
}
