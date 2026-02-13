using ExtraTime.Application.Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Activities;

public sealed class SyncCompetitionMatchesActivity(
    IFootballSyncService syncService,
    ILogger<SyncCompetitionMatchesActivity> logger)
{
    [Function(nameof(SyncCompetitionMatchesActivity))]
    public async Task<MatchSyncResult> Run(
        [ActivityTrigger] int competitionExternalId,
        CancellationToken ct)
    {
        logger.LogInformation("Syncing matches for competition {Id}", competitionExternalId);
        return await syncService.SyncMatchesForCompetitionAsync(competitionExternalId, ct);
    }
}
