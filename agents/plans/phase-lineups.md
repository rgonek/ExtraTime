# Lineups Phase: Match Lineups & Team Usual Lineup

## Overview

Sync match lineup data (formation, starting XI, bench) from an external API and calculate team usual lineups from historical data. The data source is abstracted behind `ILineupDataProvider` so the specific API can be decided and swapped later.

> **Note**: football-data.org free tier does not include lineup data. This phase requires a different free API. The `ILineupDataProvider` interface decouples lineup fetching from any specific provider.
> **Recommended free providers**:
> - API-Football `fixtures/lineups` (free plan supports 100 requests/day; preferred reliability)
> - TheSportsDB `lookuplineup.php?id={eventId}` (free fallback with variable coverage)
> **Priority rule**: lineup sync has higher priority than injury sync when request quotas are constrained.

---

## Part 1: Domain Layer

### 1.1 MatchLineup Entity

**File:** `src/ExtraTime.Domain/Entities/MatchLineup.cs`

```csharp
namespace ExtraTime.Domain.Entities;

public sealed class MatchLineup : BaseEntity
{
    public Guid MatchId { get; private set; }
    public Match Match { get; private set; } = null!;

    public Guid TeamId { get; private set; }
    public Team Team { get; private set; } = null!;

    // Tactical setup
    public string? Formation { get; private set; }

    // Coach
    public string? CoachName { get; private set; }

    // Starting XI - JSON array: [{"id": 123, "name": "...", "position": "Goalkeeper", "shirtNumber": 1}]
    public string StartingXI { get; private set; } = "[]";

    // Bench - same JSON format
    public string Bench { get; private set; } = "[]";

    // Captain
    public string? CaptainName { get; private set; }

    // Sync metadata
    public DateTime SyncedAt { get; private set; }

    private MatchLineup() { }

    public static MatchLineup Create(
        Guid matchId,
        Guid teamId,
        string? formation,
        string? coachName,
        string startingXI,
        string bench,
        string? captainName)
    {
        return new MatchLineup
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            TeamId = teamId,
            Formation = formation,
            CoachName = coachName,
            StartingXI = startingXI,
            Bench = bench,
            CaptainName = captainName,
            SyncedAt = Clock.UtcNow
        };
    }

    public void Update(
        string? formation,
        string? coachName,
        string startingXI,
        string bench,
        string? captainName)
    {
        Formation = formation;
        CoachName = coachName;
        StartingXI = startingXI;
        Bench = bench;
        CaptainName = captainName;
        SyncedAt = Clock.UtcNow;
    }

    public List<LineupPlayer> GetStartingPlayers()
    {
        try { return JsonSerializer.Deserialize<List<LineupPlayer>>(StartingXI) ?? []; }
        catch { return []; }
    }

    public List<LineupPlayer> GetBenchPlayers()
    {
        try { return JsonSerializer.Deserialize<List<LineupPlayer>>(Bench) ?? []; }
        catch { return []; }
    }
}

public sealed record LineupPlayer(
    int Id,
    string Name,
    string? Position,
    int? ShirtNumber);
```

### 1.2 TeamUsualLineup Entity

**File:** `src/ExtraTime.Domain/Entities/TeamUsualLineup.cs`

