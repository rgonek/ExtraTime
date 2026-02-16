using System.Diagnostics;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExtraTime.Infrastructure.Services.ExternalData;

public sealed class UnderstatSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<UnderstatSettings> settings,
    ILogger<UnderstatSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var syncSettings = settings.Value;
        if (!syncSettings.Enabled)
        {
            logger.LogInformation("Understat background sync is disabled");
            return;
        }

        logger.LogInformation("Understat sync background service started");

        await SyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRunUtc = GetNextRunUtc(DateTime.UtcNow, syncSettings.SyncHourUtc);
            var delay = nextRunUtc - DateTime.UtcNow;
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            logger.LogDebug("Next Understat sync scheduled for {NextRunUtc}", nextRunUtc);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await SyncAsync(stoppingToken);
        }
    }

    internal static DateTime GetNextRunUtc(DateTime utcNow, int syncHourUtc)
    {
        if (syncHourUtc is < 0 or > 23)
        {
            throw new ArgumentOutOfRangeException(nameof(syncHourUtc), "Sync hour must be between 0 and 23.");
        }

        var nextRun = utcNow.Date.AddHours(syncHourUtc);
        return utcNow >= nextRun ? nextRun.AddDays(1) : nextRun;
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();

        using var scope = scopeFactory.CreateScope();
        var understatService = scope.ServiceProvider.GetRequiredService<IUnderstatService>();
        var integrationHealth = scope.ServiceProvider.GetService<IIntegrationHealthService>();

        try
        {
            await understatService.SyncAllLeaguesAsync(cancellationToken);

            if (integrationHealth is not null)
            {
                var elapsed = Stopwatch.GetElapsedTime(startedAt);
                await integrationHealth.RecordSuccessAsync(IntegrationType.Understat, elapsed, cancellationToken);
            }

            logger.LogInformation("Understat sync completed successfully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (integrationHealth is not null)
            {
                await integrationHealth.RecordFailureAsync(
                    IntegrationType.Understat,
                    ex.Message,
                    ex.ToString(),
                    cancellationToken);
            }

            logger.LogError(ex, "Understat sync failed");
        }
    }
}
