# Phase 9.5B: Understat Integration - xG Data

## Overview
Integrate expected goals (xG) data from Understat.com to provide advanced statistical insights for bot prediction strategies.

> **Data Source**: `https://understat.com/league/{League}/{Season}` returns embedded JSON in HTML
> **Supported Leagues**: EPL, La Liga, Bundesliga, Serie A, Ligue 1
> **Sync Strategy**: Daily at 4 AM UTC. 2-second delay between league requests.
> **Rate Limit**: None official, use 2s delay between requests

> **Prerequisite**: Phase 9.5A (Integration Health) must be complete
> **Phase 7.8 Contract**: support date-effective xG retrieval (`asOfUtc`) so ML training never uses future xG state.

---

## Part 1: Domain Layer

### 1.1 TeamXgStats Entity

**File**: `src/ExtraTime.Domain/Entities/TeamXgStats.cs`

```csharp
namespace ExtraTime.Domain.Entities;

/// <summary>
/// Cached xG (expected goals) statistics for a team in a competition/season.
/// Data sourced from Understat.
/// </summary>
public sealed class TeamXgStats : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public required Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public required string Season { get; set; }  // e.g., "2024" for 2024/25

    // Core xG metrics
    public double XgFor { get; set; }           // Total expected goals scored
    public double XgAgainst { get; set; }       // Total expected goals conceded
    public double XgDiff { get; set; }          // XgFor - XgAgainst

    // Per-match averages
    public double XgPerMatch { get; set; }
    public double XgAgainstPerMatch { get; set; }

    // Actual vs Expected (positive = overperforming)
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
    public double XgOverperformance { get; set; }  // Goals - xG
    public double XgaOverperformance { get; set; } // xGA - GoalsConceded

    // Recent form (last 5 matches)
    public double RecentXgPerMatch { get; set; }
    public double RecentXgAgainstPerMatch { get; set; }

    // Match count
    public int MatchesPlayed { get; set; }

    // Metadata
    public int UnderstatTeamId { get; set; }    // External ID for syncing
    public DateTime LastSyncedAt { get; set; }

    // Computed properties
    public double GetXgStrength() => XgPerMatch > 0 ? XgPerMatch / 1.5 : 0.5;
    public double GetDefensiveXgStrength() => XgAgainstPerMatch > 0 ? 1.5 / XgAgainstPerMatch : 0.5;
    public bool IsOverperforming() => XgOverperformance > 0;
    public bool IsDefensivelySound() => XgaOverperformance > 0;
}
```

### 1.2 MatchXgStats Entity

**File**: `src/ExtraTime.Domain/Entities/MatchXgStats.cs`

```csharp
namespace ExtraTime.Domain.Entities;

/// <summary>
/// xG statistics for a specific match.
/// Used for historical analysis and form calculation.
/// </summary>
public sealed class MatchXgStats : BaseEntity
{
    public required Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    // Home team xG
    public double HomeXg { get; set; }
    public int HomeShots { get; set; }
    public int HomeShotsOnTarget { get; set; }

    // Away team xG
    public double AwayXg { get; set; }
    public int AwayShots { get; set; }
    public int AwayShotsOnTarget { get; set; }

    // Match outcome vs xG
    public bool HomeXgWin { get; set; }         // HomeXg > AwayXg
    public bool ActualHomeWin { get; set; }     // HomeGoals > AwayGoals
    public bool XgMatchedResult { get; set; }   // xG prediction matched actual

    // Metadata
    public int UnderstatMatchId { get; set; }
    public DateTime SyncedAt { get; set; }
}
```

---

## Part 2: Infrastructure Layer

### 2.1 EF Configuration

**File**: `src/ExtraTime.Infrastructure/Data/Configurations/TeamXgStatsConfiguration.cs`

```csharp
public sealed class TeamXgStatsConfiguration : IEntityTypeConfiguration<TeamXgStats>
{
    public void Configure(EntityTypeBuilder<TeamXgStats> builder)
    {
        builder.ToTable("TeamXgStats");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Season).HasMaxLength(10);

        builder.HasOne(t => t.Team)
            .WithMany()
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Competition)
            .WithMany()
            .HasForeignKey(t => t.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.TeamId, t.CompetitionId, t.Season })
            .IsUnique();
    }
}
```

### 2.2 ApplicationDbContext

Add to `ApplicationDbContext.cs`:
```csharp
public DbSet<TeamXgStats> TeamXgStats => Set<TeamXgStats>();
public DbSet<MatchXgStats> MatchXgStats => Set<MatchXgStats>();
```

Add to `IApplicationDbContext.cs`:
```csharp
DbSet<TeamXgStats> TeamXgStats { get; }
DbSet<MatchXgStats> MatchXgStats { get; }
```

