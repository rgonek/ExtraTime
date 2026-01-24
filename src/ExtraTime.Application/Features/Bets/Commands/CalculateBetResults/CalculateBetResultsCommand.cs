using ExtraTime.Application.Common;
using Mediator;

namespace ExtraTime.Application.Features.Bets.Commands.CalculateBetResults;

public sealed record CalculateBetResultsCommand(
    Guid MatchId,
    Guid CompetitionId) : IRequest<Result>;
