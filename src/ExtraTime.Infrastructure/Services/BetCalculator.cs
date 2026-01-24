using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.DTOs;
using ExtraTime.Domain.Entities;

namespace ExtraTime.Infrastructure.Services;

public sealed class BetCalculator : IBetCalculator
{
    public BetResultDto CalculateResult(Bet bet, Match match, League league)
    {
        // Check if match has final score
        if (!match.HomeScore.HasValue || !match.AwayScore.HasValue)
        {
            return new BetResultDto(0, false, false);
        }

        var actualHome = match.HomeScore.Value;
        var actualAway = match.AwayScore.Value;
        var predictedHome = bet.PredictedHomeScore;
        var predictedAway = bet.PredictedAwayScore;

        // Check exact match
        var isExactMatch = predictedHome == actualHome && predictedAway == actualAway;
        if (isExactMatch)
        {
            return new BetResultDto(league.ScoreExactMatch, true, true);
        }

        // Check correct result (win/draw/loss)
        var actualResult = GetMatchResult(actualHome, actualAway);
        var predictedResult = GetMatchResult(predictedHome, predictedAway);
        var isCorrectResult = actualResult == predictedResult;

        var points = isCorrectResult ? league.ScoreCorrectResult : 0;
        return new BetResultDto(points, false, isCorrectResult);
    }

    private static MatchResult GetMatchResult(int homeScore, int awayScore)
        => homeScore > awayScore ? MatchResult.HomeWin
         : homeScore < awayScore ? MatchResult.AwayWin
         : MatchResult.Draw;

    private enum MatchResult { HomeWin, Draw, AwayWin }
}