```csharp
namespace ExtraTime.Domain.Entities;

public sealed class TeamUsualLineup : BaseEntity
{
    public Guid TeamId { get; private set; }
    public Team Team { get; private set; } = null!;

    public Guid SeasonId { get; private set; }
    public Season Season { get; private set; } = null!;

    // Most common formation
    public string? UsualFormation { get; private set; }

    // Key players by position - JSON arrays of LineupPlayer records
    public string UsualGoalkeepers { get; private set; } = "[]";
    public string UsualDefenders { get; private set; } = "[]";
    public string UsualMidfielders { get; private set; } = "[]";
    public string UsualForwards { get; private set; } = "[]";

    // Captain (most frequently captain)
    public string? CaptainName { get; private set; }

    // Analysis metadata
    public int MatchesAnalyzed { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    private TeamUsualLineup() { }

    public static TeamUsualLineup Create(
        Guid teamId,
        Guid seasonId,
        string? usualFormation,
        string goalkeepers,
        string defenders,
        string midfielders,
        string forwards,
        string? captainName,
        int matchesAnalyzed)
    {
        return new TeamUsualLineup
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            SeasonId = seasonId,
            UsualFormation = usualFormation,
            UsualGoalkeepers = goalkeepers,
            UsualDefenders = defenders,
            UsualMidfielders = midfielders,
            UsualForwards = forwards,
            CaptainName = captainName,
            MatchesAnalyzed = matchesAnalyzed,
            CalculatedAt = Clock.UtcNow
        };
    }

    public void Update(
        string? usualFormation,
        string goalkeepers,
        string defenders,
        string midfielders,
        string forwards,
        string? captainName,
        int matchesAnalyzed)
    {
        UsualFormation = usualFormation;
        UsualGoalkeepers = goalkeepers;
        UsualDefenders = defenders;
        UsualMidfielders = midfielders;
        UsualForwards = forwards;
        CaptainName = captainName;
        MatchesAnalyzed = matchesAnalyzed;
        CalculatedAt = Clock.UtcNow;
    }

    public List<UsualPlayer> GetGoalkeepers() => DeserializePlayers(UsualGoalkeepers);
    public List<UsualPlayer> GetDefenders() => DeserializePlayers(UsualDefenders);
    public List<UsualPlayer> GetMidfielders() => DeserializePlayers(UsualMidfielders);
    public List<UsualPlayer> GetForwards() => DeserializePlayers(UsualForwards);

    public List<UsualPlayer> GetAllUsualPlayers() =>
        [.. GetGoalkeepers(), .. GetDefenders(), .. GetMidfielders(), .. GetForwards()];

    private static List<UsualPlayer> DeserializePlayers(string json)
    {
        try { return JsonSerializer.Deserialize<List<UsualPlayer>>(json) ?? []; }
        catch { return []; }
    }
}

/// <summary>
/// A player who frequently appears in a team's lineup.
/// Appearances = number of matches analyzed in which this player started.
/// </summary>
public sealed record UsualPlayer(
    int Id,
    string Name,
    string? Position,
    int Appearances);
```

### 1.3 Match Entity Update

Add navigation property to `Match.cs`:

```csharp
// Add to existing Match entity:
public ICollection<MatchLineup> Lineups { get; private set; } = [];
```

---

## Part 2: Infrastructure Layer - Database

### 2.1 MatchLineup Configuration

**File:** `src/ExtraTime.Infrastructure/Data/Configurations/MatchLineupConfiguration.cs`

```csharp
public sealed class MatchLineupConfiguration : IEntityTypeConfiguration<MatchLineup>
{
    public void Configure(EntityTypeBuilder<MatchLineup> builder)
    {
        builder.ToTable("MatchLineups");

        builder.HasKey(ml => ml.Id);
        builder.Property(ml => ml.Id).ValueGeneratedNever();

        builder.HasOne(ml => ml.Match)
            .WithMany(m => m.Lineups)
            .HasForeignKey(ml => ml.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ml => ml.Team)
            .WithMany()
            .HasForeignKey(ml => ml.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ml => ml.Formation).HasMaxLength(20);
        builder.Property(ml => ml.CoachName).HasMaxLength(150);
        builder.Property(ml => ml.CaptainName).HasMaxLength(150);
        builder.Property(ml => ml.StartingXI).HasMaxLength(4000);
        builder.Property(ml => ml.Bench).HasMaxLength(4000);

        // One lineup per team per match
        builder.HasIndex(ml => new { ml.MatchId, ml.TeamId }).IsUnique();
    }
}
```

### 2.2 TeamUsualLineup Configuration

**File:** `src/ExtraTime.Infrastructure/Data/Configurations/TeamUsualLineupConfiguration.cs`

```csharp
public sealed class TeamUsualLineupConfiguration : IEntityTypeConfiguration<TeamUsualLineup>
{
    public void Configure(EntityTypeBuilder<TeamUsualLineup> builder)
    {
        builder.ToTable("TeamUsualLineups");

        builder.HasKey(tul => tul.Id);
        builder.Property(tul => tul.Id).ValueGeneratedNever();

        builder.HasOne(tul => tul.Team)
            .WithMany()
            .HasForeignKey(tul => tul.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tul => tul.Season)
            .WithMany()
            .HasForeignKey(tul => tul.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(tul => tul.UsualFormation).HasMaxLength(20);
        builder.Property(tul => tul.CaptainName).HasMaxLength(150);
        builder.Property(tul => tul.UsualGoalkeepers).HasMaxLength(2000);
        builder.Property(tul => tul.UsualDefenders).HasMaxLength(2000);
        builder.Property(tul => tul.UsualMidfielders).HasMaxLength(2000);
        builder.Property(tul => tul.UsualForwards).HasMaxLength(2000);

        // One usual lineup per team per season
        builder.HasIndex(tul => new { tul.TeamId, tul.SeasonId }).IsUnique();
    }
}
```

