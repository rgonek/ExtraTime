# Phase 9.5: External Data Sources Integration - Detailed Implementation Plan

## Overview
Integrate external free data sources to enhance bot prediction accuracy with advanced statistics (xG), market consensus (betting odds), and injury data.

> **Prerequisite**: Phase 9 (Extended Football Data) should be complete
> **Data Sources**:
> - Understat (xG statistics) - Primary
> - Football-Data.co.uk (Historical betting odds) - Primary
> - API-Football (Injuries) - Optional/Limited

---

## Part 1: Integration Health & Monitoring

### 1.0 Integration Health Tracking

Track the status of each external data source so bots can gracefully degrade when data is unavailable.

**IntegrationStatus.cs**:
```csharp
namespace ExtraTime.Domain.Entities;

/// <summary>
/// Tracks the health status of external data integrations.
/// Used by bots to know which data sources are reliable.
/// </summary>
public sealed class IntegrationStatus : BaseEntity
{
    public required string IntegrationName { get; set; }  // e.g., "Understat", "FootballDataUk"

    // Current status
    public IntegrationHealth Health { get; set; } = IntegrationHealth.Unknown;
    public bool IsOperational => Health == IntegrationHealth.Healthy || Health == IntegrationHealth.Degraded;

    // Last sync info
    public DateTime? LastSuccessfulSync { get; set; }
    public DateTime? LastAttemptedSync { get; set; }
    public DateTime? LastFailedSync { get; set; }

    // Failure tracking
    public int ConsecutiveFailures { get; set; }
    public int TotalFailures24h { get; set; }
    public string? LastErrorMessage { get; set; }
    public string? LastErrorDetails { get; set; }  // Stack trace or details

    // Data freshness
    public DateTime? DataFreshAsOf { get; set; }     // When the data was last updated
    public bool IsDataStale => DataFreshAsOf.HasValue &&
        (DateTime.UtcNow - DataFreshAsOf.Value) > StaleThreshold;
    public TimeSpan StaleThreshold { get; set; } = TimeSpan.FromHours(48);

    // Statistics
    public int SuccessfulSyncs24h { get; set; }
    public double SuccessRate24h => (SuccessfulSyncs24h + TotalFailures24h) > 0
        ? (double)SuccessfulSyncs24h / (SuccessfulSyncs24h + TotalFailures24h) * 100
        : 0;
    public TimeSpan? AverageSyncDuration { get; set; }

    // Manual override
    public bool IsManuallyDisabled { get; set; }
    public string? DisabledReason { get; set; }
    public DateTime? DisabledAt { get; set; }
    public string? DisabledBy { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Methods
    public void RecordSuccess(TimeSpan duration)
    {
        LastSuccessfulSync = DateTime.UtcNow;
        LastAttemptedSync = DateTime.UtcNow;
        DataFreshAsOf = DateTime.UtcNow;
        ConsecutiveFailures = 0;
        SuccessfulSyncs24h++;
        Health = IntegrationHealth.Healthy;
        LastErrorMessage = null;
        LastErrorDetails = null;
        AverageSyncDuration = AverageSyncDuration.HasValue
            ? TimeSpan.FromTicks((AverageSyncDuration.Value.Ticks + duration.Ticks) / 2)
            : duration;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailure(string errorMessage, string? details = null)
    {
        LastAttemptedSync = DateTime.UtcNow;
        LastFailedSync = DateTime.UtcNow;
        ConsecutiveFailures++;
        TotalFailures24h++;
        LastErrorMessage = errorMessage;
        LastErrorDetails = details;
        UpdatedAt = DateTime.UtcNow;

        // Update health based on failure count
        Health = ConsecutiveFailures switch
        {
            1 => IntegrationHealth.Degraded,
            >= 2 and < 5 => IntegrationHealth.Degraded,
            >= 5 => IntegrationHealth.Failed,
            _ => IntegrationHealth.Unknown
        };
    }

    public void ResetDailyCounters()
    {
        SuccessfulSyncs24h = 0;
        TotalFailures24h = 0;
    }
}

public enum IntegrationHealth
{
    Unknown = 0,
    Healthy = 1,      // Working normally
    Degraded = 2,     // Some issues but data available
    Failed = 3,       // Not working, data may be stale
    Disabled = 4      // Manually disabled
}
```

**Supported Integrations Enum**:
```csharp
namespace ExtraTime.Domain.Enums;

public enum IntegrationType
{
    FootballDataOrg = 0,    // Primary match data
    Understat = 1,          // xG statistics
    FootballDataUk = 2,     // Betting odds
    ApiFootball = 3         // Injuries
}

public static class IntegrationTypeExtensions
{
    public static string ToName(this IntegrationType type) => type switch
    {
        IntegrationType.FootballDataOrg => "Football-Data.org",
        IntegrationType.Understat => "Understat",
        IntegrationType.FootballDataUk => "Football-Data.co.uk",
        IntegrationType.ApiFootball => "API-Football",
        _ => type.ToString()
    };

    public static TimeSpan GetStaleThreshold(this IntegrationType type) => type switch
    {
        IntegrationType.FootballDataOrg => TimeSpan.FromHours(6),
        IntegrationType.Understat => TimeSpan.FromHours(48),
        IntegrationType.FootballDataUk => TimeSpan.FromDays(7),
        IntegrationType.ApiFootball => TimeSpan.FromHours(48),
        _ => TimeSpan.FromHours(24)
    };
}
```

### 1.1 Integration Health Service

**IIntegrationHealthService.cs**:
```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface IIntegrationHealthService
{
    Task<IntegrationStatus> GetStatusAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default);

    Task<List<IntegrationStatus>> GetAllStatusesAsync(
        CancellationToken cancellationToken = default);

    Task RecordSuccessAsync(
        IntegrationType type,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    Task RecordFailureAsync(
        IntegrationType type,
        string errorMessage,
        string? details = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsOperationalAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default);

    Task<bool> HasFreshDataAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default);

    Task DisableIntegrationAsync(
        IntegrationType type,
        string reason,
        string disabledBy,
        CancellationToken cancellationToken = default);

    Task EnableIntegrationAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default);

    Task<DataAvailability> GetDataAvailabilityAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Summary of which data sources are available for bot predictions.
/// </summary>
public sealed record DataAvailability
{
    public bool FormDataAvailable { get; init; } = true;  // Always available (calculated)
    public bool XgDataAvailable { get; init; }
    public bool OddsDataAvailable { get; init; }
    public bool InjuryDataAvailable { get; init; }
    public bool LineupDataAvailable { get; init; }
    public bool StandingsDataAvailable { get; init; }

    public bool HasAnyExternalData => XgDataAvailable || OddsDataAvailable ||
                                       InjuryDataAvailable || LineupDataAvailable;

    public int AvailableSourceCount => new[]
    {
        FormDataAvailable, XgDataAvailable, OddsDataAvailable,
        InjuryDataAvailable, LineupDataAvailable, StandingsDataAvailable
    }.Count(x => x);
}
```

**IntegrationHealthService.cs**:
```csharp
namespace ExtraTime.Infrastructure.Services;

public sealed class IntegrationHealthService(
    IApplicationDbContext context,
    ILogger<IntegrationHealthService> logger) : IIntegrationHealthService
{
    public async Task<IntegrationStatus> GetStatusAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default)
    {
        var name = type.ToString();
        var status = await context.IntegrationStatuses
            .FirstOrDefaultAsync(s => s.IntegrationName == name, cancellationToken);

        if (status == null)
        {
            status = new IntegrationStatus
            {
                Id = Guid.NewGuid(),
                IntegrationName = name,
                Health = IntegrationHealth.Unknown,
                StaleThreshold = type.GetStaleThreshold(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.IntegrationStatuses.Add(status);
            await context.SaveChangesAsync(cancellationToken);
        }

        return status;
    }

    public async Task<List<IntegrationStatus>> GetAllStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        // Ensure all integrations exist
        foreach (IntegrationType type in Enum.GetValues<IntegrationType>())
        {
            await GetStatusAsync(type, cancellationToken);
        }

        return await context.IntegrationStatuses
            .OrderBy(s => s.IntegrationName)
            .ToListAsync(cancellationToken);
    }

    public async Task RecordSuccessAsync(
        IntegrationType type,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        status.RecordSuccess(duration);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Integration {Type} sync successful ({Duration:g})",
            type, duration);
    }

    public async Task RecordFailureAsync(
        IntegrationType type,
        string errorMessage,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        status.RecordFailure(errorMessage, details);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Integration {Type} sync failed ({Failures} consecutive): {Error}",
            type, status.ConsecutiveFailures, errorMessage);
    }

    public async Task<bool> IsOperationalAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        return status.IsOperational && !status.IsManuallyDisabled;
    }

    public async Task<bool> HasFreshDataAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        return status.IsOperational && !status.IsDataStale && !status.IsManuallyDisabled;
    }

    public async Task<DataAvailability> GetDataAvailabilityAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = await GetAllStatusesAsync(cancellationToken);

        bool IsAvailable(IntegrationType type) =>
            statuses.FirstOrDefault(s => s.IntegrationName == type.ToString())
                ?.IsOperational ?? false;

        bool HasFresh(IntegrationType type) =>
            statuses.FirstOrDefault(s => s.IntegrationName == type.ToString()) is { } s
            && s.IsOperational && !s.IsDataStale;

        return new DataAvailability
        {
            XgDataAvailable = HasFresh(IntegrationType.Understat),
            OddsDataAvailable = HasFresh(IntegrationType.FootballDataUk),
            InjuryDataAvailable = IsAvailable(IntegrationType.ApiFootball),
            LineupDataAvailable = IsAvailable(IntegrationType.FootballDataOrg),
            StandingsDataAvailable = IsAvailable(IntegrationType.FootballDataOrg)
        };
    }

    public async Task DisableIntegrationAsync(
        IntegrationType type,
        string reason,
        string disabledBy,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        status.IsManuallyDisabled = true;
        status.DisabledReason = reason;
        status.DisabledBy = disabledBy;
        status.DisabledAt = DateTime.UtcNow;
        status.Health = IntegrationHealth.Disabled;
        status.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Integration {Type} manually disabled by {User}: {Reason}",
            type, disabledBy, reason);
    }

    public async Task EnableIntegrationAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(type, cancellationToken);
        status.IsManuallyDisabled = false;
        status.DisabledReason = null;
        status.DisabledBy = null;
        status.DisabledAt = null;
        status.Health = IntegrationHealth.Unknown; // Will update on next sync
        status.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Integration {Type} re-enabled", type);
    }
}
```

### 1.2 Update External Services to Report Health

Each sync service should wrap its sync logic with health tracking:

**Example - UnderstatService updates**:
```csharp
public async Task SyncAllLeaguesAsync(CancellationToken cancellationToken = default)
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        var currentSeason = GetCurrentSeason();

        foreach (var leagueCode in LeagueMapping.Keys)
        {
            await SyncLeagueXgStatsAsync(leagueCode, currentSeason, cancellationToken);
            await Task.Delay(2000, cancellationToken);
        }

        stopwatch.Stop();
        await _healthService.RecordSuccessAsync(
            IntegrationType.Understat,
            stopwatch.Elapsed,
            cancellationToken);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        await _healthService.RecordFailureAsync(
            IntegrationType.Understat,
            ex.Message,
            ex.StackTrace,
            cancellationToken);
        throw;
    }
}
```

---

## Part 2: Architecture Overview

### 1.1 Data Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                        External Data Sources                         │
├──────────────────┬──────────────────────┬──────────────────────────┤
│    Understat     │  Football-Data.co.uk │     API-Football         │
│   (xG Stats)     │   (Betting Odds)     │     (Injuries)           │
│   [Scraping]     │   [CSV Download]     │     [REST API]           │
└────────┬─────────┴──────────┬───────────┴────────────┬─────────────┘
         │                    │                        │
         ▼                    ▼                        ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    ExtraTime Backend Services                        │
├──────────────────┬──────────────────────┬──────────────────────────┤
│ UnderstatService │ OddsDataService      │ InjuryService            │
│ - Scrape xG      │ - Parse CSVs         │ - Fetch injuries         │
│ - Cache stats    │ - Store odds         │ - Track key players      │
└────────┬─────────┴──────────┬───────────┴────────────┬─────────────┘
         │                    │                        │
         ▼                    ▼                        ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         Database Cache                               │
│  TeamXgStats  │  MatchOdds  │  TeamInjuries  │  PlayerInjury        │
└─────────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    StatsAnalyst Bot Strategy                         │
│  Enhanced prediction using xG trends, odds, and injury impact       │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.2 Rate Limits & Scheduling

| Source | Limit | Sync Strategy |
|--------|-------|---------------|
| Understat | None (scraping) | Daily at 4 AM UTC |
| Football-Data.co.uk | None (static files) | Weekly on Monday |
| API-Football | 100/day free | On-demand for upcoming matches only |

---

## Part 2: Understat Integration (xG Data)

### 2.1 New Entities

**TeamXgStats.cs** - Team expected goals statistics:
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

**MatchXgStats.cs** - Individual match xG data:
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

### 2.2 Understat Service

**IUnderstatService.cs**:
```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface IUnderstatService
{
    Task<List<TeamXgStats>> SyncLeagueXgStatsAsync(
        string leagueCode,
        string season,
        CancellationToken cancellationToken = default);

    Task<MatchXgStats?> GetMatchXgAsync(
        int understatMatchId,
        CancellationToken cancellationToken = default);

    Task SyncAllLeaguesAsync(CancellationToken cancellationToken = default);
}
```

