using System.Text.Json;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Commands.CreateBot;
using ExtraTime.Application.Features.Bots.Commands.DeleteBot;
using ExtraTime.Application.Features.Bots.Commands.UpdateBot;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Application.Features.Bots.Queries.GetBotConfigurationPresets;
using ExtraTime.Application.Features.Bots.Queries.GetBots;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.API.Features.Bots;

public static class AdminBotsEndpoints
{
    public static RouteGroupBuilder MapAdminBotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/bots")
            .WithTags("Admin - Bots")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetBots)
            .WithName("AdminGetBots");

        group.MapGet("/{id:guid}", GetBot)
            .WithName("AdminGetBot");

        group.MapPost("/", CreateBot)
            .WithName("AdminCreateBot");

        group.MapPut("/{id:guid}", UpdateBot)
            .WithName("AdminUpdateBot");

        group.MapDelete("/{id:guid}", DeleteBot)
            .WithName("AdminDeleteBot");

        group.MapGet("/presets", GetPresets)
            .WithName("GetBotPresets");

        group.MapPost("/validate-config", ValidateConfig)
            .WithName("ValidateBotConfig");

        group.MapPost("/{id:guid}/activate", ActivateBot)
            .WithName("ActivateBot");

        group.MapPost("/{id:guid}/deactivate", DeactivateBot)
            .WithName("DeactivateBot");

        group.MapPost("/stats-analyst", CreateStatsAnalystBot)
            .WithName("CreateStatsAnalystBot");

        group.MapPost("/trigger-betting", TriggerBotBetting)
            .WithName("TriggerBotBetting");

        group.MapPost("/form-cache/refresh", RefreshFormCaches)
            .WithName("RefreshFormCaches");

        group.MapGet("/form-cache/{teamId:guid}/{competitionId:guid}", GetTeamForm)
            .WithName("GetTeamForm");

        return group;
    }

    private static async Task<IResult> GetBots(
        bool? includeInactive,
        string? strategy,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        BotStrategy? parsedStrategy = null;

        if (!string.IsNullOrWhiteSpace(strategy))
        {
            if (!Enum.TryParse<BotStrategy>(strategy, true, out var strategyValue))
            {
                return Results.BadRequest(new { error = "Invalid strategy" });
            }

            parsedStrategy = strategyValue;
        }

        var result = await mediator.Send(
            new GetBotsQuery(includeInactive, parsedStrategy),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetBot(
        Guid id,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var bot = await context.Bots
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (bot is null)
        {
            return Results.NotFound();
        }

        var stats = await GetBotStatsAsync(context, bot.Id, bot.UserId, cancellationToken);
        return Results.Ok(MapBot(bot, stats));
    }

    private static async Task<IResult> CreateBot(
        CreateBotRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<BotStrategy>(request.Strategy, true, out var strategy))
        {
            return Results.BadRequest(new { error = "Invalid strategy" });
        }

        var command = new CreateBotCommand(
            request.Name,
            request.AvatarUrl,
            strategy,
            request.Configuration is null
                ? null
                : JsonSerializer.Serialize(request.Configuration));

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/admin/bots/{result.Value!.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> UpdateBot(
        Guid id,
        UpdateBotRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        BotStrategy? strategy = null;

        if (!string.IsNullOrWhiteSpace(request.Strategy))
        {
            if (!Enum.TryParse<BotStrategy>(request.Strategy, true, out var parsedStrategy))
            {
                return Results.BadRequest(new { error = "Invalid strategy" });
            }

            strategy = parsedStrategy;
        }

        var command = new UpdateBotCommand(
            id,
            request.Name,
            request.AvatarUrl,
            strategy,
            request.Configuration is null
                ? null
                : JsonSerializer.Serialize(request.Configuration),
            request.IsActive);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure && string.Equals(result.Error, "Bot not found", StringComparison.Ordinal))
        {
            return Results.NotFound(new { error = result.Error });
        }

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> DeleteBot(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteBotCommand(id), cancellationToken);

        if (result.IsFailure && string.Equals(result.Error, "Bot not found", StringComparison.Ordinal))
        {
            return Results.NotFound(new { error = result.Error });
        }

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetPresets(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBotConfigurationPresetsQuery(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static IResult ValidateConfig(BotConfigurationDto config)
    {
        var errors = new List<string>();

        var totalWeight = config.FormWeight +
                          config.HomeAdvantageWeight +
                          config.GoalTrendWeight +
                          config.StreakWeight +
                          config.XgWeight +
                          config.XgDefensiveWeight +
                          config.OddsWeight +
                          config.InjuryWeight +
                          config.LineupAnalysisWeight;

        if (totalWeight < 0.8 || totalWeight > 1.2)
        {
            errors.Add($"Weights should sum to approximately 1.0 (current: {totalWeight:F2})");
        }

        if (config.RandomVariance < 0 || config.RandomVariance > 0.5)
        {
            errors.Add("RandomVariance must be between 0 and 0.5");
        }

        if (config.MatchesAnalyzed < 3 || config.MatchesAnalyzed > 20)
        {
            errors.Add("MatchesAnalyzed must be between 3 and 20");
        }

        if (!Enum.TryParse<PredictionStyle>(config.Style, true, out _))
        {
            errors.Add("Invalid Style value");
        }

        return errors.Count > 0
            ? Results.BadRequest(new { valid = false, errors })
            : Results.Ok(new { valid = true, errors = Array.Empty<string>() });
    }

    private static async Task<IResult> ActivateBot(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateBotCommand(id, null, null, null, null, true),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> DeactivateBot(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateBotCommand(id, null, null, null, null, false),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> TriggerBotBetting(
        IBotBettingService botService,
        CancellationToken cancellationToken)
    {
        var betsPlaced = await botService.PlaceBetsForUpcomingMatchesAsync(cancellationToken);
        return Results.Ok(new { betsPlaced });
    }

    private static async Task<IResult> CreateStatsAnalystBot(
        CreateStatsAnalystBotRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var config = new StatsAnalystConfig
        {
            FormWeight = request.FormWeight,
            HomeAdvantageWeight = request.HomeAdvantageWeight,
            GoalTrendWeight = request.GoalTrendWeight,
            StreakWeight = request.StreakWeight,
            MatchesAnalyzed = request.MatchesAnalyzed,
            HighStakesBoost = request.HighStakesBoost,
            Style = Enum.Parse<PredictionStyle>(request.Style),
            RandomVariance = request.RandomVariance
        };

        var result = await mediator.Send(
            new CreateBotCommand(
                request.Name,
                request.AvatarUrl,
                BotStrategy.StatsAnalyst,
                config.ToJson()),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/admin/bots/{result.Value!.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> RefreshFormCaches(
        ITeamFormCalculator formCalculator,
        CancellationToken cancellationToken)
    {
        await formCalculator.RefreshAllFormCachesAsync(cancellationToken);
        return Results.Ok(new { message = "Form caches refreshed" });
    }

    private static async Task<IResult> GetTeamForm(
        Guid teamId,
        Guid competitionId,
        ITeamFormCalculator formCalculator,
        CancellationToken cancellationToken)
    {
        var form = await formCalculator.GetCachedFormAsync(teamId, competitionId, cancellationToken);
        return form is not null
            ? Results.Ok(form)
            : Results.NotFound(new { error = "Form cache not found" });
    }

    private static async Task<BotStatsDto> GetBotStatsAsync(
        IApplicationDbContext context,
        Guid botId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var bets = await context.Bets
            .Include(b => b.Result)
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);

        var leaguesJoined = await context.LeagueBotMembers
            .CountAsync(lbm => lbm.BotId == botId, cancellationToken);

        var betsWithResults = bets.Where(b => b.Result is not null).ToList();

        return new BotStatsDto(
            TotalBetsPlaced: bets.Count,
            LeaguesJoined: leaguesJoined,
            AveragePointsPerBet: betsWithResults.Count > 0
                ? betsWithResults.Average(b => b.Result!.PointsEarned)
                : 0,
            ExactPredictions: betsWithResults.Count(b => b.Result!.IsExactMatch),
            CorrectResults: betsWithResults.Count(b => b.Result!.IsCorrectResult));
    }

    private static BotDto MapBot(Bot bot, BotStatsDto? stats)
    {
        return new BotDto(
            bot.Id,
            bot.Name,
            bot.AvatarUrl,
            bot.Strategy.ToString(),
            bot.IsActive,
            bot.CreatedAt,
            bot.LastBetPlacedAt,
            bot.Configuration,
            stats);
    }
}