### 2.3 DbContext Updates

Add to `IApplicationDbContext.cs` and `ApplicationDbContext.cs`:
```csharp
DbSet<MatchLineup> MatchLineups { get; }
DbSet<TeamUsualLineup> TeamUsualLineups { get; }
```

### 2.4 Migration

`dotnet ef migrations add AddMatchLineupsAndTeamUsualLineups`

---

## Part 3: Application Layer - Pluggable Data Source

### 3.1 Lineup Data Provider Interface

**File:** `src/ExtraTime.Application/Common/Interfaces/ILineupDataProvider.cs`

This is the abstraction that decouples lineup fetching from any specific API.

```csharp
namespace ExtraTime.Application.Common.Interfaces;

/// <summary>
/// Abstraction for fetching match lineup data from an external source.
/// Implement this interface for each data provider (e.g., API-Football, SofaScore, etc.).
/// </summary>
public interface ILineupDataProvider
{
    /// <summary>
    /// Fetch lineup data for a specific match.
    /// The provider must map from our internal match data to its own match lookup.
    /// Returns null if lineup data is not available (match hasn't started, provider doesn't cover it).
    /// </summary>
    Task<MatchLineupData?> GetMatchLineupAsync(
        MatchLineupRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Input for lineup lookup - enough context for any provider to find the match.
/// </summary>
public sealed record MatchLineupRequest(
    int MatchExternalId,
    string HomeTeamName,
    string AwayTeamName,
    DateTime MatchDateUtc,
    string CompetitionCode);

/// <summary>
/// Lineup data returned from the provider, normalized to a common shape.
/// </summary>
public sealed record MatchLineupData(
    TeamLineupData HomeTeam,
    TeamLineupData AwayTeam);

public sealed record TeamLineupData(
    string? Formation,
    string? CoachName,
    string? CaptainName,
    IReadOnlyList<LineupPlayerData> StartingXI,
    IReadOnlyList<LineupPlayerData> Bench);

public sealed record LineupPlayerData(
    int Id,
    string Name,
    string? Position,
    int? ShirtNumber);
```

### 3.2 Lineup Sync Service Interface

**File:** `src/ExtraTime.Application/Common/Interfaces/ILineupSyncService.cs`

```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface ILineupSyncService
{
    /// <summary>
    /// Sync lineup for a single match. Called on-demand or from a scheduled function.
    /// </summary>
    Task<bool> SyncLineupForMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync lineups for matches starting within the lookAhead window
    /// that don't have lineups yet.
    /// </summary>
    Task<int> SyncLineupsForUpcomingMatchesAsync(
        TimeSpan lookAhead,
        CancellationToken cancellationToken = default);
}
```

### 3.3 Team Usual Lineup Service Interface

**File:** `src/ExtraTime.Application/Common/Interfaces/ITeamUsualLineupService.cs`

```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface ITeamUsualLineupService
{
    /// <summary>
    /// Get or calculate the usual lineup for a team in a season.
    /// Returns cached result if calculated within the last 3 days.
    /// </summary>
    Task<TeamUsualLineup> GetOrCalculateAsync(
        Guid teamId,
        Guid seasonId,
        int matchesToAnalyze = 10,
        CancellationToken cancellationToken = default);
}
```

### 3.4 DTOs

**File:** `src/ExtraTime.Application/Features/Football/DTOs/LineupDtos.cs`