**UnderstatService.cs**:
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

### 2.3 Understat Background Service

**UnderstatSyncBackgroundService.cs**:
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

## Part 3: Football-Data.co.uk Integration (Betting Odds)

### 3.1 New Entities

**MatchOdds.cs** - Historical betting odds for matches:
```csharp
namespace ExtraTime.Domain.Entities;

/// <summary>
/// Historical betting odds for a match.
/// Data sourced from Football-Data.co.uk CSV files.
/// </summary>
public sealed class MatchOdds : BaseEntity
{
    public required Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    // Average odds across bookmakers
    public double HomeWinOdds { get; set; }      // e.g., 1.85
    public double DrawOdds { get; set; }         // e.g., 3.40
    public double AwayWinOdds { get; set; }      // e.g., 4.50

    // Implied probabilities (calculated from odds)
    public double HomeWinProbability { get; set; }   // e.g., 0.50
    public double DrawProbability { get; set; }      // e.g., 0.27
    public double AwayWinProbability { get; set; }   // e.g., 0.20

    // Over/Under 2.5 goals
    public double? Over25Odds { get; set; }
    public double? Under25Odds { get; set; }

    // Both Teams To Score
    public double? BttsYesOdds { get; set; }
    public double? BttsNoOdds { get; set; }

    // Market consensus (who the market favors)
    public MatchOutcome MarketFavorite { get; set; }
    public double FavoriteConfidence { get; set; }  // 0-1 scale

    // Metadata
    public string DataSource { get; set; } = "football-data.co.uk";
    public DateTime ImportedAt { get; set; }

    // Computed methods
    public static double OddsToProbability(double odds)
    {
        return odds > 0 ? 1.0 / odds : 0;
    }

    public void CalculateProbabilities()
    {
        var total = OddsToProbability(HomeWinOdds) +
                    OddsToProbability(DrawOdds) +
                    OddsToProbability(AwayWinOdds);

        // Normalize to remove bookmaker margin
        HomeWinProbability = OddsToProbability(HomeWinOdds) / total;
        DrawProbability = OddsToProbability(DrawOdds) / total;
        AwayWinProbability = OddsToProbability(AwayWinOdds) / total;

        // Determine market favorite
        if (HomeWinProbability > DrawProbability && HomeWinProbability > AwayWinProbability)
        {
            MarketFavorite = MatchOutcome.HomeWin;
            FavoriteConfidence = HomeWinProbability;
        }
        else if (AwayWinProbability > DrawProbability)
        {
            MarketFavorite = MatchOutcome.AwayWin;
            FavoriteConfidence = AwayWinProbability;
        }
        else
        {
            MarketFavorite = MatchOutcome.Draw;
            FavoriteConfidence = DrawProbability;
        }
    }
}

public enum MatchOutcome
{
    HomeWin = 0,
    Draw = 1,
    AwayWin = 2
}
```

### 3.2 Odds Data Service

**IOddsDataService.cs**:
```csharp
public interface IOddsDataService
{
    Task ImportSeasonOddsAsync(
        string leagueCode,
        string season,
        CancellationToken cancellationToken = default);

    Task ImportAllLeaguesAsync(CancellationToken cancellationToken = default);

    Task<MatchOdds?> GetOddsForMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);
}
```

**OddsDataService.cs**:
```csharp
namespace ExtraTime.Infrastructure.Services.ExternalData;

/// <summary>
/// Imports historical betting odds from Football-Data.co.uk CSV files.
/// Free data available at: https://www.football-data.co.uk/data.php
/// </summary>
public sealed class OddsDataService(
    IHttpClientFactory httpClientFactory,
    IApplicationDbContext context,
    ILogger<OddsDataService> logger) : IOddsDataService
{
    private const string BaseUrl = "https://www.football-data.co.uk";

    // League code mapping to Football-Data.co.uk file paths
    private static readonly Dictionary<string, string> LeagueFiles = new()
    {
        { "PL", "E0" },     // Premier League
        { "ELC", "E1" },    // Championship
        { "PD", "SP1" },    // La Liga
        { "BL1", "D1" },    // Bundesliga
        { "SA", "I1" },     // Serie A
        { "FL1", "F1" },    // Ligue 1
        { "DED", "N1" },    // Eredivisie
        { "PPL", "P1" },    // Primeira Liga
    };

    public async Task ImportSeasonOddsAsync(
        string leagueCode,
        string season,
        CancellationToken cancellationToken = default)
    {
        if (!LeagueFiles.TryGetValue(leagueCode, out var fileCode))
        {
            logger.LogWarning("League {Code} not available from Football-Data.co.uk", leagueCode);
            return;
        }

        // Season format: "2425" for 2024/25
        var seasonCode = season.Length == 4
            ? season.Substring(2, 2) + (int.Parse(season.Substring(2, 2)) + 1).ToString("D2")
            : season;

        var url = $"{BaseUrl}/mmz4281/{seasonCode}/{fileCode}.csv";

        var client = httpClientFactory.CreateClient("FootballDataUk");

        try
        {
            var csvContent = await client.GetStringAsync(url, cancellationToken);
            var matchOdds = ParseCsv(csvContent, leagueCode);

            await SaveOddsAsync(matchOdds, cancellationToken);

            logger.LogInformation(
                "Imported {Count} match odds for {League} {Season}",
                matchOdds.Count, leagueCode, season);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("Odds file not found for {League} {Season}", leagueCode, season);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import odds for {League} {Season}", leagueCode, season);
        }
    }

    private List<OddsCsvRow> ParseCsv(string csvContent, string leagueCode)
    {
        var results = new List<OddsCsvRow>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2) return results;

        // Parse header
        var headers = lines[0].Split(',');
        var columnMap = headers
            .Select((h, i) => (Header: h.Trim(), Index: i))
            .ToDictionary(x => x.Header, x => x.Index);

        // Required columns
        if (!columnMap.ContainsKey("Date") ||
            !columnMap.ContainsKey("HomeTeam") ||
            !columnMap.ContainsKey("AwayTeam"))
        {
            logger.LogWarning("CSV missing required columns");
            return results;
        }

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length < headers.Length) continue;

            try
            {
                var row = new OddsCsvRow
                {
                    Date = ParseDate(GetValue(values, columnMap, "Date")),
                    HomeTeam = GetValue(values, columnMap, "HomeTeam"),
                    AwayTeam = GetValue(values, columnMap, "AwayTeam"),
                    HomeGoals = ParseIntOrNull(GetValue(values, columnMap, "FTHG")),
                    AwayGoals = ParseIntOrNull(GetValue(values, columnMap, "FTAG")),
                    // Average odds (Bet365 as primary, fallback to others)
                    HomeOdds = ParseDoubleOrNull(GetValue(values, columnMap, "B365H"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "BWH"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "AvgH")),
                    DrawOdds = ParseDoubleOrNull(GetValue(values, columnMap, "B365D"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "BWD"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "AvgD")),
                    AwayOdds = ParseDoubleOrNull(GetValue(values, columnMap, "B365A"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "BWA"))
                              ?? ParseDoubleOrNull(GetValue(values, columnMap, "AvgA")),
                    // Over/Under 2.5
                    Over25 = ParseDoubleOrNull(GetValue(values, columnMap, "B365>2.5"))
                            ?? ParseDoubleOrNull(GetValue(values, columnMap, "Avg>2.5")),
                    Under25 = ParseDoubleOrNull(GetValue(values, columnMap, "B365<2.5"))
                             ?? ParseDoubleOrNull(GetValue(values, columnMap, "Avg<2.5")),
                };

                if (row.Date.HasValue && row.HomeOdds.HasValue)
                {
                    results.Add(row);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to parse CSV row {Index}", i);
            }
        }

        return results;
    }

    private async Task SaveOddsAsync(
        List<OddsCsvRow> rows,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        foreach (var row in rows)
        {
            if (!row.Date.HasValue) continue;

            // Find matching match in our database
            var match = await FindMatchAsync(
                row.HomeTeam,
                row.AwayTeam,
                row.Date.Value,
                cancellationToken);

            if (match == null)
            {
                logger.LogDebug(
                    "No match found for {Home} vs {Away} on {Date}",
                    row.HomeTeam, row.AwayTeam, row.Date);
                continue;
            }

            // Check if odds already exist
            var existing = await context.MatchOdds
                .FirstOrDefaultAsync(o => o.MatchId == match.Id, cancellationToken);

            if (existing != null) continue; // Don't overwrite

            var odds = new MatchOdds
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                HomeWinOdds = row.HomeOdds ?? 0,
                DrawOdds = row.DrawOdds ?? 0,
                AwayWinOdds = row.AwayOdds ?? 0,
                Over25Odds = row.Over25,
                Under25Odds = row.Under25,
                ImportedAt = now
            };

            odds.CalculateProbabilities();
            context.MatchOdds.Add(odds);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Match?> FindMatchAsync(
        string homeTeam,
        string awayTeam,
        DateTime date,
        CancellationToken cancellationToken)
    {
        // Search within ±1 day to handle timezone differences
        var startDate = date.Date;
        var endDate = date.Date.AddDays(1);

        var matches = await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.MatchDateUtc >= startDate && m.MatchDateUtc < endDate)
            .ToListAsync(cancellationToken);

        var normalizedHome = NormalizeTeamName(homeTeam);
        var normalizedAway = NormalizeTeamName(awayTeam);

        return matches.FirstOrDefault(m =>
            (NormalizeTeamName(m.HomeTeam.Name) == normalizedHome ||
             NormalizeTeamName(m.HomeTeam.ShortName) == normalizedHome) &&
            (NormalizeTeamName(m.AwayTeam.Name) == normalizedAway ||
             NormalizeTeamName(m.AwayTeam.ShortName) == normalizedAway));
    }

    private static string NormalizeTeamName(string name)
    {
        return name.ToLowerInvariant()
            .Replace("fc", "")
            .Replace("cf", "")
            .Replace(" ", "")
            .Trim();
    }

    // CSV parsing helpers
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();

        foreach (var c in line)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else current.Append(c);
        }
        result.Add(current.ToString().Trim());

        return result.ToArray();
    }

    private static string GetValue(string[] values, Dictionary<string, int> map, string column)
    {
        return map.TryGetValue(column, out var index) && index < values.Length
            ? values[index]
            : "";
    }

    private static DateTime? ParseDate(string value)
    {
        if (DateTime.TryParseExact(value, "dd/MM/yyyy", null, DateTimeStyles.None, out var date))
            return date;
        if (DateTime.TryParseExact(value, "dd/MM/yy", null, DateTimeStyles.None, out date))
            return date;
        return null;
    }

    private static int? ParseIntOrNull(string value)
        => int.TryParse(value, out var result) ? result : null;

    private static double? ParseDoubleOrNull(string value)
        => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;

    public async Task ImportAllLeaguesAsync(CancellationToken cancellationToken = default)
    {
        var currentSeason = GetCurrentSeason();

        foreach (var leagueCode in LeagueFiles.Keys)
        {
            await ImportSeasonOddsAsync(leagueCode, currentSeason, cancellationToken);
            await Task.Delay(1000, cancellationToken); // Rate limiting
        }
    }

    private static string GetCurrentSeason()
    {
        var now = DateTime.UtcNow;
        var year = now.Month >= 8 ? now.Year : now.Year - 1;
        return year.ToString();
    }

    public async Task<MatchOdds?> GetOddsForMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        return await context.MatchOdds
            .FirstOrDefaultAsync(o => o.MatchId == matchId, cancellationToken);
    }
}

internal sealed class OddsCsvRow
{
    public DateTime? Date { get; set; }
    public string HomeTeam { get; set; } = "";
    public string AwayTeam { get; set; } = "";
    public int? HomeGoals { get; set; }
    public int? AwayGoals { get; set; }
    public double? HomeOdds { get; set; }
    public double? DrawOdds { get; set; }
    public double? AwayOdds { get; set; }
    public double? Over25 { get; set; }
    public double? Under25 { get; set; }
}
```

### 3.3 Odds Sync Background Service

**OddsSyncBackgroundService.cs**:
```csharp
public sealed class OddsSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OddsSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Odds Sync Service started");

        // Initial sync on startup
        await SyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait until Monday 5 AM UTC
            var now = DateTime.UtcNow;
            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;

            var nextRun = now.Date.AddDays(daysUntilMonday).AddHours(5);
            var delay = nextRun - now;

            logger.LogDebug("Next odds sync at {NextRun}", nextRun);
            await Task.Delay(delay, stoppingToken);

            await SyncAsync(stoppingToken);
        }
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IOddsDataService>();

            await service.ImportAllLeaguesAsync(cancellationToken);
            logger.LogInformation("Odds sync completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Odds sync failed");
        }
    }
}
```

---

## Part 4: API-Football Integration (Injuries) - Optional

### 4.1 New Entities

