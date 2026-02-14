namespace ExtraTime.Application.Features.Football.DTOs;

public sealed record CompetitionTeamsApiFilter(
    int? Season = null);

public sealed record CompetitionMatchesApiFilter(
    int? Season = null,
    int? Matchday = null,
    string? Status = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Stage = null,
    string? Group = null);

public sealed record CompetitionStandingsApiFilter(
    int? Season = null,
    int? Matchday = null,
    DateTime? Date = null);
