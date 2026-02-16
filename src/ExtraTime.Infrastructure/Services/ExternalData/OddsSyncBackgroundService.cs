using System.Diagnostics;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExtraTime.Infrastructure.Services.ExternalData;

public sealed class OddsSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<FootballDataUkSettings> settings,
    ILogger<OddsSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var syncSettings = settings.Value;
        if (!syncSettings.Enabled)
        {
            logger.LogInformation("Football-Data.co.uk background sync is disabled");
            return;
        }

        logger.LogInformation("Football-Data.co.uk sync background service started");

        await SyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRunUtc = GetNextRunUtc(DateTime.UtcNow, syncSettings.SyncHourUtc);
            var delay = nextRunUtc - DateTime.UtcNow;
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            logger.LogDebug("Next Football-Data.co.uk sync scheduled for {NextRunUtc}", nextRunUtc);

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

        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)utcNow.DayOfWeek + 7) % 7;
        var nextRun = utcNow.Date.AddDays(daysUntilMonday).AddHours(syncHourUtc);
        if (utcNow >= nextRun)
        {
            nextRun = nextRun.AddDays(7);
        }

        return nextRun;
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();

        using var scope = scopeFactory.CreateScope();
        var oddsDataService = scope.ServiceProvider.GetRequiredService<IOddsDataService>();
        var integrationHealthService = scope.ServiceProvider.GetService<IIntegrationHealthService>();

        try
        {
            await oddsDataService.ImportAllLeaguesAsync(cancellationToken);

            if (integrationHealthService is not null)
            {
                var elapsed = Stopwatch.GetElapsedTime(startedAt);
                await integrationHealthService.RecordSuccessAsync(
                    IntegrationType.FootballDataUk,
                    elapsed,
                    cancellationToken);
            }

            logger.LogInformation("Football-Data.co.uk sync completed successfully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (integrationHealthService is not null)
            {
                await integrationHealthService.RecordFailureAsync(
                    IntegrationType.FootballDataUk,
                    ex.Message,
                    ex.ToString(),
                    cancellationToken);
            }

            logger.LogError(ex, "Football-Data.co.uk sync failed");
        }
    }
}
