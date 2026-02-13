using ExtraTime.Application.Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Functions;

public sealed class CalculateBetResultsFunction(
    IBetResultsService betResultsService,
    ILogger<CalculateBetResultsFunction> logger)
{
    /// <summary>
    /// Calculates bet results for recently finished matches every 15 minutes.
    /// CRON: 0 */15 * * * * (every 15 minutes)
    /// </summary>
    [Function("CalculateBetResults")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("CalculateBetResults function started at: {Time}", DateTime.UtcNow);

        try
        {
            var calculatedCount = await betResultsService.CalculateAllPendingBetResultsAsync(cancellationToken);
            logger.LogInformation("CalculateBetResults completed: {Count} matches processed", calculatedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CalculateBetResults function failed");
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogInformation("Next CalculateBetResults scheduled for: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }
}
