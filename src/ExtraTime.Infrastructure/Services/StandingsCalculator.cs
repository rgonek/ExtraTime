using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Infrastructure.Services;

public sealed class StandingsCalculator(IApplicationDbContext context) : IStandingsCalculator
{
    public async Task RecalculateLeagueStandingsAsync(
        Guid leagueId,
        CancellationToken cancellationToken = default)
    {
        // Get all bets with results for this league
        var betsWithResults = await context.Bets
            .Include(b => b.Result)
            .Include(b => b.Match)
            .Where(b => b.LeagueId == leagueId && b.Result != null)
            .OrderBy(b => b.Match.MatchDateUtc)
            .ToListAsync(cancellationToken);

        // Group by user
        var userBets = betsWithResults.GroupBy(b => b.UserId);

        foreach (var userGroup in userBets)
        {
            var userId = userGroup.Key;
            var bets = userGroup.OrderBy(b => b.Match.MatchDateUtc).ToList();

            // Calculate totals
            var totalPoints = bets.Sum(b => b.Result!.PointsEarned);
            var betsPlaced = bets.Count;
            var exactMatches = bets.Count(b => b.Result!.IsExactMatch);
            var correctResults = bets.Count(b => b.Result!.IsCorrectResult);

            // Calculate streaks
            var (currentStreak, bestStreak) = CalculateStreaks(bets);

            // Upsert standing
            var standing = await context.LeagueStandings
                .FirstOrDefaultAsync(s => s.LeagueId == leagueId && s.UserId == userId, cancellationToken);

            if (standing == null)
            {
                standing = new LeagueStanding
                {
                    LeagueId = leagueId,
                    UserId = userId
                };
                context.LeagueStandings.Add(standing);
            }

            standing.TotalPoints = totalPoints;
            standing.BetsPlaced = betsPlaced;
            standing.ExactMatches = exactMatches;
            standing.CorrectResults = correctResults;
            standing.CurrentStreak = currentStreak;
            standing.BestStreak = bestStreak;
            standing.LastUpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    // PRECONDITION: All bets in the list must have non-null Result property
    // (ensured by filtering with 'b.Result != null' in the calling method)
    private static (int CurrentStreak, int BestStreak) CalculateStreaks(List<Bet> bets)
    {
        var currentStreak = 0;
        var bestStreak = 0;
        var tempStreak = 0;

        foreach (var bet in bets)
        {
            // Null-forgiving operator safe due to precondition
            if (bet.Result!.IsCorrectResult)
            {
                tempStreak++;
                bestStreak = Math.Max(bestStreak, tempStreak);
            }
            else
            {
                tempStreak = 0;
            }
        }

        // Current streak = most recent consecutive correct results
        for (var i = bets.Count - 1; i >= 0; i--)
        {
            if (bets[i].Result!.IsCorrectResult)
            {
                currentStreak++;
            }
            else
            {
                break;
            }
        }

        return (currentStreak, bestStreak);
    }
}
