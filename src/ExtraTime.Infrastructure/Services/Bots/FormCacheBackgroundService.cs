using ExtraTime.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.Bots;

public sealed class FormCacheBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<FormCacheBackgroundService> logger) : BackgroundService
{
    internal static TimeSpan Interval = TimeSpan.FromHours(4);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Form Cache Service started");

        await RefreshCachesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);

            try
            {
                await RefreshCachesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing form caches");
            }
        }
    }

    private async Task RefreshCachesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var formCalculator = scope.ServiceProvider.GetRequiredService<ITeamFormCalculator>();

        await formCalculator.RefreshAllFormCachesAsync(cancellationToken);
        logger.LogInformation("Form caches refreshed");
    }
}
