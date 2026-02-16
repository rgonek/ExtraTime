# Phase 9 (Revised): Head-to-Head Records

## Overview

Calculate and cache historical matchup records between teams from existing Match data in the database. No external API calls needed - purely local computation with on-demand caching.
Includes aggregate metrics and recent 3-meeting counters required by Phase 7.8 ML features.

**Design**: On-demand calculation with DB cache. Computed when requested (match preview, bot prediction). Cached in `HeadToHeads` table with 7-day expiry. No background functions.

---

## Part 1: Domain Layer

### 1.1 HeadToHead Entity

**File:** `src/ExtraTime.Domain/Entities/HeadToHead.cs`

```csharp
namespace ExtraTime.Domain.Entities;

public sealed class HeadToHead : BaseEntity
{
    // Teams (ordered by GUID to ensure consistent lookup)
    public Guid Team1Id { get; private set; }
    public Team Team1 { get; private set; } = null!;

    public Guid Team2Id { get; private set; }
    public Team Team2 { get; private set; } = null!;

    // Optional: scope to competition (null = all competitions)
    public Guid? CompetitionId { get; private set; }
    public Competition? Competition { get; private set; }

    // Overall stats
    public int TotalMatches { get; private set; }
    public int Team1Wins { get; private set; }
    public int Team2Wins { get; private set; }
    public int Draws { get; private set; }

    // Goals
    public int Team1Goals { get; private set; }
    public int Team2Goals { get; private set; }

    // Both Teams Scored & Over 2.5
    public int BothTeamsScoredCount { get; private set; }
    public int Over25Count { get; private set; }

    // Computed rates
    public double BttsRate => TotalMatches > 0 ? (double)BothTeamsScoredCount / TotalMatches : 0;
    public double Over25Rate => TotalMatches > 0 ? (double)Over25Count / TotalMatches : 0;

    // Home/Away breakdown for Team1
    public int Team1HomeMatches { get; private set; }
    public int Team1HomeWins { get; private set; }
    public int Team1HomeGoals { get; private set; }
    public int Team1HomeConceded { get; private set; }

    // Last match info
    public DateTime? LastMatchDate { get; private set; }
    public Guid? LastMatchId { get; private set; }

    // Recent form (last 3 meetings)
    public int RecentMatchesCount { get; private set; }
    public int RecentTeam1Wins { get; private set; }
    public int RecentTeam2Wins { get; private set; }
    public int RecentDraws { get; private set; }

    // Metadata
    public int MatchesAnalyzed { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    private HeadToHead() { }

    public static HeadToHead Create(Guid team1Id, Guid team2Id, Guid? competitionId = null)
    {
        // Ensure consistent ordering by GUID
        var (first, second) = team1Id.CompareTo(team2Id) < 0
            ? (team1Id, team2Id)
            : (team2Id, team1Id);

        return new HeadToHead
        {
            Id = Guid.NewGuid(),
            Team1Id = first,
            Team2Id = second,
            CompetitionId = competitionId,
            CalculatedAt = Clock.UtcNow
        };
    }

    public void UpdateStats(
        int totalMatches,
        int team1Wins, int team2Wins, int draws,
        int team1Goals, int team2Goals,
        int team1HomeMatches, int team1HomeWins,
        int team1HomeGoals, int team1HomeConceded,
        int bothTeamsScoredCount, int over25Count,
        DateTime? lastMatchDate, Guid? lastMatchId,
        int recentMatchesCount, int recentTeam1Wins, int recentTeam2Wins, int recentDraws,
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
                TotalMatches: TotalMatches,
                HomeMatches: Team1HomeMatches,
                HomeWins: Team1HomeWins,
                RecentWins: RecentTeam1Wins,
                RecentMatchesCount: RecentMatchesCount,
                BttsRate: BttsRate,
                Over25Rate: Over25Rate);
        }

        if (teamId == Team2Id)
        {
            return new HeadToHeadStats(
                Wins: Team2Wins,
                Losses: Team1Wins,
                Draws: Draws,
                GoalsFor: Team2Goals,
                GoalsAgainst: Team1Goals,
                TotalMatches: TotalMatches,
                HomeMatches: TotalMatches - Team1HomeMatches,
                HomeWins: Team2Wins - (Team1HomeMatches - Team1HomeWins - Draws), // approximate
                RecentWins: RecentTeam2Wins,
                RecentMatchesCount: RecentMatchesCount,
                BttsRate: BttsRate,
                Over25Rate: Over25Rate);
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
```

---

## Part 2: Infrastructure Layer

### 2.1 EF Configuration

**File:** `src/ExtraTime.Infrastructure/Data/Configurations/HeadToHeadConfiguration.cs`

```csharp
public sealed class HeadToHeadConfiguration : IEntityTypeConfiguration<HeadToHead>
{
    public void Configure(EntityTypeBuilder<HeadToHead> builder)
    {
        builder.ToTable("HeadToHeads");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.HasOne(h => h.Team1)
            .WithMany()
            .HasForeignKey(h => h.Team1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Team2)
            .WithMany()
            .HasForeignKey(h => h.Team2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Competition)
            .WithMany()
            .HasForeignKey(h => h.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // One H2H per team pair per competition (null = all competitions)
        builder.HasIndex(h => new { h.Team1Id, h.Team2Id, h.CompetitionId })
            .IsUnique()
            .HasFilter(null);
    }
}
```

