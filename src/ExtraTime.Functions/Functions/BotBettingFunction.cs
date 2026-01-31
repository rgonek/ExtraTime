using ExtraTime.Application.Features.Bots.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Functions;

public sealed class BotBettingFunction(
    IBotBettingService botBettingService,
    ILogger<BotBettingFunction> logger)
{
    /// <summary>
    /// Places bets for all bots on upcoming matches daily at 6:00 AM UTC.
    /// CRON: 0 0 6 * * * (6:00 AM every day)
    /// </summary>
    [Function("BotBetting")]
    public async Task Run(
        [TimerTrigger("0 0 6 * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("BotBetting function started at: {Time}", DateTime.UtcNow);

        try
        {
            var betsPlaced = await botBettingService.PlaceBetsForUpcomingMatchesAsync(cancellationToken);

            logger.LogInformation("BotBetting completed: {BetsPlaced} bets placed", betsPlaced);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BotBetting function failed");
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogInformation("Next BotBetting scheduled for: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }
}
