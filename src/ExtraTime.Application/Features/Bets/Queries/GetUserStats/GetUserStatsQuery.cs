using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bets.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Bets.Queries.GetUserStats;

public sealed record GetUserStatsQuery(
    Guid LeagueId,
    Guid UserId) : IRequest<Result<UserStatsDto>>;
