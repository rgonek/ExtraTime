using ExtraTime.Application.Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Orchestrators;

public static class SyncFootballDataOrchestrator
{
    private static readonly TaskOptions ActivityRetryOptions = TaskOptions.FromRetryPolicy(
        new RetryPolicy(maxNumberOfAttempts: 3, firstRetryInterval: TimeSpan.FromSeconds(20)));

    [Function(nameof(SyncFootballDataOrchestrator))]
    public static async Task RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger(nameof(SyncFootballDataOrchestrator));
        var currentHour = context.CurrentUtcDateTime.Hour;
        var is5AmSync = currentHour == 5;

        // Phase 0: Ensure competitions exist (required for all other syncs)
        logger.LogInformation("Phase 0: Syncing competitions");
        await CallActivityWithRetryAsync(context, nameof(Activities.SyncCompetitionsActivity));

        // No rate limit wait needed - next calls are DB/config reads, not API calls
        var competitionIds = await CallActivityWithRetryAsync<List<int>>(
            context,
            nameof(Activities.GetCompetitionIdsActivity));

        // Check if any competition needs initial setup (no current season)
        var competitionsNeedingSetup = await CallActivityWithRetryAsync<List<int>>(
            context,
            nameof(Activities.GetCompetitionsWithoutSeasonActivity));

        // Phase 0.5: Initialize new competitions BEFORE match sync
        // New competitions need: standings (creates season) -> teams -> then matches will work
        if (competitionsNeedingSetup.Count > 0)
        {
            logger.LogInformation("Phase 0.5: Initializing {Count} new competitions (standings + teams)",
                competitionsNeedingSetup.Count);

            await WaitForBatchWindowAsync(context);

            // Sync standings for new competitions (creates seasons + some teams from standings data)
            await ExecuteInBatchesAsync<StandingsSyncResult>(
                context,
                competitionsNeedingSetup,
                nameof(Activities.SyncCompetitionStandingsActivity));

            await WaitForBatchWindowAsync(context);

            // Sync teams for new competitions (ensures all teams are present)
            await ExecuteInBatchesAsync(
                context,
                competitionsNeedingSetup,
                nameof(Activities.SyncCompetitionTeamsActivity));
        }

        // Phase 1: Sync matches for ALL competitions
        // Now new competitions have seasons + teams, so matches will sync correctly
        logger.LogInformation("Phase 1: Syncing matches for {Count} competitions", competitionIds.Count);

        await WaitForBatchWindowAsync(context);

        var matchResults = await ExecuteInBatchesAsync<MatchSyncResult>(
            context,
            competitionIds,
            nameof(Activities.SyncCompetitionMatchesActivity));

        // Phase 2: Sync standings for existing competitions (if matches finished or 5 AM)
        // Exclude competitions that were just initialized in Phase 0.5
        var existingCompetitionsWithFinishedMatches = matchResults
            .Where(r => r.HasNewlyFinishedMatches)
            .Select(r => r.CompetitionExternalId)
            .Except(competitionsNeedingSetup)
            .ToList();

        var competitionsNeedingStandings = is5AmSync
            ? competitionIds.Except(competitionsNeedingSetup).ToList()
            : existingCompetitionsWithFinishedMatches;

        if (competitionsNeedingStandings.Count > 0)
        {
            logger.LogInformation("Phase 2: Syncing standings for {Count} competitions",
                competitionsNeedingStandings.Count);

            await WaitForBatchWindowAsync(context);

            var standingsResults = await ExecuteInBatchesAsync<StandingsSyncResult>(
                context,
                competitionsNeedingStandings,
                nameof(Activities.SyncCompetitionStandingsActivity));

            // Phase 3: Sync teams for competitions with new seasons
            var competitionsWithNewSeasons = standingsResults
                .Where(r => r.NewSeasonDetected)
                .Select(r => r.CompetitionExternalId)
                .ToList();

            if (competitionsWithNewSeasons.Count > 0)
            {
                logger.LogInformation("Phase 3: Syncing teams for {Count} competitions with new seasons",
                    competitionsWithNewSeasons.Count);

                await WaitForBatchWindowAsync(context);

                await ExecuteInBatchesAsync(
                    context,
                    competitionsWithNewSeasons,
                    nameof(Activities.SyncCompetitionTeamsActivity));
            }
        }

        logger.LogInformation("Football data sync completed");
    }

    private static async Task<List<TResult>> ExecuteInBatchesAsync<TResult>(
        TaskOrchestrationContext context,
        List<int> competitionIds,
        string activityName)
    {
        var results = new List<TResult>();
        var batches = competitionIds.Chunk(RateLimitConfig.CompetitionsPerBatch).ToList();

        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var tasks = batch.Select(id =>
                context.CallActivityAsync<TResult>(activityName, id, ActivityRetryOptions));

            var batchResults = await Task.WhenAll(tasks);
            results.AddRange(batchResults);

            if (i < batches.Count - 1)
            {
                await WaitForBatchWindowAsync(context);
            }
        }

        return results;
    }

    private static async Task ExecuteInBatchesAsync(
        TaskOrchestrationContext context,
        List<int> competitionIds,
        string activityName)
    {
        var batches = competitionIds.Chunk(RateLimitConfig.CompetitionsPerBatch).ToList();

        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var tasks = batch.Select(id =>
                context.CallActivityAsync(activityName, id, ActivityRetryOptions));

            await Task.WhenAll(tasks.Cast<Task>());

            if (i < batches.Count - 1)
            {
                await WaitForBatchWindowAsync(context);
            }
        }
    }

    private static Task<TResult> CallActivityWithRetryAsync<TResult>(
        TaskOrchestrationContext context,
        string activityName,
        object? input = null)
    {
        return context.CallActivityAsync<TResult>(activityName, input, ActivityRetryOptions);
    }

    private static Task CallActivityWithRetryAsync(
        TaskOrchestrationContext context,
        string activityName,
        object? input = null)
    {
        return context.CallActivityAsync(activityName, input, ActivityRetryOptions);
    }

    private static Task WaitForBatchWindowAsync(TaskOrchestrationContext context)
    {
        return context.CreateTimer(
            context.CurrentUtcDateTime.Add(RateLimitConfig.BatchWaitTime),
            CancellationToken.None);
    }
}
