# Phase 9: Extended Football Data - Detailed Implementation Plan

## Overview
Sync additional Football-Data.org subresources for richer match display and preparation for smarter bot predictions. This phase focuses on data infrastructure - standings, scorers, lineups, and head-to-head records.

> **Note**: This phase is data-focused. Bot integration will be added when Phase 7.5 (Intelligent Stats-Based Bots) is implemented.

---

## Part 1: Design Overview

### 1.1 Data Sources

| Resource | Football-Data.org Endpoint | Sync Frequency | Description |
|----------|---------------------------|----------------|-------------|
| Standings | `GET /competitions/{id}/standings` | Daily 4 AM UTC | League table positions |
| Scorers | `GET /competitions/{id}/scorers` | Daily 4 AM UTC | Top goal scorers |
| Lineups | `GET /matches/{id}` (expanded) | 1 hour before match | Starting XI, bench, formation |
| H2H | Calculated from Match history | On-demand | Historical matchup records |

### 1.2 New Entities Summary

```
┌─────────────────────────────────────────────────────────────┐
│                    Phase 9 Entities                          │
├─────────────────────────────────────────────────────────────┤
│  Standing         - League table position for each team     │
│  Scorer           - Top scorers in a competition            │
│  MatchLineup      - Starting XI and bench for a match       │
│  TeamUsualLineup  - Cached typical starting XI              │
│  HeadToHead       - Historical matchup record between teams │
└─────────────────────────────────────────────────────────────┘
```

### 1.3 Rate Limiting Considerations

Football-Data.org free tier: 10 requests/minute

**Daily sync budget (4 AM UTC):**
- 5 competitions × 1 standings request = 5 requests
- 5 competitions × 1 scorers request = 5 requests
- Total: 10 requests (1 minute budget)

**Lineup sync budget (before matches):**
- Sync 1 hour before each match kickoff
- 1 request per match (expanded match details)
- Spread across match times, not batched

---

## Part 2: Domain Layer - Entities

### 2.1 Standing Entity

**Standing.cs:**
```csharp
namespace ExtraTime.Domain.Entities;

public sealed class Standing : BaseEntity
{
    public required Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public required int Season { get; set; }

    // Table position
    public int Position { get; set; }

    // Match statistics
    public int PlayedGames { get; set; }
    public int Won { get; set; }
    public int Draw { get; set; }
    public int Lost { get; set; }

    // Goals
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference { get; set; }

    // Points
    public int Points { get; set; }

    // Form (last 5 matches: W, D, L)
    public string? Form { get; set; }

    // Metadata
    public DateTime LastUpdatedAt { get; set; }

    // Factory method
    public static Standing Create(
        Guid competitionId,
        Guid teamId,
        int season,
        int position,
        int playedGames,
        int won,
        int draw,
        int lost,
        int goalsFor,
        int goalsAgainst,
        int points,
        string? form)
    {
        return new Standing
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            TeamId = teamId,
            Season = season,
            Position = position,
            PlayedGames = playedGames,
            Won = won,
            Draw = draw,
            Lost = lost,
            GoalsFor = goalsFor,
            GoalsAgainst = goalsAgainst,
            GoalDifference = goalsFor - goalsAgainst,
            Points = points,
            Form = form,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        int position,
        int playedGames,
        int won,
        int draw,
        int lost,
        int goalsFor,
        int goalsAgainst,
        int points,
        string? form)
    {
        Position = position;
        PlayedGames = playedGames;
        Won = won;
        Draw = draw;
        Lost = lost;
        GoalsFor = goalsFor;
        GoalsAgainst = goalsAgainst;
        GoalDifference = goalsFor - goalsAgainst;
        Points = points;
        Form = form;
        LastUpdatedAt = DateTime.UtcNow;
    }

    // Helper methods for bot analysis (Phase 7.5 integration)
    public bool IsInTitleRace(int topPositions = 4) => Position <= topPositions;
    public bool IsInRelegationZone(int totalTeams = 20, int relegationSpots = 3)
        => Position > totalTeams - relegationSpots;
    public double GetPointsPerGame() => PlayedGames > 0 ? (double)Points / PlayedGames : 0;
}
```

### 2.2 Scorer Entity

**Scorer.cs:**
```csharp
namespace ExtraTime.Domain.Entities;

public sealed class Scorer : BaseEntity
{
    public required Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public required int Season { get; set; }

    // Player info (from Football-Data.org)
    public required int ExternalPlayerId { get; set; }
    public required string PlayerName { get; set; }
    public string? PlayerNationality { get; set; }
    public string? PlayerPosition { get; set; }
    public DateTime? PlayerDateOfBirth { get; set; }

    // Scoring stats
    public int Goals { get; set; }
    public int? Assists { get; set; }
    public int? Penalties { get; set; }
    public int? PlayedMatches { get; set; }

    // Ranking
    public int Rank { get; set; }

    // Metadata
    public DateTime LastUpdatedAt { get; set; }

    // Factory method
    public static Scorer Create(
        Guid competitionId,
        Guid teamId,
        int season,
        int externalPlayerId,
        string playerName,
        string? nationality,
        string? position,
        DateTime? dateOfBirth,
        int goals,
        int? assists,
        int? penalties,
        int? playedMatches,
        int rank)
    {
        return new Scorer
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            TeamId = teamId,
            Season = season,
            ExternalPlayerId = externalPlayerId,
            PlayerName = playerName,
            PlayerNationality = nationality,
            PlayerPosition = position,
            PlayerDateOfBirth = dateOfBirth,
            Goals = goals,
            Assists = assists,
            Penalties = penalties,
            PlayedMatches = playedMatches,
            Rank = rank,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        int goals,
        int? assists,
        int? penalties,
        int? playedMatches,
        int rank)
    {
        Goals = goals;
        Assists = assists;
        Penalties = penalties;
        PlayedMatches = playedMatches;
        Rank = rank;
        LastUpdatedAt = DateTime.UtcNow;
    }

    // Helper for bot analysis
    public bool IsTopScorer(int threshold = 3) => Rank <= threshold;
    public double GetGoalsPerMatch() => PlayedMatches > 0 ? (double)Goals / PlayedMatches.Value : 0;
}
```

### 2.3 MatchLineup Entity

**MatchLineup.cs:**
```csharp
namespace ExtraTime.Domain.Entities;

public sealed class MatchLineup : BaseEntity
{
    public required Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    // Tactical setup
    public string? Formation { get; set; }

    // Coach
    public int? CoachExternalId { get; set; }
    public string? CoachName { get; set; }
    public string? CoachNationality { get; set; }

    // Starting XI - stored as JSON arrays of player objects
    // Format: [{"id": 123, "name": "Player Name", "position": "Goalkeeper", "shirtNumber": 1}]
    public string StartingXI { get; set; } = "[]";

    // Bench players
    public string Bench { get; set; } = "[]";

    // Captain info
    public int? CaptainExternalId { get; set; }
    public string? CaptainName { get; set; }

    // Metadata
    public DateTime SyncedAt { get; set; }

    // Factory method
    public static MatchLineup Create(
        Guid matchId,
        Guid teamId,
        string? formation,
        int? coachId,
        string? coachName,
        string? coachNationality,
        string startingXI,
        string bench,
        int? captainId,
        string? captainName)
    {
        return new MatchLineup
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            TeamId = teamId,
            Formation = formation,
            CoachExternalId = coachId,
            CoachName = coachName,
            CoachNationality = coachNationality,
            StartingXI = startingXI,
            Bench = bench,
            CaptainExternalId = captainId,
            CaptainName = captainName,
            SyncedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string? formation,
        int? coachId,
        string? coachName,
        string? coachNationality,
        string startingXI,
        string bench,
        int? captainId,
        string? captainName)
    {
        Formation = formation;
        CoachExternalId = coachId;
        CoachName = coachName;
        CoachNationality = coachNationality;
        StartingXI = startingXI;
        Bench = bench;
        CaptainExternalId = captainId;
        CaptainName = captainName;
        SyncedAt = DateTime.UtcNow;
    }

    // Helper methods
    public List<LineupPlayer> GetStartingPlayers()
    {
        try
        {
            return JsonSerializer.Deserialize<List<LineupPlayer>>(StartingXI) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public List<LineupPlayer> GetBenchPlayers()
    {
        try
        {
            return JsonSerializer.Deserialize<List<LineupPlayer>>(Bench) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public List<int> GetAllPlayerIds()
    {
        var starting = GetStartingPlayers().Select(p => p.Id);
        var bench = GetBenchPlayers().Select(p => p.Id);
        return starting.Concat(bench).ToList();
    }

    public LineupPlayer? GetGoalkeeper()
    {
        return GetStartingPlayers().FirstOrDefault(p =>
            p.Position?.Equals("Goalkeeper", StringComparison.OrdinalIgnoreCase) == true);
    }
}

// Supporting record for lineup player
public sealed record LineupPlayer(
    int Id,
    string Name,
    string? Position,
    int? ShirtNumber);
```

### 2.4 TeamUsualLineup Entity

**TeamUsualLineup.cs:**
```csharp
namespace ExtraTime.Domain.Entities;

public sealed class TeamUsualLineup : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public required Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    // Most common formation
    public string? UsualFormation { get; set; }

    // Key players by position (JSON arrays of external player IDs)
    public string UsualGoalkeeperIds { get; set; } = "[]";     // Usually just 1-2 GKs
    public string UsualDefenderIds { get; set; } = "[]";       // Usually 4-5 defenders
    public string UsualMidfielderIds { get; set; } = "[]";     // Usually 4-5 midfielders
    public string UsualForwardIds { get; set; } = "[]";        // Usually 2-3 forwards

    // Top scorer in the team (for absence detection)
    public int? TopScorerExternalId { get; set; }
    public string? TopScorerName { get; set; }
    public int TopScorerGoals { get; set; }

    // Captain (for absence detection)
    public int? CaptainExternalId { get; set; }
    public string? CaptainName { get; set; }

    // Analysis metadata
    public int MatchesAnalyzed { get; set; }
    public DateTime CalculatedAt { get; set; }

    // Factory method
    public static TeamUsualLineup Create(
        Guid teamId,
        Guid competitionId,
        string? usualFormation,
        List<int> goalkeeperIds,
        List<int> defenderIds,
        List<int> midfielderIds,
        List<int> forwardIds,
        int matchesAnalyzed)
    {
        return new TeamUsualLineup
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            CompetitionId = competitionId,
            UsualFormation = usualFormation,
            UsualGoalkeeperIds = JsonSerializer.Serialize(goalkeeperIds),
            UsualDefenderIds = JsonSerializer.Serialize(defenderIds),
            UsualMidfielderIds = JsonSerializer.Serialize(midfielderIds),
            UsualForwardIds = JsonSerializer.Serialize(forwardIds),
            MatchesAnalyzed = matchesAnalyzed,
            CalculatedAt = DateTime.UtcNow
        };
    }

    // Helper methods
    public List<int> GetUsualGoalkeeperIds() => DeserializeIds(UsualGoalkeeperIds);
    public List<int> GetUsualDefenderIds() => DeserializeIds(UsualDefenderIds);
    public List<int> GetUsualMidfielderIds() => DeserializeIds(UsualMidfielderIds);
    public List<int> GetUsualForwardIds() => DeserializeIds(UsualForwardIds);

    public List<int> GetAllUsualPlayerIds()
    {
        return GetUsualGoalkeeperIds()
            .Concat(GetUsualDefenderIds())
            .Concat(GetUsualMidfielderIds())
            .Concat(GetUsualForwardIds())
            .ToList();
    }

    private static List<int> DeserializeIds(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
```

