# Phase 9.5E: API-Football Integration - Injury Data

## Overview
Integrate injury data from API-Football to track squad availability and provide injury impact analysis for bot prediction strategies.

> **Data Source**: `https://api-football-v1.p.rapidapi.com/v3/injuries`
> **Sync Strategy**: On-demand for upcoming matches (within 3 days). Limited by free tier rate limits.
> **Rate Limit**: 100 requests/day strict limit on free tier - prioritize upcoming matches only
> **Priority**: Optional/Limited
> **Quota Priority Rule**: if API-Football quota is shared with lineup sync, lineup calls win and injury sync is skipped.
> **Default in zero-cost mode**: keep injury sync disabled unless spare quota exists or an alternative free injury source is configured.

> **Prerequisite**: Phase 9.5A (Integration Health) must be complete
> **Phase 7.8 Contract**: expose `asOfUtc` injury reads from stored snapshots; if unavailable, return `null` so ML can gracefully fall back.

---

## Part 1: Domain Layer

### 1.1 TeamInjuries Entity

**File**: `src/ExtraTime.Domain/Entities/TeamInjuries.cs`

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

### 1.2 PlayerInjury Entity

**File**: `src/ExtraTime.Domain/Entities/PlayerInjury.cs`

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

---

## Part 2: Infrastructure Layer

### 2.1 EF Configuration

**File**: `src/ExtraTime.Infrastructure/Data/Configurations/TeamInjuriesConfiguration.cs`

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

### 2.2 ApplicationDbContext

Add to `ApplicationDbContext.cs`:
```csharp
public DbSet<TeamInjuries> TeamInjuries => Set<TeamInjuries>();
public DbSet<PlayerInjury> PlayerInjuries => Set<PlayerInjury>();
```

Add to `IApplicationDbContext.cs`:
```csharp
DbSet<TeamInjuries> TeamInjuries { get; }
DbSet<PlayerInjury> PlayerInjuries { get; }
```

---

## Part 3: Service Layer

### 3.1 Interface

**File**: `src/ExtraTime.Application/Common/Interfaces/IInjuryService.cs`

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

    Task<TeamInjuries?> GetTeamInjuriesAsOfAsync(
        Guid teamId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);

    double CalculateInjuryImpact(TeamInjuries injuries);
}
```

### 3.2 Implementation

**File**: `src/ExtraTime.Infrastructure/Services/ExternalData/InjuryService.cs`

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

## Part 4: DI Registration

Add to `DependencyInjection.cs`:
```csharp
services.AddScoped<IInjuryService, InjuryService>();
```

---

## Part 5: Configuration

### 5.1 appsettings.json

```json
{
  "ExternalData": {
    "ApiFootball": {
      "Enabled": false,
      "ApiKey": "",
      "MaxDailyRequests": 100
    }
  }
}
```

---

## Implementation Checklist

- [x] Create `TeamInjuries` entity
- [x] Create `PlayerInjury` entity
- [x] Create `TeamInjuriesConfiguration`
- [x] Create `IInjuryService` interface
- [x] Implement `InjuryService` (with daily rate limit tracking)
- [x] Add quota guard: do not consume requests needed for lineup sync
- [x] Add `GetTeamInjuriesAsOfAsync` (returns null when no historical snapshot exists)
- [x] Document leakage-safe fallback for historical ML training (Phase 9.6 integration)
- [x] Add API key configuration
- [x] Add `TeamInjuries` and `PlayerInjuries` DbSets to context
- [x] Add database migration
- [x] Register services in DI
- [x] Test injury sync (with rate limits)

---

## Files Summary

| Action | File |
|--------|------|
| **Create** | `Domain/Entities/TeamInjuries.cs` |
| **Create** | `Domain/Entities/PlayerInjury.cs` |
| **Create** | `Infrastructure/Data/Configurations/TeamInjuriesConfiguration.cs` |
| **Create** | `Application/Common/Interfaces/IInjuryService.cs` |
| **Create** | `Infrastructure/Services/ExternalData/InjuryService.cs` |
| **Modify** | `Application/Common/Interfaces/IApplicationDbContext.cs` |
| **Modify** | `Infrastructure/Data/ApplicationDbContext.cs` |
| **Modify** | `Infrastructure/DependencyInjection.cs` |
| **New migration** | `AddTeamInjuries` |
