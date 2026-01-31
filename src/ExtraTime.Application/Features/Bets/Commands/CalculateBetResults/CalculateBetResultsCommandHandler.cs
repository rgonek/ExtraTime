using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Bets.Commands.CalculateBetResults;

public sealed class CalculateBetResultsCommandHandler(
    IApplicationDbContext context,
    IJobDispatcher jobDispatcher) : IRequestHandler<CalculateBetResultsCommand, Result>
{
    public async ValueTask<Result> Handle(
        CalculateBetResultsCommand request,
        CancellationToken cancellationToken)
    {
        // Fetch match with final score
        var match = await context.Matches
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);

        if (match == null)
        {
            return Result.Failure(BetErrors.MatchNotFound);
        }

        // Ensure match has final scores
        if (!match.HomeScore.HasValue || !match.AwayScore.HasValue)
        {
            return Result.Failure("Match does not have final scores yet");
        }

        // Find all bets for this match across all leagues
        var bets = await context.Bets
            .Include(b => b.Result)
            .Where(b => b.MatchId == request.MatchId)
            .ToListAsync(cancellationToken);

        if (bets.Count == 0)
        {
            return Result.Success();
        }

        // Get all distinct league IDs for fetching league settings
        var leagueIds = bets.Select(b => b.LeagueId).Distinct().ToList();
        var leagues = await context.Leagues
            .AsNoTracking()
            .Where(l => leagueIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        // Calculate results for each bet
        foreach (var bet in bets)
        {
            if (!leagues.TryGetValue(bet.LeagueId, out var league))
            {
                continue;
            }

            var result = bet.CalculatePoints(match, league.ScoreExactMatch, league.ScoreCorrectResult);

            if (bet.Result == null)
            {
                // Create new result using factory method
                var betResult = BetResult.Create(
                    bet.Id,
                    result.PointsEarned,
                    result.IsExactMatch,
                    result.IsCorrectResult);
                context.BetResults.Add(betResult);
            }
            else
            {
                // Update existing result (recalculation scenario)
                bet.Result.Update(result.PointsEarned, result.IsExactMatch, result.IsCorrectResult);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        // Enqueue standings recalculation job for affected leagues
        if (leagueIds.Count > 0)
        {
            await jobDispatcher.EnqueueAsync(
                "RecalculateLeagueStandings",
                new { leagueIds = leagueIds.ToArray() },
                cancellationToken);
        }

        return Result.Success();
    }
}
