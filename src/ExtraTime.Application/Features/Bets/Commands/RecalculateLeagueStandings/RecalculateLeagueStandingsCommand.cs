using ExtraTime.Application.Common;
using Mediator;

namespace ExtraTime.Application.Features.Bets.Commands.RecalculateLeagueStandings;

public sealed record RecalculateLeagueStandingsCommand(
    Guid[] LeagueIds) : IRequest<Result>;
