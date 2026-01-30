using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Commands.CreateBot;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.ValueObjects;
using Mediator;

namespace ExtraTime.API.Features.Bots;

public static class AdminBotsEndpoints
{
    public static RouteGroupBuilder MapAdminBotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/bots")
            .WithTags("Admin - Bots")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/", CreateBot)
            .WithName("CreateBot");

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

    private static async Task<IResult> CreateBot(
        CreateBotRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<BotStrategy>(request.Strategy, true, out var strategy))
        {
            return Results.BadRequest(new { error = "Invalid strategy" });
        }

        var command = new CreateBotCommand(request.Name, request.AvatarUrl, strategy);
        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/bots/{result.Value!.Id}", result.Value)
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

        var command = new CreateBotCommand(
            request.Name,
            request.AvatarUrl,
            BotStrategy.StatsAnalyst,
            config.ToJson());

        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/bots/{result.Value!.Id}", result.Value)
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
        return form != null
            ? Results.Ok(form)
            : Results.NotFound(new { error = "Form cache not found" });
    }
}
