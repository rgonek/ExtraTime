using ExtraTime.Application.Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Activities;

public sealed class SyncCompetitionsActivity(
    IFootballSyncService syncService,
    ILogger<SyncCompetitionsActivity> logger)
{
    [Function(nameof(SyncCompetitionsActivity))]
    public async Task Run(
        [ActivityTrigger] object? _,
        CancellationToken ct)
    {
        logger.LogInformation("Syncing competitions from API");
        await syncService.SyncCompetitionsAsync(ct);
        logger.LogInformation("Competitions sync completed");
    }
}
