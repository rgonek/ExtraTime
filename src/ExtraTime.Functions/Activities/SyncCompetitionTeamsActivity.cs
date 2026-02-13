using ExtraTime.Application.Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Activities;

public sealed class SyncCompetitionTeamsActivity(
    IFootballSyncService syncService,
    ILogger<SyncCompetitionTeamsActivity> logger)
{
    [Function(nameof(SyncCompetitionTeamsActivity))]
    public async Task Run(
        [ActivityTrigger] int competitionExternalId,
        CancellationToken ct)
    {
        logger.LogInformation("Syncing teams for competition {Id}", competitionExternalId);
        await syncService.SyncTeamsForCompetitionAsync(competitionExternalId, ct);
    }
}