### 2.5 HeadToHead Entity

**HeadToHead.cs:**
```csharp
namespace ExtraTime.Domain.Entities;

public sealed class HeadToHead : BaseEntity
{
    // Teams (ordered by ID to ensure consistent lookup)
    public required Guid Team1Id { get; set; }
    public Team Team1 { get; set; } = null!;

    public required Guid Team2Id { get; set; }
    public Team Team2 { get; set; } = null!;

    // Optional: scope to competition
    public Guid? CompetitionId { get; set; }
    public Competition? Competition { get; set; }

    // Overall stats
    public int TotalMatches { get; set; }
    public int Team1Wins { get; set; }
    public int Team2Wins { get; set; }
    public int Draws { get; set; }

    // Goals
    public int Team1Goals { get; set; }
    public int Team2Goals { get; set; }

    // Home/Away breakdown for Team1
    public int Team1HomeMatches { get; set; }
    public int Team1HomeWins { get; set; }
    public int Team1HomeGoals { get; set; }
    public int Team1HomeConceded { get; set; }

    // Last match info
    public DateTime? LastMatchDate { get; set; }
    public Guid? LastMatchId { get; set; }

    // Metadata
    public int MatchesAnalyzed { get; set; }
    public DateTime CalculatedAt { get; set; }

    // Factory method
    public static HeadToHead Create(Guid team1Id, Guid team2Id, Guid? competitionId = null)
    {
        // Ensure consistent ordering
        var (first, second) = team1Id.CompareTo(team2Id) < 0
            ? (team1Id, team2Id)
            : (team2Id, team1Id);

        return new HeadToHead
        {
            Id = Guid.NewGuid(),
            Team1Id = first,
            Team2Id = second,
            CompetitionId = competitionId,
            CalculatedAt = DateTime.UtcNow
        };
    }

    // Get stats from perspective of a specific team
    public HeadToHeadStats GetStatsForTeam(Guid teamId)
    {
        if (teamId == Team1Id)
        {
            return new HeadToHeadStats(
                Wins: Team1Wins,
                Losses: Team2Wins,
                Draws: Draws,
                GoalsFor: Team1Goals,
                GoalsAgainst: Team2Goals,
                HomeWins: Team1HomeWins,
                HomeMatches: Team1HomeMatches);
        }
        else if (teamId == Team2Id)
        {
            return new HeadToHeadStats(
                Wins: Team2Wins,
                Losses: Team1Wins,
                Draws: Draws,
                GoalsFor: Team2Goals,
                GoalsAgainst: Team1Goals,
                HomeWins: Team1HomeMatches - Team1HomeWins - (Draws - (TotalMatches - Team1HomeMatches - Draws) / 2),
                HomeMatches: TotalMatches - Team1HomeMatches);
        }
        throw new ArgumentException("Team not part of this head-to-head record", nameof(teamId));
    }

    // Helper methods
    public bool HasDominantTeam(double threshold = 0.6)
    {
        if (TotalMatches < 5) return false;
        double team1WinRate = (double)Team1Wins / TotalMatches;
        return team1WinRate >= threshold || team1WinRate <= (1 - threshold);
    }

    public Guid? GetDominantTeamId(double threshold = 0.6)
    {
        if (!HasDominantTeam(threshold)) return null;
        return Team1Wins > Team2Wins ? Team1Id : Team2Id;
    }
}

public sealed record HeadToHeadStats(
    int Wins,
    int Losses,
    int Draws,
    int GoalsFor,
    int GoalsAgainst,
    int HomeWins,
    int HomeMatches)
{
    public double WinRate => (Wins + Losses + Draws) > 0
        ? (double)Wins / (Wins + Losses + Draws)
        : 0;

    public double HomeWinRate => HomeMatches > 0
        ? (double)HomeWins / HomeMatches
        : 0;

    public int GoalDifference => GoalsFor - GoalsAgainst;
}
```

### 2.6 Update Match Entity

Add navigation properties to Match entity:

**Match.cs (additions):**
```csharp
// Add to existing Match entity:

// Navigation properties for lineups
public MatchLineup? HomeLineup { get; set; }
public MatchLineup? AwayLineup { get; set; }

// Method to check if lineups are available
public bool HasLineups => HomeLineup != null && AwayLineup != null;
```

---

## Part 3: Infrastructure Layer - Database Configuration

### 3.1 Standing Configuration

**StandingConfiguration.cs:**
```csharp
public sealed class StandingConfiguration : IEntityTypeConfiguration<Standing>
{
    public void Configure(EntityTypeBuilder<Standing> builder)
    {
        builder.ToTable("Standings");

        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.Competition)
            .WithMany()
            .HasForeignKey(s => s.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Team)
            .WithMany()
            .HasForeignKey(s => s.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.Form)
            .HasMaxLength(20);

        // Unique constraint: one standing per team per competition per season
        builder.HasIndex(s => new { s.CompetitionId, s.TeamId, s.Season })
            .IsUnique();

        // Index for position lookups
        builder.HasIndex(s => new { s.CompetitionId, s.Season, s.Position });
    }
}
```

### 3.2 Scorer Configuration

**ScorerConfiguration.cs:**
```csharp
public sealed class ScorerConfiguration : IEntityTypeConfiguration<Scorer>
{
    public void Configure(EntityTypeBuilder<Scorer> builder)
    {
        builder.ToTable("Scorers");

        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.Competition)
            .WithMany()
            .HasForeignKey(s => s.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Team)
            .WithMany()
            .HasForeignKey(s => s.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.PlayerName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.PlayerNationality)
            .HasMaxLength(100);

        builder.Property(s => s.PlayerPosition)
            .HasMaxLength(50);

        // Unique constraint: one scorer entry per player per competition per season
        builder.HasIndex(s => new { s.CompetitionId, s.ExternalPlayerId, s.Season })
            .IsUnique();

        // Index for ranking lookups
        builder.HasIndex(s => new { s.CompetitionId, s.Season, s.Rank });
    }
}
```

### 3.3 MatchLineup Configuration

**MatchLineupConfiguration.cs:**
```csharp
public sealed class MatchLineupConfiguration : IEntityTypeConfiguration<MatchLineup>
{
    public void Configure(EntityTypeBuilder<MatchLineup> builder)
    {
        builder.ToTable("MatchLineups");

        builder.HasKey(ml => ml.Id);

        builder.HasOne(ml => ml.Match)
            .WithMany()
            .HasForeignKey(ml => ml.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ml => ml.Team)
            .WithMany()
            .HasForeignKey(ml => ml.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(ml => ml.Formation)
            .HasMaxLength(20);

        builder.Property(ml => ml.CoachName)
            .HasMaxLength(150);

        builder.Property(ml => ml.CoachNationality)
            .HasMaxLength(100);

        builder.Property(ml => ml.CaptainName)
            .HasMaxLength(150);

        // JSON columns for players
        builder.Property(ml => ml.StartingXI)
            .HasMaxLength(4000); // ~11 players with full details

        builder.Property(ml => ml.Bench)
            .HasMaxLength(4000); // ~9 bench players

        // Unique constraint: one lineup per team per match
        builder.HasIndex(ml => new { ml.MatchId, ml.TeamId })
            .IsUnique();
    }
}
```

### 3.4 TeamUsualLineup Configuration

**TeamUsualLineupConfiguration.cs:**
```csharp
public sealed class TeamUsualLineupConfiguration : IEntityTypeConfiguration<TeamUsualLineup>
{
    public void Configure(EntityTypeBuilder<TeamUsualLineup> builder)
    {
        builder.ToTable("TeamUsualLineups");

        builder.HasKey(tul => tul.Id);

        builder.HasOne(tul => tul.Team)
            .WithMany()
            .HasForeignKey(tul => tul.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tul => tul.Competition)
            .WithMany()
            .HasForeignKey(tul => tul.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(tul => tul.UsualFormation)
            .HasMaxLength(20);

        builder.Property(tul => tul.TopScorerName)
            .HasMaxLength(150);

        builder.Property(tul => tul.CaptainName)
            .HasMaxLength(150);

        // JSON columns for player IDs
        builder.Property(tul => tul.UsualGoalkeeperIds)
            .HasMaxLength(500);

        builder.Property(tul => tul.UsualDefenderIds)
            .HasMaxLength(500);

        builder.Property(tul => tul.UsualMidfielderIds)
            .HasMaxLength(500);

        builder.Property(tul => tul.UsualForwardIds)
            .HasMaxLength(500);

        // Unique constraint: one usual lineup per team per competition
        builder.HasIndex(tul => new { tul.TeamId, tul.CompetitionId })
            .IsUnique();
    }
}
```

### 3.5 HeadToHead Configuration

**HeadToHeadConfiguration.cs:**
```csharp
public sealed class HeadToHeadConfiguration : IEntityTypeConfiguration<HeadToHead>
{
    public void Configure(EntityTypeBuilder<HeadToHead> builder)
    {
        builder.ToTable("HeadToHeads");

        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.Team1)
            .WithMany()
            .HasForeignKey(h => h.Team1Id)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade conflicts

        builder.HasOne(h => h.Team2)
            .WithMany()
            .HasForeignKey(h => h.Team2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Competition)
            .WithMany()
            .HasForeignKey(h => h.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one H2H per team pair per competition (null = all competitions)
        builder.HasIndex(h => new { h.Team1Id, h.Team2Id, h.CompetitionId })
            .IsUnique();
    }
}
```

### 3.6 Update ApplicationDbContext

**ApplicationDbContext.cs (additions):**
```csharp
public DbSet<Standing> Standings => Set<Standing>();
public DbSet<Scorer> Scorers => Set<Scorer>();
public DbSet<MatchLineup> MatchLineups => Set<MatchLineup>();
public DbSet<TeamUsualLineup> TeamUsualLineups => Set<TeamUsualLineup>();
public DbSet<HeadToHead> HeadToHeads => Set<HeadToHead>();
```

### 3.7 Migration

Create migration: `AddExtendedFootballData`
- Creates `Standings` table
- Creates `Scorers` table
- Creates `MatchLineups` table
- Creates `TeamUsualLineups` table
- Creates `HeadToHeads` table
- Adds appropriate indexes

---

## Part 4: API DTOs from Football-Data.org

### 4.1 Standings API DTOs

