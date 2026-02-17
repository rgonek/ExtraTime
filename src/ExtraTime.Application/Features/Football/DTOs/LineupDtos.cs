namespace ExtraTime.Application.Features.Football.DTOs;

public sealed record MatchLineupDto(
    Guid MatchId,
    Guid TeamId,
    string TeamName,
    string? Formation,
    string? CoachName,
    string? CaptainName,
    IReadOnlyList<LineupPlayerDto> StartingXi,
    IReadOnlyList<LineupPlayerDto> Bench);

public sealed record LineupPlayerDto(
    int Id,
    string Name,
    string? Position,
    int? ShirtNumber);

public sealed record TeamUsualLineupDto(
    Guid TeamId,
    string TeamName,
    string? UsualFormation,
    string? CaptainName,
    IReadOnlyList<UsualPlayerDto> Goalkeepers,
    IReadOnlyList<UsualPlayerDto> Defenders,
    IReadOnlyList<UsualPlayerDto> Midfielders,
    IReadOnlyList<UsualPlayerDto> Forwards,
    int MatchesAnalyzed,
    DateTime CalculatedAt);

public sealed record UsualPlayerDto(
    int Id,
    string Name,
    string? Position,
    int Appearances);
