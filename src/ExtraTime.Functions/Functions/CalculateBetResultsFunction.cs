using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Functions.Functions;

public sealed class CalculateBetResultsFunction(
    ApplicationDbContext dbContext,
    IBetCalculator betCalculator,
    ILogger<CalculateBetResultsFunction> logger)
{
    /// <summary>
    /// Calculates bet results for recently finished matches every 15 minutes.
    /// CRON: 0 */15 * * * * (every 15 minutes)
    /// </summary>
    [Function("CalculateBetResults")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("CalculateBetResults function started at: {Time}", DateTime.UtcNow);

        try
        {
            // Find all bets for finished matches that don't have results yet
            var uncalculatedBets = await dbContext.Bets
                .Include(b => b.Match)
                .Include(b => b.League)
                .Include(b => b.Result)
                .Where(b => b.Match.Status == MatchStatus.Finished
                         && b.Match.HomeScore.HasValue
                         && b.Match.AwayScore.HasValue
                         && b.Result == null)
                .ToListAsync(cancellationToken);

            if (uncalculatedBets.Count == 0)
            {
                logger.LogInformation("CalculateBetResults: No uncalculated bets found");
                return;
            }

            logger.LogInformation("CalculateBetResults: Found {Count} uncalculated bets", uncalculatedBets.Count);

            var calculatedCount = 0;
            foreach (var bet in uncalculatedBets)
            {
                var resultDto = betCalculator.CalculateResult(bet, bet.Match, bet.League);
                var betResult = BetResult.Create(
                    bet.Id,
                    resultDto.PointsEarned,
                    resultDto.IsExactMatch,
                    resultDto.IsCorrectResult);

                dbContext.BetResults.Add(betResult);
                calculatedCount++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("CalculateBetResults completed: {Count} bets calculated", calculatedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CalculateBetResults function failed");
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogInformation("Next CalculateBetResults scheduled for: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }
}