**StandingsApiDtos.cs:**
```csharp
namespace ExtraTime.Application.Features.Football.DTOs;

public sealed record StandingsApiResponse(
    CompetitionApiDto Competition,
    SeasonApiDto Season,
    List<StandingTableApiDto> Standings);

public sealed record StandingTableApiDto(
    string Stage,        // "REGULAR_SEASON", "GROUP_STAGE", etc.
    string Type,         // "TOTAL", "HOME", "AWAY"
    string? Group,       // "GROUP_A", etc. for group stages
    List<StandingTableEntryApiDto> Table);

public sealed record StandingTableEntryApiDto(
    int Position,
    TeamApiDto Team,
    int PlayedGames,
    string? Form,
    int Won,
    int Draw,
    int Lost,
    int Points,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference);

public sealed record SeasonApiDto(
    int Id,
    string StartDate,
    string EndDate,
    int? CurrentMatchday);
```

### 4.2 Scorers API DTOs

**ScorersApiDtos.cs:**
```csharp
namespace ExtraTime.Application.Features.Football.DTOs;

public sealed record ScorersApiResponse(
    CompetitionApiDto Competition,
    SeasonApiDto Season,
    List<ScorerApiDto> Scorers);

public sealed record ScorerApiDto(
    PlayerApiDto Player,
    TeamApiDto Team,
    int? PlayedMatches,
    int Goals,
    int? Assists,
    int? Penalties);

public sealed record PlayerApiDto(
    int Id,
    string Name,
    string? FirstName,
    string? LastName,
    string? DateOfBirth,
    string? Nationality,
    string? Position);
```

### 4.3 Extended Match API DTOs (Lineup Support)

**MatchApiDtos.cs (additions):**
```csharp
// Add to existing MatchApiDto or create extended version:

public sealed record MatchDetailApiDto(
    int Id,
    CompetitionApiDto Competition,
    TeamWithLineupApiDto HomeTeam,
    TeamWithLineupApiDto AwayTeam,
    string UtcDate,
    string Status,
    int? Matchday,
    string? Stage,
    string? Group,
    ScoreApiDto Score,
    string? Venue);

public sealed record TeamWithLineupApiDto(
    int Id,
    string Name,
    string? ShortName,
    string? Tla,
    string? Crest,
    CoachApiDto? Coach,
    string? Formation,
    int? LeagueRank,
    List<LineupPlayerApiDto>? Lineup,
    List<LineupPlayerApiDto>? Bench);

public sealed record CoachApiDto(
    int? Id,
    string? Name,
    string? Nationality);

public sealed record LineupPlayerApiDto(
    int Id,
    string Name,
    string? Position,
    int? ShirtNumber);
```

---

## Part 5: Application Layer - Services

### 5.1 Football Data Service Updates

**IFootballDataService.cs (additions):**
```csharp
public interface IFootballDataService
{
    // Existing methods...

    // New methods for Phase 9
    Task<StandingsApiResponse?> GetStandingsAsync(
        int competitionExternalId,
        CancellationToken cancellationToken = default);

    Task<ScorersApiResponse?> GetScorersAsync(
        int competitionExternalId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<MatchDetailApiDto?> GetMatchDetailsAsync(
        int matchExternalId,
        CancellationToken cancellationToken = default);
}
```

**FootballDataService.cs (additions):**
```csharp
public async Task<StandingsApiResponse?> GetStandingsAsync(
    int competitionExternalId,
    CancellationToken cancellationToken = default)
{
    try
    {
        var response = await _httpClient.GetAsync(
            $"competitions/{competitionExternalId}/standings",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Failed to fetch standings for competition {CompetitionId}: {StatusCode}",
                competitionExternalId, response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<StandingsApiResponse>(
            cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Error fetching standings for competition {CompetitionId}",
            competitionExternalId);
        return null;
    }
}

public async Task<ScorersApiResponse?> GetScorersAsync(
    int competitionExternalId,
    int limit = 50,
    CancellationToken cancellationToken = default)
{
    try
    {
        var response = await _httpClient.GetAsync(
            $"competitions/{competitionExternalId}/scorers?limit={limit}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Failed to fetch scorers for competition {CompetitionId}: {StatusCode}",
                competitionExternalId, response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ScorersApiResponse>(
            cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Error fetching scorers for competition {CompetitionId}",
            competitionExternalId);
        return null;
    }
}

public async Task<MatchDetailApiDto?> GetMatchDetailsAsync(
    int matchExternalId,
    CancellationToken cancellationToken = default)
{
    try
    {
        var response = await _httpClient.GetAsync(
            $"matches/{matchExternalId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Failed to fetch match details for {MatchId}: {StatusCode}",
                matchExternalId, response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MatchDetailApiDto>(
            cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Error fetching match details for {MatchId}",
            matchExternalId);
        return null;
    }
}
```

### 5.2 Standings Sync Service

**IStandingsSyncService.cs:**
```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface IStandingsSyncService
{
    Task SyncStandingsAsync(CancellationToken cancellationToken = default);
    Task SyncStandingsForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default);
}
```

**StandingsSyncService.cs:**
```csharp
namespace ExtraTime.Infrastructure.Services.Football;

public sealed class StandingsSyncService(
    IApplicationDbContext context,
    IFootballDataService footballDataService,
    FootballDataSettings settings,
    ILogger<StandingsSyncService> logger) : IStandingsSyncService
{
    public async Task SyncStandingsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting standings sync for all competitions");

        foreach (var competitionExternalId in settings.SupportedCompetitionIds)
        {
            var competition = await context.Competitions
                .FirstOrDefaultAsync(c => c.ExternalId == competitionExternalId, cancellationToken);

            if (competition == null)
            {
                logger.LogWarning("Competition {ExternalId} not found in database", competitionExternalId);
                continue;
            }

            await SyncStandingsForCompetitionInternalAsync(competition, cancellationToken);
        }

        logger.LogInformation("Standings sync completed");
    }

    public async Task SyncStandingsForCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition = await context.Competitions
            .FirstOrDefaultAsync(c => c.Id == competitionId, cancellationToken);

        if (competition == null)
        {
            logger.LogWarning("Competition {Id} not found", competitionId);
            return;
        }

        await SyncStandingsForCompetitionInternalAsync(competition, cancellationToken);
    }

    private async Task SyncStandingsForCompetitionInternalAsync(
        Competition competition,
        CancellationToken cancellationToken)
    {
        var standingsResponse = await footballDataService.GetStandingsAsync(
            competition.ExternalId, cancellationToken);

        if (standingsResponse == null)
        {
            logger.LogWarning("No standings data for competition {Name}", competition.Name);
            return;
        }

        // Get current season year
        var season = DateTime.TryParse(standingsResponse.Season.StartDate, out var startDate)
            ? startDate.Year
            : DateTime.UtcNow.Year;

        // Find the TOTAL standings table (not HOME or AWAY)
        var totalTable = standingsResponse.Standings
            .FirstOrDefault(s => s.Type == "TOTAL" && s.Stage == "REGULAR_SEASON")
            ?? standingsResponse.Standings.FirstOrDefault(s => s.Type == "TOTAL");

        if (totalTable == null)
        {
            logger.LogWarning("No TOTAL standings table for competition {Name}", competition.Name);
            return;
        }

        // Get team lookup
        var teamExternalIds = totalTable.Table.Select(t => t.Team.Id).ToList();
        var teams = await context.Teams
            .Where(t => teamExternalIds.Contains(t.ExternalId))
            .ToDictionaryAsync(t => t.ExternalId, cancellationToken);

        // Get existing standings for this competition/season
        var existingStandings = await context.Standings
            .Where(s => s.CompetitionId == competition.Id && s.Season == season)
            .ToDictionaryAsync(s => s.TeamId, cancellationToken);

        foreach (var entry in totalTable.Table)
        {
            if (!teams.TryGetValue(entry.Team.Id, out var team))
            {
                logger.LogWarning("Team {ExternalId} not found in database", entry.Team.Id);
                continue;
            }

            if (existingStandings.TryGetValue(team.Id, out var existing))
            {
                existing.Update(
                    entry.Position,
                    entry.PlayedGames,
                    entry.Won,
                    entry.Draw,
                    entry.Lost,
                    entry.GoalsFor,
                    entry.GoalsAgainst,
                    entry.Points,
                    entry.Form);
            }
            else
            {
                var standing = Standing.Create(
                    competition.Id,
                    team.Id,
                    season,
                    entry.Position,
                    entry.PlayedGames,
                    entry.Won,
                    entry.Draw,
                    entry.Lost,
                    entry.GoalsFor,
                    entry.GoalsAgainst,
                    entry.Points,
                    entry.Form);

                context.Standings.Add(standing);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Synced standings for {Competition}: {TeamCount} teams",
            competition.Name, totalTable.Table.Count);
    }
}
```

### 5.3 Scorers Sync Service

**IScorersSyncService.cs:**
```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface IScorersSyncService
{
    Task SyncScorersAsync(CancellationToken cancellationToken = default);
    Task SyncScorersForCompetitionAsync(Guid competitionId, int limit = 50, CancellationToken cancellationToken = default);
}
```

**ScorersSyncService.cs:**
```csharp
namespace ExtraTime.Infrastructure.Services.Football;

public sealed class ScorersSyncService(
    IApplicationDbContext context,
    IFootballDataService footballDataService,
    FootballDataSettings settings,
    ILogger<ScorersSyncService> logger) : IScorersSyncService
{
    private const int DefaultLimit = 50;

    public async Task SyncScorersAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting scorers sync for all competitions");

        foreach (var competitionExternalId in settings.SupportedCompetitionIds)
        {
            var competition = await context.Competitions
                .FirstOrDefaultAsync(c => c.ExternalId == competitionExternalId, cancellationToken);

            if (competition == null)
            {
                logger.LogWarning("Competition {ExternalId} not found in database", competitionExternalId);
                continue;
            }

            await SyncScorersForCompetitionInternalAsync(competition, DefaultLimit, cancellationToken);
        }

        logger.LogInformation("Scorers sync completed");
    }

    public async Task SyncScorersForCompetitionAsync(
        Guid competitionId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var competition = await context.Competitions
            .FirstOrDefaultAsync(c => c.Id == competitionId, cancellationToken);

        if (competition == null)
        {
            logger.LogWarning("Competition {Id} not found", competitionId);
            return;
        }

        await SyncScorersForCompetitionInternalAsync(competition, limit, cancellationToken);
    }

    private async Task SyncScorersForCompetitionInternalAsync(
        Competition competition,
        int limit,
        CancellationToken cancellationToken)
    {
        var scorersResponse = await footballDataService.GetScorersAsync(
            competition.ExternalId, limit, cancellationToken);

        if (scorersResponse == null)
        {
            logger.LogWarning("No scorers data for competition {Name}", competition.Name);
            return;
        }

        // Get current season year
        var season = DateTime.TryParse(scorersResponse.Season.StartDate, out var startDate)
            ? startDate.Year
            : DateTime.UtcNow.Year;

        // Get team lookup
        var teamExternalIds = scorersResponse.Scorers.Select(s => s.Team.Id).Distinct().ToList();
        var teams = await context.Teams
            .Where(t => teamExternalIds.Contains(t.ExternalId))
            .ToDictionaryAsync(t => t.ExternalId, cancellationToken);

        // Get existing scorers for this competition/season
        var existingScorers = await context.Scorers
            .Where(s => s.CompetitionId == competition.Id && s.Season == season)
            .ToDictionaryAsync(s => s.ExternalPlayerId, cancellationToken);

        int rank = 1;
        foreach (var scorerData in scorersResponse.Scorers)
        {
            if (!teams.TryGetValue(scorerData.Team.Id, out var team))
            {
                logger.LogWarning("Team {ExternalId} not found for scorer {Name}",
                    scorerData.Team.Id, scorerData.Player.Name);
                continue;
            }

            DateTime? dateOfBirth = null;
            if (!string.IsNullOrEmpty(scorerData.Player.DateOfBirth) &&
                DateTime.TryParse(scorerData.Player.DateOfBirth, out var dob))
            {
                dateOfBirth = dob;
            }

            if (existingScorers.TryGetValue(scorerData.Player.Id, out var existing))
            {
                existing.Update(
                    scorerData.Goals,
                    scorerData.Assists,
                    scorerData.Penalties,
                    scorerData.PlayedMatches,
                    rank);
            }
            else
            {
                var scorer = Scorer.Create(
                    competition.Id,
                    team.Id,
                    season,
                    scorerData.Player.Id,
                    scorerData.Player.Name,
                    scorerData.Player.Nationality,
                    scorerData.Player.Position,
                    dateOfBirth,
                    scorerData.Goals,
                    scorerData.Assists,
                    scorerData.Penalties,
                    scorerData.PlayedMatches,
                    rank);

                context.Scorers.Add(scorer);
            }

            rank++;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Synced scorers for {Competition}: {Count} players",
            competition.Name, scorersResponse.Scorers.Count);
    }
}
```

