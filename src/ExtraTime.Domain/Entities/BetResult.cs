namespace ExtraTime.Domain.Entities;

public sealed class BetResult
{
    public Guid BetId { get; set; }  // Primary key (one-to-one with Bet)
    public Bet Bet { get; set; } = null!;

    // Scoring
    public int PointsEarned { get; set; }
    public bool IsExactMatch { get; set; }
    public bool IsCorrectResult { get; set; }

    // Calculation metadata
    public DateTime CalculatedAt { get; set; }
}
