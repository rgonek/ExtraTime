using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Leagues.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Leagues.Commands.UpdateLeague;

public sealed record UpdateLeagueCommand(
    Guid LeagueId,
    string Name,
    string? Description,
    bool IsPublic,
    int MaxMembers,
    int ScoreExactMatch,
    int ScoreCorrectResult,
    int BettingDeadlineMinutes,
    Guid[]? AllowedCompetitionIds) : IRequest<Result<LeagueDto>>;
