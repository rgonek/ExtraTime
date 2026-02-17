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

        group.MapPost("/suspensions/sync", SyncSuspensions)
            .WithName("SyncSuspensions");

        group.MapGet("/suspensions/{teamId:guid}", GetTeamSuspensions)
            .WithName("GetTeamSuspensions");

        group.MapPost("/backfill/league", BackfillLeagueData)
            .WithName("BackfillLeagueData");

        group.MapPost("/backfill/elo", BackfillGlobalElo)
            .WithName("BackfillGlobalElo");

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

    private static async Task<IResult> SyncSuspensions(
        ISuspensionService service,
        CancellationToken cancellationToken)
    {
        await service.SyncSuspensionsForUpcomingMatchesAsync(3, cancellationToken);
        return Results.Accepted(value: new { message = "Suspensions sync started" });
    }

    private static async Task<IResult> GetTeamSuspensions(
        Guid teamId,
        ISuspensionService service,
        CancellationToken cancellationToken)
    {
        var suspensions = await service.GetTeamSuspensionsAsOfAsync(teamId, DateTime.UtcNow, cancellationToken);
        return suspensions is not null
            ? Results.Ok(suspensions)
            : Results.NotFound();
    }

    private static async Task<IResult> BackfillLeagueData(
        LeagueBackfillRequest request,
        IExternalDataBackfillService service,
        CancellationToken cancellationToken)
    {
        await service.BackfillForLeagueAsync(
            request.LeagueCode,
            request.FromSeason,
            request.ToSeason,
            cancellationToken);

        return Results.Accepted(value: new
        {
            message = "League backfill completed",
            request.LeagueCode,
            request.FromSeason,
            request.ToSeason
        });
    }

    private static async Task<IResult> BackfillGlobalElo(
        EloBackfillRequest request,
        IExternalDataBackfillService service,
        CancellationToken cancellationToken)
    {
        await service.BackfillGlobalEloAsync(request.FromDateUtc, request.ToDateUtc, cancellationToken);
        return Results.Accepted(value: new
        {
            message = "Global Elo backfill completed",
            request.FromDateUtc,
            request.ToDateUtc
        });
    }

    public sealed record LeagueBackfillRequest(string LeagueCode, int FromSeason, int ToSeason);
    public sealed record EloBackfillRequest(DateTime FromDateUtc, DateTime ToDateUtc);
}