```csharp
namespace ExtraTime.Application.Features.Football.DTOs;

public sealed record MatchLineupDto(
    Guid MatchId,
    Guid TeamId,
    string TeamName,
    string? Formation,
    string? CoachName,
    string? CaptainName,
    IReadOnlyList<LineupPlayerDto> StartingXI,
    IReadOnlyList<LineupPlayerDto> Bench);

public sealed record LineupPlayerDto(
    int Id,
    string Name,
    string? Position,
    int? ShirtNumber);

public sealed record TeamUsualLineupDto(
    Guid TeamId,
    string TeamName,
    string? UsualFormation,
    string? CaptainName,
    IReadOnlyList<UsualPlayerDto> Goalkeepers,
    IReadOnlyList<UsualPlayerDto> Defenders,
    IReadOnlyList<UsualPlayerDto> Midfielders,
    IReadOnlyList<UsualPlayerDto> Forwards,
    int MatchesAnalyzed,
    DateTime CalculatedAt);

public sealed record UsualPlayerDto(
    int Id,
    string Name,
    string? Position,
    int Appearances);
```

---

## Part 4: Service Implementations

### 4.1 LineupSyncService

**File:** `src/ExtraTime.Infrastructure/Services/Football/LineupSyncService.cs`

```csharp
namespace ExtraTime.Infrastructure.Services.Football;

public sealed class LineupSyncService(
    IApplicationDbContext context,
    ILineupDataProvider lineupDataProvider,
    ILogger<LineupSyncService> logger) : ILineupSyncService
{
    public async Task<bool> SyncLineupForMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Competition)
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

        if (match == null)
        {
            logger.LogWarning("Match {Id} not found", matchId);
            return false;
        }

        var request = new MatchLineupRequest(
            match.ExternalId,
            match.HomeTeam.Name,
            match.AwayTeam.Name,
            match.MatchDateUtc,
            match.Competition.Code);

        var lineupData = await lineupDataProvider.GetMatchLineupAsync(request, cancellationToken);
        if (lineupData == null)
        {
            logger.LogDebug("No lineup data available for match {Id}", matchId);
            return false;
        }

        await UpsertLineupAsync(match.Id, match.HomeTeamId, lineupData.HomeTeam, cancellationToken);
        await UpsertLineupAsync(match.Id, match.AwayTeamId, lineupData.AwayTeam, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        logger.LogDebug("Synced lineups for match {MatchId}", matchId);
        return true;
    }

    public async Task<int> SyncLineupsForUpcomingMatchesAsync(
        TimeSpan lookAhead,
        CancellationToken cancellationToken = default)
    {
        var now = Clock.UtcNow;
        var cutoff = now.Add(lookAhead);

        var matchIds = await context.Matches
            .Where(m => m.MatchDateUtc >= now && m.MatchDateUtc <= cutoff)
            .Where(m => m.Status == MatchStatus.Scheduled || m.Status == MatchStatus.Timed)
            .Where(m => !context.MatchLineups.Any(ml => ml.MatchId == m.Id))
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Found {Count} upcoming matches needing lineup sync within {LookAhead}",
            matchIds.Count, lookAhead);

        int synced = 0;
        foreach (var matchId in matchIds)
        {
            if (await SyncLineupForMatchAsync(matchId, cancellationToken))
                synced++;
        }

        return synced;
    }

    private async Task UpsertLineupAsync(
        Guid matchId,
        Guid teamId,
        TeamLineupData data,
        CancellationToken cancellationToken)
    {
        var startingXI = JsonSerializer.Serialize(
            data.StartingXI.Select(p => new LineupPlayer(p.Id, p.Name, p.Position, p.ShirtNumber)));

        var bench = JsonSerializer.Serialize(
            data.Bench.Select(p => new LineupPlayer(p.Id, p.Name, p.Position, p.ShirtNumber)));

        var existing = await context.MatchLineups
            .FirstOrDefaultAsync(ml => ml.MatchId == matchId && ml.TeamId == teamId, cancellationToken);

        if (existing != null)
        {
            existing.Update(data.Formation, data.CoachName, startingXI, bench, data.CaptainName);
        }
        else
        {
            var lineup = MatchLineup.Create(
                matchId, teamId, data.Formation, data.CoachName,
                startingXI, bench, data.CaptainName);
            context.MatchLineups.Add(lineup);
        }
    }
}
```

### 4.2 TeamUsualLineupService

**File:** `src/ExtraTime.Infrastructure/Services/Football/TeamUsualLineupService.cs`

