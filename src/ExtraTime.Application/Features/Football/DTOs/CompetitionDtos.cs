namespace ExtraTime.Application.Features.Football.DTOs;

// DTOs from Football-Data.org API
public sealed record CompetitionApiDto(
    int Id,
    string Name,
    string Code,
    AreaApiDto Area,
    CurrentSeasonApiDto? CurrentSeason,
    string? Emblem);

public sealed record AreaApiDto(string Name);

public sealed record CurrentSeasonApiDto(
    int Id,
    int? CurrentMatchday,
    DateTime? StartDate,
    DateTime? EndDate);

// Application DTOs (for our API responses)
public sealed record CompetitionDto(
    Guid Id,
    int ExternalId,
    string Name,
    string Code,
    string Country,
    string? LogoUrl,
    int? CurrentMatchday,
    DateTime? CurrentSeasonStart,
    DateTime? CurrentSeasonEnd,
    DateTime LastSyncedAt);

public sealed record CompetitionSummaryDto(
    Guid Id,
    string Name,
    string Code,
    string Country,
    string? LogoUrl);