### 5.4 Lineup Sync Service

**ILineupSyncService.cs:**
```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface ILineupSyncService
{
    Task SyncLineupForMatchAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task SyncLineupsForUpcomingMatchesAsync(TimeSpan lookAhead, CancellationToken cancellationToken = default);
}
```

**LineupSyncService.cs:**
```csharp
namespace ExtraTime.Infrastructure.Services.Football;

public sealed class LineupSyncService(
    IApplicationDbContext context,
    IFootballDataService footballDataService,
    ILogger<LineupSyncService> logger) : ILineupSyncService
{
    public async Task SyncLineupForMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

        if (match == null)
        {
            logger.LogWarning("Match {Id} not found", matchId);
            return;
        }

        await SyncLineupForMatchInternalAsync(match, cancellationToken);
    }

    public async Task SyncLineupsForUpcomingMatchesAsync(
        TimeSpan lookAhead,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.Add(lookAhead);

        // Get matches starting within the lookAhead window that don't have lineups yet
        var matches = await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.MatchDateUtc >= now && m.MatchDateUtc <= cutoff)
            .Where(m => m.Status == MatchStatus.Scheduled || m.Status == MatchStatus.Timed)
            .ToListAsync(cancellationToken);

        // Check which matches already have lineups
        var matchIds = matches.Select(m => m.Id).ToList();
        var existingLineupMatchIds = await context.MatchLineups
            .Where(ml => matchIds.Contains(ml.MatchId))
            .Select(ml => ml.MatchId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var matchesNeedingLineups = matches
            .Where(m => !existingLineupMatchIds.Contains(m.Id))
            .ToList();

        logger.LogInformation(
            "Found {Count} matches needing lineup sync within {LookAhead}",
            matchesNeedingLineups.Count, lookAhead);

        foreach (var match in matchesNeedingLineups)
        {
            await SyncLineupForMatchInternalAsync(match, cancellationToken);
        }
    }

    private async Task SyncLineupForMatchInternalAsync(
        Match match,
        CancellationToken cancellationToken)
    {
        var matchDetails = await footballDataService.GetMatchDetailsAsync(
            match.ExternalId, cancellationToken);

        if (matchDetails == null)
        {
            logger.LogWarning("No match details for match {Id} (external: {ExternalId})",
                match.Id, match.ExternalId);
            return;
        }

        // Sync home team lineup
        if (matchDetails.HomeTeam.Lineup != null && matchDetails.HomeTeam.Lineup.Count > 0)
        {
            await UpsertLineupAsync(
                match.Id,
                match.HomeTeamId,
                matchDetails.HomeTeam,
                cancellationToken);
        }

        // Sync away team lineup
        if (matchDetails.AwayTeam.Lineup != null && matchDetails.AwayTeam.Lineup.Count > 0)
        {
            await UpsertLineupAsync(
                match.Id,
                match.AwayTeamId,
                matchDetails.AwayTeam,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Synced lineups for match {MatchId}", match.Id);
    }

    private async Task UpsertLineupAsync(
        Guid matchId,
        Guid teamId,
        TeamWithLineupApiDto teamData,
        CancellationToken cancellationToken)
    {
        var startingXI = JsonSerializer.Serialize(
            teamData.Lineup?.Select(p => new LineupPlayer(p.Id, p.Name, p.Position, p.ShirtNumber)) ?? []);

        var bench = JsonSerializer.Serialize(
            teamData.Bench?.Select(p => new LineupPlayer(p.Id, p.Name, p.Position, p.ShirtNumber)) ?? []);

        var existingLineup = await context.MatchLineups
            .FirstOrDefaultAsync(ml => ml.MatchId == matchId && ml.TeamId == teamId, cancellationToken);

        if (existingLineup != null)
        {
            existingLineup.Update(
                teamData.Formation,
                teamData.Coach?.Id,
                teamData.Coach?.Name,
                teamData.Coach?.Nationality,
                startingXI,
                bench,
                null, // Captain ID not directly available in this structure
                null);
        }
        else
        {
            var lineup = MatchLineup.Create(
                matchId,
                teamId,
                teamData.Formation,
                teamData.Coach?.Id,
                teamData.Coach?.Name,
                teamData.Coach?.Nationality,
                startingXI,
                bench,
                null,
                null);

            context.MatchLineups.Add(lineup);
        }
    }
}
```

### 5.5 HeadToHead Calculator Service

**IHeadToHeadService.cs:**
```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface IHeadToHeadService
{
    Task<HeadToHead> GetOrCalculateAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default);

    Task RefreshAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default);
}
```

**HeadToHeadService.cs:**
```csharp
namespace ExtraTime.Infrastructure.Services.Football;

public sealed class HeadToHeadService(
    IApplicationDbContext context,
    ILogger<HeadToHeadService> logger) : IHeadToHeadService
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromDays(7);

    public async Task<HeadToHead> GetOrCalculateAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default)
    {
        // Ensure consistent ordering
        var (first, second) = team1Id.CompareTo(team2Id) < 0
            ? (team1Id, team2Id)
            : (team2Id, team1Id);

        // Check for cached H2H
        var cached = await context.HeadToHeads
            .FirstOrDefaultAsync(h =>
                h.Team1Id == first &&
                h.Team2Id == second &&
                h.CompetitionId == competitionId,
                cancellationToken);

        if (cached != null && (DateTime.UtcNow - cached.CalculatedAt) < CacheExpiry)
        {
            return cached;
        }

        // Calculate fresh H2H
        return await CalculateAndStoreAsync(first, second, competitionId, cached, cancellationToken);
    }

    public async Task RefreshAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default)
    {
        var (first, second) = team1Id.CompareTo(team2Id) < 0
            ? (team1Id, team2Id)
            : (team2Id, team1Id);

        var cached = await context.HeadToHeads
            .FirstOrDefaultAsync(h =>
                h.Team1Id == first &&
                h.Team2Id == second &&
                h.CompetitionId == competitionId,
                cancellationToken);

        await CalculateAndStoreAsync(first, second, competitionId, cached, cancellationToken);
    }

    private async Task<HeadToHead> CalculateAndStoreAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId,
        HeadToHead? existing,
        CancellationToken cancellationToken)
    {
        // Get all finished matches between these teams
        var matchesQuery = context.Matches
            .Where(m => m.Status == MatchStatus.Finished)
            .Where(m =>
                (m.HomeTeamId == team1Id && m.AwayTeamId == team2Id) ||
                (m.HomeTeamId == team2Id && m.AwayTeamId == team1Id));

        if (competitionId.HasValue)
        {
            matchesQuery = matchesQuery.Where(m => m.CompetitionId == competitionId.Value);
        }

        var matches = await matchesQuery
            .OrderByDescending(m => m.MatchDateUtc)
            .ToListAsync(cancellationToken);

        // Calculate stats
        var h2h = existing ?? HeadToHead.Create(team1Id, team2Id, competitionId);

        h2h.TotalMatches = matches.Count;
        h2h.Team1Wins = 0;
        h2h.Team2Wins = 0;
        h2h.Draws = 0;
        h2h.Team1Goals = 0;
        h2h.Team2Goals = 0;
        h2h.Team1HomeMatches = 0;
        h2h.Team1HomeWins = 0;
        h2h.Team1HomeGoals = 0;
        h2h.Team1HomeConceded = 0;

        foreach (var match in matches)
        {
            bool team1IsHome = match.HomeTeamId == team1Id;
            int team1Score = team1IsHome ? match.HomeScore!.Value : match.AwayScore!.Value;
            int team2Score = team1IsHome ? match.AwayScore!.Value : match.HomeScore!.Value;

            h2h.Team1Goals += team1Score;
            h2h.Team2Goals += team2Score;

            if (team1Score > team2Score) h2h.Team1Wins++;
            else if (team2Score > team1Score) h2h.Team2Wins++;
            else h2h.Draws++;

            if (team1IsHome)
            {
                h2h.Team1HomeMatches++;
                h2h.Team1HomeGoals += team1Score;
                h2h.Team1HomeConceded += team2Score;
                if (team1Score > team2Score) h2h.Team1HomeWins++;
            }
        }

        h2h.MatchesAnalyzed = matches.Count;
        h2h.LastMatchDate = matches.FirstOrDefault()?.MatchDateUtc;
        h2h.LastMatchId = matches.FirstOrDefault()?.Id;
        h2h.CalculatedAt = DateTime.UtcNow;

        if (existing == null)
        {
            context.HeadToHeads.Add(h2h);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogDebug(
            "Calculated H2H between teams {Team1} and {Team2}: {Matches} matches",
            team1Id, team2Id, matches.Count);

        return h2h;
    }
}
```

### 5.6 TeamUsualLineup Calculator Service

**ITeamUsualLineupService.cs:**
```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface ITeamUsualLineupService
{
    Task<TeamUsualLineup> GetOrCalculateAsync(
        Guid teamId,
        Guid competitionId,
        int matchesToAnalyze = 10,
        CancellationToken cancellationToken = default);

    Task RefreshAllAsync(CancellationToken cancellationToken = default);
}
```