**TeamInjuries.cs** - Aggregated injury status for a team:
```csharp
namespace ExtraTime.Domain.Entities;

/// <summary>
/// Current injury status for a team.
/// Data sourced from API-Football (limited free tier).
/// </summary>
public sealed class TeamInjuries : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    // Injury counts by severity
    public int TotalInjured { get; set; }
    public int KeyPlayersInjured { get; set; }
    public int LongTermInjuries { get; set; }      // > 30 days
    public int ShortTermInjuries { get; set; }      // < 7 days
    public int Doubtful { get; set; }               // Game-time decisions

    // Specific key player injuries
    public string InjuredPlayerNames { get; set; } = ""; // JSON array
    public bool TopScorerInjured { get; set; }
    public bool CaptainInjured { get; set; }
    public bool FirstChoiceGkInjured { get; set; }

    // Impact score (0-100, higher = more impacted)
    public double InjuryImpactScore { get; set; }

    // Metadata
    public DateTime LastSyncedAt { get; set; }
    public DateTime? NextSyncDue { get; set; }
}
```

**PlayerInjury.cs** - Individual player injuries:
```csharp
namespace ExtraTime.Domain.Entities;

public sealed class PlayerInjury : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    // Player info
    public int ExternalPlayerId { get; set; }
    public string PlayerName { get; set; } = "";
    public string Position { get; set; } = "";      // GK, DEF, MID, FWD
    public bool IsKeyPlayer { get; set; }

    // Injury info
    public string InjuryType { get; set; } = "";    // e.g., "Hamstring", "ACL"
    public string InjurySeverity { get; set; } = ""; // Minor, Moderate, Severe
    public DateTime? InjuryDate { get; set; }
    public DateTime? ExpectedReturn { get; set; }
    public bool IsDoubtful { get; set; }            // Game-time decision

    // Status
    public bool IsActive { get; set; } = true;      // Still injured
    public DateTime LastUpdatedAt { get; set; }
}
```

### 4.2 Injury Service

**IInjuryService.cs**:
```csharp
public interface IInjuryService
{
    /// <summary>
    /// Fetch injuries for teams playing in upcoming matches.
    /// Limited to 100 requests/day on free tier.
    /// </summary>
    Task SyncInjuriesForUpcomingMatchesAsync(
        int daysAhead = 3,
        CancellationToken cancellationToken = default);

    Task<TeamInjuries?> GetTeamInjuriesAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);

    double CalculateInjuryImpact(TeamInjuries injuries);
}
```

**InjuryService.cs**:
```csharp
namespace ExtraTime.Infrastructure.Services.ExternalData;

/// <summary>
/// Fetches injury data from API-Football.
/// Free tier: 100 requests/day - use sparingly!
/// </summary>
public sealed class InjuryService(
    IHttpClientFactory httpClientFactory,
    IApplicationDbContext context,
    IConfiguration configuration,
    ILogger<InjuryService> logger) : IInjuryService
{
    private const string BaseUrl = "https://api-football-v1.p.rapidapi.com/v3";
    private const int MaxDailyRequests = 100;
    private static int _dailyRequestCount = 0;
    private static DateTime _lastResetDate = DateTime.UtcNow.Date;

    public async Task SyncInjuriesForUpcomingMatchesAsync(
        int daysAhead = 3,
        CancellationToken cancellationToken = default)
    {
        ResetDailyCounterIfNeeded();

        if (_dailyRequestCount >= MaxDailyRequests)
        {
            logger.LogWarning("API-Football daily limit reached ({Count}/{Max})",
                _dailyRequestCount, MaxDailyRequests);
            return;
        }

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(daysAhead);

        // Get upcoming matches
        var matches = await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.MatchDateUtc >= now && m.MatchDateUtc <= cutoff)
            .Where(m => m.Status == MatchStatus.Scheduled || m.Status == MatchStatus.Timed)
            .ToListAsync(cancellationToken);

        // Get unique teams
        var teamIds = matches
            .SelectMany(m => new[] { m.HomeTeamId, m.AwayTeamId })
            .Distinct()
            .ToList();

        // Check which teams need updating (not synced in last 24h)
        var staleThreshold = now.AddHours(-24);
        var teamsToSync = new List<Guid>();

        foreach (var teamId in teamIds)
        {
            if (_dailyRequestCount >= MaxDailyRequests) break;

            var existing = await context.TeamInjuries
                .FirstOrDefaultAsync(t => t.TeamId == teamId, cancellationToken);

            if (existing == null || existing.LastSyncedAt < staleThreshold)
            {
                teamsToSync.Add(teamId);
            }
        }

        logger.LogInformation(
            "Syncing injuries for {Count} teams ({Requests} API requests remaining)",
            teamsToSync.Count, MaxDailyRequests - _dailyRequestCount);

        foreach (var teamId in teamsToSync)
        {
            await SyncTeamInjuriesAsync(teamId, cancellationToken);
            await Task.Delay(500, cancellationToken); // Rate limit
        }
    }

    private async Task SyncTeamInjuriesAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await context.Teams
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team == null) return;

        var apiKey = configuration["ApiFootball:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("API-Football API key not configured");
            return;
        }

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-RapidAPI-Key", apiKey);
        client.DefaultRequestHeaders.Add("X-RapidAPI-Host", "api-football-v1.p.rapidapi.com");

        var url = $"{BaseUrl}/injuries?team={team.ExternalId}&season={DateTime.UtcNow.Year}";

        try
        {
            _dailyRequestCount++;

            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<ApiFootballInjuryResponse>(json);

            if (data?.Response == null) return;

            await ProcessInjuryResponseAsync(teamId, data.Response, cancellationToken);

            logger.LogDebug("Synced injuries for team {TeamId} ({Count} injuries)",
                teamId, data.Response.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch injuries for team {TeamId}", teamId);
        }
    }

    private async Task ProcessInjuryResponseAsync(
        Guid teamId,
        List<ApiFootballInjury> injuries,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Clear old injuries for this team
        var oldInjuries = await context.PlayerInjuries
            .Where(i => i.TeamId == teamId)
            .ToListAsync(cancellationToken);

        context.PlayerInjuries.RemoveRange(oldInjuries);

        // Add current injuries
        var activeInjuries = injuries
            .Where(i => i.Player?.Type != "Missing") // Skip suspended players
            .ToList();

        foreach (var injury in activeInjuries)
        {
            var playerInjury = new PlayerInjury
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                ExternalPlayerId = injury.Player?.Id ?? 0,
                PlayerName = injury.Player?.Name ?? "",
                Position = MapPosition(injury.Player?.Type),
                InjuryType = injury.Player?.Reason ?? "Unknown",
                IsActive = true,
                LastUpdatedAt = now
            };

            context.PlayerInjuries.Add(playerInjury);
        }

        // Update team injury summary
        var teamInjuries = await context.TeamInjuries
            .FirstOrDefaultAsync(t => t.TeamId == teamId, cancellationToken);

        if (teamInjuries == null)
        {
            teamInjuries = new TeamInjuries
            {
                Id = Guid.NewGuid(),
                TeamId = teamId
            };
            context.TeamInjuries.Add(teamInjuries);
        }

        teamInjuries.TotalInjured = activeInjuries.Count;
        teamInjuries.InjuredPlayerNames = JsonSerializer.Serialize(
            activeInjuries.Select(i => i.Player?.Name).Where(n => n != null).ToList());
        teamInjuries.LastSyncedAt = now;
        teamInjuries.InjuryImpactScore = CalculateInjuryImpact(teamInjuries);

        await context.SaveChangesAsync(cancellationToken);
    }

    public double CalculateInjuryImpact(TeamInjuries injuries)
    {
        double impact = 0;

        impact += injuries.TotalInjured * 5;
        impact += injuries.KeyPlayersInjured * 15;
        impact += injuries.TopScorerInjured ? 20 : 0;
        impact += injuries.CaptainInjured ? 10 : 0;
        impact += injuries.FirstChoiceGkInjured ? 15 : 0;

        return Math.Min(impact, 100);
    }

    private static string MapPosition(string? type)
    {
        return type?.ToUpperInvariant() switch
        {
            "GOALKEEPER" => "GK",
            "DEFENDER" => "DEF",
            "MIDFIELDER" => "MID",
            "ATTACKER" => "FWD",
            _ => "UNK"
        };
    }

    public async Task<TeamInjuries?> GetTeamInjuriesAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return await context.TeamInjuries
            .FirstOrDefaultAsync(t => t.TeamId == teamId, cancellationToken);
    }

    private static void ResetDailyCounterIfNeeded()
    {
        var today = DateTime.UtcNow.Date;
        if (_lastResetDate != today)
        {
            _dailyRequestCount = 0;
            _lastResetDate = today;
        }
    }
}

// API-Football response DTOs
internal sealed class ApiFootballInjuryResponse
{
    [JsonPropertyName("response")]
    public List<ApiFootballInjury> Response { get; set; } = [];
}

internal sealed class ApiFootballInjury
{
    [JsonPropertyName("player")]
    public ApiFootballPlayer? Player { get; set; }
}

internal sealed class ApiFootballPlayer
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
```

---

## Part 5: Graceful Degradation in Bot Strategy

### 5.0 Bot Data Awareness

Bots need to know when data is unavailable and adjust accordingly. The strategy should:
1. Check which data sources are available before prediction
2. Redistribute weights when sources are missing
3. Fall back to simpler strategies if critical data is unavailable

**PredictionContext.cs** - Captures what data is available for a prediction:
```csharp
namespace ExtraTime.Application.Features.Bots.Strategies;

/// <summary>
/// Context for a bot prediction, including data availability.
/// Used to determine which factors can be used and how to weight them.
/// </summary>
public sealed class PredictionContext
{
    public required Match Match { get; init; }
    public required StatsAnalystConfig Config { get; init; }
    public required DataAvailability DataAvailability { get; init; }

    // Collected data (null if unavailable)
    public TeamFormCache? HomeForm { get; set; }
    public TeamFormCache? AwayForm { get; set; }
    public TeamXgStats? HomeXg { get; set; }
    public TeamXgStats? AwayXg { get; set; }
    public MatchOdds? Odds { get; set; }
    public TeamInjuries? HomeInjuries { get; set; }
    public TeamInjuries? AwayInjuries { get; set; }
    public MatchLineupAnalysis? HomeLineupAnalysis { get; set; }
    public MatchLineupAnalysis? AwayLineupAnalysis { get; set; }

    // What's actually usable for this prediction
    public bool CanUseForm => HomeForm != null && AwayForm != null;
    public bool CanUseXg => DataAvailability.XgDataAvailable && HomeXg != null && AwayXg != null;
    public bool CanUseOdds => DataAvailability.OddsDataAvailable && Odds != null;
    public bool CanUseInjuries => DataAvailability.InjuryDataAvailable &&
                                   (HomeInjuries != null || AwayInjuries != null);
    public bool CanUseLineups => DataAvailability.LineupDataAvailable &&
                                  (HomeLineupAnalysis != null || AwayLineupAnalysis != null);

    // Calculate effective weights (redistribute unavailable weight)
    public EffectiveWeights CalculateEffectiveWeights()
    {
        var weights = new Dictionary<string, double>();
        double totalConfiguredWeight = 0;
        double totalAvailableWeight = 0;

        // Form is always available (calculated from matches)
        if (Config.FormWeight > 0)
        {
            weights["Form"] = Config.FormWeight;
            totalConfiguredWeight += Config.FormWeight;
            if (CanUseForm) totalAvailableWeight += Config.FormWeight;
        }

        if (Config.HomeAdvantageWeight > 0)
        {
            weights["HomeAdvantage"] = Config.HomeAdvantageWeight;
            totalConfiguredWeight += Config.HomeAdvantageWeight;
            totalAvailableWeight += Config.HomeAdvantageWeight; // Always available
        }

        if (Config.XgWeight > 0)
        {
            weights["Xg"] = Config.XgWeight;
            totalConfiguredWeight += Config.XgWeight;
            if (CanUseXg) totalAvailableWeight += Config.XgWeight;
        }

        if (Config.XgDefensiveWeight > 0)
        {
            weights["XgDefensive"] = Config.XgDefensiveWeight;
            totalConfiguredWeight += Config.XgDefensiveWeight;
            if (CanUseXg) totalAvailableWeight += Config.XgDefensiveWeight;
        }

        if (Config.OddsWeight > 0)
        {
            weights["Odds"] = Config.OddsWeight;
            totalConfiguredWeight += Config.OddsWeight;
            if (CanUseOdds) totalAvailableWeight += Config.OddsWeight;
        }

        if (Config.InjuryWeight > 0)
        {
            weights["Injury"] = Config.InjuryWeight;
            totalConfiguredWeight += Config.InjuryWeight;
            if (CanUseInjuries) totalAvailableWeight += Config.InjuryWeight;
        }

        if (Config.LineupAnalysisWeight > 0)
        {
            weights["Lineup"] = Config.LineupAnalysisWeight;
            totalConfiguredWeight += Config.LineupAnalysisWeight;
            if (CanUseLineups) totalAvailableWeight += Config.LineupAnalysisWeight;
        }

        // Redistribute: scale available weights up to compensate for missing ones
        double scaleFactor = totalAvailableWeight > 0
            ? totalConfiguredWeight / totalAvailableWeight
            : 1.0;

        return new EffectiveWeights
        {
            FormWeight = CanUseForm ? Config.FormWeight * scaleFactor : 0,
            HomeAdvantageWeight = Config.HomeAdvantageWeight * scaleFactor,
            XgWeight = CanUseXg ? Config.XgWeight * scaleFactor : 0,
            XgDefensiveWeight = CanUseXg ? Config.XgDefensiveWeight * scaleFactor : 0,
            OddsWeight = CanUseOdds ? Config.OddsWeight * scaleFactor : 0,
            InjuryWeight = CanUseInjuries ? Config.InjuryWeight * scaleFactor : 0,
            LineupWeight = CanUseLineups ? Config.LineupAnalysisWeight * scaleFactor : 0,
            TotalConfiguredSources = weights.Count,
            TotalAvailableSources = new[] { CanUseForm, true, CanUseXg, CanUseOdds, CanUseInjuries, CanUseLineups }.Count(x => x),
            DataQualityScore = totalAvailableWeight / totalConfiguredWeight * 100
        };
    }

    // Determine if this bot can make a meaningful prediction
    public bool CanMakePrediction()
    {
        // Always need basic form data
        if (!CanUseForm) return false;

        // Check if bot is heavily dependent on unavailable data
        var effectiveWeights = CalculateEffectiveWeights();

        // If we've lost more than 50% of configured weight, prediction quality is low
        if (effectiveWeights.DataQualityScore < 50) return false;

        return true;
    }

    // Get warning message for degraded predictions
    public string? GetDegradationWarning()
    {
        var missing = new List<string>();

        if (Config.XgWeight > 0.15 && !CanUseXg)
            missing.Add("xG data unavailable");
        if (Config.OddsWeight > 0.15 && !CanUseOdds)
            missing.Add("Odds data unavailable");
        if (Config.InjuryWeight > 0.10 && !CanUseInjuries)
            missing.Add("Injury data unavailable");
        if (Config.LineupAnalysisWeight > 0.10 && !CanUseLineups)
            missing.Add("Lineup data unavailable");

        return missing.Count > 0
            ? $"Degraded prediction: {string.Join(", ", missing)}"
            : null;
    }
}

public sealed record EffectiveWeights
{
    public double FormWeight { get; init; }
    public double HomeAdvantageWeight { get; init; }
    public double XgWeight { get; init; }
    public double XgDefensiveWeight { get; init; }
    public double OddsWeight { get; init; }
    public double InjuryWeight { get; init; }
    public double LineupWeight { get; init; }

    public int TotalConfiguredSources { get; init; }
    public int TotalAvailableSources { get; init; }
    public double DataQualityScore { get; init; }  // 0-100
}
```