### 2.2 ApplicationDbContext

Add to `ApplicationDbContext.cs`:
```csharp
public DbSet<HeadToHead> HeadToHeads => Set<HeadToHead>();
```

Add to `IApplicationDbContext.cs`:
```csharp
DbSet<HeadToHead> HeadToHeads { get; }
```

### 2.3 Migration

`dotnet ef migrations add AddHeadToHead`

---

## Part 3: Application Layer

### 3.1 Interface

**File:** `src/ExtraTime.Application/Common/Interfaces/IHeadToHeadService.cs`

```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface IHeadToHeadService
{
    Task<HeadToHead> GetOrCalculateAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default);

    Task<HeadToHead> RefreshAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default);
}
```

### 3.2 DTOs

**File:** `src/ExtraTime.Application/Features/Football/DTOs/HeadToHeadDtos.cs`

```csharp
namespace ExtraTime.Application.Features.Football.DTOs;

public sealed record HeadToHeadDto(
    Guid Team1Id,
    string Team1Name,
    Guid Team2Id,
    string Team2Name,
    int TotalMatches,
    int Team1Wins,
    int Team2Wins,
    int Draws,
    int Team1Goals,
    int Team2Goals,
    double BttsRate,
    double Over25Rate,
    int RecentTeam1Wins,
    int RecentTeam2Wins,
    int RecentDraws,
    DateTime? LastMatchDate,
    DateTime CalculatedAt);
```

---

## Part 4: Service Implementation

### 4.1 HeadToHeadService

**File:** `src/ExtraTime.Infrastructure/Services/Football/HeadToHeadService.cs`

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

        var cached = await context.HeadToHeads
            .FirstOrDefaultAsync(h =>
                h.Team1Id == first &&
                h.Team2Id == second &&
                h.CompetitionId == competitionId,
                cancellationToken);

        if (cached != null && (Clock.UtcNow - cached.CalculatedAt) < CacheExpiry)
            return cached;

        return await CalculateAndStoreAsync(first, second, competitionId, cached, cancellationToken);
    }

    public async Task<HeadToHead> RefreshAsync(
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

        return await CalculateAndStoreAsync(first, second, competitionId, cached, cancellationToken);
    }

    private async Task<HeadToHead> CalculateAndStoreAsync(
        Guid team1Id,
        Guid team2Id,
        Guid? competitionId,
        HeadToHead? existing,
        CancellationToken cancellationToken)
    {
        // Query finished matches between these two teams
        var matchesQuery = context.Matches
            .Where(m => m.Status == MatchStatus.Finished)
            .Where(m =>
                (m.HomeTeamId == team1Id && m.AwayTeamId == team2Id) ||
                (m.HomeTeamId == team2Id && m.AwayTeamId == team1Id));

        if (competitionId.HasValue)
            matchesQuery = matchesQuery.Where(m => m.CompetitionId == competitionId.Value);

        var matches = await matchesQuery
            .OrderByDescending(m => m.MatchDateUtc)
            .ToListAsync(cancellationToken);

        var h2h = existing ?? HeadToHead.Create(team1Id, team2Id, competitionId);

        // Aggregate stats
        int totalMatches = 0, team1Wins = 0, team2Wins = 0, draws = 0;
        int team1Goals = 0, team2Goals = 0;
        int team1HomeMatches = 0, team1HomeWins = 0;
        int team1HomeGoals = 0, team1HomeConceded = 0;
        int bothTeamsScoredCount = 0, over25Count = 0;
        int recentMatchesCount = 0, recentTeam1Wins = 0, recentTeam2Wins = 0, recentDraws = 0;

        foreach (var match in matches)
        {
            if (!match.HomeScore.HasValue || !match.AwayScore.HasValue)
                continue;

            totalMatches++;
            bool team1IsHome = match.HomeTeamId == team1Id;
            int t1Score = team1IsHome ? match.HomeScore.Value : match.AwayScore.Value;
            int t2Score = team1IsHome ? match.AwayScore.Value : match.HomeScore.Value;

            team1Goals += t1Score;
            team2Goals += t2Score;

            if (t1Score > t2Score) team1Wins++;
            else if (t2Score > t1Score) team2Wins++;
            else draws++;

            if (team1IsHome)
            {
                team1HomeMatches++;
                team1HomeGoals += t1Score;
                team1HomeConceded += t2Score;
                if (t1Score > t2Score) team1HomeWins++;
            }

            // Both teams scored
            if (t1Score > 0 && t2Score > 0) bothTeamsScoredCount++;

            // Over 2.5 total goals
            if (t1Score + t2Score > 2) over25Count++;
        }

        var recentMatches = matches
            .Where(m => m.HomeScore.HasValue && m.AwayScore.HasValue)
            .Take(3)
            .ToList();

        recentMatchesCount = recentMatches.Count;
        foreach (var recent in recentMatches)
        {
            bool team1IsHome = recent.HomeTeamId == team1Id;
            int t1Score = team1IsHome ? recent.HomeScore!.Value : recent.AwayScore!.Value;
            int t2Score = team1IsHome ? recent.AwayScore!.Value : recent.HomeScore!.Value;

            if (t1Score > t2Score) recentTeam1Wins++;
            else if (t2Score > t1Score) recentTeam2Wins++;
            else recentDraws++;
        }

        h2h.UpdateStats(
            totalMatches, team1Wins, team2Wins, draws,
            team1Goals, team2Goals,
            team1HomeMatches, team1HomeWins,
            team1HomeGoals, team1HomeConceded,
            bothTeamsScoredCount, over25Count,
            matches.FirstOrDefault()?.MatchDateUtc,
            matches.FirstOrDefault()?.Id,
            recentMatchesCount, recentTeam1Wins, recentTeam2Wins, recentDraws,
            totalMatches);

        if (existing == null)
            context.HeadToHeads.Add(h2h);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogDebug(
            "Calculated H2H: {Team1} vs {Team2} = {Matches} matches",
            team1Id, team2Id, totalMatches);

        return h2h;
    }
}
```

### 4.2 DI Registration

Add to `DependencyInjection.cs`:
```csharp
services.AddScoped<IHeadToHeadService, HeadToHeadService>();
```

---

## Part 5: API Endpoint (Optional)

### 5.1 HeadToHead Endpoint

**File:** `src/ExtraTime.API/Features/Football/HeadToHeadEndpoints.cs`

```csharp
public static class HeadToHeadEndpoints
{
    public static RouteGroupBuilder MapHeadToHeadEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/football/head-to-head")
            .WithTags("HeadToHead");

