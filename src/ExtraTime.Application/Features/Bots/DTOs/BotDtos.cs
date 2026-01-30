namespace ExtraTime.Application.Features.Bots.DTOs;

public sealed record BotDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastBetPlacedAt);

public sealed record BotSummaryDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy);

public sealed record LeagueBotDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy,
    DateTime AddedAt);

public sealed record CreateBotRequest(
    string Name,
    string? AvatarUrl,
    string Strategy);

public sealed record AddBotToLeagueRequest(Guid BotId);