---

## Part 3: Service Layer

### 3.1 Interface

**File**: `src/ExtraTime.Application/Common/Interfaces/IUnderstatService.cs`

```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface IUnderstatService
{
    Task<List<TeamXgStats>> SyncLeagueXgStatsAsync(
        string leagueCode,
        string season,
        CancellationToken cancellationToken = default);

    Task SyncLeagueSeasonRangeAsync(
        string leagueCode,
        int fromSeason,
        int toSeason,
        CancellationToken cancellationToken = default);

    Task<TeamXgStats?> GetTeamXgAsync(
        Guid teamId,
        Guid competitionId,
        string season,
        CancellationToken cancellationToken = default);

    Task<TeamXgStats?> GetTeamXgAsOfAsync(
        Guid teamId,
        Guid competitionId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);

    Task<MatchXgStats?> GetMatchXgAsync(
        int understatMatchId,
        CancellationToken cancellationToken = default);

    Task SyncAllLeaguesAsync(CancellationToken cancellationToken = default);
}
```

### 3.2 Implementation

**File**: `src/ExtraTime.Infrastructure/Services/ExternalData/UnderstatService.cs`

```csharp
namespace ExtraTime.Infrastructure.Services.ExternalData;

/// <summary>
/// Scrapes xG data from Understat.com
/// Supported leagues: EPL, La Liga, Bundesliga, Serie A, Ligue 1, RFPL
/// </summary>
public sealed class UnderstatService(
    IHttpClientFactory httpClientFactory,
    IApplicationDbContext context,
    ILogger<UnderstatService> logger) : IUnderstatService
{
    private static readonly Dictionary<string, string> LeagueMapping = new()
    {
        { "PL", "EPL" },           // Premier League
        { "PD", "La_liga" },       // La Liga
        { "BL1", "Bundesliga" },   // Bundesliga
        { "SA", "Serie_A" },       // Serie A
        { "FL1", "Ligue_1" },      // Ligue 1
    };

    private const string BaseUrl = "https://understat.com";

    public async Task<List<TeamXgStats>> SyncLeagueXgStatsAsync(
        string leagueCode,
        string season,
        CancellationToken cancellationToken = default)
    {
        if (!LeagueMapping.TryGetValue(leagueCode, out var understatLeague))
        {
            logger.LogWarning("League {LeagueCode} not supported by Understat", leagueCode);
            return [];
        }

        var client = httpClientFactory.CreateClient("Understat");
        var url = $"{BaseUrl}/league/{understatLeague}/{season}";

        try
        {
            var html = await client.GetStringAsync(url, cancellationToken);
            var teamStats = ParseTeamStats(html, season);

            // Map to our teams and save
            var savedStats = await SaveTeamXgStatsAsync(teamStats, leagueCode, season, cancellationToken);

            logger.LogInformation(
                "Synced xG stats for {League} {Season}: {Count} teams",
                leagueCode, season, savedStats.Count);

            return savedStats;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync Understat data for {League}", leagueCode);
            return [];
        }
    }

    private List<UnderstatTeamData> ParseTeamStats(string html, string season)
    {
        // Understat embeds JSON data in script tags
        // Pattern: var teamsData = JSON.parse('...')
        var results = new List<UnderstatTeamData>();

        var jsonMatch = Regex.Match(html, @"var teamsData\s*=\s*JSON\.parse\('(.+?)'\)");
        if (!jsonMatch.Success)
        {
            logger.LogWarning("Could not find teamsData in Understat response");
            return results;
        }

        // Decode escaped JSON
        var jsonString = jsonMatch.Groups[1].Value
            .Replace(@"\x", @"\u00")
            .Replace(@"\'", "'");

        jsonString = Regex.Unescape(jsonString);

        var teamsData = JsonSerializer.Deserialize<Dictionary<string, UnderstatTeamJson>>(jsonString);
        if (teamsData == null) return results;

        foreach (var (teamId, data) in teamsData)
        {
            var stats = new UnderstatTeamData
            {
                UnderstatId = int.Parse(teamId),
                TeamName = data.Title,
                XgFor = data.History.Sum(h => h.Xg),
                XgAgainst = data.History.Sum(h => h.XgA),
                GoalsScored = data.History.Sum(h => h.Scored),
                GoalsConceded = data.History.Sum(h => h.Missed),
                MatchesPlayed = data.History.Count,
                RecentMatches = data.History.TakeLast(5).ToList()
            };

            results.Add(stats);
        }

        return results;
    }

    private async Task<List<TeamXgStats>> SaveTeamXgStatsAsync(
        List<UnderstatTeamData> understatData,
        string leagueCode,
        string season,
        CancellationToken cancellationToken)
    {
        var competition = await context.Competitions
            .FirstOrDefaultAsync(c => c.Code == leagueCode, cancellationToken);

        if (competition == null)
        {
            logger.LogWarning("Competition {Code} not found in database", leagueCode);
            return [];
        }

        var results = new List<TeamXgStats>();
        var now = DateTime.UtcNow;

        foreach (var data in understatData)
        {
            // Try to match team by name (fuzzy matching)
            var team = await FindTeamByNameAsync(data.TeamName, competition.Id, cancellationToken);
            if (team == null)
            {
                logger.LogDebug("Could not match Understat team: {TeamName}", data.TeamName);
                continue;
            }

            // Upsert stats
            var existing = await context.TeamXgStats
                .FirstOrDefaultAsync(x =>
                    x.TeamId == team.Id &&
                    x.CompetitionId == competition.Id &&
                    x.Season == season,
                    cancellationToken);

            if (existing != null)
            {
                UpdateXgStats(existing, data, now);
            }
            else
            {
                existing = CreateXgStats(team.Id, competition.Id, season, data, now);
                context.TeamXgStats.Add(existing);
            }

            results.Add(existing);
        }

        await context.SaveChangesAsync(cancellationToken);
        return results;
    }

    private TeamXgStats CreateXgStats(
        Guid teamId,
        Guid competitionId,
        string season,
        UnderstatTeamData data,
        DateTime now)
    {
        return new TeamXgStats
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            CompetitionId = competitionId,
            Season = season,
            XgFor = data.XgFor,
            XgAgainst = data.XgAgainst,
            XgDiff = data.XgFor - data.XgAgainst,
            XgPerMatch = data.MatchesPlayed > 0 ? data.XgFor / data.MatchesPlayed : 0,
            XgAgainstPerMatch = data.MatchesPlayed > 0 ? data.XgAgainst / data.MatchesPlayed : 0,
            GoalsScored = data.GoalsScored,
            GoalsConceded = data.GoalsConceded,
            XgOverperformance = data.GoalsScored - data.XgFor,
            XgaOverperformance = data.XgAgainst - data.GoalsConceded,
            RecentXgPerMatch = data.RecentMatches.Count > 0
                ? data.RecentMatches.Average(m => m.Xg) : 0,
            RecentXgAgainstPerMatch = data.RecentMatches.Count > 0
                ? data.RecentMatches.Average(m => m.XgA) : 0,
            MatchesPlayed = data.MatchesPlayed,
            UnderstatTeamId = data.UnderstatId,
            LastSyncedAt = now
        };
    }

    private void UpdateXgStats(TeamXgStats existing, UnderstatTeamData data, DateTime now)
    {
        existing.XgFor = data.XgFor;
        existing.XgAgainst = data.XgAgainst;
        existing.XgDiff = data.XgFor - data.XgAgainst;
        existing.XgPerMatch = data.MatchesPlayed > 0 ? data.XgFor / data.MatchesPlayed : 0;
        existing.XgAgainstPerMatch = data.MatchesPlayed > 0 ? data.XgAgainst / data.MatchesPlayed : 0;
        existing.GoalsScored = data.GoalsScored;
        existing.GoalsConceded = data.GoalsConceded;
        existing.XgOverperformance = data.GoalsScored - data.XgFor;
        existing.XgaOverperformance = data.XgAgainst - data.GoalsConceded;
        existing.RecentXgPerMatch = data.RecentMatches.Count > 0
            ? data.RecentMatches.Average(m => m.Xg) : 0;
        existing.RecentXgAgainstPerMatch = data.RecentMatches.Count > 0
            ? data.RecentMatches.Average(m => m.XgA) : 0;
        existing.MatchesPlayed = data.MatchesPlayed;
        existing.LastSyncedAt = now;
    }

    private async Task<Team?> FindTeamByNameAsync(
        string understatName,
        Guid competitionId,
        CancellationToken cancellationToken)
    {
        // Direct match first
        var team = await context.Teams
            .FirstOrDefaultAsync(t =>
                t.Name == understatName ||
                t.ShortName == understatName,
                cancellationToken);

        if (team != null) return team;

        // Fuzzy matching for common variations
        var normalizedName = NormalizeTeamName(understatName);
        var teams = await context.CompetitionTeams
            .Where(ct => ct.CompetitionId == competitionId)
            .Include(ct => ct.Team)
            .Select(ct => ct.Team)
            .ToListAsync(cancellationToken);

        return teams.FirstOrDefault(t =>
            NormalizeTeamName(t.Name) == normalizedName ||
            NormalizeTeamName(t.ShortName) == normalizedName);
    }

    private static string NormalizeTeamName(string name)
    {
        return name.ToLowerInvariant()
            .Replace("fc", "")
            .Replace("cf", "")
            .Replace("afc", "")
            .Replace(" ", "")
            .Trim();
    }

    public async Task SyncAllLeaguesAsync(CancellationToken cancellationToken = default)
    {
        var currentSeason = DateTime.UtcNow.Year.ToString();
        if (DateTime.UtcNow.Month < 8) // Before August, use previous season
        {
            currentSeason = (DateTime.UtcNow.Year - 1).ToString();
        }

        foreach (var leagueCode in LeagueMapping.Keys)
        {
            await SyncLeagueXgStatsAsync(leagueCode, currentSeason, cancellationToken);
            await Task.Delay(2000, cancellationToken); // Rate limiting
        }
    }
}

// DTOs for JSON parsing
internal sealed class UnderstatTeamJson
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("history")]
    public List<UnderstatMatchHistory> History { get; set; } = [];
}

internal sealed class UnderstatMatchHistory
{
    [JsonPropertyName("xG")]
    public double Xg { get; set; }

    [JsonPropertyName("xGA")]
    public double XgA { get; set; }

    [JsonPropertyName("scored")]
    public int Scored { get; set; }

    [JsonPropertyName("missed")]
    public int Missed { get; set; }
}

internal sealed class UnderstatTeamData
{
    public int UnderstatId { get; set; }
    public string TeamName { get; set; } = "";
    public double XgFor { get; set; }
    public double XgAgainst { get; set; }
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
    public int MatchesPlayed { get; set; }
    public List<UnderstatMatchHistory> RecentMatches { get; set; } = [];
}
```

