using ExtraTime.Application.Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Orchestrators;

public static class SyncFootballDataOrchestrator
{
    [Function(nameof(SyncFootballDataOrchestrator))]
    public static async Task RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger(nameof(SyncFootballDataOrchestrator));
        var currentHour = context.CurrentUtcDateTime.Hour;
        var is5AmSync = currentHour == 5;

        var competitionIds = await context.CallActivityAsync<List<int>>(
            nameof(Activities.GetCompetitionIdsActivity), Array.Empty<object>());

        logger.LogInformation("Phase 1: Syncing matches for {Count} competitions", competitionIds.Count);

        var matchResults = new List<MatchSyncResult>();
        var batches = competitionIds.Chunk(RateLimitConfig.CompetitionsPerBatch).ToList();

        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var tasks = batch.Select(id =>
                context.CallActivityAsync<MatchSyncResult>(nameof(Activities.SyncCompetitionMatchesActivity), id));

            var results = await Task.WhenAll(tasks);
            matchResults.AddRange(results);

            if (i < batches.Count - 1)
            {
                await context.CreateTimer(
                    context.CurrentUtcDateTime.Add(RateLimitConfig.BatchWaitTime),
                    CancellationToken.None);
            }
        }

        var competitionsNeedingStandings = is5AmSync
            ? competitionIds
            : matchResults
                .Where(r => r.HasNewlyFinishedMatches)
                .Select(r => r.CompetitionExternalId)
                .ToList();

        if (competitionsNeedingStandings.Count > 0)
        {
            logger.LogInformation("Phase 2: Syncing standings for {Count} competitions",
                competitionsNeedingStandings.Count);

            await context.CreateTimer(
                context.CurrentUtcDateTime.Add(RateLimitConfig.BatchWaitTime),
                CancellationToken.None);

            var standingsResults = new List<StandingsSyncResult>();
            var standingsBatches = competitionsNeedingStandings
                .Chunk(RateLimitConfig.CompetitionsPerBatch)
                .ToList();

            for (var i = 0; i < standingsBatches.Count; i++)
            {
                var batch = standingsBatches[i];
                var tasks = batch.Select(id =>
                    context.CallActivityAsync<StandingsSyncResult>(nameof(Activities.SyncCompetitionStandingsActivity), id));

                var results = await Task.WhenAll(tasks);
                standingsResults.AddRange(results);

                if (i < standingsBatches.Count - 1)
                {
                    await context.CreateTimer(
                        context.CurrentUtcDateTime.Add(RateLimitConfig.BatchWaitTime),
                        CancellationToken.None);
                }
            }

            var newSeasonCompetitions = standingsResults
                .Where(r => r.NewSeasonDetected)
                .Select(r => r.CompetitionExternalId)
                .ToList();

            if (newSeasonCompetitions.Count > 0)
            {
                logger.LogInformation("Phase 3: New seasons detected for {Count} competitions, syncing teams",
                    newSeasonCompetitions.Count);

                await context.CreateTimer(
                    context.CurrentUtcDateTime.Add(RateLimitConfig.BatchWaitTime),
                    CancellationToken.None);

                var teamsBatches = newSeasonCompetitions
                    .Chunk(RateLimitConfig.CompetitionsPerBatch)
                    .ToList();

                for (var i = 0; i < teamsBatches.Count; i++)
                {
                    var batch = teamsBatches[i];
                    var tasks = batch.Select(id =>
                        context.CallActivityAsync(nameof(Activities.SyncCompetitionTeamsActivity), id));
                    await Task.WhenAll(tasks.Cast<Task>());

                    if (i < teamsBatches.Count - 1)
                    {
                        await context.CreateTimer(
                            context.CurrentUtcDateTime.Add(RateLimitConfig.BatchWaitTime),
                            CancellationToken.None);
                    }
                }
            }
        }

        logger.LogInformation("Football data sync completed");
    }
}
