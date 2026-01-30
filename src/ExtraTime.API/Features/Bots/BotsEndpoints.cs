using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Application.Features.Bots.Queries.GetBots;
using Mediator;

namespace ExtraTime.API.Features.Bots;

public static class BotsEndpoints
{
    public static RouteGroupBuilder MapBotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/bots")
            .WithTags("Bots");

        group.MapGet("/", GetBots)
            .WithName("GetBots")
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetBots(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBotsQuery(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }
}