        group.MapGet("/", GetHeadToHead)
            .WithName("GetHeadToHead");

        return group;
    }

    private static async Task<IResult> GetHeadToHead(
        [FromQuery] Guid team1Id,
        [FromQuery] Guid team2Id,
        [FromQuery] Guid? competitionId,
        IHeadToHeadService headToHeadService,
        CancellationToken ct)
    {
        var h2h = await headToHeadService.GetOrCalculateAsync(
            team1Id, team2Id, competitionId, ct);

        return Results.Ok(new HeadToHeadDto(
            h2h.Team1Id, h2h.Team1?.Name ?? "",
            h2h.Team2Id, h2h.Team2?.Name ?? "",
            h2h.TotalMatches,
            h2h.Team1Wins, h2h.Team2Wins, h2h.Draws,
            h2h.Team1Goals, h2h.Team2Goals,
            h2h.BttsRate, h2h.Over25Rate,
            h2h.RecentTeam1Wins, h2h.RecentTeam2Wins, h2h.RecentDraws,
            h2h.LastMatchDate,
            h2h.CalculatedAt));
    }
}
```

---

## Implementation Tasks

- [x] **1.1** Create `HeadToHead` entity at `src/ExtraTime.Domain/Entities/HeadToHead.cs` with `HeadToHeadStats` record (includes BothTeamsScoredCount, Over25Count, BttsRate, Over25Rate, and recent 3-match counters)
- [x] **1.2** Create `HeadToHeadConfiguration` at `src/ExtraTime.Infrastructure/Data/Configurations/HeadToHeadConfiguration.cs`
- [x] **1.3** Add `DbSet<HeadToHead> HeadToHeads` to `IApplicationDbContext` and `ApplicationDbContext`
- [x] **1.4** Generate migration: `dotnet ef migrations add AddHeadToHead`
- [x] **2.1** Create `IHeadToHeadService` interface at `src/ExtraTime.Application/Common/Interfaces/IHeadToHeadService.cs`
- [ ] **2.2** Create `HeadToHeadDtos.cs` at `src/ExtraTime.Application/Features/Football/DTOs/HeadToHeadDtos.cs`
- [ ] **2.3** Create `HeadToHeadService` at `src/ExtraTime.Infrastructure/Services/Football/HeadToHeadService.cs`
- [ ] **2.4** Register `IHeadToHeadService` in `DependencyInjection.cs`
- [ ] **3.1** (Optional) Create `HeadToHeadEndpoints.cs` and register in API
- [ ] **4.1** Build and run tests

---

## Files Summary

| Action | File |
|--------|------|
| **Create** | `Domain/Entities/HeadToHead.cs` |
| **Create** | `Infrastructure/Data/Configurations/HeadToHeadConfiguration.cs` |
| **Create** | `Application/Common/Interfaces/IHeadToHeadService.cs` |
| **Create** | `Application/Features/Football/DTOs/HeadToHeadDtos.cs` |
| **Create** | `Infrastructure/Services/Football/HeadToHeadService.cs` |
| **Create** | `API/Features/Football/HeadToHeadEndpoints.cs` (optional) |
| **Modify** | `Application/Common/Interfaces/IApplicationDbContext.cs` |
| **Modify** | `Infrastructure/Data/ApplicationDbContext.cs` |
| **Modify** | `Infrastructure/DependencyInjection.cs` |
| **New migration** | `AddHeadToHead` |
