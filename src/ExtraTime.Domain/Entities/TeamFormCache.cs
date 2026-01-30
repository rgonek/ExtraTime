using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class TeamFormCache : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public required Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }

    public int HomeMatchesPlayed { get; set; }
    public int HomeWins { get; set; }
    public int HomeGoalsScored { get; set; }
    public int HomeGoalsConceded { get; set; }

    public int AwayMatchesPlayed { get; set; }
    public int AwayWins { get; set; }
    public int AwayGoalsScored { get; set; }
    public int AwayGoalsConceded { get; set; }

    public double PointsPerMatch { get; set; }
    public double GoalsPerMatch { get; set; }
    public double GoalsConcededPerMatch { get; set; }
    public double HomeWinRate { get; set; }
    public double AwayWinRate { get; set; }

    public int CurrentStreak { get; set; }
    public string RecentForm { get; set; } = "";

    public int MatchesAnalyzed { get; set; }
    public DateTime CalculatedAt { get; set; }
    public DateTime? LastMatchDate { get; set; }

    public double GetFormScore()
    {
        if (MatchesPlayed == 0) return 50.0;
        return (PointsPerMatch / 3.0) * 100;
    }

    public double GetHomeStrength()
    {
        if (HomeMatchesPlayed == 0) return 0.5;
        return HomeWinRate;
    }

    public double GetAwayStrength()
    {
        if (AwayMatchesPlayed == 0) return 0.3;
        return AwayWinRate;
    }

    public double GetAttackStrength()
    {
        if (MatchesPlayed == 0) return 1.5;
        return GoalsPerMatch;
    }

    public double GetDefenseStrength()
    {
        if (MatchesPlayed == 0) return 1.5;
        return GoalsConcededPerMatch;
    }
}
