using ExtraTime.Application.Features.Bots.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Functions;

public sealed class PlaceMlBotBetsFunction(
    IBotBettingService botBettingService,
    ILogger<PlaceMlBotBetsFunction> logger)
{
    [Function("PlaceMlBotBets")]
    public async Task RunAsync(
        [TimerTrigger("0 0 6 * * 1-5")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("PlaceMlBotBets started at: {Time}", DateTime.UtcNow);

        var betsPlaced = await botBettingService.PlaceBetsForUpcomingMatchesAsync(cancellationToken);
        logger.LogInformation("PlaceMlBotBets completed: {BetsPlaced} bets placed", betsPlaced);

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogInformation("Next PlaceMlBotBets schedule: {NextRun}", timerInfo.ScheduleStatus.Next);
        }
    }
}