**TeamUsualLineupService.cs:**
```csharp
namespace ExtraTime.Infrastructure.Services.Football;

public sealed class TeamUsualLineupService(
    IApplicationDbContext context,
    ILogger<TeamUsualLineupService> logger) : ITeamUsualLineupService
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromDays(3);
    private const int DefaultMatchesToAnalyze = 10;

    public async Task<TeamUsualLineup> GetOrCalculateAsync(
        Guid teamId,
        Guid competitionId,
        int matchesToAnalyze = 10,
        CancellationToken cancellationToken = default)
    {
        var cached = await context.TeamUsualLineups
            .FirstOrDefaultAsync(t =>
                t.TeamId == teamId && t.CompetitionId == competitionId,
                cancellationToken);

        if (cached != null && (DateTime.UtcNow - cached.CalculatedAt) < CacheExpiry)
        {
            return cached;
        }

        return await CalculateAndStoreAsync(teamId, competitionId, matchesToAnalyze, cached, cancellationToken);
    }

    public async Task RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        // Get all team-competition pairs that have lineups
        var pairs = await context.MatchLineups
            .Include(ml => ml.Match)
            .Select(ml => new { ml.TeamId, ml.Match.CompetitionId })
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var pair in pairs)
        {
            await GetOrCalculateAsync(pair.TeamId, pair.CompetitionId, DefaultMatchesToAnalyze, cancellationToken);
        }

        logger.LogInformation("Refreshed usual lineups for {Count} team-competition pairs", pairs.Count);
    }

    private async Task<TeamUsualLineup> CalculateAndStoreAsync(
        Guid teamId,
        Guid competitionId,
        int matchesToAnalyze,
        TeamUsualLineup? existing,
        CancellationToken cancellationToken)
    {
        // Get recent match lineups for this team
        var lineups = await context.MatchLineups
            .Include(ml => ml.Match)
            .Where(ml => ml.TeamId == teamId && ml.Match.CompetitionId == competitionId)
            .Where(ml => ml.Match.Status == MatchStatus.Finished)
            .OrderByDescending(ml => ml.Match.MatchDateUtc)
            .Take(matchesToAnalyze)
            .ToListAsync(cancellationToken);

        if (lineups.Count == 0)
        {
            // Return default if no lineups available
            var defaultLineup = existing ?? TeamUsualLineup.Create(
                teamId, competitionId, null, [], [], [], [], 0);
            return defaultLineup;
        }

        // Count formation occurrences
        var formationCounts = lineups
            .Where(l => !string.IsNullOrEmpty(l.Formation))
            .GroupBy(l => l.Formation)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        // Count player occurrences by position
        var allPlayers = lineups.SelectMany(l => l.GetStartingPlayers()).ToList();

        var goalkeeperCounts = CountPlayersByPosition(allPlayers, "Goalkeeper");
        var defenderCounts = CountPlayersByPosition(allPlayers, "Defender", "Centre-Back", "Left-Back", "Right-Back");
        var midfielderCounts = CountPlayersByPosition(allPlayers, "Midfielder", "Central Midfield", "Defensive Midfield", "Attacking Midfield", "Left Midfield", "Right Midfield");
        var forwardCounts = CountPlayersByPosition(allPlayers, "Forward", "Attacker", "Centre-Forward", "Left Winger", "Right Winger");

        // Get top scorers from the team
        var season = DateTime.UtcNow.Year;
        var topScorer = await context.Scorers
            .Where(s => s.TeamId == teamId && s.CompetitionId == competitionId && s.Season == season)
            .OrderByDescending(s => s.Goals)
            .FirstOrDefaultAsync(cancellationToken);

        // Create or update
        if (existing != null)
        {
            existing.UsualFormation = formationCounts?.Key;
            existing.UsualGoalkeeperIds = JsonSerializer.Serialize(goalkeeperCounts.Take(2).Select(c => c.Key));
            existing.UsualDefenderIds = JsonSerializer.Serialize(defenderCounts.Take(5).Select(c => c.Key));
            existing.UsualMidfielderIds = JsonSerializer.Serialize(midfielderCounts.Take(5).Select(c => c.Key));
            existing.UsualForwardIds = JsonSerializer.Serialize(forwardCounts.Take(3).Select(c => c.Key));
            existing.TopScorerExternalId = topScorer?.ExternalPlayerId;
            existing.TopScorerName = topScorer?.PlayerName;
            existing.TopScorerGoals = topScorer?.Goals ?? 0;
            existing.MatchesAnalyzed = lineups.Count;
            existing.CalculatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            return existing;
        }
        else
        {
            var usualLineup = TeamUsualLineup.Create(
                teamId,
                competitionId,
                formationCounts?.Key,
                goalkeeperCounts.Take(2).Select(c => c.Key).ToList(),
                defenderCounts.Take(5).Select(c => c.Key).ToList(),
                midfielderCounts.Take(5).Select(c => c.Key).ToList(),
                forwardCounts.Take(3).Select(c => c.Key).ToList(),
                lineups.Count);

            usualLineup.TopScorerExternalId = topScorer?.ExternalPlayerId;
            usualLineup.TopScorerName = topScorer?.PlayerName;
            usualLineup.TopScorerGoals = topScorer?.Goals ?? 0;

            context.TeamUsualLineups.Add(usualLineup);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogDebug(
                "Calculated usual lineup for team {TeamId}: {Formation}",
                teamId, formationCounts?.Key);

            return usualLineup;
        }
    }

    private static List<KeyValuePair<int, int>> CountPlayersByPosition(
        List<LineupPlayer> players,
        params string[] positions)
    {
        return players
            .Where(p => positions.Any(pos =>
                p.Position?.Contains(pos, StringComparison.OrdinalIgnoreCase) == true))
            .GroupBy(p => p.Id)
            .Select(g => new KeyValuePair<int, int>(g.Key, g.Count()))
            .OrderByDescending(kvp => kvp.Value)
            .ToList();
    }
}
```

---

## Part 6: Background Sync Service

### 6.1 Extended Data Sync Background Service

**ExtendedDataSyncService.cs:**
```csharp
namespace ExtraTime.Infrastructure.Services.Football;

public sealed class ExtendedDataSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExtendedDataSyncService> logger) : BackgroundService
{
    // Daily sync at 4 AM UTC for standings and scorers
    private static readonly TimeSpan DailySyncTime = TimeSpan.FromHours(4);

    // Check for lineup sync every 15 minutes
    private static readonly TimeSpan LineupCheckInterval = TimeSpan.FromMinutes(15);

    // Sync lineups 1 hour before kickoff
    private static readonly TimeSpan LineupSyncWindow = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Extended Data Sync Service started");

        // Run initial sync after startup
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        await RunDailySyncAsync(stoppingToken);

        // Calculate next daily sync time
        var nextDailySync = CalculateNextDailySyncTime();
        var lastLineupCheck = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            // Daily standings/scorers sync
            if (now >= nextDailySync)
            {
                await RunDailySyncAsync(stoppingToken);
                nextDailySync = CalculateNextDailySyncTime();
            }

            // Periodic lineup sync check
            if ((now - lastLineupCheck) >= LineupCheckInterval)
            {
                await RunLineupSyncAsync(stoppingToken);
                lastLineupCheck = now;
            }

            // Wait until next check
            var waitTime = TimeSpan.FromMinutes(5);
            await Task.Delay(waitTime, stoppingToken);
        }
    }

    private async Task RunDailySyncAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running daily standings and scorers sync");

        try
        {
            using var scope = scopeFactory.CreateScope();

            var standingsService = scope.ServiceProvider.GetRequiredService<IStandingsSyncService>();
            await standingsService.SyncStandingsAsync(cancellationToken);

            var scorersService = scope.ServiceProvider.GetRequiredService<IScorersSyncService>();
            await scorersService.SyncScorersAsync(cancellationToken);

            logger.LogInformation("Daily sync completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during daily sync");
        }
    }

    private async Task RunLineupSyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();

            var lineupService = scope.ServiceProvider.GetRequiredService<ILineupSyncService>();
            await lineupService.SyncLineupsForUpcomingMatchesAsync(LineupSyncWindow, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during lineup sync");
        }
    }

    private static DateTime CalculateNextDailySyncTime()
    {
        var now = DateTime.UtcNow;
        var todaySync = now.Date.Add(DailySyncTime);

        return now < todaySync ? todaySync : todaySync.AddDays(1);
    }
}
```

---

## Part 7: API Layer - Endpoints

### 7.1 Standings Endpoints

**StandingsEndpoints.cs:**
```csharp
namespace ExtraTime.API.Features.Football;

public static class StandingsEndpoints
{
    public static RouteGroupBuilder MapStandingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/competitions/{competitionId:guid}/standings")
            .WithTags("Standings");

        group.MapGet("/", GetStandings)
            .WithName("GetStandings")
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetStandings(
        Guid competitionId,
        IApplicationDbContext context,
        int? season = null,
        CancellationToken cancellationToken = default)
    {
        var targetSeason = season ?? DateTime.UtcNow.Year;

        var standings = await context.Standings
            .Include(s => s.Team)
            .Where(s => s.CompetitionId == competitionId && s.Season == targetSeason)
            .OrderBy(s => s.Position)
            .Select(s => new StandingDto(
                s.Id,
                s.Position,
                new TeamSummaryDto(s.Team.Id, s.Team.Name, s.Team.ShortName, s.Team.LogoUrl),
                s.PlayedGames,
                s.Won,
                s.Draw,
                s.Lost,
                s.GoalsFor,
                s.GoalsAgainst,
                s.GoalDifference,
                s.Points,
                s.Form,
                s.LastUpdatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(standings);
    }
}
```

### 7.2 Scorers Endpoints

**ScorersEndpoints.cs:**
```csharp
namespace ExtraTime.API.Features.Football;

public static class ScorersEndpoints
{
    public static RouteGroupBuilder MapScorersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/competitions/{competitionId:guid}/scorers")
            .WithTags("Scorers");

        group.MapGet("/", GetScorers)
            .WithName("GetScorers")
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetScorers(
        Guid competitionId,
        IApplicationDbContext context,
        int? season = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var targetSeason = season ?? DateTime.UtcNow.Year;

        var scorers = await context.Scorers
            .Include(s => s.Team)
            .Where(s => s.CompetitionId == competitionId && s.Season == targetSeason)
            .OrderBy(s => s.Rank)
            .Take(limit)
            .Select(s => new ScorerDto(
                s.Id,
                s.Rank,
                s.PlayerName,
                s.PlayerNationality,
                s.PlayerPosition,
                new TeamSummaryDto(s.Team.Id, s.Team.Name, s.Team.ShortName, s.Team.LogoUrl),
                s.Goals,
                s.Assists,
                s.Penalties,
                s.PlayedMatches,
                s.LastUpdatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(scorers);
    }
}
```

### 7.3 Lineup Endpoints