### 3.3 Background Sync Service

**File**: `src/ExtraTime.Infrastructure/Services/ExternalData/UnderstatSyncBackgroundService.cs`

```csharp
public sealed class UnderstatSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<UnderstatSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Understat Sync Service started");

        // Initial sync on startup
        await SyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait until 4 AM UTC
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(4);
            var delay = nextRun - now;

            logger.LogDebug("Next Understat sync at {NextRun}", nextRun);
            await Task.Delay(delay, stoppingToken);

            await SyncAsync(stoppingToken);
        }
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IUnderstatService>();

            await service.SyncAllLeaguesAsync(cancellationToken);
            logger.LogInformation("Understat sync completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Understat sync failed");
        }
    }
}
```

---

## Part 4: DI Registration

Add to `DependencyInjection.cs`:
```csharp
services.AddScoped<IUnderstatService, UnderstatService>();
services.AddHostedService<UnderstatSyncBackgroundService>();

services.AddHttpClient("Understat", client =>
{
    client.BaseAddress = new Uri("https://understat.com");
    client.DefaultRequestHeaders.Add("User-Agent", "ExtraTime/1.0");
});
```

---

## Part 5: Configuration

### 5.1 appsettings.json

```json
{
  "ExternalData": {
    "Understat": {
      "Enabled": true,
      "SyncSchedule": "0 4 * * *"
    }
  }
}
```