**Fallback Strategy** - When StatsAnalyst can't make a quality prediction:
```csharp
public sealed class FallbackStrategy
{
    private readonly Random _random = new();

    /// <summary>
    /// Simple prediction based only on home advantage.
    /// Used when data quality is too low for sophisticated analysis.
    /// </summary>
    public (int HomeScore, int AwayScore) GenerateBasicPrediction()
    {
        // Simple: home team slight advantage
        int homeScore = _random.NextDouble() < 0.55 ? 2 : 1;
        int awayScore = _random.NextDouble() < 0.40 ? 1 : 0;

        // Occasionally predict draws (20%)
        if (_random.NextDouble() < 0.20)
        {
            awayScore = homeScore;
        }

        return (homeScore, awayScore);
    }
}
```

### 5.1 Updated Configuration

**StatsAnalystConfig.cs (extended)**:
```csharp
public sealed record StatsAnalystConfig
{
    // === Existing weights (Phase 7.5) ===
    public double FormWeight { get; init; } = 0.20;
    public double HomeAdvantageWeight { get; init; } = 0.15;
    public double GoalTrendWeight { get; init; } = 0.10;
    public double StreakWeight { get; init; } = 0.05;

    // === Phase 9 weights ===
    public double LineupAnalysisWeight { get; init; } = 0.10;

    // === Phase 9.5 weights - External Data ===
    public double XgWeight { get; init; } = 0.20;           // Understat xG data
    public double XgDefensiveWeight { get; init; } = 0.10;  // xGA data
    public double OddsWeight { get; init; } = 0.05;         // Market consensus
    public double InjuryWeight { get; init; } = 0.05;       // Injury impact

    // === Analysis parameters ===
    public int MatchesAnalyzed { get; init; } = 5;
    public bool HighStakesBoost { get; init; } = true;
    public int LateSeasonMatchday { get; init; } = 30;

    // === Prediction style ===
    public PredictionStyle Style { get; init; } = PredictionStyle.Moderate;
    public double RandomVariance { get; init; } = 0.1;

    // === Feature flags ===
    public bool UseXgData { get; init; } = true;
    public bool UseOddsData { get; init; } = true;
    public bool UseInjuryData { get; init; } = true;
    public bool UseLineupData { get; init; } = true;

    // === Preset configurations ===

    public static StatsAnalystConfig FullAnalysis => new()
    {
        FormWeight = 0.15,
        HomeAdvantageWeight = 0.10,
        GoalTrendWeight = 0.05,
        StreakWeight = 0.05,
        LineupAnalysisWeight = 0.10,
        XgWeight = 0.25,
        XgDefensiveWeight = 0.15,
        OddsWeight = 0.10,
        InjuryWeight = 0.05,
        Style = PredictionStyle.Moderate
    };

    public static StatsAnalystConfig XgFocused => new()
    {
        FormWeight = 0.10,
        HomeAdvantageWeight = 0.10,
        XgWeight = 0.40,
        XgDefensiveWeight = 0.25,
        OddsWeight = 0.10,
        InjuryWeight = 0.05,
        Style = PredictionStyle.Moderate
    };

    public static StatsAnalystConfig MarketFollower => new()
    {
        FormWeight = 0.15,
        HomeAdvantageWeight = 0.10,
        XgWeight = 0.15,
        OddsWeight = 0.50,  // Heavy market weight
        InjuryWeight = 0.10,
        Style = PredictionStyle.Conservative
    };

    public static StatsAnalystConfig InjuryAware => new()
    {
        FormWeight = 0.20,
        HomeAdvantageWeight = 0.15,
        XgWeight = 0.20,
        LineupAnalysisWeight = 0.20,
        InjuryWeight = 0.25,  // Heavy injury weight
        Style = PredictionStyle.Moderate
    };
}
```

### 5.2 Enhanced Strategy Implementation

**StatsAnalystStrategy.cs (Phase 9.5 updates)**:
```csharp
public sealed class StatsAnalystStrategy : IBotBettingStrategy
{
    private readonly ITeamFormCalculator _formCalculator;
    private readonly IUnderstatService? _understatService;
    private readonly IOddsDataService? _oddsService;
    private readonly IInjuryService? _injuryService;
    private readonly ILineupAnalyzer? _lineupAnalyzer;
    private readonly IApplicationDbContext _context;
    private readonly Random _random = new();

    public BotStrategy StrategyType => BotStrategy.StatsAnalyst;

    public StatsAnalystStrategy(
        ITeamFormCalculator formCalculator,
        IApplicationDbContext context,
        IUnderstatService? understatService = null,
        IOddsDataService? oddsService = null,
        IInjuryService? injuryService = null,
        ILineupAnalyzer? lineupAnalyzer = null)
    {
        _formCalculator = formCalculator;
        _context = context;
        _understatService = understatService;
        _oddsService = oddsService;
        _injuryService = injuryService;
        _lineupAnalyzer = lineupAnalyzer;
    }

    public async Task<(int HomeScore, int AwayScore)> GeneratePredictionAsync(
        Match match,
        string? configuration,
        CancellationToken cancellationToken = default)
    {
        var config = StatsAnalystConfig.FromJson(configuration);

        // Gather all available data
        var analysisData = await GatherAnalysisDataAsync(match, config, cancellationToken);

        // Calculate expected goals for each team
        var (expectedHome, expectedAway) = CalculateExpectedGoals(analysisData, config);

        // Apply style and variance
        var (homeScore, awayScore) = ConvertToScoreline(expectedHome, expectedAway, config);

        return (homeScore, awayScore);
    }

    private async Task<MatchAnalysisData> GatherAnalysisDataAsync(
        Match match,
        StatsAnalystConfig config,
        CancellationToken cancellationToken)
    {
        var data = new MatchAnalysisData { Match = match };

        // Form data (always available from Phase 7.5)
        data.HomeForm = await _formCalculator.CalculateFormAsync(
            match.HomeTeamId, match.CompetitionId, config.MatchesAnalyzed, cancellationToken);
        data.AwayForm = await _formCalculator.CalculateFormAsync(
            match.AwayTeamId, match.CompetitionId, config.MatchesAnalyzed, cancellationToken);

        // xG data (Phase 9.5 - Understat)
        if (config.UseXgData && _understatService != null)
        {
            data.HomeXg = await GetTeamXgAsync(match.HomeTeamId, match.CompetitionId, cancellationToken);
            data.AwayXg = await GetTeamXgAsync(match.AwayTeamId, match.CompetitionId, cancellationToken);
        }

        // Odds data (Phase 9.5 - Football-Data.co.uk)
        if (config.UseOddsData && _oddsService != null)
        {
            data.Odds = await _oddsService.GetOddsForMatchAsync(match.Id, cancellationToken);
        }

        // Injury data (Phase 9.5 - API-Football)
        if (config.UseInjuryData && _injuryService != null)
        {
            data.HomeInjuries = await _injuryService.GetTeamInjuriesAsync(match.HomeTeamId, cancellationToken);
            data.AwayInjuries = await _injuryService.GetTeamInjuriesAsync(match.AwayTeamId, cancellationToken);
        }

        // Lineup analysis (Phase 9)
        if (config.UseLineupData && _lineupAnalyzer != null)
        {
            data.HomeLineupAnalysis = await _lineupAnalyzer.AnalyzeMatchLineupAsync(
                match.Id, match.HomeTeamId, cancellationToken);
            data.AwayLineupAnalysis = await _lineupAnalyzer.AnalyzeMatchLineupAsync(
                match.Id, match.AwayTeamId, cancellationToken);
        }

        return data;
    }

    private (double homeExpected, double awayExpected) CalculateExpectedGoals(
        MatchAnalysisData data,
        StatsAnalystConfig config)
    {
        double homeBase = 1.5; // League average
        double awayBase = 1.2; // Slight away disadvantage

        // === Form-based adjustments ===
        if (config.FormWeight > 0 && data.HomeForm != null && data.AwayForm != null)
        {
            var homeFormModifier = data.HomeForm.GetFormScore() / 50.0; // Normalize to ~1.0
            var awayFormModifier = data.AwayForm.GetFormScore() / 50.0;

            homeBase *= (1 + (homeFormModifier - 1) * config.FormWeight);
            awayBase *= (1 + (awayFormModifier - 1) * config.FormWeight);
        }

        // === xG-based adjustments (Phase 9.5) ===
        if (config.XgWeight > 0 && data.HomeXg != null && data.AwayXg != null)
        {
            // Use xG per match as base
            var homeXgBase = data.HomeXg.XgPerMatch;
            var awayXgBase = data.AwayXg.XgPerMatch;

            // Weight by xG data
            homeBase = homeBase * (1 - config.XgWeight) + homeXgBase * config.XgWeight;
            awayBase = awayBase * (1 - config.XgWeight) + awayXgBase * config.XgWeight;

            // Defensive xG adjustment
            if (config.XgDefensiveWeight > 0)
            {
                // If opponent concedes high xGA, increase expected goals
                homeBase *= 1 + (data.AwayXg.XgAgainstPerMatch - 1.3) * config.XgDefensiveWeight;
                awayBase *= 1 + (data.HomeXg.XgAgainstPerMatch - 1.3) * config.XgDefensiveWeight;
            }
        }

        // === Home advantage adjustment ===
        if (config.HomeAdvantageWeight > 0)
        {
            homeBase *= 1 + (0.15 * config.HomeAdvantageWeight); // ~15% home boost
            awayBase *= 1 - (0.10 * config.HomeAdvantageWeight); // ~10% away penalty
        }

        // === Odds-based adjustment (Phase 9.5) ===
        if (config.OddsWeight > 0 && data.Odds != null)
        {
            // If market heavily favors home win, boost home expected goals
            if (data.Odds.MarketFavorite == MatchOutcome.HomeWin)
            {
                var confidence = data.Odds.FavoriteConfidence;
                homeBase *= 1 + ((confidence - 0.4) * config.OddsWeight);
            }
            else if (data.Odds.MarketFavorite == MatchOutcome.AwayWin)
            {
                var confidence = data.Odds.FavoriteConfidence;
                awayBase *= 1 + ((confidence - 0.3) * config.OddsWeight);
            }
        }

        // === Injury adjustment (Phase 9.5) ===
        if (config.InjuryWeight > 0)
        {
            if (data.HomeInjuries != null)
            {
                var impact = data.HomeInjuries.InjuryImpactScore / 100.0;
                homeBase *= 1 - (impact * config.InjuryWeight);
            }
            if (data.AwayInjuries != null)
            {
                var impact = data.AwayInjuries.InjuryImpactScore / 100.0;
                awayBase *= 1 - (impact * config.InjuryWeight);
            }
        }

        // === Lineup adjustment (Phase 9) ===
        if (config.LineupAnalysisWeight > 0)
        {
            if (data.HomeLineupAnalysis != null)
            {
                homeBase *= data.HomeLineupAnalysis.SquadStrengthModifier;
            }
            if (data.AwayLineupAnalysis != null)
            {
                awayBase *= data.AwayLineupAnalysis.SquadStrengthModifier;
            }
        }

        return (homeBase, awayBase);
    }

    private (int home, int away) ConvertToScoreline(
        double homeExpected,
        double awayExpected,
        StatsAnalystConfig config)
    {
        // Apply random variance
        if (config.RandomVariance > 0)
        {
            homeExpected += (_random.NextDouble() - 0.5) * 2 * config.RandomVariance * homeExpected;
            awayExpected += (_random.NextDouble() - 0.5) * 2 * config.RandomVariance * awayExpected;
        }

        // Convert to goals based on style
        int homeGoals = config.Style switch
        {
            PredictionStyle.Conservative => (int)Math.Floor(homeExpected),
            PredictionStyle.Bold => (int)Math.Ceiling(homeExpected),
            _ => (int)Math.Round(homeExpected)
        };

        int awayGoals = config.Style switch
        {
            PredictionStyle.Conservative => (int)Math.Floor(awayExpected),
            PredictionStyle.Bold => (int)Math.Ceiling(awayExpected),
            _ => (int)Math.Round(awayExpected)
        };

        // Clamp to valid range
        homeGoals = Math.Clamp(homeGoals, 0, 6);
        awayGoals = Math.Clamp(awayGoals, 0, 5);

        return (homeGoals, awayGoals);
    }

    private async Task<TeamXgStats?> GetTeamXgAsync(
        Guid teamId,
        Guid competitionId,
        CancellationToken cancellationToken)
    {
        var season = DateTime.UtcNow.Year.ToString();
        if (DateTime.UtcNow.Month < 8) season = (DateTime.UtcNow.Year - 1).ToString();

        return await _context.TeamXgStats
            .FirstOrDefaultAsync(x =>
                x.TeamId == teamId &&
                x.CompetitionId == competitionId &&
                x.Season == season,
                cancellationToken);
    }

    // Interface compliance
    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        // Sync wrapper - uses cached data only
        return (1, 1); // Fallback
    }
}

internal sealed class MatchAnalysisData
{
    public required Match Match { get; init; }
    public TeamFormCache? HomeForm { get; set; }
    public TeamFormCache? AwayForm { get; set; }
    public TeamXgStats? HomeXg { get; set; }
    public TeamXgStats? AwayXg { get; set; }
    public MatchOdds? Odds { get; set; }
    public TeamInjuries? HomeInjuries { get; set; }
    public TeamInjuries? AwayInjuries { get; set; }
    public MatchLineupAnalysis? HomeLineupAnalysis { get; set; }
    public MatchLineupAnalysis? AwayLineupAnalysis { get; set; }
}
```

