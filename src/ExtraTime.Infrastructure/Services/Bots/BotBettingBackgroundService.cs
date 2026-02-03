using ExtraTime.Application.Features.Bots.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.Bots;

public sealed class BotBettingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<BotBettingBackgroundService> logger) : BackgroundService
{
    internal static TimeSpan Interval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Bot Betting Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                if (now.Hour >= 8 && now.Hour <= 23)
                {
                    await PlaceBotBetsAsync(stoppingToken);
                }
                else
                {
                    logger.LogDebug("Outside active hours, skipping bot betting run");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during bot betting run");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task PlaceBotBetsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var botService = scope.ServiceProvider.GetRequiredService<IBotBettingService>();

        var betsPlaced = await botService.PlaceBetsForUpcomingMatchesAsync(cancellationToken);

        if (betsPlaced > 0)
        {
            logger.LogInformation("Bot betting run completed: {BetsPlaced} bets placed", betsPlaced);
        }
    }
}
