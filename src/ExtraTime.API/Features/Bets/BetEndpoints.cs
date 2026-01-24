using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Commands.DeleteBet;
using ExtraTime.Application.Features.Bets.Commands.PlaceBet;
using ExtraTime.Application.Features.Bets.DTOs;
using ExtraTime.Application.Features.Bets.Queries.GetLeagueStandings;
using ExtraTime.Application.Features.Bets.Queries.GetMatchBets;
using ExtraTime.Application.Features.Bets.Queries.GetMyBets;
using ExtraTime.Application.Features.Bets.Queries.GetUserStats;
using FluentValidation;
using Mediator;

namespace ExtraTime.API.Features.Bets;

public static class BetEndpoints
{
    public static void MapBetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leagues/{leagueId}")
            .WithTags("Bets")
            .RequireAuthorization();

        group.MapPost("/bets", PlaceBetAsync)
            .WithName("PlaceBet");

        group.MapDelete("/bets/{betId}", DeleteBetAsync)
            .WithName("DeleteBet");

        group.MapGet("/bets/my", GetMyBetsAsync)
            .WithName("GetMyBets");

        group.MapGet("/matches/{matchId}/bets", GetMatchBetsAsync)
            .WithName("GetMatchBets");

        group.MapGet("/standings", GetLeagueStandingsAsync)
            .WithName("GetLeagueStandings");

        group.MapGet("/users/{userId}/stats", GetUserStatsAsync)
            .WithName("GetUserStats");
    }

    private static async Task<IResult> PlaceBetAsync(
        Guid leagueId,
        PlaceBetRequest request,
        IMediator mediator,
        IValidator<PlaceBetCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new PlaceBetCommand(
            leagueId,
            request.MatchId,
            request.PredictedHomeScore,
            request.PredictedAwayScore);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == BetErrors.LeagueNotFound || result.Error == BetErrors.MatchNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            if (result.Error == BetErrors.NotALeagueMember)
            {
                return Results.Forbid();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        // Check if this was an update (LastUpdatedAt is set) or a new bet
        if (result.Value!.LastUpdatedAt.HasValue)
        {
            return Results.Ok(result.Value);
        }

        return Results.Created($"/api/leagues/{leagueId}/bets/my", result.Value);
    }

    private static async Task<IResult> DeleteBetAsync(
        Guid leagueId,
        Guid betId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new DeleteBetCommand(leagueId, betId);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == BetErrors.BetNotFound || result.Error == BetErrors.LeagueNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            if (result.Error == BetErrors.NotBetOwner)
            {
                return Results.Forbid();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetMyBetsAsync(
        Guid leagueId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetMyBetsQuery(leagueId);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == BetErrors.NotALeagueMember)
            {
                return Results.Forbid();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetMatchBetsAsync(
        Guid leagueId,
        Guid matchId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetMatchBetsQuery(leagueId, matchId);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == BetErrors.LeagueNotFound || result.Error == BetErrors.MatchNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            if (result.Error == BetErrors.NotALeagueMember)
            {
                return Results.Forbid();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetLeagueStandingsAsync(
        Guid leagueId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetLeagueStandingsQuery(leagueId);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == BetErrors.NotALeagueMember)
            {
                return Results.Forbid();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetUserStatsAsync(
        Guid leagueId,
        Guid userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetUserStatsQuery(leagueId, userId);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == BetErrors.UserNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            if (result.Error == BetErrors.NotALeagueMember)
            {
                return Results.Forbid();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }
}