```csharp
namespace ExtraTime.Infrastructure.Services.Football;

public sealed class TeamUsualLineupService(
    IApplicationDbContext context,
    ILogger<TeamUsualLineupService> logger) : ITeamUsualLineupService
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromDays(3);

    public async Task<TeamUsualLineup> GetOrCalculateAsync(
        Guid teamId,
        Guid seasonId,
        int matchesToAnalyze = 10,
        CancellationToken cancellationToken = default)
    {
        var cached = await context.TeamUsualLineups
            .FirstOrDefaultAsync(t =>
                t.TeamId == teamId && t.SeasonId == seasonId,
                cancellationToken);

        if (cached != null && (Clock.UtcNow - cached.CalculatedAt) < CacheExpiry)
            return cached;

        return await CalculateAndStoreAsync(teamId, seasonId, matchesToAnalyze, cached, cancellationToken);
    }

    private async Task<TeamUsualLineup> CalculateAndStoreAsync(
        Guid teamId,
        Guid seasonId,
        int matchesToAnalyze,
        TeamUsualLineup? existing,
        CancellationToken cancellationToken)
    {
        // Get recent finished match lineups for this team in this season
        var lineups = await context.MatchLineups
            .Include(ml => ml.Match)
            .Where(ml => ml.TeamId == teamId && ml.Match.SeasonId == seasonId)
            .Where(ml => ml.Match.Status == MatchStatus.Finished)
            .OrderByDescending(ml => ml.Match.MatchDateUtc)
            .Take(matchesToAnalyze)
            .ToListAsync(cancellationToken);

        if (lineups.Count == 0)
        {
            var empty = existing ?? TeamUsualLineup.Create(
                teamId, seasonId, null, "[]", "[]", "[]", "[]", null, 0);
            if (existing == null)
                context.TeamUsualLineups.Add(empty);
            return empty;
        }

        // Count formation occurrences
        var topFormation = lineups
            .Where(l => !string.IsNullOrEmpty(l.Formation))
            .GroupBy(l => l.Formation)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        // Aggregate player appearances by position
        var allPlayers = lineups.SelectMany(l => l.GetStartingPlayers()).ToList();

        var goalkeepers = GetUsualPlayersByPosition(allPlayers, "Goalkeeper");
        var defenders = GetUsualPlayersByPosition(allPlayers,
            "Defender", "Centre-Back", "Left-Back", "Right-Back");
        var midfielders = GetUsualPlayersByPosition(allPlayers,
            "Midfielder", "Central Midfield", "Defensive Midfield",
            "Attacking Midfield", "Left Midfield", "Right Midfield");
        var forwards = GetUsualPlayersByPosition(allPlayers,
            "Forward", "Attacker", "Centre-Forward", "Left Winger", "Right Winger");

        // Most frequent captain
        var topCaptain = lineups
            .Where(l => !string.IsNullOrEmpty(l.CaptainName))
            .GroupBy(l => l.CaptainName)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        var gkJson = JsonSerializer.Serialize(goalkeepers);
        var defJson = JsonSerializer.Serialize(defenders);
        var midJson = JsonSerializer.Serialize(midfielders);
        var fwdJson = JsonSerializer.Serialize(forwards);

        if (existing != null)
        {
            existing.Update(topFormation, gkJson, defJson, midJson, fwdJson, topCaptain, lineups.Count);
        }
        else
        {
            var usualLineup = TeamUsualLineup.Create(
                teamId, seasonId, topFormation,
                gkJson, defJson, midJson, fwdJson,
                topCaptain, lineups.Count);
            context.TeamUsualLineups.Add(usualLineup);
            existing = usualLineup;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogDebug(
            "Calculated usual lineup for team {TeamId} season {SeasonId}: {Formation}, {Matches} matches",
            teamId, seasonId, topFormation, lineups.Count);

        return existing;
    }

    private static List<UsualPlayer> GetUsualPlayersByPosition(
        List<LineupPlayer> players,
        params string[] positions)
    {
        return players
            .Where(p => positions.Any(pos =>
                p.Position?.Contains(pos, StringComparison.OrdinalIgnoreCase) == true))
            .GroupBy(p => new { p.Id, p.Name, p.Position })
            .Select(g => new UsualPlayer(g.Key.Id, g.Key.Name, g.Key.Position, g.Count()))
            .OrderByDescending(p => p.Appearances)
            .ToList();
    }
}
```

### 4.3 Stub Data Provider (placeholder until API is chosen)

**File:** `src/ExtraTime.Infrastructure/Services/Football/NullLineupDataProvider.cs`

