using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class LeagueStanding : BaseEntity
{
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Core Stats
    public int TotalPoints { get; set; }
    public int BetsPlaced { get; set; }
    public int ExactMatches { get; set; }
    public int CorrectResults { get; set; }

    // Streak Tracking
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    // Metadata
    public DateTime LastUpdatedAt { get; set; }
}
