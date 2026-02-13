# Season Entity and Football Standings Implementation Plan

## Context

The current data model embeds season information directly in `Competition` (CurrentMatchday, CurrentSeasonStart, CurrentSeasonEnd) and uses `CompetitionTeam` with an integer `Season` field. This makes it difficult to:
- Detect when a new season starts
- Know when to sync teams for a new season
- Track historical season data
- Sync football league standings

This plan introduces proper `Season` and `FootballStanding` entities with a **Durable Functions** architecture using **smart sync** - standings are only fetched when matches actually finish, minimizing API calls while keeping data consistent.

**Design principles:**
- Minimal cost, robust rate limiting
- Smart sync: fetch data only when it changes
- Scalable to 50+ competitions
- Consistent UX: scores and standings update together

---

## Sync Strategy: Smart Unified Sync

### The Problem

| Competitions | Calls per Sync (naive) | Exceeds 10/min? |
|--------------|------------------------|-----------------|
| 5 | 10 (5 matches + 5 standings) | No |
| 10 | 20 | **Yes** |
| 20 | 40 | **Yes** |

### The Solution: Smart Conditional Sync

Instead of always fetching both matches AND standings, we:
1. **Always sync matches** (hourly)
2. **Only sync standings when matches finished** OR at 5 AM daily

```
┌─────────────────────────────────────────────────────────────────────────┐
│              SyncFootballDataOrchestrator (Hourly)                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  PHASE 1: Sync Matches (always)                                         │
│  ───────────────────────────────────────────────────────────────────    │
│  For each batch of 8 competitions:                                      │
│    → Sync matches (1 API call each)                                     │
│    → Track: "Did any match transition to FINISHED?"                     │
│    → Wait 65s for rate limit                                            │
│                                                                         │
│  PHASE 2: Sync Standings (conditional)                                  │
│  ───────────────────────────────────────────────────────────────────    │
│  Sync standings only for competitions where:                            │
│    • Any match finished since last sync, OR                             │
│    • It's 5 AM UTC (daily forced sync for season detection)             │
│                                                                         │
│  For each batch of 8 competitions needing standings:                    │
│    → Sync standings (1 API call each)                                   │
│    → Detect new seasons from response                                   │
│    → Wait 65s for rate limit                                            │
│                                                                         │
│  PHASE 3: Sync Teams (rare - new season only)                           │
│  ───────────────────────────────────────────────────────────────────    │
│  If new seasons detected:                                               │
│    → Sync teams for those competitions                                  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### API Call Budget

| Scenario | Matches | Standings | Total |
|----------|---------|-----------|-------|
| Quiet hour (no matches finish) | 5-20 | 0 | **5-20** |
| Busy hour (2 competitions had matches finish) | 5-20 | 2 | **7-22** |
| 5 AM forced sync | 5-20 | 5-20 | **10-40** |

With batching (8 per minute), even 40 calls only takes ~5 minutes.

### Why Force Daily Sync at 5 AM?

- Season detection happens during standings sync
- During off-season → no matches finish → never detect new season
- 5 AM ensures new season detected within 24 hours

---

## Durable Functions Billing

| Component | Billing | Notes |
|-----------|---------|-------|
| Orchestrator execution | 1 execution | When orchestrator starts |
| Activity execution | 1 per activity | Each competition = 1 execution |
| CreateTimer (waiting) | **$0** | Function hibernates |
| Storage transactions | ~$0.0001 | Table storage for state |

### Monthly Cost Estimate

| Competitions | Monthly Executions | Cost |
|--------------|-------------------|------|
| 5 | ~3,000 | $0 |
| 20 | ~6,000 | $0 |
| 50 | ~12,000 | $0 |

**Free tier: 1,000,000 executions/month**

---

## Implementation Tasks

### Task 1: Domain Layer - New Entities

- [ ] **1.1** Create `src/ExtraTime.Domain/Enums/StandingType.cs`
- [ ] **1.2** Create `src/ExtraTime.Domain/Entities/Season.cs`
- [ ] **1.3** Create `src/ExtraTime.Domain/Entities/SeasonTeam.cs`
- [ ] **1.4** Create `src/ExtraTime.Domain/Entities/FootballStanding.cs`
- [ ] **1.5** Modify `Match.cs` - Add `SeasonId` property (nullable Guid)
- [ ] **1.6** Modify `Competition.cs` - Add `Seasons` navigation property
- [ ] **1.7** Modify `Team.cs` - Add `SeasonTeams` and `FootballStandings` navigations

#### StandingType Enum
```csharp
namespace ExtraTime.Domain.Enums;