---

## Part 6: New Bot Personalities

### 6.1 Phase 9.5 Bot Seeds

| Bot Name | Strategy | Configuration | Description |
|----------|----------|---------------|-------------|
| 🔬 Data Scientist | StatsAnalyst | FullAnalysis | Uses all available data sources |
| 📊 xG Expert | StatsAnalyst | XgFocused | Heavy xG weighting |
| 💰 Market Follower | StatsAnalyst | MarketFollower | Follows betting odds |
| 🏥 Injury Tracker | StatsAnalyst | InjuryAware | Focuses on squad availability |

**BotSeeder.cs (additions)**:
```csharp
// Phase 9.5 - External data bots
CreateBot("Data Scientist", BotStrategy.StatsAnalyst,
    StatsAnalystConfig.FullAnalysis.ToJson(), "🔬"),
CreateBot("xG Expert", BotStrategy.StatsAnalyst,
    StatsAnalystConfig.XgFocused.ToJson(), "📊"),
CreateBot("Market Follower", BotStrategy.StatsAnalyst,
    StatsAnalystConfig.MarketFollower.ToJson(), "💰"),
CreateBot("Injury Tracker", BotStrategy.StatsAnalyst,
    StatsAnalystConfig.InjuryAware.ToJson(), "🏥"),
```

---

## Part 7: Database Configuration

### 7.1 Entity Configurations

**TeamXgStatsConfiguration.cs**:
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

**MatchOddsConfiguration.cs**:
```csharp
public sealed class MatchOddsConfiguration : IEntityTypeConfiguration<MatchOdds>
{
    public void Configure(EntityTypeBuilder<MatchOdds> builder)
    {
        builder.ToTable("MatchOdds");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.DataSource).HasMaxLength(50);

        builder.HasOne(o => o.Match)
            .WithOne()
            .HasForeignKey<MatchOdds>(o => o.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.MatchId).IsUnique();
    }
}
```

**TeamInjuriesConfiguration.cs**:
```csharp
public sealed class TeamInjuriesConfiguration : IEntityTypeConfiguration<TeamInjuries>
{
    public void Configure(EntityTypeBuilder<TeamInjuries> builder)
    {
        builder.ToTable("TeamInjuries");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.InjuredPlayerNames).HasMaxLength(2000);

        builder.HasOne(t => t.Team)
            .WithOne()
            .HasForeignKey<TeamInjuries>(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TeamId).IsUnique();
    }
}
```

### 7.2 Migration

Create migration: `AddExternalDataSources`
- Creates `TeamXgStats` table
- Creates `MatchXgStats` table
- Creates `MatchOdds` table
- Creates `TeamInjuries` table
- Creates `PlayerInjuries` table

---

## Part 8: Admin Bot Management

### 8.0 Bot CRUD Operations

Allow admins to create, modify, and delete bots from the admin panel.

**DTOs for Bot Management**:
```csharp
namespace ExtraTime.Application.Features.Bots.DTOs;

public sealed record BotDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy,
    string? Configuration,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastBetPlacedAt,
    BotStatsDto? Stats);

public sealed record BotStatsDto(
    int TotalBetsPlaced,
    int LeaguesJoined,
    double AveragePointsPerBet,
    int ExactPredictions,
    int CorrectResults);

public sealed record CreateBotRequest(
    string Name,
    string? AvatarUrl,
    string Strategy,
    Dictionary<string, object>? Configuration);

public sealed record UpdateBotRequest(
    string? Name,
    string? AvatarUrl,
    string? Strategy,
    Dictionary<string, object>? Configuration,
    bool? IsActive);

public sealed record BotConfigurationDto(
    // Form analysis
    double FormWeight,
    double HomeAdvantageWeight,
    double GoalTrendWeight,
    double StreakWeight,

    // External data
    double XgWeight,
    double XgDefensiveWeight,
    double OddsWeight,
    double InjuryWeight,
    double LineupAnalysisWeight,

    // Behavior
    int MatchesAnalyzed,
    bool HighStakesBoost,
    string Style,  // Conservative, Moderate, Bold
    double RandomVariance,

    // Feature flags
    bool UseXgData,
    bool UseOddsData,
    bool UseInjuryData,
    bool UseLineupData);

// Configuration presets for easy bot creation
public sealed record ConfigurationPresetDto(
    string Name,
    string Description,
    BotConfigurationDto Configuration);
```

**Commands**:

**CreateBotCommand.cs**:
```csharp
public sealed record CreateBotCommand(
    string Name,
    string? AvatarUrl,
    BotStrategy Strategy,
    string? Configuration) : IRequest<Result<BotDto>>;

public sealed class CreateBotCommandHandler(
    IApplicationDbContext context,
    ILogger<CreateBotCommandHandler> logger) : IRequestHandler<CreateBotCommand, Result<BotDto>>
{
    public async ValueTask<Result<BotDto>> Handle(
        CreateBotCommand request,
        CancellationToken cancellationToken)
    {
        // Validate name uniqueness
        var existingBot = await context.Bots
            .FirstOrDefaultAsync(b => b.Name == request.Name, cancellationToken);

        if (existingBot != null)
            return Result<BotDto>.Failure($"Bot with name '{request.Name}' already exists");

        // Create bot user account
        var email = $"bot_{request.Name.ToLowerInvariant().Replace(" ", "_")}@extratime.local";
        var password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

        var existingUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existingUser != null)
            return Result<BotDto>.Failure("Bot user account already exists");

        var user = User.Register(email, request.Name, password);
        user.MarkAsBot();
        context.Users.Add(user);

        // Create bot
        var bot = new Bot
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = request.Name,
            AvatarUrl = request.AvatarUrl,
            Strategy = request.Strategy,
            Configuration = request.Configuration,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Bots.Add(bot);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created bot {BotName} with strategy {Strategy}",
            request.Name, request.Strategy);

        return Result<BotDto>.Success(MapToDto(bot));
    }

    private static BotDto MapToDto(Bot bot) => new(
        bot.Id,
        bot.Name,
        bot.AvatarUrl,
        bot.Strategy.ToString(),
        bot.Configuration,
        bot.IsActive,
        bot.CreatedAt,
        bot.LastBetPlacedAt,
        null);
}
```

**UpdateBotCommand.cs**:
```csharp
public sealed record UpdateBotCommand(
    Guid BotId,
    string? Name,
    string? AvatarUrl,
    BotStrategy? Strategy,
    string? Configuration,
    bool? IsActive) : IRequest<Result<BotDto>>;

public sealed class UpdateBotCommandHandler(
    IApplicationDbContext context,
    ILogger<UpdateBotCommandHandler> logger) : IRequestHandler<UpdateBotCommand, Result<BotDto>>
{
    public async ValueTask<Result<BotDto>> Handle(
        UpdateBotCommand request,
        CancellationToken cancellationToken)
    {
        var bot = await context.Bots
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == request.BotId, cancellationToken);

        if (bot == null)
            return Result<BotDto>.Failure("Bot not found");

        // Update fields if provided
        if (!string.IsNullOrEmpty(request.Name) && request.Name != bot.Name)
        {
            // Check uniqueness
            var existing = await context.Bots
                .FirstOrDefaultAsync(b => b.Name == request.Name && b.Id != request.BotId, cancellationToken);

            if (existing != null)
                return Result<BotDto>.Failure($"Bot with name '{request.Name}' already exists");

            bot.Name = request.Name;
            bot.User.UpdateProfile(bot.User.Email, request.Name);
        }

        if (request.AvatarUrl != null)
            bot.AvatarUrl = request.AvatarUrl;

        if (request.Strategy.HasValue)
            bot.Strategy = request.Strategy.Value;

        if (request.Configuration != null)
            bot.Configuration = request.Configuration;

        if (request.IsActive.HasValue)
            bot.IsActive = request.IsActive.Value;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated bot {BotId}: {BotName}", bot.Id, bot.Name);

        return Result<BotDto>.Success(MapToDto(bot));
    }

    private static BotDto MapToDto(Bot bot) => new(
        bot.Id,
        bot.Name,
        bot.AvatarUrl,
        bot.Strategy.ToString(),
        bot.Configuration,
        bot.IsActive,
        bot.CreatedAt,
        bot.LastBetPlacedAt,
        null);
}
```

**DeleteBotCommand.cs**:
```csharp
public sealed record DeleteBotCommand(Guid BotId) : IRequest<Result>;

public sealed class DeleteBotCommandHandler(
    IApplicationDbContext context,
    ILogger<DeleteBotCommandHandler> logger) : IRequestHandler<DeleteBotCommand, Result>
{
    public async ValueTask<Result> Handle(
        DeleteBotCommand request,
        CancellationToken cancellationToken)
    {
        var bot = await context.Bots
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == request.BotId, cancellationToken);

        if (bot == null)
            return Result.Failure("Bot not found");

        // Check if bot has placed bets (soft delete consideration)
        var hasBets = await context.Bets
            .AnyAsync(b => b.UserId == bot.UserId, cancellationToken);

        if (hasBets)
        {
            // Soft delete - just deactivate
            bot.IsActive = false;
            logger.LogInformation(
                "Bot {BotName} deactivated (has historical bets)",
                bot.Name);
        }
        else
        {
            // Hard delete - no bets placed
            context.Bots.Remove(bot);
            context.Users.Remove(bot.User);
            logger.LogInformation("Bot {BotName} deleted", bot.Name);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

**Queries**:

**GetBotsQuery.cs**:
```csharp
public sealed record GetBotsQuery(
    bool? IncludeInactive = false,
    BotStrategy? Strategy = null) : IRequest<Result<List<BotDto>>>;

public sealed class GetBotsQueryHandler(
    IApplicationDbContext context) : IRequestHandler<GetBotsQuery, Result<List<BotDto>>>
{
    public async ValueTask<Result<List<BotDto>>> Handle(
        GetBotsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Bots.AsQueryable();

        if (request.IncludeInactive != true)
            query = query.Where(b => b.IsActive);

        if (request.Strategy.HasValue)
            query = query.Where(b => b.Strategy == request.Strategy.Value);

        var bots = await query
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        // Get stats for each bot
        var botDtos = new List<BotDto>();
        foreach (var bot in bots)
        {
            var stats = await GetBotStatsAsync(bot.UserId, cancellationToken);
            botDtos.Add(new BotDto(
                bot.Id,
                bot.Name,
                bot.AvatarUrl,
                bot.Strategy.ToString(),
                bot.Configuration,
                bot.IsActive,
                bot.CreatedAt,
                bot.LastBetPlacedAt,
                stats));
        }

        return Result<List<BotDto>>.Success(botDtos);
    }

    private async Task<BotStatsDto> GetBotStatsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var bets = await context.Bets
            .Include(b => b.Result)
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);

        var leaguesJoined = await context.LeagueBotMembers
            .CountAsync(lbm => lbm.Bot.UserId == userId, cancellationToken);

        var betsWithResults = bets.Where(b => b.Result != null).ToList();

        return new BotStatsDto(
            TotalBetsPlaced: bets.Count,
            LeaguesJoined: leaguesJoined,
            AveragePointsPerBet: betsWithResults.Count > 0
                ? betsWithResults.Average(b => b.Result!.PointsEarned)
                : 0,
            ExactPredictions: betsWithResults.Count(b => b.Result!.IsExactMatch),
            CorrectResults: betsWithResults.Count(b => b.Result!.IsCorrectResult));
    }
}
```

**GetBotConfigurationPresetsQuery.cs**:
```csharp
public sealed record GetBotConfigurationPresetsQuery : IRequest<Result<List<ConfigurationPresetDto>>>;