```csharp
namespace ExtraTime.Infrastructure.Services.Football;

/// <summary>
/// No-op provider that always returns null. Register this until
/// a real lineup data provider (API-Football, etc.) is implemented.
/// </summary>
public sealed class NullLineupDataProvider(
    ILogger<NullLineupDataProvider> logger) : ILineupDataProvider
{
    public Task<MatchLineupData?> GetMatchLineupAsync(
        MatchLineupRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "NullLineupDataProvider: no provider configured. Match {ExternalId} skipped.",
            request.MatchExternalId);
        return Task.FromResult<MatchLineupData?>(null);
    }
}
```

### 4.4 DI Registration

Add to `DependencyInjection.cs`:
```csharp
// Lineup services
services.AddScoped<ILineupDataProvider, NullLineupDataProvider>();  // Swap when real provider is added
services.AddScoped<ILineupSyncService, LineupSyncService>();
services.AddScoped<ITeamUsualLineupService, TeamUsualLineupService>();
```

---

## Part 5: Sync Integration

### 5.1 Option A: Timer-Triggered Function (Standalone)

A separate function that checks for upcoming matches and syncs lineups.

**File:** `src/ExtraTime.Functions/Functions/SyncLineupsFunction.cs`

```csharp
public sealed class SyncLineupsFunction(
    ILineupSyncService lineupSyncService,
    ILogger<SyncLineupsFunction> logger)
{
    // Run every 15 minutes
    [Function("SyncLineups")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *")] TimerInfo timerInfo,
        CancellationToken ct)
    {
        logger.LogInformation("SyncLineups started at: {Time}", Clock.UtcNow);

        // Sync lineups for matches starting within the next hour
        var synced = await lineupSyncService.SyncLineupsForUpcomingMatchesAsync(
            TimeSpan.FromHours(1), ct);

        logger.LogInformation("SyncLineups completed: {Count} matches synced", synced);
    }
}
```

### 5.2 Option B: Add Phase to Orchestrator

Alternatively, add a Phase 4 to the existing `SyncFootballDataOrchestrator`. This keeps all sync in one place but adds complexity.

> **Recommendation**: Start with Option A (standalone function). Simpler, easier to toggle on/off, and lineup sync uses a different API with different rate limits than football-data.org.

---

## Part 6: DevTriggers

### 6.1 Add sync-lineups command

Add to `DevTriggers/Program.cs`:

```csharp
case "sync-lineups":
    await RunSyncLineupsAsync(scope.ServiceProvider, logger);
    break;

// ...

static async Task RunSyncLineupsAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Starting lineup sync for upcoming matches...");
    var lineupSyncService = services.GetRequiredService<ILineupSyncService>();
    var synced = await lineupSyncService.SyncLineupsForUpcomingMatchesAsync(TimeSpan.FromHours(2));
    logger.LogInformation("Synced lineups for {Count} matches", synced);
}
```

---

## Part 7: Implementing a Real Provider (Future)

When you choose an API, create a new implementation of `ILineupDataProvider`. Example skeleton:

```csharp
// Example: ApiFootballLineupDataProvider.cs
public sealed class ApiFootballLineupDataProvider(
    IApiFootballClient apiClient,  // Your Refit or HttpClient wrapper
    ILogger<ApiFootballLineupDataProvider> logger) : ILineupDataProvider
{
    public async Task<MatchLineupData?> GetMatchLineupAsync(
        MatchLineupRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Map our match data to the provider's fixture ID
        //    (search by teams + date, or maintain an ID mapping table)
        // 2. Call the provider's lineup endpoint
        // 3. Map the response to MatchLineupData

        throw new NotImplementedException("Replace with actual API integration");
    }
}
```

Then swap in DI:
```csharp
// Replace NullLineupDataProvider:
services.AddScoped<ILineupDataProvider, ApiFootballLineupDataProvider>();
```

---

## Implementation Tasks

### Domain
- [ ] **1.1** Create `MatchLineup` entity and `LineupPlayer` record at `src/ExtraTime.Domain/Entities/MatchLineup.cs`
- [ ] **1.2** Create `TeamUsualLineup` entity and `UsualPlayer` record at `src/ExtraTime.Domain/Entities/TeamUsualLineup.cs`
- [ ] **1.3** Add `ICollection<MatchLineup> Lineups` navigation to `Match.cs`

