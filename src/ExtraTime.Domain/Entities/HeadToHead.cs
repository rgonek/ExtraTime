using ExtraTime.Domain.Common;

namespace ExtraTime.Domain.Entities;

public sealed class HeadToHead : BaseEntity
{
    public Guid Team1Id { get; private set; }
    public Team Team1 { get; private set; } = null!;

    public Guid Team2Id { get; private set; }
    public Team Team2 { get; private set; } = null!;

    public Guid? CompetitionId { get; private set; }
    public Competition? Competition { get; private set; }

    public int TotalMatches { get; private set; }
    public int Team1Wins { get; private set; }
    public int Team2Wins { get; private set; }
    public int Draws { get; private set; }

    public int Team1Goals { get; private set; }
    public int Team2Goals { get; private set; }

    public int BothTeamsScoredCount { get; private set; }
    public int Over25Count { get; private set; }

    public double BttsRate => TotalMatches > 0 ? (double)BothTeamsScoredCount / TotalMatches : 0;
    public double Over25Rate => TotalMatches > 0 ? (double)Over25Count / TotalMatches : 0;

    public int Team1HomeMatches { get; private set; }
    public int Team1HomeWins { get; private set; }
    public int Team1HomeGoals { get; private set; }
    public int Team1HomeConceded { get; private set; }

    public DateTime? LastMatchDate { get; private set; }
    public Guid? LastMatchId { get; private set; }

    public int RecentMatchesCount { get; private set; }
    public int RecentTeam1Wins { get; private set; }
    public int RecentTeam2Wins { get; private set; }
    public int RecentDraws { get; private set; }

    public int MatchesAnalyzed { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    private HeadToHead() { }

    public static HeadToHead Create(Guid team1Id, Guid team2Id, Guid? competitionId = null)
    {
        if (team1Id == team2Id)
            throw new ArgumentException("Team IDs must be different", nameof(team2Id));

        var (first, second) = team1Id.CompareTo(team2Id) < 0
            ? (team1Id, team2Id)
            : (team2Id, team1Id);

        return new HeadToHead
        {
            Team1Id = first,
            Team2Id = second,
            CompetitionId = competitionId,
            CalculatedAt = Clock.UtcNow
        };
    }

    public void UpdateStats(
        int totalMatches,
        int team1Wins,
        int team2Wins,
        int draws,
        int team1Goals,
        int team2Goals,
        int team1HomeMatches,
        int team1HomeWins,
        int team1HomeGoals,
        int team1HomeConceded,
        int bothTeamsScoredCount,
        int over25Count,
        DateTime? lastMatchDate,
        Guid? lastMatchId,
        int recentMatchesCount,
        int recentTeam1Wins,
        int recentTeam2Wins,
        int recentDraws,
        int matchesAnalyzed)
    {
        TotalMatches = totalMatches;
        Team1Wins = team1Wins;
        Team2Wins = team2Wins;
        Draws = draws;
        Team1Goals = team1Goals;
        Team2Goals = team2Goals;
        Team1HomeMatches = team1HomeMatches;
        Team1HomeWins = team1HomeWins;
        Team1HomeGoals = team1HomeGoals;
        Team1HomeConceded = team1HomeConceded;
        BothTeamsScoredCount = bothTeamsScoredCount;
        Over25Count = over25Count;
        LastMatchDate = lastMatchDate;
        LastMatchId = lastMatchId;
        RecentMatchesCount = recentMatchesCount;
        RecentTeam1Wins = recentTeam1Wins;
        RecentTeam2Wins = recentTeam2Wins;
        RecentDraws = recentDraws;
        MatchesAnalyzed = matchesAnalyzed;
        CalculatedAt = Clock.UtcNow;
    }

    public HeadToHeadStats GetStatsForTeam(Guid teamId)
    {
        if (teamId == Team1Id)
        {
            return new HeadToHeadStats(
                Team1Wins,
                Team2Wins,
                Draws,
                Team1Goals,
                Team2Goals,
                TotalMatches,
                Team1HomeMatches,
                Team1HomeWins,
                RecentTeam1Wins,
                RecentMatchesCount,
                BttsRate,
                Over25Rate);
        }

        if (teamId == Team2Id)
        {
            var team2HomeMatches = TotalMatches - Team1HomeMatches;
            var team2HomeWins = Team2Wins - (Team1HomeMatches - Team1HomeWins - Draws);

            return new HeadToHeadStats(
                Team2Wins,
                Team1Wins,
                Draws,
                Team2Goals,
                Team1Goals,
                TotalMatches,
                team2HomeMatches,
                Math.Max(0, team2HomeWins),
                RecentTeam2Wins,
                RecentMatchesCount,
                BttsRate,
                Over25Rate);
        }

        throw new ArgumentException("Team not part of this head-to-head record", nameof(teamId));
    }
}

public sealed record HeadToHeadStats(
    int Wins,
    int Losses,
    int Draws,
    int GoalsFor,
    int GoalsAgainst,
    int TotalMatches,
    int HomeMatches,
    int HomeWins,
    int RecentWins,
    int RecentMatchesCount,
    double BttsRate,
    double Over25Rate)
{
    public double WinRate => TotalMatches > 0 ? (double)Wins / TotalMatches : 0;
    public int GoalDifference => GoalsFor - GoalsAgainst;
}
