using ExtraTime.Domain.Enums;

namespace ExtraTime.Application.Features.Leagues.DTOs;

// Request DTOs
public sealed record CreateLeagueRequest(
    string Name,
    string? Description,
    bool IsPublic,
    int MaxMembers,
    int ScoreExactMatch,
    int ScoreCorrectResult,
    int BettingDeadlineMinutes,
    Guid[]? AllowedCompetitionIds,
    DateTime? InviteCodeExpiresAt);

public sealed record UpdateLeagueRequest(
    string Name,
    string? Description,
    bool IsPublic,
    int MaxMembers,
    int ScoreExactMatch,
    int ScoreCorrectResult,
    int BettingDeadlineMinutes,
    Guid[]? AllowedCompetitionIds);

public sealed record JoinLeagueRequest(string InviteCode);

public sealed record RegenerateInviteCodeRequest(DateTime? ExpiresAt);

// Response DTOs
public sealed record LeagueDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerUsername,
    bool IsPublic,
    int MaxMembers,
    int CurrentMemberCount,
    int ScoreExactMatch,
    int ScoreCorrectResult,
    int BettingDeadlineMinutes,
    Guid[]? AllowedCompetitionIds,
    string InviteCode,
    DateTime? InviteCodeExpiresAt,
    DateTime CreatedAt);

public sealed record LeagueSummaryDto(
    Guid Id,
    string Name,
    string OwnerUsername,
    int MemberCount,
    bool IsPublic,
    DateTime CreatedAt);

public sealed record LeagueMemberDto(
    Guid UserId,
    string Username,
    string Email,
    MemberRole Role,
    DateTime JoinedAt);

public sealed record LeagueDetailDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerUsername,
    bool IsPublic,
    int MaxMembers,
    int ScoreExactMatch,
    int ScoreCorrectResult,
    int BettingDeadlineMinutes,
    Guid[]? AllowedCompetitionIds,
    string InviteCode,
    DateTime? InviteCodeExpiresAt,
    DateTime CreatedAt,
    List<LeagueMemberDto> Members);
