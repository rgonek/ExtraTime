using ExtraTime.Application.Features.Bots.Commands.CreateBot;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Domain.Enums;
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

        group.MapPost("/trigger-betting", TriggerBotBetting)
            .WithName("TriggerBotBetting");

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
}