**LineupsEndpoints.cs:**
```csharp
namespace ExtraTime.API.Features.Football;

public static class LineupsEndpoints
{
    public static RouteGroupBuilder MapLineupsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/matches/{matchId:guid}/lineups")
            .WithTags("Lineups");

        group.MapGet("/", GetMatchLineups)
            .WithName("GetMatchLineups")
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetMatchLineups(
        Guid matchId,
        IApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var lineups = await context.MatchLineups
            .Include(ml => ml.Team)
            .Where(ml => ml.MatchId == matchId)
            .Select(ml => new MatchLineupDto(
                ml.Id,
                new TeamSummaryDto(ml.Team.Id, ml.Team.Name, ml.Team.ShortName, ml.Team.LogoUrl),
                ml.Formation,
                ml.CoachName,
                ml.GetStartingPlayers().Select(p => new LineupPlayerDto(p.Id, p.Name, p.Position, p.ShirtNumber)).ToList(),
                ml.GetBenchPlayers().Select(p => new LineupPlayerDto(p.Id, p.Name, p.Position, p.ShirtNumber)).ToList(),
                ml.CaptainName,
                ml.SyncedAt))
            .ToListAsync(cancellationToken);

        if (lineups.Count == 0)
        {
            return Results.NotFound(new { error = "Lineups not available for this match" });
        }

        return Results.Ok(lineups);
    }
}
```

### 7.4 HeadToHead Endpoints

**HeadToHeadEndpoints.cs:**
```csharp
namespace ExtraTime.API.Features.Football;

public static class HeadToHeadEndpoints
{
    public static RouteGroupBuilder MapHeadToHeadEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/teams/{team1Id:guid}/head-to-head/{team2Id:guid}")
            .WithTags("Head to Head");

        group.MapGet("/", GetHeadToHead)
            .WithName("GetHeadToHead")
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetHeadToHead(
        Guid team1Id,
        Guid team2Id,
        IHeadToHeadService h2hService,
        IApplicationDbContext context,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default)
    {
        var h2h = await h2hService.GetOrCalculateAsync(team1Id, team2Id, competitionId, cancellationToken);

        // Get team names
        var teams = await context.Teams
            .Where(t => t.Id == h2h.Team1Id || t.Id == h2h.Team2Id)
            .ToDictionaryAsync(t => t.Id, t => new TeamSummaryDto(t.Id, t.Name, t.ShortName, t.LogoUrl), cancellationToken);

        return Results.Ok(new HeadToHeadDto(
            teams.GetValueOrDefault(h2h.Team1Id),
            teams.GetValueOrDefault(h2h.Team2Id),
            h2h.TotalMatches,
            h2h.Team1Wins,
            h2h.Team2Wins,
            h2h.Draws,
            h2h.Team1Goals,
            h2h.Team2Goals,
            h2h.LastMatchDate,
            h2h.CalculatedAt));
    }
}
```

### 7.5 Admin Sync Endpoints

**ExtendedDataSyncEndpoints.cs:**
```csharp
namespace ExtraTime.API.Features.Admin;

public static class ExtendedDataSyncEndpoints
{
    public static RouteGroupBuilder MapExtendedDataSyncEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/sync")
            .WithTags("Admin - Extended Data Sync")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/standings", SyncStandings)
            .WithName("SyncStandings");

        group.MapPost("/standings/{competitionId:guid}", SyncStandingsForCompetition)
            .WithName("SyncStandingsForCompetition");

        group.MapPost("/scorers", SyncScorers)
            .WithName("SyncScorers");

        group.MapPost("/scorers/{competitionId:guid}", SyncScorersForCompetition)
            .WithName("SyncScorersForCompetition");

        group.MapPost("/lineups", SyncUpcomingLineups)
            .WithName("SyncUpcomingLineups");

        group.MapPost("/lineups/{matchId:guid}", SyncMatchLineups)
            .WithName("SyncMatchLineups");

        group.MapPost("/usual-lineups", RefreshUsualLineups)
            .WithName("RefreshUsualLineups");

        return group;
    }

    private static async Task<IResult> SyncStandings(
        IStandingsSyncService standingsService,
        CancellationToken cancellationToken)
    {
        await standingsService.SyncStandingsAsync(cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> SyncStandingsForCompetition(
        Guid competitionId,
        IStandingsSyncService standingsService,
        CancellationToken cancellationToken)
    {
        await standingsService.SyncStandingsForCompetitionAsync(competitionId, cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> SyncScorers(
        IScorersSyncService scorersService,
        CancellationToken cancellationToken)
    {
        await scorersService.SyncScorersAsync(cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> SyncScorersForCompetition(
        Guid competitionId,
        IScorersSyncService scorersService,
        CancellationToken cancellationToken)
    {
        await scorersService.SyncScorersForCompetitionAsync(competitionId, 50, cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> SyncUpcomingLineups(
        ILineupSyncService lineupService,
        CancellationToken cancellationToken)
    {
        await lineupService.SyncLineupsForUpcomingMatchesAsync(TimeSpan.FromHours(2), cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> SyncMatchLineups(
        Guid matchId,
        ILineupSyncService lineupService,
        CancellationToken cancellationToken)
    {
        await lineupService.SyncLineupForMatchAsync(matchId, cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> RefreshUsualLineups(
        ITeamUsualLineupService usualLineupService,
        CancellationToken cancellationToken)
    {
        await usualLineupService.RefreshAllAsync(cancellationToken);
        return Results.Accepted();
    }
}
```

### 7.6 Response DTOs

**ExtendedFootballDtos.cs:**
```csharp
namespace ExtraTime.Application.Features.Football.DTOs;

public sealed record TeamSummaryDto(
    Guid Id,
    string Name,
    string? ShortName,
    string? LogoUrl);

public sealed record StandingDto(
    Guid Id,
    int Position,
    TeamSummaryDto Team,
    int PlayedGames,
    int Won,
    int Draw,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points,
    string? Form,
    DateTime LastUpdatedAt);

public sealed record ScorerDto(
    Guid Id,
    int Rank,
    string PlayerName,
    string? Nationality,
    string? Position,
    TeamSummaryDto Team,
    int Goals,
    int? Assists,
    int? Penalties,
    int? PlayedMatches,
    DateTime LastUpdatedAt);

public sealed record LineupPlayerDto(
    int Id,
    string Name,
    string? Position,
    int? ShirtNumber);

public sealed record MatchLineupDto(
    Guid Id,
    TeamSummaryDto Team,
    string? Formation,
    string? CoachName,
    List<LineupPlayerDto> StartingXI,
    List<LineupPlayerDto> Bench,
    string? CaptainName,
    DateTime SyncedAt);

public sealed record HeadToHeadDto(
    TeamSummaryDto? Team1,
    TeamSummaryDto? Team2,
    int TotalMatches,
    int Team1Wins,
    int Team2Wins,
    int Draws,
    int Team1Goals,
    int Team2Goals,
    DateTime? LastMatchDate,
    DateTime CalculatedAt);
```

---

## Part 8: API Endpoints Summary

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/competitions/{id}/standings` | Yes | Get league standings |
| GET | `/api/competitions/{id}/scorers` | Yes | Get top scorers |
| GET | `/api/matches/{id}/lineups` | Yes | Get match lineups |
| GET | `/api/teams/{id}/head-to-head/{id}` | Yes | Get H2H between teams |
| POST | `/api/admin/sync/standings` | Admin | Sync all standings |
| POST | `/api/admin/sync/standings/{id}` | Admin | Sync standings for competition |
| POST | `/api/admin/sync/scorers` | Admin | Sync all scorers |
| POST | `/api/admin/sync/scorers/{id}` | Admin | Sync scorers for competition |
| POST | `/api/admin/sync/lineups` | Admin | Sync upcoming lineups |
| POST | `/api/admin/sync/lineups/{id}` | Admin | Sync lineup for match |
| POST | `/api/admin/sync/usual-lineups` | Admin | Refresh usual lineups |

---

## Part 9: Frontend Implementation

### 9.1 Types

**types/standings.ts:**
```typescript
export interface Standing {
  id: string;
  position: number;
  team: TeamSummary;
  playedGames: number;
  won: number;
  draw: number;
  lost: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
  form: string | null;
  lastUpdatedAt: string;
}

export interface TeamSummary {
  id: string;
  name: string;
  shortName: string | null;
  logoUrl: string | null;
}
```

**types/scorers.ts:**
```typescript
export interface Scorer {
  id: string;
  rank: number;
  playerName: string;
  nationality: string | null;
  position: string | null;
  team: TeamSummary;
  goals: number;
  assists: number | null;
  penalties: number | null;
  playedMatches: number | null;
  lastUpdatedAt: string;
}
```

**types/lineups.ts:**
```typescript
export interface MatchLineup {
  id: string;
  team: TeamSummary;
  formation: string | null;
  coachName: string | null;
  startingXI: LineupPlayer[];
  bench: LineupPlayer[];
  captainName: string | null;
  syncedAt: string;
}

export interface LineupPlayer {
  id: number;
  name: string;
  position: string | null;
  shirtNumber: number | null;
}
```

**types/head-to-head.ts:**
```typescript
export interface HeadToHead {
  team1: TeamSummary | null;
  team2: TeamSummary | null;
  totalMatches: number;
  team1Wins: number;
  team2Wins: number;
  draws: number;
  team1Goals: number;
  team2Goals: number;
  lastMatchDate: string | null;
  calculatedAt: string;
}
```

### 9.2 API Hooks

**hooks/use-standings.ts:**
```typescript
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { Standing } from '@/types/standings';

export function useStandings(competitionId: string, season?: number) {
  return useQuery({
    queryKey: ['standings', competitionId, season],
    queryFn: () =>
      apiClient.get<Standing[]>(
        `/competitions/${competitionId}/standings`,
        { params: season ? { season } : undefined }
      ),
    staleTime: 1000 * 60 * 15, // 15 minutes
  });
}
```

**hooks/use-scorers.ts:**
```typescript
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { Scorer } from '@/types/scorers';

export function useScorers(competitionId: string, limit = 20, season?: number) {
  return useQuery({
    queryKey: ['scorers', competitionId, limit, season],
    queryFn: () =>
      apiClient.get<Scorer[]>(
        `/competitions/${competitionId}/scorers`,
        { params: { limit, ...(season ? { season } : {}) } }
      ),
    staleTime: 1000 * 60 * 15, // 15 minutes
  });
}
```

**hooks/use-lineups.ts:**
```typescript
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { MatchLineup } from '@/types/lineups';

export function useMatchLineups(matchId: string, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: ['lineups', matchId],
    queryFn: () => apiClient.get<MatchLineup[]>(`/matches/${matchId}/lineups`),
    enabled: options?.enabled ?? true,
    staleTime: 1000 * 60 * 5, // 5 minutes
    retry: false, // Lineups may not be available yet
  });
}
```

**hooks/use-head-to-head.ts:**
```typescript
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { HeadToHead } from '@/types/head-to-head';

