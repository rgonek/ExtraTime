using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

/// <summary>
/// Cached expected goals statistics for a team in a competition and season.
/// </summary>
public sealed class TeamXgStats : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public required Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public required string Season { get; set; }

    public double XgFor { get; set; }
    public double XgAgainst { get; set; }
    public double XgDiff { get; set; }

    public double XgPerMatch { get; set; }
    public double XgAgainstPerMatch { get; set; }

    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
    public double XgOverperformance { get; set; }
    public double XgaOverperformance { get; set; }

    public double RecentXgPerMatch { get; set; }
    public double RecentXgAgainstPerMatch { get; set; }

    public int MatchesPlayed { get; set; }

    public int UnderstatTeamId { get; set; }
    public DateTime LastSyncedAt { get; set; }

    public double GetXgStrength() => XgPerMatch > 0 ? XgPerMatch / 1.5 : 0.5;
    public double GetDefensiveXgStrength() => XgAgainstPerMatch > 0 ? 1.5 / XgAgainstPerMatch : 0.5;
    public bool IsOverperforming() => XgOverperformance > 0;
    public bool IsDefensivelySound() => XgaOverperformance > 0;
}
