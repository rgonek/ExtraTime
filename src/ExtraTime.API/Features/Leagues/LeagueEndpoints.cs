using ExtraTime.Application.Features.Leagues;
using ExtraTime.Application.Features.Leagues.Commands.CreateLeague;
using ExtraTime.Application.Features.Leagues.Commands.DeleteLeague;
using ExtraTime.Application.Features.Leagues.Commands.JoinLeague;
using ExtraTime.Application.Features.Leagues.Commands.KickMember;
using ExtraTime.Application.Features.Leagues.Commands.LeaveLeague;
using ExtraTime.Application.Features.Leagues.Commands.RegenerateInviteCode;
using ExtraTime.Application.Features.Leagues.Commands.UpdateLeague;
using ExtraTime.Application.Features.Leagues.DTOs;
using ExtraTime.Application.Features.Leagues.Queries.GetLeague;
using ExtraTime.Application.Features.Leagues.Queries.GetUserLeagues;
using FluentValidation;
using Mediator;

namespace ExtraTime.API.Features.Leagues;

public static class LeagueEndpoints
{
    public static void MapLeagueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leagues")
            .WithTags("Leagues")
            .RequireAuthorization();

        group.MapPost("/", CreateLeagueAsync)
            .WithName("CreateLeague");

        group.MapGet("/", GetUserLeaguesAsync)
            .WithName("GetUserLeagues");

        group.MapGet("/{id}", GetLeagueAsync)
            .WithName("GetLeague");

        group.MapPut("/{id}", UpdateLeagueAsync)
            .WithName("UpdateLeague");

        group.MapDelete("/{id}", DeleteLeagueAsync)
            .WithName("DeleteLeague");

        group.MapPost("/{id}/join", JoinLeagueAsync)
            .WithName("JoinLeague");

        group.MapDelete("/{id}/leave", LeaveLeagueAsync)
            .WithName("LeaveLeague");

        group.MapDelete("/{id}/members/{userId}", KickMemberAsync)
            .WithName("KickMember");

        group.MapPost("/{id}/invite-code/regenerate", RegenerateInviteCodeAsync)
            .WithName("RegenerateInviteCode");
    }

    private static async Task<IResult> CreateLeagueAsync(
        CreateLeagueRequest request,
        IMediator mediator,
        IValidator<CreateLeagueCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new CreateLeagueCommand(
            request.Name,
            request.Description,
            request.IsPublic,
            request.MaxMembers,
            request.ScoreExactMatch,
            request.ScoreCorrectResult,
            request.BettingDeadlineMinutes,
            request.AllowedCompetitionIds,
            request.InviteCodeExpiresAt);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Created($"/api/leagues/{result.Value!.Id}", result.Value);
    }

    private static async Task<IResult> GetUserLeaguesAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetUserLeaguesQuery();
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetLeagueAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetLeagueQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == LeagueErrors.LeagueNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            if (result.Error == LeagueErrors.NotAMember)
            {
                return Results.Forbid();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateLeagueAsync(
        Guid id,
        UpdateLeagueRequest request,
        IMediator mediator,
        IValidator<UpdateLeagueCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLeagueCommand(
            id,
            request.Name,
            request.Description,
            request.IsPublic,
            request.MaxMembers,
            request.ScoreExactMatch,
            request.ScoreCorrectResult,
            request.BettingDeadlineMinutes,
            request.AllowedCompetitionIds);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == LeagueErrors.LeagueNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            if (result.Error == LeagueErrors.NotTheOwner)
            {
                return Results.Forbid();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> DeleteLeagueAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new DeleteLeagueCommand(id);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == LeagueErrors.LeagueNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            if (result.Error == LeagueErrors.NotTheOwner)
            {
                return Results.Forbid();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.NoContent();
    }

    private static async Task<IResult> JoinLeagueAsync(
        Guid id,
        JoinLeagueRequest request,
        IMediator mediator,
        IValidator<JoinLeagueCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new JoinLeagueCommand(id, request.InviteCode);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == LeagueErrors.LeagueNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(new { message = "Successfully joined league" });
    }

    private static async Task<IResult> LeaveLeagueAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new LeaveLeagueCommand(id);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == LeagueErrors.NotAMember)
            {
                return Results.NotFound(new { error = result.Error });
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.NoContent();
    }

    private static async Task<IResult> KickMemberAsync(
        Guid id,
        Guid userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new KickMemberCommand(id, userId);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == LeagueErrors.LeagueNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            if (result.Error == LeagueErrors.NotTheOwner)
            {
                return Results.Forbid();
            }
            if (result.Error == LeagueErrors.MemberNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.NoContent();
    }

    private static async Task<IResult> RegenerateInviteCodeAsync(
        Guid id,
        RegenerateInviteCodeRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new RegenerateInviteCodeCommand(id, request.ExpiresAt);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == LeagueErrors.LeagueNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }
            if (result.Error == LeagueErrors.NotTheOwner)
            {
                return Results.Forbid();
            }
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(result.Value);
    }
}
