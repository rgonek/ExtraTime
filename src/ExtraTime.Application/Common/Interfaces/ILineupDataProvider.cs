namespace ExtraTime.Application.Common.Interfaces;

public interface ILineupDataProvider
{
    Task<MatchLineupData?> GetMatchLineupAsync(
        MatchLineupRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record MatchLineupRequest(
    int MatchExternalId,
    string HomeTeamName,
    string AwayTeamName,
    DateTime MatchDateUtc,
    string CompetitionCode);

public sealed record MatchLineupData(
    TeamLineupData HomeTeam,
    TeamLineupData AwayTeam);

public sealed record TeamLineupData(
    string? Formation,
    string? CoachName,
    string? CaptainName,
    IReadOnlyList<LineupPlayerData> StartingXi,
    IReadOnlyList<LineupPlayerData> Bench);

public sealed record LineupPlayerData(
    int Id,
    string Name,
    string? Position,
    int? ShirtNumber);
