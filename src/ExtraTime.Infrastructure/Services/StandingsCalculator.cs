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

            // Upsert standing
            var standing = await context.LeagueStandings
                .FirstOrDefaultAsync(s => s.LeagueId == leagueId && s.UserId == userId, cancellationToken);

            if (standing == null)
            {
                standing = LeagueStanding.Create(leagueId, userId);
                context.LeagueStandings.Add(standing);
            }
            else
            {
                standing.Reset();
            }

            foreach (var bet in bets)
            {
                standing.ApplyBetResult(
                    bet.Result!.PointsEarned,
                    bet.Result.IsExactMatch,
                    bet.Result.IsCorrectResult);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
