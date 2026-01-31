using ExtraTime.Domain.Common;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Entities;

public sealed class BetResult : BaseEntity
{
    public Guid BetId { get; private set; }
    public Bet Bet { get; private set; } = null!;

    // Scoring
    public int PointsEarned { get; private set; }
    public bool IsExactMatch { get; private set; }
    public bool IsCorrectResult { get; private set; }

    // Calculation metadata
    public DateTime CalculatedAt { get; private set; }

    private BetResult() { } // Required for EF Core

    public static BetResult Create(
        Guid betId,
        int pointsEarned,
        bool isExactMatch,
        bool isCorrectResult)
    {
        if (pointsEarned < 0)
            throw new ArgumentException("Points earned cannot be negative", nameof(pointsEarned));

        var result = new BetResult
        {
            Id = betId, // Same as BetId for one-to-one
            BetId = betId,
            PointsEarned = pointsEarned,
            IsExactMatch = isExactMatch,
            IsCorrectResult = isCorrectResult,
            CalculatedAt = Clock.UtcNow
        };

        result.AddDomainEvent(new BetResultCalculated(betId, pointsEarned, isExactMatch, isCorrectResult));
        return result;
    }

    public static BetResult CalculateFrom(Bet bet, Match match, int exactMatchPoints, int correctResultPoints)
    {
        if (match.HomeScore == null || match.AwayScore == null)
            throw new InvalidOperationException("Cannot calculate result for match without scores");

        var calculation = bet.CalculatePoints(match, exactMatchPoints, correctResultPoints);
        return Create(bet.Id, calculation.PointsEarned, calculation.IsExactMatch, calculation.IsCorrectResult);
    }

    public void Update(int pointsEarned, bool isExactMatch, bool isCorrectResult)
    {
        if (pointsEarned < 0)
            throw new ArgumentException("Points earned cannot be negative", nameof(pointsEarned));

        PointsEarned = pointsEarned;
        IsExactMatch = isExactMatch;
        IsCorrectResult = isCorrectResult;
        CalculatedAt = Clock.UtcNow;

        AddDomainEvent(new BetResultUpdated(BetId, pointsEarned, isExactMatch, isCorrectResult));
    }
}
