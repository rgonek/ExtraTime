namespace ExtraTime.Application.Features.Football.DTOs;

// DTOs from Football-Data.org API
public sealed record TeamApiDto(
    int Id,
    string Name,
    string ShortName,
    string? Tla,
    string? Crest,
    string? ClubColors,
    string? Venue);

public sealed record TeamsApiResponse(IReadOnlyList<TeamApiDto> Teams);

// Application DTOs (for our API responses)
public sealed record TeamDto(
    Guid Id,
    int ExternalId,
    string Name,
    string ShortName,
    string? Tla,
    string? LogoUrl,
    string? ClubColors,
    string? Venue,
    DateTime LastSyncedAt);

public sealed record TeamSummaryDto(
    Guid Id,
    string Name,
    string ShortName,
    string? Tla,
    string? LogoUrl);
