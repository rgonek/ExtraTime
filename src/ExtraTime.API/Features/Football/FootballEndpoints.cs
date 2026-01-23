using ExtraTime.Application.Features.Football;
using ExtraTime.Application.Features.Football.Queries.GetCompetitions;
using ExtraTime.Application.Features.Football.Queries.GetMatchById;
using ExtraTime.Application.Features.Football.Queries.GetMatches;
using ExtraTime.Domain.Enums;
using Mediator;

namespace ExtraTime.API.Features.Football;

public static class FootballEndpoints
{
    public static void MapFootballEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Football");

        group.MapGet("/competitions", GetCompetitionsAsync)
            .WithName("GetCompetitions")
            .AllowAnonymous();

        group.MapGet("/matches", GetMatchesAsync)
            .WithName("GetMatches")
            .AllowAnonymous();

        group.MapGet("/matches/{id:guid}", GetMatchByIdAsync)
            .WithName("GetMatchById")
            .AllowAnonymous();
    }

    private static async Task<IResult> GetCompetitionsAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetCompetitionsQuery();
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetMatchesAsync(
        IMediator mediator,
        int page = 1,
        int pageSize = 20,
        Guid? competitionId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        MatchStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMatchesQuery(page, pageSize, competitionId, dateFrom, dateTo, status);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetMatchByIdAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetMatchByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == FootballErrors.MatchNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }
}
