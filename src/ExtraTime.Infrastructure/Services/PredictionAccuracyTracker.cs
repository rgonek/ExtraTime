using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services;

public sealed class PredictionAccuracyTracker(
    IApplicationDbContext context,
    ILogger<PredictionAccuracyTracker> logger)
{
    public async Task RecalculateAccuracyAsync(
        DateTime fromDate,
        DateTime toDate,
        string periodType = "custom",
        CancellationToken cancellationToken = default)
    {
        var botsByUserId = await context.Bots
            .AsNoTracking()
            .ToDictionaryAsync(bot => bot.UserId, cancellationToken);

        var bets = await context.Bets
            .AsNoTracking()
            .Include(bet => bet.Match)
            .Include(bet => bet.Result)
            .Where(bet =>
                bet.Match.Status == MatchStatus.Finished &&
                bet.Match.MatchDateUtc >= fromDate &&
                bet.Match.MatchDateUtc < toDate)
            .ToListAsync(cancellationToken);

        var botBets = bets
            .Where(bet => botsByUserId.ContainsKey(bet.UserId))
            .ToList();

        var grouped = botBets.GroupBy(bet => botsByUserId[bet.UserId].Id);

        foreach (var group in grouped)
        {
            var bot = botsByUserId[group.First().UserId];
            var accuracy = CalculateAccuracy(group.ToList());

            var existing = await context.BotPredictionAccuracies
                .FirstOrDefaultAsync(record =>
                        record.BotId == bot.Id &&
                        record.PeriodType == periodType &&
                        record.PeriodStart == fromDate &&
                        record.PeriodEnd == toDate,
                    cancellationToken);

            if (existing is null)
            {
                existing = new BotPredictionAccuracy
                {
                    BotId = bot.Id,
                    Strategy = bot.Strategy,
                    PeriodStart = fromDate,
                    PeriodEnd = toDate,
                    PeriodType = periodType
                };
                context.BotPredictionAccuracies.Add(existing);
            }

            existing.TotalPredictions = accuracy.TotalPredictions;
            existing.ExactScores = accuracy.ExactScores;
            existing.CorrectResults = accuracy.CorrectResults;
            existing.GoalsOffBy1 = accuracy.GoalsOffBy1;
            existing.GoalsOffBy2 = accuracy.GoalsOffBy2;
            existing.GoalsOffBy3Plus = accuracy.GoalsOffBy3Plus;
            existing.ExactScoreAccuracy = accuracy.ExactScoreAccuracy;
            existing.CorrectResultAccuracy = accuracy.CorrectResultAccuracy;
            existing.Within1GoalAccuracy = accuracy.Within1GoalAccuracy;
            existing.MeanAbsoluteError = accuracy.MeanAbsoluteError;
            existing.RootMeanSquaredError = accuracy.RootMeanSquaredError;
            existing.HomeScoreMAE = accuracy.HomeScoreMAE;
            existing.AwayScoreMAE = accuracy.AwayScoreMAE;
            existing.TotalPointsEarned = accuracy.TotalPointsEarned;
            existing.AvgPointsPerBet = accuracy.AveragePointsPerBet;
            existing.BetsWon = accuracy.BetsWon;
            existing.BetsLost = accuracy.BetsLost;
            existing.LastUpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Recalculated prediction accuracy for {BotCount} bots", grouped.Count());
    }

    private static AccuracyMetrics CalculateAccuracy(List<Bet> bets)
    {
        if (bets.Count == 0)
        {
            return AccuracyMetrics.Empty;
        }

        var totalPredictions = bets.Count;
        var exactScores = 0;
        var correctResults = 0;
        var goalsOffBy1 = 0;
        var goalsOffBy2 = 0;
        var goalsOffBy3Plus = 0;
        var homeAbsoluteError = 0d;
        var awayAbsoluteError = 0d;
        var squaredError = 0d;
        var totalPoints = 0d;
        var betsWon = 0;

        foreach (var bet in bets)
        {
            var actualHome = bet.Match.HomeScore ?? 0;
            var actualAway = bet.Match.AwayScore ?? 0;

            if (bet.PredictedHomeScore == actualHome && bet.PredictedAwayScore == actualAway)
            {
                exactScores++;
            }

            var predictedOutcome = Math.Sign(bet.PredictedHomeScore - bet.PredictedAwayScore);
            var actualOutcome = Math.Sign(actualHome - actualAway);
            if (predictedOutcome == actualOutcome)
            {
                correctResults++;
            }

            var homeError = Math.Abs(actualHome - bet.PredictedHomeScore);
            var awayError = Math.Abs(actualAway - bet.PredictedAwayScore);
            var totalError = homeError + awayError;

            switch (totalError)
            {
                case <= 1:
                    goalsOffBy1++;
                    break;
                case 2:
                    goalsOffBy2++;
                    break;
                default:
                    goalsOffBy3Plus++;
                    break;
            }

            homeAbsoluteError += homeError;
            awayAbsoluteError += awayError;
            squaredError += Math.Pow(homeError, 2) + Math.Pow(awayError, 2);

            var points = bet.Result?.PointsEarned ?? 0;
            totalPoints += points;
            if (points > 0)
            {
                betsWon++;
            }
        }

        var totalAbsoluteError = homeAbsoluteError + awayAbsoluteError;
        return new AccuracyMetrics(
            totalPredictions,
            exactScores,
            correctResults,
            goalsOffBy1,
            goalsOffBy2,
            goalsOffBy3Plus,
            exactScores / (double)totalPredictions,
            correctResults / (double)totalPredictions,
            goalsOffBy1 / (double)totalPredictions,
            (totalAbsoluteError / totalPredictions) / 2d,
            Math.Sqrt(squaredError / (totalPredictions * 2d)),
            homeAbsoluteError / totalPredictions,
            awayAbsoluteError / totalPredictions,
            totalPoints,
            totalPoints / totalPredictions,
            betsWon,
            totalPredictions - betsWon);
    }

    private sealed record AccuracyMetrics(
        int TotalPredictions,
        int ExactScores,
        int CorrectResults,
        int GoalsOffBy1,
        int GoalsOffBy2,
        int GoalsOffBy3Plus,
        double ExactScoreAccuracy,
        double CorrectResultAccuracy,
        double Within1GoalAccuracy,
        double MeanAbsoluteError,
        double RootMeanSquaredError,
        double HomeScoreMAE,
        double AwayScoreMAE,
        double TotalPointsEarned,
        double AveragePointsPerBet,
        int BetsWon,
        int BetsLost)
    {
        public static AccuracyMetrics Empty { get; } = new(
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
    }
}
