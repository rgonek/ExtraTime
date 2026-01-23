using ExtraTime.Application.Common.Interfaces;

namespace ExtraTime.API.Features.Football;

public static class FootballSyncEndpoints
{
    public static void MapFootballSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/sync")
            .WithTags("Admin - Football Sync")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/competitions", SyncCompetitionsAsync)
            .WithName("SyncCompetitions");

        group.MapPost("/teams/{competitionId:guid}", SyncTeamsAsync)
            .WithName("SyncTeams");

        group.MapPost("/matches", SyncMatchesAsync)
            .WithName("SyncMatches");

        group.MapPost("/live", SyncLiveMatchesAsync)
            .WithName("SyncLiveMatches");
    }

    private static async Task<IResult> SyncCompetitionsAsync(
        IFootballSyncService syncService,
        CancellationToken cancellationToken)
    {
        await syncService.SyncCompetitionsAsync(cancellationToken);
        return Results.Accepted(value: new { message = "Competition sync started" });
    }

    private static async Task<IResult> SyncTeamsAsync(
        Guid competitionId,
        IFootballSyncService syncService,
        CancellationToken cancellationToken)
    {
        await syncService.SyncTeamsForCompetitionAsync(competitionId, cancellationToken);
        return Results.Accepted(value: new { message = "Team sync started" });
    }

    private static async Task<IResult> SyncMatchesAsync(
        IFootballSyncService syncService,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        await syncService.SyncMatchesAsync(dateFrom, dateTo, cancellationToken);
        return Results.Accepted(value: new { message = "Match sync started" });
    }

    private static async Task<IResult> SyncLiveMatchesAsync(
        IFootballSyncService syncService,
        CancellationToken cancellationToken)
    {
        await syncService.SyncLiveMatchResultsAsync(cancellationToken);
        return Results.Accepted(value: new { message = "Live match sync started" });
    }
}
