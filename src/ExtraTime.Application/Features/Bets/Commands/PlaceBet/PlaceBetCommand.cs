using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bets.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Bets.Commands.PlaceBet;

public sealed record PlaceBetCommand(
    Guid LeagueId,
    Guid MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore) : IRequest<Result<BetDto>>;