export function useHeadToHead(
  team1Id: string,
  team2Id: string,
  competitionId?: string
) {
  return useQuery({
    queryKey: ['head-to-head', team1Id, team2Id, competitionId],
    queryFn: () =>
      apiClient.get<HeadToHead>(
        `/teams/${team1Id}/head-to-head/${team2Id}`,
        { params: competitionId ? { competitionId } : undefined }
      ),
    staleTime: 1000 * 60 * 60, // 1 hour
  });
}
```

### 9.3 Components

**components/standings/standings-table.tsx:**
```typescript
'use client';

import { useStandings } from '@/hooks/use-standings';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';

interface StandingsTableProps {
  competitionId: string;
  className?: string;
}

export function StandingsTable({ competitionId, className }: StandingsTableProps) {
  const { data: standings, isLoading, error } = useStandings(competitionId);

  if (isLoading) {
    return <StandingsTableSkeleton />;
  }

  if (error || !standings) {
    return <div className="text-center text-muted-foreground py-8">Failed to load standings</div>;
  }

  return (
    <Table className={className}>
      <TableHeader>
        <TableRow>
          <TableHead className="w-12">#</TableHead>
          <TableHead>Team</TableHead>
          <TableHead className="text-center">P</TableHead>
          <TableHead className="text-center">W</TableHead>
          <TableHead className="text-center">D</TableHead>
          <TableHead className="text-center">L</TableHead>
          <TableHead className="text-center">GF</TableHead>
          <TableHead className="text-center">GA</TableHead>
          <TableHead className="text-center">GD</TableHead>
          <TableHead className="text-center font-bold">Pts</TableHead>
          <TableHead className="text-center">Form</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {standings.map((standing) => (
          <TableRow key={standing.id} className={getPositionRowClass(standing.position)}>
            <TableCell className="font-medium">{standing.position}</TableCell>
            <TableCell>
              <div className="flex items-center gap-2">
                <Avatar className="h-6 w-6">
                  <AvatarImage src={standing.team.logoUrl ?? undefined} alt={standing.team.name} />
                  <AvatarFallback className="text-xs">
                    {standing.team.shortName?.slice(0, 2) ?? standing.team.name.slice(0, 2)}
                  </AvatarFallback>
                </Avatar>
                <span className="font-medium">{standing.team.name}</span>
              </div>
            </TableCell>
            <TableCell className="text-center">{standing.playedGames}</TableCell>
            <TableCell className="text-center text-green-600 dark:text-green-400">{standing.won}</TableCell>
            <TableCell className="text-center text-muted-foreground">{standing.draw}</TableCell>
            <TableCell className="text-center text-red-600 dark:text-red-400">{standing.lost}</TableCell>
            <TableCell className="text-center">{standing.goalsFor}</TableCell>
            <TableCell className="text-center">{standing.goalsAgainst}</TableCell>
            <TableCell className="text-center font-medium">
              {standing.goalDifference > 0 ? `+${standing.goalDifference}` : standing.goalDifference}
            </TableCell>
            <TableCell className="text-center font-bold">{standing.points}</TableCell>
            <TableCell className="text-center">
              {standing.form && <FormBadges form={standing.form} />}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

function FormBadges({ form }: { form: string }) {
  return (
    <div className="flex gap-0.5 justify-center">
      {form.split('').slice(0, 5).map((result, i) => (
        <Badge
          key={i}
          variant="outline"
          className={cn(
            'w-5 h-5 p-0 flex items-center justify-center text-xs rounded-full',
            result === 'W' && 'bg-green-500 text-white border-green-500',
            result === 'D' && 'bg-muted text-muted-foreground',
            result === 'L' && 'bg-red-500 text-white border-red-500'
          )}
        >
          {result}
        </Badge>
      ))}
    </div>
  );
}

function getPositionRowClass(position: number): string {
  // Champions League positions (top 4 in most leagues)
  if (position <= 4) return 'bg-blue-50 dark:bg-blue-950/20';
  // Europa League position (typically 5th)
  if (position === 5) return 'bg-orange-50 dark:bg-orange-950/20';
  // Conference League (typically 6th)
  if (position === 6) return 'bg-green-50 dark:bg-green-950/20';
  // Relegation zone (bottom 3)
  if (position >= 18) return 'bg-red-50 dark:bg-red-950/20';
  return '';
}

function StandingsTableSkeleton() {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className="w-12">#</TableHead>
          <TableHead>Team</TableHead>
          <TableHead className="text-center">P</TableHead>
          <TableHead className="text-center">Pts</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {Array.from({ length: 10 }).map((_, i) => (
          <TableRow key={i}>
            <TableCell><Skeleton className="h-4 w-4" /></TableCell>
            <TableCell><Skeleton className="h-4 w-32" /></TableCell>
            <TableCell><Skeleton className="h-4 w-4 mx-auto" /></TableCell>
            <TableCell><Skeleton className="h-4 w-6 mx-auto" /></TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
```

**components/scorers/scorers-list.tsx:**
```typescript
'use client';

import { useScorers } from '@/hooks/use-scorers';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';

interface ScorersListProps {
  competitionId: string;
  limit?: number;
  className?: string;
}

export function ScorersList({ competitionId, limit = 10, className }: ScorersListProps) {
  const { data: scorers, isLoading, error } = useScorers(competitionId, limit);

  if (isLoading) {
    return <ScorersListSkeleton count={limit} />;
  }

  if (error || !scorers) {
    return <div className="text-center text-muted-foreground py-8">Failed to load scorers</div>;
  }

  return (
    <div className={className}>
      <div className="space-y-3">
        {scorers.map((scorer) => (
          <div
            key={scorer.id}
            className="flex items-center justify-between p-3 rounded-lg bg-card border"
          >
            <div className="flex items-center gap-3">
              <Badge
                variant={scorer.rank <= 3 ? 'default' : 'secondary'}
                className="w-8 h-8 rounded-full flex items-center justify-center"
              >
                {scorer.rank}
              </Badge>
              <div>
                <p className="font-medium">{scorer.playerName}</p>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Avatar className="h-4 w-4">
                    <AvatarImage src={scorer.team.logoUrl ?? undefined} />
                    <AvatarFallback className="text-[8px]">
                      {scorer.team.shortName?.slice(0, 2)}
                    </AvatarFallback>
                  </Avatar>
                  <span>{scorer.team.name}</span>
                </div>
              </div>
            </div>
            <div className="text-right">
              <p className="text-2xl font-bold">{scorer.goals}</p>
              <p className="text-xs text-muted-foreground">
                {scorer.assists !== null && `${scorer.assists} assists`}
              </p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function ScorersListSkeleton({ count }: { count: number }) {
  return (
    <div className="space-y-3">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="flex items-center justify-between p-3 rounded-lg border">
          <div className="flex items-center gap-3">
            <Skeleton className="w-8 h-8 rounded-full" />
            <div>
              <Skeleton className="h-4 w-32 mb-1" />
              <Skeleton className="h-3 w-24" />
            </div>
          </div>
          <Skeleton className="h-8 w-8" />
        </div>
      ))}
    </div>
  );
}
```

**components/lineups/match-lineup.tsx:**
```typescript
'use client';

import { useMatchLineups } from '@/hooks/use-lineups';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import type { MatchLineup as MatchLineupType } from '@/types/lineups';

interface MatchLineupProps {
  matchId: string;
  className?: string;
}

export function MatchLineup({ matchId, className }: MatchLineupProps) {
  const { data: lineups, isLoading, error } = useMatchLineups(matchId);

  if (isLoading) {
    return <MatchLineupSkeleton />;
  }

  if (error || !lineups || lineups.length === 0) {
    return (
      <Card className={className}>
        <CardContent className="py-8 text-center text-muted-foreground">
          Lineups not yet available
        </CardContent>
      </Card>
    );
  }

  const [home, away] = lineups;

  return (
    <div className={cn('grid md:grid-cols-2 gap-4', className)}>
      <TeamLineupCard lineup={home} isHome />
      <TeamLineupCard lineup={away} isHome={false} />
    </div>
  );
}

function TeamLineupCard({ lineup, isHome }: { lineup: MatchLineupType; isHome: boolean }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Avatar className="h-8 w-8">
              <AvatarImage src={lineup.team.logoUrl ?? undefined} />
              <AvatarFallback>{lineup.team.shortName?.slice(0, 2)}</AvatarFallback>
            </Avatar>
            <CardTitle className="text-lg">{lineup.team.name}</CardTitle>
          </div>
          {lineup.formation && (
            <Badge variant="secondary">{lineup.formation}</Badge>
          )}
        </div>
        {lineup.coachName && (
          <p className="text-sm text-muted-foreground">Coach: {lineup.coachName}</p>
        )}
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          <div>
            <h4 className="text-sm font-medium mb-2">Starting XI</h4>
            <div className="grid grid-cols-2 gap-1">
              {lineup.startingXI.map((player) => (
                <div
                  key={player.id}
                  className="flex items-center gap-2 text-sm py-1"
                >
                  <span className="text-muted-foreground w-6 text-right">
                    {player.shirtNumber}
                  </span>
                  <span className={cn(
                    player.name === lineup.captainName && 'font-medium'
                  )}>
                    {player.name}
                    {player.name === lineup.captainName && ' (C)'}
                  </span>
                </div>
              ))}
            </div>
          </div>
          {lineup.bench.length > 0 && (
            <div>
              <h4 className="text-sm font-medium mb-2 text-muted-foreground">Substitutes</h4>
              <div className="grid grid-cols-2 gap-1">
                {lineup.bench.map((player) => (
                  <div
                    key={player.id}
                    className="flex items-center gap-2 text-sm py-1 text-muted-foreground"
                  >
                    <span className="w-6 text-right">{player.shirtNumber}</span>
                    <span>{player.name}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

function MatchLineupSkeleton() {
  return (
    <div className="grid md:grid-cols-2 gap-4">
      {[0, 1].map((i) => (
        <Card key={i}>
          <CardHeader>
            <Skeleton className="h-6 w-32" />
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {Array.from({ length: 11 }).map((_, j) => (
                <Skeleton key={j} className="h-4 w-full" />
              ))}
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
```

**components/head-to-head/head-to-head-card.tsx:**
```typescript
'use client';

import { useHeadToHead } from '@/hooks/use-head-to-head';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Progress } from '@/components/ui/progress';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';

interface HeadToHeadCardProps {
  team1Id: string;
  team2Id: string;
  competitionId?: string;
  className?: string;
}

export function HeadToHeadCard({
  team1Id,
  team2Id,
  competitionId,
  className,
}: HeadToHeadCardProps) {
  const { data: h2h, isLoading, error } = useHeadToHead(team1Id, team2Id, competitionId);

  if (isLoading) {
    return <HeadToHeadSkeleton />;
  }

  if (error || !h2h) {
    return (
      <Card className={className}>
        <CardContent className="py-8 text-center text-muted-foreground">
          No head-to-head data available
        </CardContent>
      </Card>
    );
  }

  const total = h2h.team1Wins + h2h.team2Wins + h2h.draws;
  const team1Pct = total > 0 ? (h2h.team1Wins / total) * 100 : 0;
  const drawPct = total > 0 ? (h2h.draws / total) * 100 : 0;
  const team2Pct = total > 0 ? (h2h.team2Wins / total) * 100 : 0;

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle className="text-center">Head to Head</CardTitle>
        <p className="text-center text-sm text-muted-foreground">
          {h2h.totalMatches} matches played
        </p>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Team headers */}
        <div className="flex items-center justify-between">
          <TeamHeader team={h2h.team1} />
          <span className="text-muted-foreground text-sm">vs</span>
          <TeamHeader team={h2h.team2} />
        </div>

        {/* Win distribution bar */}
        <div className="space-y-2">
          <div className="flex h-4 rounded-full overflow-hidden">
            <div
              className="bg-blue-500 transition-all"
              style={{ width: `${team1Pct}%` }}
            />
            <div
              className="bg-muted transition-all"
              style={{ width: `${drawPct}%` }}
            />
            <div
              className="bg-red-500 transition-all"
              style={{ width: `${team2Pct}%` }}
            />
          </div>
          <div className="flex justify-between text-sm">
            <span className="font-medium">{h2h.team1Wins} wins</span>
            <span className="text-muted-foreground">{h2h.draws} draws</span>
            <span className="font-medium">{h2h.team2Wins} wins</span>
          </div>
        </div>

        {/* Goals */}
        <div className="text-center">
          <p className="text-sm text-muted-foreground">Total Goals</p>
          <p className="text-2xl font-bold">
            {h2h.team1Goals} - {h2h.team2Goals}
          </p>
        </div>
      </CardContent>
    </Card>
  );
}

function TeamHeader({ team }: { team: { name: string; shortName: string | null; logoUrl: string | null } | null }) {
  if (!team) return <div className="w-20" />;

  return (
    <div className="flex flex-col items-center gap-2">
      <Avatar className="h-12 w-12">
        <AvatarImage src={team.logoUrl ?? undefined} />
        <AvatarFallback>{team.shortName?.slice(0, 2)}</AvatarFallback>
      </Avatar>
      <span className="text-sm font-medium text-center">{team.shortName || team.name}</span>
    </div>
  );
}

function HeadToHeadSkeleton() {
  return (
    <Card>
      <CardHeader>
        <Skeleton className="h-6 w-32 mx-auto" />
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex justify-between">
          <Skeleton className="h-12 w-12 rounded-full" />
          <Skeleton className="h-12 w-12 rounded-full" />
        </div>
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-8 w-24 mx-auto" />
      </CardContent>
    </Card>
  );
}
```

### 9.4 Pages

**app/(authenticated)/competitions/[id]/standings/page.tsx:**
```typescript
import { StandingsTable } from '@/components/standings/standings-table';
import { PageHeader } from '@/components/layout/page-header';

interface StandingsPageProps {
  params: { id: string };
}

export default function StandingsPage({ params }: StandingsPageProps) {
  return (
    <div className="container py-6 space-y-6">
      <PageHeader
        title="League Table"
        description="Current standings for this competition"
      />
      <StandingsTable competitionId={params.id} />
    </div>
  );
}
```

**app/(authenticated)/competitions/[id]/scorers/page.tsx:**
```typescript
import { ScorersList } from '@/components/scorers/scorers-list';
import { PageHeader } from '@/components/layout/page-header';

interface ScorersPageProps {
  params: { id: string };
}

export default function ScorersPage({ params }: ScorersPageProps) {
  return (
    <div className="container py-6 space-y-6">
      <PageHeader
        title="Top Scorers"
        description="Leading goal scorers this season"
      />
      <ScorersList competitionId={params.id} limit={20} />
    </div>
  );
}
```

---

## Part 10: Dependency Injection Registration

**DependencyInjection.cs (additions):**
```csharp
// In AddInfrastructureServices:

// Extended football data services
services.AddScoped<IStandingsSyncService, StandingsSyncService>();
services.AddScoped<IScorersSyncService, ScorersSyncService>();
services.AddScoped<ILineupSyncService, LineupSyncService>();
services.AddScoped<IHeadToHeadService, HeadToHeadService>();
services.AddScoped<ITeamUsualLineupService, TeamUsualLineupService>();

// Background service for extended data sync
services.AddHostedService<ExtendedDataSyncService>();
```

---

## Part 11: Implementation Checklist

### Phase 9A: Domain Layer - Entities
- [ ] Create `Standing` entity
- [ ] Create `Scorer` entity
- [ ] Create `MatchLineup` entity with `LineupPlayer` record
- [ ] Create `TeamUsualLineup` entity
- [ ] Create `HeadToHead` entity with `HeadToHeadStats` record
- [ ] Update `Match` entity with lineup navigation properties

### Phase 9B: Infrastructure Layer - Database
- [ ] Create `StandingConfiguration`
- [ ] Create `ScorerConfiguration`
- [ ] Create `MatchLineupConfiguration`
- [ ] Create `TeamUsualLineupConfiguration`
- [ ] Create `HeadToHeadConfiguration`
- [ ] Update `ApplicationDbContext` with new DbSets
- [ ] Create migration `AddExtendedFootballData`
- [ ] Run migration

### Phase 9C: Application Layer - DTOs
- [ ] Create `StandingsApiResponse` and related API DTOs
- [ ] Create `ScorersApiResponse` and related API DTOs
- [ ] Create `MatchDetailApiDto` with lineup support
- [ ] Create response DTOs (`StandingDto`, `ScorerDto`, etc.)

### Phase 9D: Infrastructure Layer - Football Data Service
- [ ] Add `GetStandingsAsync` to `IFootballDataService`
- [ ] Add `GetScorersAsync` to `IFootballDataService`
- [ ] Add `GetMatchDetailsAsync` to `IFootballDataService`
- [ ] Implement methods in `FootballDataService`

### Phase 9E: Application Layer - Sync Services
- [ ] Create `IStandingsSyncService` interface
- [ ] Implement `StandingsSyncService`
- [ ] Create `IScorersSyncService` interface
- [ ] Implement `ScorersSyncService`
- [ ] Create `ILineupSyncService` interface
- [ ] Implement `LineupSyncService`

### Phase 9F: Application Layer - Calculator Services
- [ ] Create `IHeadToHeadService` interface
- [ ] Implement `HeadToHeadService`
- [ ] Create `ITeamUsualLineupService` interface
- [ ] Implement `TeamUsualLineupService`

### Phase 9G: Infrastructure Layer - Background Service
- [ ] Create `ExtendedDataSyncService`
- [ ] Configure daily sync at 4 AM UTC
- [ ] Configure lineup sync 1 hour before matches

### Phase 9H: API Layer - Endpoints
- [ ] Create `StandingsEndpoints`
- [ ] Create `ScorersEndpoints`
- [ ] Create `LineupsEndpoints`
- [ ] Create `HeadToHeadEndpoints`
- [ ] Create `ExtendedDataSyncEndpoints` (admin)
- [ ] Register all endpoints in Program.cs

### Phase 9I: Dependency Injection
- [ ] Register all sync services
- [ ] Register calculator services
- [ ] Register background service

### Phase 9J: Frontend - Types
- [ ] Create `types/standings.ts`
- [ ] Create `types/scorers.ts`
- [ ] Create `types/lineups.ts`
- [ ] Create `types/head-to-head.ts`

### Phase 9K: Frontend - Hooks
- [ ] Create `use-standings.ts`
- [ ] Create `use-scorers.ts`
- [ ] Create `use-lineups.ts`
- [ ] Create `use-head-to-head.ts`

### Phase 9L: Frontend - Components
- [ ] Create `standings-table.tsx`
- [ ] Create `scorers-list.tsx`
- [ ] Create `match-lineup.tsx`
- [ ] Create `head-to-head-card.tsx`

### Phase 9M: Frontend - Pages
- [ ] Create standings page
- [ ] Create scorers page
- [ ] Update match detail page with lineups
- [ ] Add H2H to match preview

### Phase 9N: Testing & Verification
- [ ] Test standings sync for all competitions
- [ ] Test scorers sync
- [ ] Test lineup sync before matches
- [ ] Test H2H calculation accuracy
- [ ] Test usual lineup calculation
- [ ] Verify rate limiting compliance
- [ ] Test frontend components

---

## Future Integration: Bot System (Phase 7.5)

When Phase 7.5 (Intelligent Stats-Based Bots) is implemented, the following enhancements become available:

### Standings-Based Bot Analysis

```csharp
// Example integration in StatsAnalystStrategy
public double CalculatePositionFactor(Standing homeStanding, Standing awayStanding)
{
    int positionGap = awayStanding.Position - homeStanding.Position;

    // Large position gap increases home advantage
    if (positionGap > 10)
    {
        return 1.15; // 15% boost for home team
    }
    else if (positionGap < -10)
    {
        return 0.85; // 15% reduction for home team
    }

    // Title race detection
    if (homeStanding.IsInTitleRace() && awayStanding.IsInTitleRace())
    {
        return 1.0; // Tight matches, reduce prediction confidence
    }

    // Relegation battle
    if (homeStanding.IsInRelegationZone() && awayStanding.IsInRelegationZone())
    {
        return 1.0; // Desperate matches, unpredictable
    }

    return 1.0 + (positionGap * 0.01); // Small adjustment per position
}
```

### Lineup-Based Bot Analysis

```csharp
// Example integration for lineup analysis
public double CalculateLineupStrength(MatchLineup current, TeamUsualLineup usual)
{
    double modifier = 1.0;

    var currentPlayers = current.GetAllPlayerIds();
    var usualPlayers = usual.GetAllUsualPlayerIds();

    // Check goalkeeper change
    var currentGK = current.GetGoalkeeper()?.Id;
    var usualGKs = usual.GetUsualGoalkeeperIds();
    if (currentGK.HasValue && !usualGKs.Contains(currentGK.Value))
    {
        modifier -= 0.08; // 8% reduction
    }

    // Check top scorer absence
    if (usual.TopScorerExternalId.HasValue &&
        !currentPlayers.Contains(usual.TopScorerExternalId.Value))
    {
        modifier -= 0.12; // 12% reduction
    }

    // Check formation change
    if (!string.IsNullOrEmpty(usual.UsualFormation) &&
        current.Formation != usual.UsualFormation)
    {
        modifier -= 0.03; // 3% reduction
    }

    return Math.Clamp(modifier, 0.5, 1.0);
}
```

---

## Notes

### Rate Limiting Compliance
- Daily sync budget: 10 requests (5 standings + 5 scorers)
- Lineup sync: 1 request per match, spread across time
- All syncs respect the existing `RateLimitingHandler`

### Data Freshness
- Standings/Scorers: Updated daily at 4 AM UTC
- Lineups: Synced 1 hour before each match
- H2H: Calculated on-demand, cached for 7 days
- Usual Lineups: Recalculated every 3 days

### Error Handling
- All sync operations are fault-tolerant
- Individual competition failures don't block others
- Missing data returns null/empty rather than throwing

### Future Considerations
- Phase 9.5 adds external data sources (xG, odds, injuries)
- Bot integration happens when Phase 7.5 is implemented
- All services are designed for extensibility
