using ExtraTime.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.API.Features.Admin;

public static class AdminExternalDataEndpoints
{
    public static RouteGroupBuilder MapAdminExternalDataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/external-data")
            .WithTags("Admin - External Data")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/understat/sync", SyncUnderstat)
            .WithName("SyncUnderstat");

        group.MapGet("/understat/stats/{teamId:guid}", GetTeamXgStats)
            .WithName("GetTeamXgStats");

        group.MapPost("/odds/sync", SyncOdds)
            .WithName("SyncOdds");

        group.MapGet("/odds/{matchId:guid}", GetMatchOdds)
            .WithName("GetMatchOdds");

        group.MapPost("/injuries/sync", SyncInjuries)
            .WithName("SyncInjuries");

        group.MapGet("/injuries/{teamId:guid}", GetTeamInjuries)
            .WithName("GetTeamInjuries");

        return group;
    }

    private static async Task<IResult> SyncUnderstat(
        IUnderstatService service,
        CancellationToken cancellationToken)
    {
        await service.SyncAllLeaguesAsync(cancellationToken);
        return Results.Accepted(value: new { message = "Understat sync started" });
    }

    private static async Task<IResult> SyncOdds(
        IOddsDataService service,
        CancellationToken cancellationToken)
    {
        await service.ImportAllLeaguesAsync(cancellationToken);
        return Results.Accepted(value: new { message = "Odds import started" });
    }

    private static async Task<IResult> SyncInjuries(
        IInjuryService service,
        CancellationToken cancellationToken)
    {
        await service.SyncInjuriesForUpcomingMatchesAsync(3, cancellationToken);
        return Results.Accepted(value: new { message = "Injuries sync started" });
    }

    private static async Task<IResult> GetTeamXgStats(
        Guid teamId,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var stats = await context.TeamXgStats
            .Where(x => x.TeamId == teamId)
            .OrderByDescending(x => x.LastSyncedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return stats is not null
            ? Results.Ok(stats)
            : Results.NotFound();
    }

    private static async Task<IResult> GetMatchOdds(
        Guid matchId,
        IOddsDataService service,
        CancellationToken cancellationToken)
    {
        var odds = await service.GetOddsForMatchAsync(matchId, cancellationToken);
        return odds is not null
            ? Results.Ok(odds)
            : Results.NotFound();
    }

    private static async Task<IResult> GetTeamInjuries(
        Guid teamId,
        IInjuryService service,
        CancellationToken cancellationToken)
    {
        var injuries = await service.GetTeamInjuriesAsync(teamId, cancellationToken);
        return injuries is not null
            ? Results.Ok(injuries)
            : Results.NotFound();
    }
}
