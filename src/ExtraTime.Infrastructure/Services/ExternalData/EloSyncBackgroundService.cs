using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExtraTime.Infrastructure.Services.ExternalData;

public sealed class EloSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ClubEloSettings> settings,
    ILogger<EloSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var syncSettings = settings.Value;
        if (!syncSettings.Enabled)
        {
            logger.LogInformation("ClubElo background sync is disabled");
            return;
        }

        logger.LogInformation("ClubElo sync background service started");

        await SyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRunUtc = GetNextRunUtc(DateTime.UtcNow, syncSettings.SyncHourUtc);
            var delay = nextRunUtc - DateTime.UtcNow;
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            logger.LogDebug("Next ClubElo sync scheduled for {NextRunUtc}", nextRunUtc);

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
        using var scope = scopeFactory.CreateScope();
        var eloRatingService = scope.ServiceProvider.GetRequiredService<IEloRatingService>();

        try
        {
            await eloRatingService.SyncEloRatingsAsync(cancellationToken);
            logger.LogInformation("ClubElo sync completed successfully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ClubElo sync failed");
        }
    }
}
