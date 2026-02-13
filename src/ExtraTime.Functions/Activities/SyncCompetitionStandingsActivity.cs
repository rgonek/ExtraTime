using ExtraTime.Application.Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Activities;

public sealed class SyncCompetitionStandingsActivity(
    IFootballSyncService syncService,
    ILogger<SyncCompetitionStandingsActivity> logger)
{
    [Function(nameof(SyncCompetitionStandingsActivity))]
    public async Task<StandingsSyncResult> Run(
        [ActivityTrigger] int competitionExternalId,
        CancellationToken ct)
    {
        logger.LogInformation("Syncing standings for competition {Id}", competitionExternalId);
        return await syncService.SyncStandingsForCompetitionAsync(competitionExternalId, ct);
    }
}