---

## Implementation Checklist

- [x] Create `TeamXgStats` entity
- [x] Create `MatchXgStats` entity
- [x] Create `TeamXgStatsConfiguration`
- [ ] Create `IUnderstatService` interface
- [ ] Implement `UnderstatService` (HTML scraping + JSON parsing)
- [ ] Add date-effective lookup `GetTeamXgAsOfAsync` for leakage-safe ML training
- [ ] Add seasonal range backfill entrypoint `SyncLeagueSeasonRangeAsync` (used by Phase 9.6)
- [ ] Create `UnderstatSyncBackgroundService`
- [x] Add `TeamXgStats` and `MatchXgStats` DbSets to context
- [x] Add database migration
- [ ] Register services in DI
- [ ] Configure HTTP client
- [ ] Test xG data sync

---

## Files Summary

| Action | File |
|--------|------|
| **Create** | `Domain/Entities/TeamXgStats.cs` |
| **Create** | `Domain/Entities/MatchXgStats.cs` |
| **Create** | `Infrastructure/Data/Configurations/TeamXgStatsConfiguration.cs` |
| **Create** | `Application/Common/Interfaces/IUnderstatService.cs` |
| **Create** | `Infrastructure/Services/ExternalData/UnderstatService.cs` |
| **Create** | `Infrastructure/Services/ExternalData/UnderstatSyncBackgroundService.cs` |
| **Modify** | `Application/Common/Interfaces/IApplicationDbContext.cs` |
| **Modify** | `Infrastructure/Data/ApplicationDbContext.cs` |
| **Modify** | `Infrastructure/DependencyInjection.cs` |
| **New migration** | `AddUnderstatXgData` |
