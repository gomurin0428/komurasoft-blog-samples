using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DesktopHostSample;

public sealed class DevicePollingBackgroundService(
    IServiceScopeFactory scopeFactory,
    StatusStore statusStore,
    ILogger<DevicePollingBackgroundService> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Device polling service is starting.");
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Device polling loop started.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                IDeviceStatusReader reader =
                    scope.ServiceProvider.GetRequiredService<IDeviceStatusReader>();

                DeviceStatus status = await reader.ReadAsync(stoppingToken);
                statusStore.Update(status);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Device polling failed.");
            }
        }

        logger.LogInformation("Device polling loop finished.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Device polling service is stopping.");
        await base.StopAsync(cancellationToken);
        logger.LogInformation("Device polling service stopped.");
    }
}
