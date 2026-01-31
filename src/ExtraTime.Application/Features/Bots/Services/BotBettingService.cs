using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Application.Features.Bots.Services;

public sealed class BotBettingService(
    IApplicationDbContext context,
    BotStrategyFactory strategyFactory,
    ITeamFormCalculator formCalculator,
    TimeProvider timeProvider,
    ILogger<BotBettingService> logger) : IBotBettingService
{
    public async Task<int> PlaceBetsForUpcomingMatchesAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cutoffTime = now.AddHours(24);

        await formCalculator.RefreshAllFormCachesAsync(cancellationToken);

        var leagues = await context.Leagues
            .Include(l => l.BotMembers)
                .ThenInclude(bm => bm.Bot)
            .Where(l => l.BotsEnabled && l.BotMembers.Any())
            .ToListAsync(cancellationToken);

        int totalBetsPlaced = 0;

        foreach (var league in leagues)
        {
            var betsPlaced = await PlaceBetsForLeagueInternalAsync(league, now, cutoffTime, cancellationToken);
            totalBetsPlaced += betsPlaced;
        }

        return totalBetsPlaced;
    }

    public async Task<int> PlaceBetsForLeagueAsync(Guid leagueId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cutoffTime = now.AddHours(24);

        var league = await context.Leagues
            .Include(l => l.BotMembers)
                .ThenInclude(bm => bm.Bot)
            .FirstOrDefaultAsync(l => l.Id == leagueId, cancellationToken);

        if (league == null || !league.BotsEnabled)
            return 0;

        return await PlaceBetsForLeagueInternalAsync(league, now, cutoffTime, cancellationToken);
    }

    private async Task<int> PlaceBetsForLeagueInternalAsync(
        League league,
        DateTime now,
        DateTime cutoffTime,
        CancellationToken cancellationToken)
    {
        var matches = await context.Matches
            .Where(m => m.Status == MatchStatus.Scheduled || m.Status == MatchStatus.Timed)
            .Where(m => m.MatchDateUtc > now && m.MatchDateUtc <= cutoffTime)
            .Where(m => league.CanAcceptBet(m.CompetitionId))
            .ToListAsync(cancellationToken);

        int betsPlaced = 0;

        foreach (var botMember in league.BotMembers)
        {
            var bot = botMember.Bot;
            if (!bot.IsActive) continue;

            var strategy = strategyFactory.GetStrategy(bot.Strategy);

            foreach (var match in matches)
            {
                var existingBet = await context.Bets
                    .FirstOrDefaultAsync(b =>
                        b.LeagueId == league.Id &&
                        b.UserId == bot.UserId &&
                        b.MatchId == match.Id,
                        cancellationToken);

                if (existingBet != null) continue;

                if (!match.IsOpenForBetting(league.BettingDeadlineMinutes, now)) continue;

                int homeScore, awayScore;
                if (strategy is StatsAnalystStrategy statsStrategy)
                {
                    (homeScore, awayScore) = await statsStrategy.GeneratePredictionAsync(
                        match, bot.Configuration, cancellationToken);
                }
                else
                {
                    (homeScore, awayScore) = strategy.GeneratePrediction(match, bot.Configuration);
                }

                var bet = Bet.Place(
                    league.Id,
                    bot.UserId,
                    match.Id,
                    homeScore,
                    awayScore);

                context.Bets.Add(bet);
                betsPlaced++;

                logger.LogDebug(
                    "Bot {BotName} ({Strategy}) placed bet {HomeScore}-{AwayScore} for match {MatchId} in league {LeagueId}",
                    bot.Name, bot.Strategy, homeScore, awayScore, match.Id, league.Id);
            }

            bot.RecordBetPlaced();
        }

        if (betsPlaced > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return betsPlaced;
    }
}
