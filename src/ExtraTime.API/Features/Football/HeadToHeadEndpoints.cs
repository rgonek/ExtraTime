using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Football.DTOs;

namespace ExtraTime.API.Features.Football;

public static class HeadToHeadEndpoints
{
    public static void MapHeadToHeadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/football/head-to-head")
            .WithTags("HeadToHead");

        group.MapGet("/", GetHeadToHeadAsync)
            .WithName("GetHeadToHead")
            .AllowAnonymous();
    }

    private static async Task<IResult> GetHeadToHeadAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId,
        IHeadToHeadService headToHeadService,
        CancellationToken cancellationToken)
    {
        if (team1Id == team2Id)
        {
            return Results.BadRequest(new { error = "Team IDs must be different" });
        }

        var h2h = await headToHeadService.GetOrCalculateAsync(
            team1Id,
            team2Id,
            competitionId,
            cancellationToken);

        var response = new HeadToHeadDto(
            h2h.Team1Id,
            h2h.Team1?.Name ?? string.Empty,
            h2h.Team2Id,
            h2h.Team2?.Name ?? string.Empty,
            h2h.TotalMatches,
            h2h.Team1Wins,
            h2h.Team2Wins,
            h2h.Draws,
            h2h.Team1Goals,
            h2h.Team2Goals,
            h2h.BttsRate,
            h2h.Over25Rate,
            h2h.RecentTeam1Wins,
            h2h.RecentTeam2Wins,
            h2h.RecentDraws,
            h2h.LastMatchDate,
            h2h.CalculatedAt);

        return Results.Ok(response);
    }
}