public enum StandingType
{
    Total = 0,
    Home = 1,
    Away = 2
}
```

#### Season Entity
```csharp
public sealed class Season : BaseEntity
{
    public int ExternalId { get; private set; }
    public Guid CompetitionId { get; private set; }
    public int StartYear { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int CurrentMatchday { get; private set; }
    public Guid? WinnerTeamId { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTime? TeamsLastSyncedAt { get; private set; }
    public DateTime? StandingsLastSyncedAt { get; private set; }

    public Competition Competition { get; private set; } = null!;
    public Team? Winner { get; private set; }
    public ICollection<SeasonTeam> SeasonTeams { get; private set; } = [];
    public ICollection<Match> Matches { get; private set; } = [];
    public ICollection<FootballStanding> Standings { get; private set; } = [];

    private Season() { }

    public static Season Create(
        int externalId, Guid competitionId, int startYear,
        DateTime startDate, DateTime endDate, int currentMatchday,
        bool isCurrent = true) { ... }

    public void UpdateMatchday(int matchday) { ... }
    public void SetAsNotCurrent() { ... }
    public void SetWinner(Guid teamId) { ... }
    public void RecordTeamsSync() => TeamsLastSyncedAt = DateTime.UtcNow;
    public void RecordStandingsSync() => StandingsLastSyncedAt = DateTime.UtcNow;
}
```

#### SeasonTeam Entity
```csharp
public sealed class SeasonTeam : BaseEntity
{
    public Guid SeasonId { get; private set; }
    public Guid TeamId { get; private set; }

    public Season Season { get; private set; } = null!;
    public Team Team { get; private set; } = null!;

    private SeasonTeam() { }
    public static SeasonTeam Create(Guid seasonId, Guid teamId) { ... }
}
```

#### FootballStanding Entity
```csharp
public sealed class FootballStanding : BaseEntity
{
    public Guid SeasonId { get; private set; }
    public Guid TeamId { get; private set; }
    public StandingType Type { get; private set; }
    public string? Stage { get; private set; }
    public string? Group { get; private set; }
    public int Position { get; private set; }
    public int PlayedGames { get; private set; }
    public int Won { get; private set; }
    public int Draw { get; private set; }
    public int Lost { get; private set; }
    public int GoalsFor { get; private set; }
    public int GoalsAgainst { get; private set; }
    public int GoalDifference { get; private set; }
    public int Points { get; private set; }
    public string? Form { get; private set; }

    public Season Season { get; private set; } = null!;
    public Team Team { get; private set; } = null!;

    private FootballStanding() { }
    public static FootballStanding Create(...) { ... }
    public void Update(...) { ... }
}
```

---

### Task 2: Application Layer - DTOs and Interfaces

- [ ] **2.1** Create `src/ExtraTime.Application/Features/Football/DTOs/StandingsDtos.cs`
- [ ] **2.2** Modify `CompetitionDtos.cs` - Add `Id` to `CurrentSeasonApiDto`
- [ ] **2.3** Modify `IFootballDataService.cs` - Add `GetStandingsAsync`
- [ ] **2.4** Modify `IFootballSyncService.cs` - Add per-competition sync methods
- [ ] **2.5** Modify `IApplicationDbContext.cs` - Add `Seasons`, `SeasonTeams`, `FootballStandings` DbSets
- [ ] **2.6** Create `src/ExtraTime.Application/Common/Interfaces/IBetResultsService.cs`

#### New DTOs
```csharp
// API Response DTOs
public sealed record StandingsApiResponse(
    StandingsCompetitionApiDto Competition,
    SeasonApiDto Season,
    IReadOnlyList<StandingTableApiDto> Standings);

public sealed record SeasonApiDto(
    int Id,
    DateTime StartDate,
    DateTime EndDate,
    int CurrentMatchday,
    StandingsTeamApiDto? Winner);

public sealed record StandingTableApiDto(
    string Stage,
    string Type,
    string? Group,
    IReadOnlyList<StandingRowApiDto> Table);

public sealed record StandingRowApiDto(
    int Position,
    StandingsTeamApiDto Team,
    int PlayedGames, int Won, int Draw, int Lost,
    int GoalsFor, int GoalsAgainst, int GoalDifference,
    int Points, string? Form);

public sealed record StandingsTeamApiDto(int Id, string Name, string ShortName, string? Crest);
public sealed record StandingsCompetitionApiDto(int Id, string Name, string Code);
```

#### Updated IFootballSyncService
```csharp
public interface IFootballSyncService
{
    // Existing
    Task SyncMatchesAsync(CancellationToken ct = default);

    // New: Per-competition methods for Durable Functions activities
    Task<MatchSyncResult> SyncMatchesForCompetitionAsync(int competitionExternalId, CancellationToken ct = default);
    Task<StandingsSyncResult> SyncStandingsForCompetitionAsync(int competitionExternalId, CancellationToken ct = default);
    Task SyncTeamsForCompetitionAsync(int competitionExternalId, CancellationToken ct = default);
}

public sealed record MatchSyncResult(int CompetitionExternalId, bool HasNewlyFinishedMatches);
public sealed record StandingsSyncResult(int CompetitionExternalId, bool NewSeasonDetected);
```

#### IBetResultsService (removes duplication)
```csharp
public interface IBetResultsService
{
    Task<int> CalculateAllPendingBetResultsAsync(CancellationToken ct = default);
}
```

---

### Task 3: Infrastructure Layer - EF Configurations

- [ ] **3.1** Create `src/ExtraTime.Infrastructure/Data/Configurations/SeasonConfiguration.cs`
- [ ] **3.2** Create `src/ExtraTime.Infrastructure/Data/Configurations/SeasonTeamConfiguration.cs`
- [ ] **3.3** Create `src/ExtraTime.Infrastructure/Data/Configurations/FootballStandingConfiguration.cs`
- [ ] **3.4** Modify `MatchConfiguration.cs` - Add SeasonId FK and index
- [ ] **3.5** Modify `ApplicationDbContext.cs` - Add DbSets

#### SeasonConfiguration
```csharp
public sealed class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("Seasons");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.HasIndex(s => new { s.CompetitionId, s.ExternalId }).IsUnique();
        builder.HasIndex(s => new { s.CompetitionId, s.IsCurrent });

        builder.HasOne(s => s.Competition)
            .WithMany(c => c.Seasons)
            .HasForeignKey(s => s.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Winner)
            .WithMany()
            .HasForeignKey(s => s.WinnerTeamId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

#### FootballStandingConfiguration
```csharp
public sealed class FootballStandingConfiguration : IEntityTypeConfiguration<FootballStanding>
{
    public void Configure(EntityTypeBuilder<FootballStanding> builder)
    {
        builder.ToTable("FootballStandings");
        builder.HasKey(fs => fs.Id);
        builder.Property(fs => fs.Id).ValueGeneratedNever();

        builder.Property(fs => fs.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(fs => fs.Stage).HasMaxLength(50);
        builder.Property(fs => fs.Group).HasMaxLength(20);
        builder.Property(fs => fs.Form).HasMaxLength(50);

        builder.HasIndex(fs => new { fs.SeasonId, fs.TeamId, fs.Type, fs.Stage, fs.Group }).IsUnique();
        builder.HasIndex(fs => new { fs.SeasonId, fs.Type, fs.Position });

        builder.HasOne(fs => fs.Season)
            .WithMany(s => s.Standings)
            .HasForeignKey(fs => fs.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fs => fs.Team)
            .WithMany(t => t.FootballStandings)
            .HasForeignKey(fs => fs.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

### Task 4: Database Migration

- [ ] **4.1** Create migration: `dotnet ef migrations add AddSeasonAndStandings --startup-project ../ExtraTime.API`
- [ ] **4.2** Review generated migration
- [ ] **4.3** Add data migration SQL to seed existing seasons from Competition data
- [ ] **4.4** Test migration locally
- [ ] **4.5** Apply migration

#### Data Migration SQL
```sql
-- Create seasons from existing competition data
INSERT INTO Seasons (Id, ExternalId, CompetitionId, StartYear, StartDate, EndDate, CurrentMatchday, IsCurrent, CreatedAt, UpdatedAt)
SELECT NEWID(), 0, c.Id, YEAR(c.CurrentSeasonStart), c.CurrentSeasonStart, c.CurrentSeasonEnd,
       ISNULL(c.CurrentMatchday, 1), 1, GETUTCDATE(), GETUTCDATE()
FROM Competitions c WHERE c.CurrentSeasonStart IS NOT NULL;

-- Create SeasonTeams from CompetitionTeams
INSERT INTO SeasonTeams (Id, SeasonId, TeamId, CreatedAt, UpdatedAt)
SELECT NEWID(), s.Id, ct.TeamId, GETUTCDATE(), GETUTCDATE()
FROM CompetitionTeams ct
INNER JOIN Seasons s ON s.CompetitionId = ct.CompetitionId AND s.StartYear = ct.Season;
```

---

### Task 5: Infrastructure Layer - Service Updates

- [ ] **5.1** Modify `FootballDataService.cs` - Add `GetStandingsAsync`
- [ ] **5.2** Modify `FootballSyncService.cs` - Add per-competition sync methods with smart detection
- [ ] **5.3** Create `src/ExtraTime.Infrastructure/Services/BetResultsService.cs`
- [ ] **5.4** Modify `DependencyInjection.cs` - Register `IBetResultsService`

#### FootballSyncService - Smart Match Detection
```csharp
public async Task<MatchSyncResult> SyncMatchesForCompetitionAsync(
    int competitionExternalId,
    CancellationToken ct = default)
{
    var apiMatches = await footballDataService.GetMatchesAsync(competitionExternalId, ct);

    // Get IDs of matches that are FINISHED in API response
    var finishedExternalIds = apiMatches
        .Where(m => m.Status == "FINISHED")
        .Select(m => m.Id)
        .ToHashSet();

    // Check if any were NOT finished in our DB (newly finished)
    var hasNewlyFinishedMatches = await context.Matches
        .AnyAsync(m => finishedExternalIds.Contains(m.ExternalId)
                    && m.Status != MatchStatus.Finished, ct);

    // Process and save matches...
    await ProcessMatchesAsync(apiMatches, competitionExternalId, ct);

    return new MatchSyncResult(competitionExternalId, hasNewlyFinishedMatches);
}
```

#### BetResultsService (shared logic)
```csharp
public sealed class BetResultsService(
    IApplicationDbContext context,
    IMediator mediator,
    ILogger<BetResultsService> logger) : IBetResultsService
{
    public async Task<int> CalculateAllPendingBetResultsAsync(CancellationToken ct = default)
    {
        var uncalculatedMatches = await context.Bets
            .Include(b => b.Match)
            .Where(b => b.Match.Status == MatchStatus.Finished
                     && b.Match.HomeScore.HasValue
                     && b.Match.AwayScore.HasValue
                     && b.Result == null)
            .Select(b => new { b.Match.Id, b.Match.CompetitionId })
            .Distinct()
            .ToListAsync(ct);

        if (uncalculatedMatches.Count == 0) return 0;

        var processedCount = 0;
        foreach (var match in uncalculatedMatches)
        {
            var command = new CalculateBetResultsCommand(match.Id, match.CompetitionId);
            var result = await mediator.Send(command, ct);
            if (result.IsSuccess) processedCount++;
        }

        return processedCount;
    }
}
```

---

### Task 6: Durable Functions

- [ ] **6.1** Add NuGet package to `ExtraTime.Functions.csproj`:
  ```xml
  <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.DurableTask" Version="1.2.2" />
  ```
- [ ] **6.2** Create `src/ExtraTime.Functions/RateLimitConfig.cs`
- [ ] **6.3** Create `src/ExtraTime.Functions/Orchestrators/SyncFootballDataOrchestrator.cs`
- [ ] **6.4** Create `src/ExtraTime.Functions/Activities/GetCompetitionIdsActivity.cs`
- [ ] **6.5** Create `src/ExtraTime.Functions/Activities/SyncCompetitionMatchesActivity.cs`
- [ ] **6.6** Create `src/ExtraTime.Functions/Activities/SyncCompetitionStandingsActivity.cs`
- [ ] **6.7** Create `src/ExtraTime.Functions/Activities/SyncCompetitionTeamsActivity.cs`
- [ ] **6.8** Create `src/ExtraTime.Functions/Triggers/SyncFootballDataTrigger.cs`
- [ ] **6.9** Delete `src/ExtraTime.Functions/Functions/SyncMatchesFunction.cs`
- [ ] **6.10** Modify `CalculateBetResultsFunction.cs` - Use `IBetResultsService`

#### RateLimitConfig
```csharp
namespace ExtraTime.Functions;

public static class RateLimitConfig
{
    public const int MaxCallsPerMinute = 10;
    public const int CompetitionsPerBatch = 8;
    public static readonly TimeSpan BatchWaitTime = TimeSpan.FromSeconds(65);
}
```

#### SyncFootballDataOrchestrator
```csharp
[Function(nameof(SyncFootballDataOrchestrator))]
public static async Task RunOrchestrator(
    [OrchestrationTrigger] TaskOrchestrationContext context)
{
    var logger = context.CreateReplaySafeLogger(nameof(SyncFootballDataOrchestrator));
    var currentHour = context.CurrentUtcDateTime.Hour;
    var is5AmSync = currentHour == 5;

    // Get all competition IDs
    var competitionIds = await context.CallActivityAsync<List<int>>(
        nameof(GetCompetitionIdsActivity), null);

    // PHASE 1: Sync Matches (always)
    logger.LogInformation("Phase 1: Syncing matches for {Count} competitions", competitionIds.Count);

    var matchResults = new List<MatchSyncResult>();
    var batches = competitionIds.Chunk(RateLimitConfig.CompetitionsPerBatch).ToList();

    for (var i = 0; i < batches.Count; i++)
    {
        var batch = batches[i];
        var tasks = batch.Select(id =>
            context.CallActivityAsync<MatchSyncResult>(nameof(SyncCompetitionMatchesActivity), id));

        var results = await Task.WhenAll(tasks);
        matchResults.AddRange(results);

        if (i < batches.Count - 1)
        {
            await context.CreateTimer(
                context.CurrentUtcDateTime.Add(RateLimitConfig.BatchWaitTime),
                CancellationToken.None);
        }
    }

    // PHASE 2: Sync Standings (conditional)
    var competitionsNeedingStandings = is5AmSync
        ? competitionIds  // Full sync at 5 AM
        : matchResults
            .Where(r => r.HasNewlyFinishedMatches)
            .Select(r => r.CompetitionExternalId)
            .ToList();

    if (competitionsNeedingStandings.Count > 0)
    {
        logger.LogInformation("Phase 2: Syncing standings for {Count} competitions",
            competitionsNeedingStandings.Count);

        // Wait for rate limit before standings
        await context.CreateTimer(
            context.CurrentUtcDateTime.Add(RateLimitConfig.BatchWaitTime),
            CancellationToken.None);

        var standingsResults = new List<StandingsSyncResult>();
        var standingsBatches = competitionsNeedingStandings.Chunk(RateLimitConfig.CompetitionsPerBatch).ToList();

        for (var i = 0; i < standingsBatches.Count; i++)
        {
            var batch = standingsBatches[i];
            var tasks = batch.Select(id =>
                context.CallActivityAsync<StandingsSyncResult>(nameof(SyncCompetitionStandingsActivity), id));

            var results = await Task.WhenAll(tasks);
            standingsResults.AddRange(results);

            if (i < standingsBatches.Count - 1)
            {
                await context.CreateTimer(
                    context.CurrentUtcDateTime.Add(RateLimitConfig.BatchWaitTime),
                    CancellationToken.None);
            }
        }

        // PHASE 3: Sync Teams (new season only)
        var newSeasonCompetitions = standingsResults
            .Where(r => r.NewSeasonDetected)
            .Select(r => r.CompetitionExternalId)
            .ToList();

        if (newSeasonCompetitions.Count > 0)
        {
            logger.LogInformation("Phase 3: New seasons detected for {Count} competitions, syncing teams",
                newSeasonCompetitions.Count);

            await context.CreateTimer(
                context.CurrentUtcDateTime.Add(RateLimitConfig.BatchWaitTime),
                CancellationToken.None);

            var teamsBatches = newSeasonCompetitions.Chunk(RateLimitConfig.CompetitionsPerBatch).ToList();
            for (var i = 0; i < teamsBatches.Count; i++)
            {
                var batch = teamsBatches[i];
                var tasks = batch.Select(id =>
                    context.CallActivityAsync(nameof(SyncCompetitionTeamsActivity), id));
                await Task.WhenAll(tasks);

                if (i < teamsBatches.Count - 1)
                {
                    await context.CreateTimer(
                        context.CurrentUtcDateTime.Add(RateLimitConfig.BatchWaitTime),
                        CancellationToken.None);
                }
            }
        }
    }

    logger.LogInformation("Football data sync completed");
}
```

#### SyncCompetitionMatchesActivity
```csharp
public sealed class SyncCompetitionMatchesActivity(
    IFootballSyncService syncService,
    ILogger<SyncCompetitionMatchesActivity> logger)
{
    [Function(nameof(SyncCompetitionMatchesActivity))]
    public async Task<MatchSyncResult> Run(
        [ActivityTrigger] int competitionExternalId,
        CancellationToken ct)
    {
        logger.LogInformation("Syncing matches for competition {Id}", competitionExternalId);
        return await syncService.SyncMatchesForCompetitionAsync(competitionExternalId, ct);
    }
}
```

#### SyncFootballDataTrigger
```csharp
public sealed class SyncFootballDataTrigger(ILogger<SyncFootballDataTrigger> logger)
{
    [Function("SyncFootballDataTrigger")]
    public async Task Run(
        [TimerTrigger("0 0 * * * *")] TimerInfo timerInfo,
        [DurableClient] DurableTaskClient client,
        CancellationToken ct)
    {
        logger.LogInformation("Starting football data sync at: {Time}", DateTime.UtcNow);

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(SyncFootballDataOrchestrator),
            cancellationToken: ct);

        logger.LogInformation("Started orchestration: {InstanceId}", instanceId);
    }
}
```

#### Updated CalculateBetResultsFunction
```csharp
public sealed class CalculateBetResultsFunction(
    IBetResultsService betResultsService,
    ILogger<CalculateBetResultsFunction> logger)
{
    [Function("CalculateBetResults")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *")] TimerInfo timerInfo,
        CancellationToken ct)
    {
        logger.LogInformation("CalculateBetResults started at: {Time}", DateTime.UtcNow);
        var count = await betResultsService.CalculateAllPendingBetResultsAsync(ct);
        logger.LogInformation("CalculateBetResults completed: {Count} matches processed", count);
    }
}
```

---

### Task 7: DevTriggers Updates

- [ ] **7.1** Modify `Program.cs` - Add `sync-standings` command
- [ ] **7.2** Modify `Program.cs` - Update `calculate-bets` to use `IBetResultsService`
- [ ] **7.3** Modify `AppHost/Program.cs` - Add `sync-standings` resource

#### Updated DevTriggers Program.cs
```csharp
case "sync-standings":
    await RunSyncStandingsAsync(scope.ServiceProvider, logger);
    break;

case "calculate-bets":
    await RunCalculateBetsAsync(scope.ServiceProvider, logger);
    break;

// ...

static async Task RunSyncStandingsAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Starting standings sync...");
    var syncService = services.GetRequiredService<IFootballSyncService>();
    var settings = services.GetRequiredService<IOptions<FootballDataSettings>>();

    foreach (var competitionId in settings.Value.SupportedCompetitionIds)
    {
        logger.LogInformation("Syncing standings for competition {Id}...", competitionId);
        var result = await syncService.SyncStandingsForCompetitionAsync(competitionId);

        if (result.NewSeasonDetected)
        {
            logger.LogInformation("New season detected! Syncing teams...");
            await syncService.SyncTeamsForCompetitionAsync(competitionId);
        }
    }
    logger.LogInformation("Standings sync completed");
}

static async Task RunCalculateBetsAsync(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Starting bet calculation...");
    var betResultsService = services.GetRequiredService<IBetResultsService>();
    var count = await betResultsService.CalculateAllPendingBetResultsAsync();
    logger.LogInformation("Processed {Count} matches", count);
}
```

---

### Task 8: API Endpoints (Optional)

- [ ] **8.1** Add `GET /api/football/competitions/{id}/standings` endpoint
- [ ] **8.2** Add `GET /api/football/competitions/{id}/seasons` endpoint

---

## File Summary

| Action | File |
|--------|------|
| **Create** | `Domain/Enums/StandingType.cs` |
| **Create** | `Domain/Entities/Season.cs` |
| **Create** | `Domain/Entities/SeasonTeam.cs` |
| **Create** | `Domain/Entities/FootballStanding.cs` |
| **Create** | `Application/Features/Football/DTOs/StandingsDtos.cs` |
| **Create** | `Application/Common/Interfaces/IBetResultsService.cs` |
| **Create** | `Infrastructure/Data/Configurations/SeasonConfiguration.cs` |
| **Create** | `Infrastructure/Data/Configurations/SeasonTeamConfiguration.cs` |
| **Create** | `Infrastructure/Data/Configurations/FootballStandingConfiguration.cs` |
| **Create** | `Infrastructure/Services/BetResultsService.cs` |
| **Create** | `Functions/RateLimitConfig.cs` |
| **Create** | `Functions/Orchestrators/SyncFootballDataOrchestrator.cs` |
| **Create** | `Functions/Activities/GetCompetitionIdsActivity.cs` |
| **Create** | `Functions/Activities/SyncCompetitionMatchesActivity.cs` |
| **Create** | `Functions/Activities/SyncCompetitionStandingsActivity.cs` |
| **Create** | `Functions/Activities/SyncCompetitionTeamsActivity.cs` |
| **Create** | `Functions/Triggers/SyncFootballDataTrigger.cs` |
| **Modify** | `Domain/Entities/Match.cs` |
| **Modify** | `Domain/Entities/Competition.cs` |
| **Modify** | `Domain/Entities/Team.cs` |
| **Modify** | `Application/Features/Football/DTOs/CompetitionDtos.cs` |
| **Modify** | `Application/Common/Interfaces/IFootballDataService.cs` |
| **Modify** | `Application/Common/Interfaces/IFootballSyncService.cs` |
| **Modify** | `Application/Common/Interfaces/IApplicationDbContext.cs` |
| **Modify** | `Infrastructure/Data/Configurations/MatchConfiguration.cs` |
| **Modify** | `Infrastructure/Data/ApplicationDbContext.cs` |
| **Modify** | `Infrastructure/Services/Football/FootballDataService.cs` |
| **Modify** | `Infrastructure/Services/Football/FootballSyncService.cs` |
| **Modify** | `Infrastructure/DependencyInjection.cs` |
| **Modify** | `Functions/ExtraTime.Functions.csproj` |
| **Modify** | `Functions/Functions/CalculateBetResultsFunction.cs` |
| **Modify** | `DevTriggers/Program.cs` |
| **Modify** | `AppHost/Program.cs` |
| **Delete** | `Functions/Functions/SyncMatchesFunction.cs` |

---

## Verification Checklist

### Domain & Data
- [ ] Season entity creates correctly with all fields
- [ ] FootballStanding entity creates and updates
- [ ] Migration applies without errors
- [ ] Existing data migrated to Seasons/SeasonTeams

### Smart Sync Logic
- [ ] `SyncMatchesForCompetitionAsync` correctly detects newly finished matches
- [ ] Standings only sync when `HasNewlyFinishedMatches = true` OR at 5 AM
- [ ] New season detection works from standings API response
- [ ] Teams sync triggers only on new season

### Durable Functions
- [ ] Orchestrator starts from timer trigger
- [ ] Batches execute with proper 65s delays
- [ ] CreateTimer doesn't cause billing during wait
- [ ] Orchestration completes successfully
- [ ] Monitor shows correct execution history

### Rate Limiting
- [ ] With 5 competitions: completes in < 2 minutes
- [ ] With 20 competitions: completes in < 5 minutes
- [ ] No 429 errors from Football-Data API

### CalculateBets Consistency
- [ ] Function uses `IBetResultsService`
- [ ] DevTriggers uses `IBetResultsService`
- [ ] Both produce same results

### DevTriggers
- [ ] `sync-matches` works
- [ ] `sync-standings` works
- [ ] `calculate-bets` works
- [ ] All visible in Aspire dashboard