### Infrastructure - Database
- [ ] **2.1** Create `MatchLineupConfiguration.cs`
- [ ] **2.2** Create `TeamUsualLineupConfiguration.cs`
- [ ] **2.3** Add `DbSet<MatchLineup>` and `DbSet<TeamUsualLineup>` to `IApplicationDbContext` and `ApplicationDbContext`
- [ ] **2.4** Generate migration: `dotnet ef migrations add AddMatchLineupsAndTeamUsualLineups`

### Application - Interfaces & DTOs
- [ ] **3.1** Create `ILineupDataProvider` interface at `src/ExtraTime.Application/Common/Interfaces/ILineupDataProvider.cs`
- [ ] **3.2** Create `ILineupSyncService` interface at `src/ExtraTime.Application/Common/Interfaces/ILineupSyncService.cs`
- [ ] **3.3** Create `ITeamUsualLineupService` interface at `src/ExtraTime.Application/Common/Interfaces/ITeamUsualLineupService.cs`
- [ ] **3.4** Create `LineupDtos.cs` at `src/ExtraTime.Application/Features/Football/DTOs/LineupDtos.cs`

### Infrastructure - Services
- [ ] **4.1** Create `LineupSyncService.cs`
- [ ] **4.2** Create `TeamUsualLineupService.cs`
- [ ] **4.3** Create `NullLineupDataProvider.cs` (stub)
- [ ] **4.4** Register services in `DependencyInjection.cs`

### Functions
- [ ] **5.1** Create `SyncLineupsFunction.cs` (timer-triggered, every 15 min)

### DevTriggers
- [ ] **6.1** Add `sync-lineups` command to `DevTriggers/Program.cs`

### Verification
- [ ] **7.1** Build and run tests
- [ ] **7.2** Run `sync-lineups` dev trigger (should complete with 0 synced using NullProvider)

---

## Files Summary

| Action | File |
|--------|------|
| **Create** | `Domain/Entities/MatchLineup.cs` |
| **Create** | `Domain/Entities/TeamUsualLineup.cs` |
| **Create** | `Application/Common/Interfaces/ILineupDataProvider.cs` |
| **Create** | `Application/Common/Interfaces/ILineupSyncService.cs` |
| **Create** | `Application/Common/Interfaces/ITeamUsualLineupService.cs` |
| **Create** | `Application/Features/Football/DTOs/LineupDtos.cs` |
| **Create** | `Infrastructure/Data/Configurations/MatchLineupConfiguration.cs` |
| **Create** | `Infrastructure/Data/Configurations/TeamUsualLineupConfiguration.cs` |
| **Create** | `Infrastructure/Services/Football/LineupSyncService.cs` |
| **Create** | `Infrastructure/Services/Football/TeamUsualLineupService.cs` |
| **Create** | `Infrastructure/Services/Football/NullLineupDataProvider.cs` |
| **Create** | `Functions/Functions/SyncLineupsFunction.cs` |
| **Modify** | `Domain/Entities/Match.cs` |
| **Modify** | `Application/Common/Interfaces/IApplicationDbContext.cs` |
| **Modify** | `Infrastructure/Data/ApplicationDbContext.cs` |
| **Modify** | `Infrastructure/DependencyInjection.cs` |
| **Modify** | `DevTriggers/Program.cs` |
| **New migration** | `AddMatchLineupsAndTeamUsualLineups` |

---

## Design Decisions

1. **`SeasonId` instead of `CompetitionId`** on TeamUsualLineup - Season already belongs to a Competition, so SeasonId is sufficient and more precise. Usual lineups change season to season.

2. **Standalone function for lineup sync** instead of adding to the orchestrator - Different data source with different rate limits. Easier to enable/disable independently.

3. **`NullLineupDataProvider` stub** - Lets you build and deploy everything now. The infrastructure works, just returns empty data. Swap the DI registration when you pick an API.

4. **`ILineupDataProvider` takes rich context** (`MatchLineupRequest`) - Different APIs identify matches differently. Some use fixture IDs, others need team names + date. Providing all context lets any provider find the match.

5. **`LineupPlayer` stored as JSON** - Avoids a separate `Players` table. Player data is denormalized from the external source. Good enough for display and analysis without modeling a full Player entity.
