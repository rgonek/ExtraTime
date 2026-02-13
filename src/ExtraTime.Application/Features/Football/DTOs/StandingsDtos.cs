namespace ExtraTime.Application.Features.Football.DTOs;

public sealed record StandingsApiResponse(
    StandingsCompetitionApiDto Competition,
    SeasonApiDto Season,
    IReadOnlyList<StandingTableApiDto> Standings);

public sealed record SeasonApiDto(
    int Id,
    DateTime StartDate,
    DateTime EndDate,
    int CurrentMatchday,
    StandingsTeamApiDto? Winner);

public sealed record StandingTableApiDto(
    string Stage,
    string Type,
    string? Group,
    IReadOnlyList<StandingRowApiDto> Table);

public sealed record StandingRowApiDto(
    int Position,
    StandingsTeamApiDto Team,
    int PlayedGames,
    int Won,
    int Draw,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points,
    string? Form);

public sealed record StandingsTeamApiDto(int Id, string Name, string ShortName, string? Crest);
public sealed record StandingsCompetitionApiDto(int Id, string Name, string Code);
