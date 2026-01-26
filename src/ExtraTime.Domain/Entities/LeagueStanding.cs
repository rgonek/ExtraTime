using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class LeagueStanding : BaseEntity
{
    public Guid LeagueId { get; private set; }
    public League League { get; private set; } = null!;

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    // Core Stats
    public int TotalPoints { get; internal set; }
    public int BetsPlaced { get; internal set; }
    public int ExactMatches { get; internal set; }
    public int CorrectResults { get; internal set; }

    // Streak Tracking
    public int CurrentStreak { get; internal set; }
    public int BestStreak { get; internal set; }

    // Metadata
    public DateTime LastUpdatedAt { get; private set; }

    private LeagueStanding() { } // Required for EF Core

    public static LeagueStanding Create(Guid leagueId, Guid userId)
    {
        return new LeagueStanding
        {
            LeagueId = leagueId,
            UserId = userId,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    public void ApplyBetResult(int points, bool isExactMatch, bool isCorrectResult)
    {
        TotalPoints += points;
        BetsPlaced++;

        if (isExactMatch) ExactMatches++;
        if (isCorrectResult) CorrectResults++;

        if (points > 0)
        {
            CurrentStreak++;
            if (CurrentStreak > BestStreak)
            {
                BestStreak = CurrentStreak;
            }
        }
        else
        {
            CurrentStreak = 0;
        }

        LastUpdatedAt = DateTime.UtcNow;
    }

    public void Reset()
    {
        TotalPoints = 0;
        BetsPlaced = 0;
        ExactMatches = 0;
        CorrectResults = 0;
        CurrentStreak = 0;
        BestStreak = 0;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