public sealed class GetBotConfigurationPresetsQueryHandler
    : IRequestHandler<GetBotConfigurationPresetsQuery, Result<List<ConfigurationPresetDto>>>
{
    public ValueTask<Result<List<ConfigurationPresetDto>>> Handle(
        GetBotConfigurationPresetsQuery request,
        CancellationToken cancellationToken)
    {
        var presets = new List<ConfigurationPresetDto>
        {
            new("Balanced", "All-round analysis using all available data",
                MapConfig(StatsAnalystConfig.Balanced)),
            new("Form Focused", "Heavily weights recent match results",
                MapConfig(StatsAnalystConfig.FormFocused)),
            new("Home Advantage", "Believes home teams always win",
                MapConfig(StatsAnalystConfig.HomeAdvantage)),
            new("Goal Focused", "Predicts high-scoring matches",
                MapConfig(StatsAnalystConfig.GoalFocused)),
            new("Conservative", "Low-risk, low-score predictions",
                MapConfig(StatsAnalystConfig.Conservative)),
            new("Chaotic", "Unpredictable wild predictions",
                MapConfig(StatsAnalystConfig.Chaotic)),
            new("Full Analysis", "Uses all external data sources",
                MapConfig(StatsAnalystConfig.FullAnalysis)),
            new("xG Expert", "Heavy expected goals weighting",
                MapConfig(StatsAnalystConfig.XgFocused)),
            new("Market Follower", "Follows betting odds consensus",
                MapConfig(StatsAnalystConfig.MarketFollower)),
            new("Injury Aware", "Focuses on squad availability",
                MapConfig(StatsAnalystConfig.InjuryAware)),
        };

        return ValueTask.FromResult(Result<List<ConfigurationPresetDto>>.Success(presets));
    }

    private static BotConfigurationDto MapConfig(StatsAnalystConfig config) => new(
        FormWeight: config.FormWeight,
        HomeAdvantageWeight: config.HomeAdvantageWeight,
        GoalTrendWeight: config.GoalTrendWeight,
        StreakWeight: config.StreakWeight,
        XgWeight: config.XgWeight,
        XgDefensiveWeight: config.XgDefensiveWeight,
        OddsWeight: config.OddsWeight,
        InjuryWeight: config.InjuryWeight,
        LineupAnalysisWeight: config.LineupAnalysisWeight,
        MatchesAnalyzed: config.MatchesAnalyzed,
        HighStakesBoost: config.HighStakesBoost,
        Style: config.Style.ToString(),
        RandomVariance: config.RandomVariance,
        UseXgData: config.UseXgData,
        UseOddsData: config.UseOddsData,
        UseInjuryData: config.UseInjuryData,
        UseLineupData: config.UseLineupData);
}
```

### 8.1 Admin Bot Endpoints

**AdminBotsEndpoints.cs**:
```csharp
public static class AdminBotsEndpoints
{
    public static RouteGroupBuilder MapAdminBotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/bots")
            .WithTags("Admin - Bots")
            .RequireAuthorization("AdminOnly");

        // CRUD
        group.MapGet("/", GetBots).WithName("AdminGetBots");
        group.MapGet("/{id:guid}", GetBot).WithName("AdminGetBot");
        group.MapPost("/", CreateBot).WithName("AdminCreateBot");
        group.MapPut("/{id:guid}", UpdateBot).WithName("AdminUpdateBot");
        group.MapDelete("/{id:guid}", DeleteBot).WithName("AdminDeleteBot");

        // Configuration
        group.MapGet("/presets", GetPresets).WithName("GetBotPresets");
        group.MapPost("/validate-config", ValidateConfig).WithName("ValidateBotConfig");

        // Actions
        group.MapPost("/{id:guid}/activate", ActivateBot).WithName("ActivateBot");
        group.MapPost("/{id:guid}/deactivate", DeactivateBot).WithName("DeactivateBot");
        group.MapPost("/trigger-betting", TriggerBotBetting).WithName("TriggerBotBetting");

