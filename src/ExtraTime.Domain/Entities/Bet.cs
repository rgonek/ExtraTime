using ExtraTime.Domain.Common;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Entities;

public sealed class Bet : BaseAuditableEntity
{
    public Guid LeagueId { get; private set; }
    public League League { get; internal set; } = null!;

    public Guid UserId { get; private set; }
    public User User { get; internal set; } = null!;

    public Guid MatchId { get; private set; }
    public Match Match { get; internal set; } = null!;

    // Prediction
    public int PredictedHomeScore { get; private set; }
    public int PredictedAwayScore { get; private set; }

    // Timestamps
    public DateTime PlacedAt { get; private set; }
    public DateTime? LastUpdatedAt { get; private set; }

    // Navigation
    public BetResult? Result { get; internal set; }

    private Bet() { } // Required for EF Core

    public static Bet Place(
        Guid leagueId,
        Guid userId,
        Guid matchId,
        int homeScore,
        int awayScore)
    {
        if (homeScore < 0 || awayScore < 0)
            throw new ArgumentException("Scores cannot be negative");

        var bet = new Bet
        {
            LeagueId = leagueId,
            UserId = userId,
            MatchId = matchId,
            PredictedHomeScore = homeScore,
            PredictedAwayScore = awayScore,
            PlacedAt = DateTime.UtcNow
        };

        bet.AddDomainEvent(new BetPlaced(bet.Id, userId, matchId));
        return bet;
    }

    public void Update(int homeScore, int awayScore, DateTime currentTime, int deadlineMinutes, DateTime matchStartTime)
    {
        if (!CanBeModified(currentTime, deadlineMinutes, matchStartTime))
            throw new InvalidOperationException("Betting is closed for this match");

        if (homeScore < 0 || awayScore < 0)
            throw new ArgumentException("Scores cannot be negative");

        PredictedHomeScore = homeScore;
        PredictedAwayScore = awayScore;
        LastUpdatedAt = currentTime;

        AddDomainEvent(new BetUpdated(Id, homeScore, awayScore));
    }

    public bool CanBeModified(DateTime currentTime, int deadlineMinutes, DateTime matchStartTime)
    {
        return currentTime <= matchStartTime.AddMinutes(-deadlineMinutes);
    }

    public BetCalculationResult CalculatePoints(Match match, int exactMatchPoints, int correctResultPoints)
    {
        if (match.HomeScore == null || match.AwayScore == null)
            return new BetCalculationResult(0, false, false);

        int points;
        bool isExactMatch = false;
        bool isCorrectResult = false;

        if (match.HomeScore == PredictedHomeScore && match.AwayScore == PredictedAwayScore)
        {
            points = exactMatchPoints;
            isExactMatch = true;
            isCorrectResult = true;
        }
        else
        {
            var predictedResult = Math.Sign(PredictedHomeScore - PredictedAwayScore);
            var actualResult = Math.Sign(match.HomeScore.Value - match.AwayScore.Value);

            isCorrectResult = predictedResult == actualResult;
            points = isCorrectResult ? correctResultPoints : 0;
        }

        AddDomainEvent(new BetScored(Id, points));
        return new BetCalculationResult(points, isExactMatch, isCorrectResult);
    }
}

public sealed record BetCalculationResult(int PointsEarned, bool IsExactMatch, bool IsCorrectResult);
