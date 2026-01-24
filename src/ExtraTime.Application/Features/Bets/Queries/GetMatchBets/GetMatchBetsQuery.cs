using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bets.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Bets.Queries.GetMatchBets;

public sealed record GetMatchBetsQuery(
    Guid LeagueId,
    Guid MatchId) : IRequest<Result<List<MatchBetDto>>>;