        return group;
    }

    private static async Task<IResult> GetBots(
        [AsParameters] GetBotsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetBot(
        Guid id,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var bot = await context.Bots
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        return bot != null ? Results.Ok(bot) : Results.NotFound();
    }

    private static async Task<IResult> CreateBot(
        CreateBotRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<BotStrategy>(request.Strategy, true, out var strategy))
            return Results.BadRequest(new { error = "Invalid strategy" });

        var configJson = request.Configuration != null
            ? JsonSerializer.Serialize(request.Configuration)
            : null;

        var command = new CreateBotCommand(
            request.Name,
            request.AvatarUrl,
            strategy,
            configJson);

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/admin/bots/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> UpdateBot(
        Guid id,
        UpdateBotRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        BotStrategy? strategy = null;
        if (!string.IsNullOrEmpty(request.Strategy))
        {
            if (!Enum.TryParse<BotStrategy>(request.Strategy, true, out var parsed))
                return Results.BadRequest(new { error = "Invalid strategy" });
            strategy = parsed;
        }

        var configJson = request.Configuration != null
            ? JsonSerializer.Serialize(request.Configuration)
            : null;

        var command = new UpdateBotCommand(
            id,
            request.Name,
            request.AvatarUrl,
            strategy,
            configJson,
            request.IsActive);

        var result = await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> DeleteBot(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteBotCommand(id), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetPresets(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBotConfigurationPresetsQuery(), cancellationToken);
        return Results.Ok(result.Value);
    }

    private static IResult ValidateConfig(BotConfigurationDto config)
    {
        var errors = new List<string>();

        // Validate weights sum to approximately 1.0
        var totalWeight = config.FormWeight + config.HomeAdvantageWeight +
                         config.GoalTrendWeight + config.StreakWeight +
                         config.XgWeight + config.XgDefensiveWeight +
                         config.OddsWeight + config.InjuryWeight +
                         config.LineupAnalysisWeight;

        if (totalWeight < 0.8 || totalWeight > 1.2)
            errors.Add($"Weights should sum to approximately 1.0 (current: {totalWeight:F2})");

        // Validate ranges
        if (config.RandomVariance < 0 || config.RandomVariance > 0.5)
            errors.Add("RandomVariance must be between 0 and 0.5");

        if (config.MatchesAnalyzed < 3 || config.MatchesAnalyzed > 20)
            errors.Add("MatchesAnalyzed must be between 3 and 20");

        if (!Enum.TryParse<PredictionStyle>(config.Style, true, out _))
            errors.Add("Invalid Style value");

        return errors.Count > 0
            ? Results.BadRequest(new { valid = false, errors })
            : Results.Ok(new { valid = true, errors = Array.Empty<string>() });
    }

    private static async Task<IResult> ActivateBot(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBotCommand(id, null, null, null, null, true);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok() : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> DeactivateBot(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBotCommand(id, null, null, null, null, false);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok() : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> TriggerBotBetting(
        IBotBettingService botService,
        CancellationToken cancellationToken)
    {
        var betsPlaced = await botService.PlaceBetsForUpcomingMatchesAsync(cancellationToken);
        return Results.Ok(new { betsPlaced });
    }
}
```

### 8.2 Admin Integration Health Endpoints

**AdminIntegrationEndpoints.cs**:
```csharp
public static class AdminIntegrationEndpoints
{
    public static RouteGroupBuilder MapAdminIntegrationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/integrations")
            .WithTags("Admin - Integrations")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetAllStatuses).WithName("GetIntegrationStatuses");
        group.MapGet("/{type}", GetStatus).WithName("GetIntegrationStatus");
        group.MapPost("/{type}/disable", DisableIntegration).WithName("DisableIntegration");
        group.MapPost("/{type}/enable", EnableIntegration).WithName("EnableIntegration");
        group.MapPost("/{type}/sync", TriggerSync).WithName("TriggerIntegrationSync");
        group.MapGet("/availability", GetDataAvailability).WithName("GetDataAvailability");

        return group;
    }

    private static async Task<IResult> GetAllStatuses(
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        var statuses = await healthService.GetAllStatusesAsync(cancellationToken);
        return Results.Ok(statuses);
    }

    private static async Task<IResult> GetStatus(
        string type,
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
            return Results.BadRequest(new { error = "Invalid integration type" });

        var status = await healthService.GetStatusAsync(integrationType, cancellationToken);
        return Results.Ok(status);
    }

    private static async Task<IResult> DisableIntegration(
        string type,
        DisableIntegrationRequest request,
        IIntegrationHealthService healthService,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
            return Results.BadRequest(new { error = "Invalid integration type" });

        await healthService.DisableIntegrationAsync(
            integrationType,
            request.Reason,
            currentUser.UserId.ToString(),
            cancellationToken);

        return Results.Ok(new { message = $"{type} disabled" });
    }

    private static async Task<IResult> EnableIntegration(
        string type,
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
            return Results.BadRequest(new { error = "Invalid integration type" });

        await healthService.EnableIntegrationAsync(integrationType, cancellationToken);
        return Results.Ok(new { message = $"{type} enabled" });
    }

    private static async Task<IResult> TriggerSync(
        string type,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationType>(type, true, out var integrationType))
            return Results.BadRequest(new { error = "Invalid integration type" });

        try
        {
            switch (integrationType)
            {
                case IntegrationType.Understat:
                    var understat = serviceProvider.GetRequiredService<IUnderstatService>();
                    await understat.SyncAllLeaguesAsync(cancellationToken);
                    break;
                case IntegrationType.FootballDataUk:
                    var odds = serviceProvider.GetRequiredService<IOddsDataService>();
                    await odds.ImportAllLeaguesAsync(cancellationToken);
                    break;
                case IntegrationType.ApiFootball:
                    var injuries = serviceProvider.GetRequiredService<IInjuryService>();
                    await injuries.SyncInjuriesForUpcomingMatchesAsync(3, cancellationToken);
                    break;
                case IntegrationType.FootballDataOrg:
                    var football = serviceProvider.GetRequiredService<IFootballSyncService>();
                    await football.SyncMatchesAsync(cancellationToken);
                    break;
            }

            return Results.Ok(new { message = $"{type} sync triggered" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetDataAvailability(
        IIntegrationHealthService healthService,
        CancellationToken cancellationToken)
    {
        var availability = await healthService.GetDataAvailabilityAsync(cancellationToken);
        return Results.Ok(availability);
    }
}

public sealed record DisableIntegrationRequest(string Reason);
```

### 8.3 External Data Admin Endpoints

**AdminExternalDataEndpoints.cs**:
```csharp
public static class AdminExternalDataEndpoints
{
    public static RouteGroupBuilder MapAdminExternalDataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/external-data")
            .WithTags("Admin - External Data")
            .RequireAuthorization("AdminOnly");

        // Understat
        group.MapPost("/understat/sync", SyncUnderstat)
            .WithName("SyncUnderstat");

        group.MapGet("/understat/stats/{teamId:guid}", GetTeamXgStats)
            .WithName("GetTeamXgStats");

        // Odds
        group.MapPost("/odds/sync", SyncOdds)
            .WithName("SyncOdds");

        group.MapGet("/odds/{matchId:guid}", GetMatchOdds)
            .WithName("GetMatchOdds");

        // Injuries
        group.MapPost("/injuries/sync", SyncInjuries)
            .WithName("SyncInjuries");

        group.MapGet("/injuries/{teamId:guid}", GetTeamInjuries)
            .WithName("GetTeamInjuries");

        return group;
    }

    private static async Task<IResult> SyncUnderstat(
        IUnderstatService service,
        CancellationToken cancellationToken)
    {
        await service.SyncAllLeaguesAsync(cancellationToken);
        return Results.Ok(new { message = "Understat sync started" });
    }

    private static async Task<IResult> SyncOdds(
        IOddsDataService service,
        CancellationToken cancellationToken)
    {
        await service.ImportAllLeaguesAsync(cancellationToken);
        return Results.Ok(new { message = "Odds import started" });
    }

    private static async Task<IResult> SyncInjuries(
        IInjuryService service,
        CancellationToken cancellationToken)
    {
        await service.SyncInjuriesForUpcomingMatchesAsync(3, cancellationToken);
        return Results.Ok(new { message = "Injuries sync started" });
    }

    private static async Task<IResult> GetTeamXgStats(
        Guid teamId,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var stats = await context.TeamXgStats
            .Where(x => x.TeamId == teamId)
            .OrderByDescending(x => x.Season)
            .FirstOrDefaultAsync(cancellationToken);

        return stats != null ? Results.Ok(stats) : Results.NotFound();
    }

    private static async Task<IResult> GetMatchOdds(
        Guid matchId,
        IOddsDataService service,
        CancellationToken cancellationToken)
    {
        var odds = await service.GetOddsForMatchAsync(matchId, cancellationToken);
        return odds != null ? Results.Ok(odds) : Results.NotFound();
    }

    private static async Task<IResult> GetTeamInjuries(
        Guid teamId,
        IInjuryService service,
        CancellationToken cancellationToken)
    {
        var injuries = await service.GetTeamInjuriesAsync(teamId, cancellationToken);
        return injuries != null ? Results.Ok(injuries) : Results.NotFound();
    }
}
```

---

## Part 9: Admin Frontend Components

### 9.1 Bot Management Page

**app/(admin)/admin/bots/page.tsx**:
```typescript
'use client';

import { useState } from 'react';
import { useBots, useDeleteBot } from '@/hooks/use-admin-bots';
import { BotCard } from '@/components/admin/bot-card';
import { CreateBotModal } from '@/components/admin/create-bot-modal';
import { Button } from '@/components/ui/button';
import { Plus, Bot as BotIcon } from 'lucide-react';

export default function AdminBotsPage() {
  const [showCreateModal, setShowCreateModal] = useState(false);
  const { data: bots, isLoading } = useBots({ includeInactive: true });
  const deleteBot = useDeleteBot();

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Bot Management</h1>
          <p className="text-muted-foreground">
            Create and configure AI betting bots
          </p>
        </div>
        <Button onClick={() => setShowCreateModal(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Create Bot
        </Button>
      </div>

      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {[...Array(6)].map((_, i) => (
            <div key={i} className="h-48 animate-pulse rounded-lg bg-muted" />
          ))}
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {bots?.map((bot) => (
            <BotCard
              key={bot.id}
              bot={bot}
              onDelete={() => deleteBot.mutate(bot.id)}
            />
          ))}
        </div>
      )}

      <CreateBotModal
        open={showCreateModal}
        onOpenChange={setShowCreateModal}
      />
    </div>
  );
}
```

**components/admin/bot-card.tsx**:
```typescript
'use client';

import { Bot } from '@/types/bot';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { MoreVertical, Edit, Trash2, Power, PowerOff } from 'lucide-react';
import { useUpdateBot } from '@/hooks/use-admin-bots';
import { useState } from 'react';
import { EditBotModal } from './edit-bot-modal';

interface BotCardProps {
  bot: Bot;
  onDelete: () => void;
}

const strategyEmojis: Record<string, string> = {
  Random: '🎲',
  HomeFavorer: '🏠',
  UnderdogSupporter: '🐕',
  DrawPredictor: '🤝',
  HighScorer: '⚽',
  StatsAnalyst: '🧠',
};

export function BotCard({ bot, onDelete }: BotCardProps) {
  const [showEditModal, setShowEditModal] = useState(false);
  const updateBot = useUpdateBot();

  const toggleActive = () => {
    updateBot.mutate({ id: bot.id, isActive: !bot.isActive });
  };

  return (
    <>
      <Card className={bot.isActive ? '' : 'opacity-60'}>
        <CardHeader className="flex flex-row items-center justify-between pb-2">
          <div className="flex items-center gap-2">
            <span className="text-2xl">{strategyEmojis[bot.strategy] || '🤖'}</span>
            <CardTitle className="text-lg">{bot.name}</CardTitle>
          </div>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon">
                <MoreVertical className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => setShowEditModal(true)}>
                <Edit className="mr-2 h-4 w-4" />
                Edit
              </DropdownMenuItem>
              <DropdownMenuItem onClick={toggleActive}>
                {bot.isActive ? (
                  <>
                    <PowerOff className="mr-2 h-4 w-4" />
                    Deactivate
                  </>
                ) : (
                  <>
                    <Power className="mr-2 h-4 w-4" />
                    Activate
                  </>
                )}
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={onDelete}
                className="text-destructive"
              >
                <Trash2 className="mr-2 h-4 w-4" />
                Delete
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <Badge variant={bot.isActive ? 'default' : 'secondary'}>
                {bot.isActive ? 'Active' : 'Inactive'}
              </Badge>
              <Badge variant="outline">{bot.strategy}</Badge>
            </div>

            {bot.stats && (
              <div className="grid grid-cols-2 gap-2 text-sm">
                <div>
                  <span className="text-muted-foreground">Bets placed:</span>
                  <span className="ml-1 font-medium">{bot.stats.totalBetsPlaced}</span>
                </div>
                <div>
                  <span className="text-muted-foreground">Leagues:</span>
                  <span className="ml-1 font-medium">{bot.stats.leaguesJoined}</span>
                </div>
                <div>
                  <span className="text-muted-foreground">Avg pts:</span>
                  <span className="ml-1 font-medium">
                    {bot.stats.averagePointsPerBet.toFixed(2)}
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground">Exact:</span>
                  <span className="ml-1 font-medium">{bot.stats.exactPredictions}</span>
                </div>
              </div>
            )}

            {bot.lastBetPlacedAt && (
              <p className="text-xs text-muted-foreground">
                Last bet: {new Date(bot.lastBetPlacedAt).toLocaleDateString()}
              </p>
            )}
          </div>
        </CardContent>
      </Card>

      <EditBotModal
        bot={bot}
        open={showEditModal}
        onOpenChange={setShowEditModal}
      />
    </>
  );
}
```

**components/admin/create-bot-modal.tsx**:
```typescript
'use client';

import { useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Slider } from '@/components/ui/slider';
import { Switch } from '@/components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useCreateBot, useBotPresets } from '@/hooks/use-admin-bots';
import { toast } from 'sonner';

interface CreateBotModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CreateBotModal({ open, onOpenChange }: CreateBotModalProps) {
  const createBot = useCreateBot();
  const { data: presets } = useBotPresets();

  const [name, setName] = useState('');
  const [strategy, setStrategy] = useState('StatsAnalyst');
  const [selectedPreset, setSelectedPreset] = useState<string | null>(null);
  const [config, setConfig] = useState({
    formWeight: 0.20,
    homeAdvantageWeight: 0.15,
    xgWeight: 0.20,
    xgDefensiveWeight: 0.10,
    oddsWeight: 0.05,
    injuryWeight: 0.05,
    lineupAnalysisWeight: 0.10,
    matchesAnalyzed: 5,
    style: 'Moderate',
    randomVariance: 0.10,
    useXgData: true,
    useOddsData: true,
    useInjuryData: true,
    useLineupData: true,
  });

  const handlePresetSelect = (presetName: string) => {
    const preset = presets?.find((p) => p.name === presetName);
    if (preset) {
      setSelectedPreset(presetName);
      setConfig({
        ...config,
        ...preset.configuration,
      });
    }
  };

  const handleSubmit = async () => {
    if (!name.trim()) {
      toast.error('Bot name is required');
      return;
    }

    try {
      await createBot.mutateAsync({
        name,
        strategy,
        configuration: strategy === 'StatsAnalyst' ? config : undefined,
      });
      toast.success(`Bot "${name}" created`);
      onOpenChange(false);
      setName('');
    } catch (error) {
      toast.error('Failed to create bot');
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create New Bot</DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
          {/* Basic Info */}
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="name">Bot Name</Label>
              <Input
                id="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g., xG Master"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="strategy">Strategy</Label>
              <Select value={strategy} onValueChange={setStrategy}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Random">🎲 Random</SelectItem>
                  <SelectItem value="HomeFavorer">🏠 Home Favorer</SelectItem>
                  <SelectItem value="UnderdogSupporter">🐕 Underdog Supporter</SelectItem>
                  <SelectItem value="DrawPredictor">🤝 Draw Predictor</SelectItem>
                  <SelectItem value="HighScorer">⚽ High Scorer</SelectItem>
                  <SelectItem value="StatsAnalyst">🧠 Stats Analyst</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* StatsAnalyst Configuration */}
          {strategy === 'StatsAnalyst' && (
            <Tabs defaultValue="preset">
              <TabsList>
                <TabsTrigger value="preset">Use Preset</TabsTrigger>
                <TabsTrigger value="custom">Custom Config</TabsTrigger>
              </TabsList>

              <TabsContent value="preset" className="space-y-4">
                <div className="grid gap-2 md:grid-cols-2">
                  {presets?.map((preset) => (
                    <button
                      key={preset.name}
                      onClick={() => handlePresetSelect(preset.name)}
                      className={`p-4 text-left rounded-lg border transition-colors ${
                        selectedPreset === preset.name
                          ? 'border-primary bg-primary/5'
                          : 'border-border hover:border-primary/50'
                      }`}
                    >
                      <div className="font-medium">{preset.name}</div>
                      <div className="text-sm text-muted-foreground">
                        {preset.description}
                      </div>
                    </button>
                  ))}
                </div>
              </TabsContent>

              <TabsContent value="custom" className="space-y-6">
                {/* Weight Sliders */}
                <div className="space-y-4">
                  <h4 className="font-medium">Analysis Weights</h4>
                  <div className="grid gap-4">
                    <WeightSlider
                      label="Form Analysis"
                      value={config.formWeight}
                      onChange={(v) => setConfig({ ...config, formWeight: v })}
                    />
                    <WeightSlider
                      label="Home Advantage"
                      value={config.homeAdvantageWeight}
                      onChange={(v) => setConfig({ ...config, homeAdvantageWeight: v })}
                    />
                    <WeightSlider
                      label="xG (Expected Goals)"
                      value={config.xgWeight}
                      onChange={(v) => setConfig({ ...config, xgWeight: v })}
                    />
                    <WeightSlider
                      label="xG Defensive"
                      value={config.xgDefensiveWeight}
                      onChange={(v) => setConfig({ ...config, xgDefensiveWeight: v })}
                    />
                    <WeightSlider
                      label="Betting Odds"
                      value={config.oddsWeight}
                      onChange={(v) => setConfig({ ...config, oddsWeight: v })}
                    />
                    <WeightSlider
                      label="Injuries"
                      value={config.injuryWeight}
                      onChange={(v) => setConfig({ ...config, injuryWeight: v })}
                    />
                    <WeightSlider
                      label="Lineup Analysis"
                      value={config.lineupAnalysisWeight}
                      onChange={(v) => setConfig({ ...config, lineupAnalysisWeight: v })}
                    />
                  </div>
                </div>

                {/* Style */}
                <div className="space-y-2">
                  <Label>Prediction Style</Label>
                  <Select
                    value={config.style}
                    onValueChange={(v) => setConfig({ ...config, style: v })}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Conservative">Conservative</SelectItem>
                      <SelectItem value="Moderate">Moderate</SelectItem>
                      <SelectItem value="Bold">Bold</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                {/* Feature Flags */}
                <div className="space-y-4">
                  <h4 className="font-medium">Data Sources</h4>
                  <div className="grid gap-4 md:grid-cols-2">
                    <FeatureToggle
                      label="Use xG Data"
                      checked={config.useXgData}
                      onChange={(v) => setConfig({ ...config, useXgData: v })}
                    />
                    <FeatureToggle
                      label="Use Odds Data"
                      checked={config.useOddsData}
                      onChange={(v) => setConfig({ ...config, useOddsData: v })}
                    />
                    <FeatureToggle
                      label="Use Injury Data"
                      checked={config.useInjuryData}
                      onChange={(v) => setConfig({ ...config, useInjuryData: v })}
                    />
                    <FeatureToggle
                      label="Use Lineup Data"
                      checked={config.useLineupData}
                      onChange={(v) => setConfig({ ...config, useLineupData: v })}
                    />
                  </div>
                </div>
              </TabsContent>
            </Tabs>
          )}

          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button onClick={handleSubmit} disabled={createBot.isPending}>
              {createBot.isPending ? 'Creating...' : 'Create Bot'}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function WeightSlider({
  label,
  value,
  onChange,
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
}) {
  return (
    <div className="space-y-2">
      <div className="flex justify-between">
        <Label>{label}</Label>
        <span className="text-sm text-muted-foreground">
          {(value * 100).toFixed(0)}%
        </span>
      </div>
      <Slider
        value={[value * 100]}
        onValueChange={([v]) => onChange(v / 100)}
        max={50}
        step={5}
      />
    </div>
  );
}

function FeatureToggle({
  label,
  checked,
  onChange,
}: {
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
}) {
  return (
    <div className="flex items-center justify-between">
      <Label>{label}</Label>
      <Switch checked={checked} onCheckedChange={onChange} />
    </div>
  );
}
```

### 9.2 Integration Health Dashboard

**app/(admin)/admin/integrations/page.tsx**:
```typescript
'use client';

import { useIntegrationStatuses, useDataAvailability } from '@/hooks/use-admin-integrations';
import { IntegrationCard } from '@/components/admin/integration-card';
import { DataAvailabilityCard } from '@/components/admin/data-availability-card';

export default function AdminIntegrationsPage() {
  const { data: statuses, isLoading } = useIntegrationStatuses();
  const { data: availability } = useDataAvailability();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Integration Health</h1>
        <p className="text-muted-foreground">
          Monitor external data sources and their availability
        </p>
      </div>

      {/* Data Availability Summary */}
      {availability && <DataAvailabilityCard availability={availability} />}

      {/* Integration Cards */}
      <div className="grid gap-4 md:grid-cols-2">
        {isLoading ? (
          [...Array(4)].map((_, i) => (
            <div key={i} className="h-48 animate-pulse rounded-lg bg-muted" />
          ))
        ) : (
          statuses?.map((status) => (
            <IntegrationCard key={status.integrationName} status={status} />
          ))
        )}
      </div>
    </div>
  );
}
```

**components/admin/integration-card.tsx**:
```typescript
'use client';

import { IntegrationStatus } from '@/types/integration';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { RefreshCw, Power, PowerOff, AlertCircle, CheckCircle, XCircle } from 'lucide-react';
import { useTriggerSync, useToggleIntegration } from '@/hooks/use-admin-integrations';
import { formatDistanceToNow } from 'date-fns';

interface IntegrationCardProps {
  status: IntegrationStatus;
}

const healthColors = {
  Healthy: 'bg-green-500',
  Degraded: 'bg-yellow-500',
  Failed: 'bg-red-500',
  Disabled: 'bg-gray-500',
  Unknown: 'bg-gray-400',
};

const healthIcons = {
  Healthy: CheckCircle,
  Degraded: AlertCircle,
  Failed: XCircle,
  Disabled: PowerOff,
  Unknown: AlertCircle,
};

export function IntegrationCard({ status }: IntegrationCardProps) {
  const triggerSync = useTriggerSync();
  const toggleIntegration = useToggleIntegration();

  const Icon = healthIcons[status.health] || AlertCircle;

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="flex items-center gap-2">
          <div className={`h-3 w-3 rounded-full ${healthColors[status.health]}`} />
          {status.integrationName}
        </CardTitle>
        <Badge variant={status.health === 'Healthy' ? 'default' : 'secondary'}>
          {status.health}
        </Badge>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-2 text-sm">
          <div>
            <span className="text-muted-foreground">Last sync:</span>
            <p className="font-medium">
              {status.lastSuccessfulSync
                ? formatDistanceToNow(new Date(status.lastSuccessfulSync), { addSuffix: true })
                : 'Never'}
            </p>
          </div>
          <div>
            <span className="text-muted-foreground">Success rate (24h):</span>
            <p className="font-medium">{status.successRate24h.toFixed(1)}%</p>
          </div>
          <div>
            <span className="text-muted-foreground">Consecutive failures:</span>
            <p className="font-medium">{status.consecutiveFailures}</p>
          </div>
          <div>
            <span className="text-muted-foreground">Data fresh:</span>
            <p className="font-medium">{status.isDataStale ? '❌ Stale' : '✅ Fresh'}</p>
          </div>
        </div>

        {status.lastErrorMessage && (
          <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">
            <p className="font-medium">Last error:</p>
            <p className="truncate">{status.lastErrorMessage}</p>
          </div>
        )}

        <div className="flex gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => triggerSync.mutate(status.integrationName)}
            disabled={triggerSync.isPending || status.isManuallyDisabled}
          >
            <RefreshCw className={`mr-2 h-4 w-4 ${triggerSync.isPending ? 'animate-spin' : ''}`} />
            Sync Now
          </Button>
          <Button
            variant={status.isManuallyDisabled ? 'default' : 'outline'}
            size="sm"
            onClick={() =>
              toggleIntegration.mutate({
                type: status.integrationName,
                enable: status.isManuallyDisabled,
              })
            }
          >
            {status.isManuallyDisabled ? (
              <>
                <Power className="mr-2 h-4 w-4" />
                Enable
              </>
            ) : (
              <>
                <PowerOff className="mr-2 h-4 w-4" />
                Disable
              </>
            )}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
```

**components/admin/data-availability-card.tsx**:
```typescript
'use client';

import { DataAvailability } from '@/types/integration';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle, XCircle } from 'lucide-react';

interface DataAvailabilityCardProps {
  availability: DataAvailability;
}

export function DataAvailabilityCard({ availability }: DataAvailabilityCardProps) {
  const sources = [
    { name: 'Form Data', available: availability.formDataAvailable },
    { name: 'xG Statistics', available: availability.xgDataAvailable },
    { name: 'Betting Odds', available: availability.oddsDataAvailable },
    { name: 'Injury Reports', available: availability.injuryDataAvailable },
    { name: 'Lineup Data', available: availability.lineupDataAvailable },
    { name: 'Standings', available: availability.standingsDataAvailable },
  ];

  const availableCount = sources.filter((s) => s.available).length;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          <span>Bot Data Availability</span>
          <span className="text-lg font-normal">
            {availableCount}/{sources.length} sources
          </span>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid gap-2 md:grid-cols-3">
          {sources.map((source) => (
            <div
              key={source.name}
              className={`flex items-center gap-2 rounded-lg p-3 ${
                source.available ? 'bg-green-500/10' : 'bg-red-500/10'
              }`}
            >
              {source.available ? (
                <CheckCircle className="h-5 w-5 text-green-500" />
              ) : (
                <XCircle className="h-5 w-5 text-red-500" />
              )}
              <span className="font-medium">{source.name}</span>
            </div>
          ))}
        </div>
        {!availability.hasAnyExternalData && (
          <p className="mt-4 text-sm text-muted-foreground">
            ⚠️ No external data sources available. Bots will use basic form analysis only.
          </p>
        )}
      </CardContent>
    </Card>
  );
}
```

### 9.3 Admin Hooks

**hooks/use-admin-bots.ts**:
```typescript
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import { Bot, CreateBotRequest, UpdateBotRequest, ConfigurationPreset } from '@/types/bot';

export function useBots(options?: { includeInactive?: boolean; strategy?: string }) {
  return useQuery({
    queryKey: ['admin', 'bots', options],
    queryFn: () => {
      const params = new URLSearchParams();
      if (options?.includeInactive) params.set('includeInactive', 'true');
      if (options?.strategy) params.set('strategy', options.strategy);
      return apiClient.get<Bot[]>(`/admin/bots?${params}`);
    },
  });
}

export function useBotPresets() {
  return useQuery({
    queryKey: ['admin', 'bots', 'presets'],
    queryFn: () => apiClient.get<ConfigurationPreset[]>('/admin/bots/presets'),
  });
}

export function useCreateBot() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateBotRequest) =>
      apiClient.post<Bot>('/admin/bots', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'bots'] });
      queryClient.invalidateQueries({ queryKey: ['bots'] });
    },
  });
}

