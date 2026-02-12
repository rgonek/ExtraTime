using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.Commands.CalculateBetResults;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.API.Features.DevTriggers;

public static class DevTriggerEndpoints
{
    public static void MapDevTriggerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dev/trigger")
            .WithTags("Dev - Triggers")
            .AllowAnonymous();

        group.MapPost("/sync-matches", SyncMatchesAsync)
            .WithName("DevTriggerSyncMatches");

        group.MapPost("/calculate-bets", CalculateBetsAsync)
            .WithName("DevTriggerCalculateBets");

        group.MapPost("/bot-betting", BotBettingAsync)
            .WithName("DevTriggerBotBetting");
    }

    private static async Task<IResult> SyncMatchesAsync(
        IFootballSyncService syncService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("DevTriggers.SyncMatches");
        logger.LogInformation("[DEV-TRIGGER] Starting match sync operation");
        var startTime = DateTime.UtcNow;

        try
        {
            await syncService.SyncMatchesAsync(ct: cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            logger.LogInformation("[DEV-TRIGGER] Match sync completed in {Duration}ms", duration.TotalMilliseconds);

            return Results.Ok(new
            {
                message = "Match sync completed",
                durationMs = duration.TotalMilliseconds,
                completedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DEV-TRIGGER] Match sync failed");
            return Results.Ok(new
            {
                message = "Match sync failed",
                error = ex.Message
            });
        }
    }

    private static async Task<IResult> CalculateBetsAsync(
        IApplicationDbContext context,
        IMediator mediator,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("DevTriggers.CalculateBets");
        logger.LogInformation("[DEV-TRIGGER] Starting bet calculation operation");
        var startTime = DateTime.UtcNow;

        try
        {
            // Find all finished matches with uncalculated bets
            var uncalculatedMatches = await context.Bets
                .Include(b => b.Match)
                .Where(b => b.Match.Status == MatchStatus.Finished
                         && b.Match.HomeScore.HasValue
                         && b.Match.AwayScore.HasValue
                         && b.Result == null)
                .Select(b => new { b.Match.Id, b.Match.CompetitionId })
                .Distinct()
                .ToListAsync(cancellationToken);

            logger.LogInformation("[DEV-TRIGGER] Found {Count} matches with uncalculated bets", uncalculatedMatches.Count);

            if (uncalculatedMatches.Count == 0)
            {
                return Results.Ok(new { message = "No uncalculated bets found", matchesProcessed = 0, totalMatches = 0 });
            }

            var processedCount = 0;
            foreach (var match in uncalculatedMatches)
            {
                logger.LogInformation("[DEV-TRIGGER] Processing match {MatchId}", match.Id);
                var command = new CalculateBetResultsCommand(match.Id, match.CompetitionId);
                var result = await mediator.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    processedCount++;
                    logger.LogInformation("[DEV-TRIGGER] Match {MatchId} processed successfully", match.Id);
                }
                else
                {
                    logger.LogWarning("[DEV-TRIGGER] Match {MatchId} processing failed", match.Id);
                }
            }

            var duration = DateTime.UtcNow - startTime;
            logger.LogInformation("[DEV-TRIGGER] Bet calculation completed in {Duration}ms. Processed {Processed}/{Total} matches",
                duration.TotalMilliseconds, processedCount, uncalculatedMatches.Count);

            return Results.Ok(new
            {
                message = "Bet calculation completed",
                matchesProcessed = processedCount,
                totalMatches = uncalculatedMatches.Count,
                durationMs = duration.TotalMilliseconds,
                completedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DEV-TRIGGER] Bet calculation failed");
            return Results.Ok(new
            {
                message = "Bet calculation failed",
                error = ex.Message,
                matchesProcessed = 0,
                totalMatches = 0
            });
        }
    }

    private static async Task<IResult> BotBettingAsync(
        IBotBettingService botService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("DevTriggers.BotBetting");
        logger.LogInformation("[DEV-TRIGGER] Starting bot betting operation");
        var startTime = DateTime.UtcNow;

        try
        {
            var betsPlaced = await botService.PlaceBetsForUpcomingMatchesAsync(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            logger.LogInformation("[DEV-TRIGGER] Bot betting completed in {Duration}ms. Placed {Count} bets",
                duration.TotalMilliseconds, betsPlaced);

            return Results.Ok(new
            {
                message = "Bot betting completed",
                betsPlaced,
                durationMs = duration.TotalMilliseconds,
                completedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DEV-TRIGGER] Bot betting failed");
            return Results.Ok(new
            {
                message = "Bot betting failed",
                error = ex.Message,
                betsPlaced = 0
            });
        }
    }
}
