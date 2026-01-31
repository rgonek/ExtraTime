using ExtraTime.Application.Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Functions;

public sealed class SyncMatchesFunction(
    IFootballSyncService footballSyncService,
    ILogger<SyncMatchesFunction> logger)
{
    /// <summary>
    /// Syncs football matches from external API every hour.
    /// CRON: 0 0 * * * * (at minute 0 of every hour)
    /// </summary>
    [Function("SyncMatches")]
    public async Task Run(
        [TimerTrigger("0 0 * * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("SyncMatches function started at: {Time}", DateTime.UtcNow);

        try
        {
            await footballSyncService.SyncMatchesAsync(
                dateFrom: null,
                dateTo: null,
                cancellationToken);

            logger.LogInformation("SyncMatches completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SyncMatches function failed");
            throw; // Re-throw to mark function execution as failed
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogInformation("Next SyncMatches scheduled for: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }
}