export function useUpdateBot() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, ...data }: UpdateBotRequest & { id: string }) =>
      apiClient.put<Bot>(`/admin/bots/${id}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'bots'] });
      queryClient.invalidateQueries({ queryKey: ['bots'] });
    },
  });
}

export function useDeleteBot() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/admin/bots/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'bots'] });
      queryClient.invalidateQueries({ queryKey: ['bots'] });
    },
  });
}
```

**hooks/use-admin-integrations.ts**:
```typescript
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import { IntegrationStatus, DataAvailability } from '@/types/integration';

export function useIntegrationStatuses() {
  return useQuery({
    queryKey: ['admin', 'integrations'],
    queryFn: () => apiClient.get<IntegrationStatus[]>('/admin/integrations'),
    refetchInterval: 30000, // Refresh every 30 seconds
  });
}

export function useDataAvailability() {
  return useQuery({
    queryKey: ['admin', 'integrations', 'availability'],
    queryFn: () => apiClient.get<DataAvailability>('/admin/integrations/availability'),
    refetchInterval: 60000,
  });
}

export function useTriggerSync() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (type: string) =>
      apiClient.post(`/admin/integrations/${type}/sync`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'integrations'] });
    },
  });
}

export function useToggleIntegration() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ type, enable }: { type: string; enable: boolean }) =>
      enable
        ? apiClient.post(`/admin/integrations/${type}/enable`)
        : apiClient.post(`/admin/integrations/${type}/disable`, { reason: 'Manual disable' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'integrations'] });
    },
  });
}
```

---

## Part 10: Implementation Checklist

### Phase 9.5A: Integration Health System
- [ ] Create `IntegrationStatus` entity
- [ ] Create `IntegrationType` enum
- [ ] Create `IntegrationStatusConfiguration`
- [ ] Create `IIntegrationHealthService` interface
- [ ] Implement `IntegrationHealthService`
- [ ] Add `IntegrationStatuses` DbSet to context
- [ ] Create database migration
- [ ] Register services in DI

### Phase 9.5B: Understat Integration
- [ ] Create `TeamXgStats` entity
- [ ] Create `MatchXgStats` entity
- [ ] Create `TeamXgStatsConfiguration`
- [ ] Create `IUnderstatService` interface
- [ ] Implement `UnderstatService` (scraping)
- [ ] Create `UnderstatSyncBackgroundService`
- [ ] Add database migration
- [ ] Register services in DI
- [ ] Test xG data sync

### Phase 9.5B: Football-Data.co.uk Integration
- [ ] Create `MatchOdds` entity
- [ ] Create `MatchOutcome` enum
- [ ] Create `MatchOddsConfiguration`
- [ ] Create `IOddsDataService` interface
- [ ] Implement `OddsDataService` (CSV parsing)
- [ ] Create `OddsSyncBackgroundService`
- [ ] Add database migration
- [ ] Register services in DI
- [ ] Test odds import

### Phase 9.5C: API-Football Integration (Optional)
- [ ] Create `TeamInjuries` entity
- [ ] Create `PlayerInjury` entity
- [ ] Create configurations
- [ ] Create `IInjuryService` interface
- [ ] Implement `InjuryService`
- [ ] Add API key configuration
- [ ] Add database migration
- [ ] Register services in DI
- [ ] Test injury sync (with rate limits)

### Phase 9.5D: Enhanced StatsAnalyst Strategy
- [ ] Update `StatsAnalystConfig` with new weights
- [ ] Add preset configurations
- [ ] Update `StatsAnalystStrategy` with new data sources
- [ ] Update `BotStrategyFactory` with new dependencies
- [ ] Test prediction accuracy

### Phase 9.5E: Admin Endpoints
- [ ] Create `AdminExternalDataEndpoints`
- [ ] Register endpoints in Program.cs
- [ ] Test manual sync triggers

### Phase 9.5F: Graceful Degradation
- [ ] Create `PredictionContext` class
- [ ] Create `EffectiveWeights` record
- [ ] Implement weight redistribution logic
- [ ] Create `FallbackStrategy` class
- [ ] Update `StatsAnalystStrategy` to check data availability
- [ ] Add degradation warnings to predictions
- [ ] Test bot behavior with missing data sources

### Phase 9.5G: Admin Bot Management
- [ ] Create `CreateBotCommand` + Handler
- [ ] Create `UpdateBotCommand` + Handler
- [ ] Create `DeleteBotCommand` + Handler
- [ ] Create `GetBotsQuery` + Handler (with stats)
- [ ] Create `GetBotConfigurationPresetsQuery` + Handler
- [ ] Create `AdminBotsEndpoints`
- [ ] Create `AdminIntegrationEndpoints`
- [ ] Register endpoints in Program.cs
- [ ] Test bot CRUD operations

### Phase 9.5H: Admin Frontend
- [ ] Create `app/(admin)/admin/bots/page.tsx`
- [ ] Create `BotCard` component
- [ ] Create `CreateBotModal` component
- [ ] Create `EditBotModal` component
- [ ] Create `app/(admin)/admin/integrations/page.tsx`
- [ ] Create `IntegrationCard` component
- [ ] Create `DataAvailabilityCard` component
- [ ] Create `use-admin-bots.ts` hooks
- [ ] Create `use-admin-integrations.ts` hooks
- [ ] Add admin navigation links

### Phase 9.5I: New Bots
- [ ] Update `BotSeeder` with 4 new bots
- [ ] Test new bot predictions
- [ ] Compare bot performance

### Phase 9.5J: Configuration
- [ ] Add API-Football API key to appsettings
- [ ] Configure HTTP clients
- [ ] Set up background service scheduling
- [ ] Test all sync services

---

## Part 10: Configuration

### 10.1 appsettings.json

```json
{
  "ExternalData": {
    "Understat": {
      "Enabled": true,
      "SyncSchedule": "0 4 * * *"
    },
    "FootballDataUk": {
      "Enabled": true,
      "SyncSchedule": "0 5 * * 1"
    },
    "ApiFootball": {
      "Enabled": false,
      "ApiKey": "",
      "MaxDailyRequests": 100
    }
  }
}
```

### 10.2 HTTP Client Configuration

```csharp
// In DependencyInjection.cs
services.AddHttpClient("Understat", client =>
{
    client.BaseAddress = new Uri("https://understat.com");
    client.DefaultRequestHeaders.Add("User-Agent", "ExtraTime/1.0");
});

services.AddHttpClient("FootballDataUk", client =>
{
    client.BaseAddress = new Uri("https://www.football-data.co.uk");
    client.DefaultRequestHeaders.Add("User-Agent", "ExtraTime/1.0");
});
```

---

## Notes

### Data Freshness
| Source | Refresh Rate | Staleness Threshold |
|--------|--------------|---------------------|
| Understat | Daily 4 AM | 24 hours |
| Odds | Weekly Monday | 7 days |
| Injuries | On-demand | 24 hours |

### Rate Limiting
- Understat: No official limit, use 2s delay between requests
- Football-Data.co.uk: No limit (static files)
- API-Football: 100/day strict limit - prioritize upcoming matches

### Error Handling
- All sync services use try/catch with logging
- Failed syncs don't block other syncs
- Stale data is used if fresh data unavailable

### Future Enhancements
- Add Sofascore for possession/shots data
- Add WhoScored for player ratings
- Add weather data for outdoor match conditions
