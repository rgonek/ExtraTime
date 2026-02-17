using ExtraTime.Application.Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Functions;

public sealed class SyncLineupsFunction(
    ILineupSyncService lineupSyncService,
    ILogger<SyncLineupsFunction> logger)
{
    [Function("SyncLineups")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("SyncLineups started at: {Time}", DateTime.UtcNow);

        try
        {
            var synced = await lineupSyncService.SyncLineupsForUpcomingMatchesAsync(
                TimeSpan.FromHours(1),
                cancellationToken);
            logger.LogInformation("SyncLineups completed: {Count} matches synced", synced);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SyncLineups failed");
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogInformation("Next SyncLineups scheduled for: {NextRun}", timerInfo.ScheduleStatus.Next);
        }
    }
}
