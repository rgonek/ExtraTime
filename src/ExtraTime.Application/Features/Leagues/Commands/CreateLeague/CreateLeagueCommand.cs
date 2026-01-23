using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Leagues.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Leagues.Commands.CreateLeague;

public sealed record CreateLeagueCommand(
    string Name,
    string? Description,
    bool IsPublic,
    int MaxMembers,
    int ScoreExactMatch,
    int ScoreCorrectResult,
    int BettingDeadlineMinutes,
    Guid[]? AllowedCompetitionIds,
    DateTime? InviteCodeExpiresAt) : IRequest<Result<LeagueDto>>;
