using ExtraTime.Application.Common;
using Mediator;

namespace ExtraTime.Application.Features.Bets.Commands.DeleteBet;

public sealed record DeleteBetCommand(
    Guid LeagueId,
    Guid BetId) : IRequest<Result>;
