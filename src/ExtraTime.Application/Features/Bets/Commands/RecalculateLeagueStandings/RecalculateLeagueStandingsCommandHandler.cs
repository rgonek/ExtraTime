using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using Mediator;

namespace ExtraTime.Application.Features.Bets.Commands.RecalculateLeagueStandings;

public sealed class RecalculateLeagueStandingsCommandHandler(
    IStandingsCalculator standingsCalculator) : IRequestHandler<RecalculateLeagueStandingsCommand, Result>
{
    public async ValueTask<Result> Handle(
        RecalculateLeagueStandingsCommand request,
        CancellationToken cancellationToken)
    {
        foreach (var leagueId in request.LeagueIds)
        {
            await standingsCalculator.RecalculateLeagueStandingsAsync(leagueId, cancellationToken);
        }

        return Result.Success();
    }
}
