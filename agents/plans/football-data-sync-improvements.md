# Football Data Sync Improvements

## Context

Comparison with the football-data.org API v4 documentation revealed several gaps: missing match statuses causing live matches in extra time / penalties to be missed, N+1 query patterns in sync loops, `SaveChangesAsync` called per-iteration instead of batched, a diverged duplicate overload of `SyncTeamsForCompetitionAsync`, missing `CompetitionType` from the domain model, and a repetitive orchestrator with three copy-paste batch methods. The `FootballDataService` also uses manual HttpClient plumbing that Refit can replace, and exception handling misses `TaskCanceledException` (timeouts).

---

## Phase 1: Missing Match Statuses (Items 1 & 2)

No migration needed — MatchStatus is stored as string via `HasConversion<string>()`.

### Step 1.1 — Add enum values
**File:** `src/ExtraTime.Domain/Enums/MatchStatus.cs`

Append after `Cancelled = 7`:
```
ExtraTime = 8,
PenaltyShootout = 9,
Awarded = 10
```

### Step 1.2 — Update ParseMatchStatus
**File:** `src/ExtraTime.Infrastructure/Services/Football/FootballSyncService.cs` (line 550)

Add three new cases:
```
"EXTRA_TIME" => MatchStatus.ExtraTime,
"PENALTY_SHOOTOUT" => MatchStatus.PenaltyShootout,
"AWARDED" => MatchStatus.Awarded,
```

### Step 1.3 — Fix live match status filter
**File:** `src/ExtraTime.Infrastructure/Services/Football/FootballDataService.cs` (line 93)

Change to: `status=IN_PLAY,PAUSED,EXTRA_TIME,PENALTY_SHOOTOUT`

---

## Phase 2: Competition Type (Item 8)

### Step 2.1 — Create CompetitionType enum
**New file:** `src/ExtraTime.Domain/Enums/CompetitionType.cs`
Values: `League = 0, LeagueCup = 1, Cup = 2, Playoffs = 3`

### Step 2.2 — Add Type to Competition entity
**File:** `src/ExtraTime.Domain/Entities/Competition.cs`

- Add `public CompetitionType Type { get; private set; }` property
- Add `CompetitionType type = CompetitionType.League` parameter to `Create()` and `UpdateDetails()`

### Step 2.3 — Update CompetitionApiDto
**File:** `src/ExtraTime.Application/Features/Football/DTOs/CompetitionDtos.cs`

- Add `string? Type` to `CompetitionApiDto`
- Add `CompetitionType Type` to `CompetitionDto` and `CompetitionSummaryDto`

### Step 2.4 — Add ParseCompetitionType + wire into sync
**File:** `src/ExtraTime.Infrastructure/Services/Football/FootballSyncService.cs`

Add static parser method. Update `SyncCompetitionsAsync` to pass parsed type to `Create()` / `UpdateDetails()`.

### Step 2.5 — EF configuration
**File:** `src/ExtraTime.Infrastructure/Data/Configurations/CompetitionConfiguration.cs`

Add: `.HasConversion<string>().HasMaxLength(20).HasDefaultValue(CompetitionType.League)`

### Step 2.6 — Generate migration
`dotnet ef migrations add AddCompetitionType`

---

## Phase 3: Consolidate Duplicate Overloads (Item 3)

### Current callers
| Caller | Overload | Source |
|--------|----------|--------|
| `FootballSyncEndpoints.cs:39` | **Guid** | Admin API (`POST /api/admin/sync/teams/{competitionId:guid}`) |
| `FootballSyncHostedService.cs:51` | **Guid** | Hosted service (commented out in DI) |
| `SyncCompetitionTeamsActivity.cs:17` | **int** | Orchestrator |
| `DevTriggers/Program.cs:128,183,219` | **int** | Dev tooling |
| Tests | **Guid** | Unit tests |

### Step 3.1 — Make Guid overload delegate to int overload
**File:** `src/ExtraTime.Infrastructure/Services/Football/FootballSyncService.cs`

Replace the Guid overload body (lines 72-141) with:
```csharp
public async Task SyncTeamsForCompetitionAsync(Guid competitionId, CancellationToken ct = default)
{
    var competition = await context.Competitions
        .FirstOrDefaultAsync(c => c.Id == competitionId, ct);
    if (competition is null)
    {
        logger.LogWarning("Competition {CompetitionId} not found", competitionId);
        return;
    }
    await SyncTeamsForCompetitionAsync(competition.ExternalId, ct);
}
```

This ensures all team sync goes through the `SeasonTeams` path. The admin API endpoint keeps working with Guid.

### Step 3.2 — Update tests
**File:** `tests/ExtraTime.UnitTests/Infrastructure/Services/FootballSyncServiceTests.cs`

Update test to verify `SeasonTeams` are created (not `CompetitionTeams`).

---

## Phase 4: Performance Fixes (Items 4 & 5)

### Step 4.1 — Pre-fetch teams in ProcessMatchesAsync
**File:** `src/ExtraTime.Infrastructure/Services/Football/FootballSyncService.cs` (line 464)

Before the loop:
- Collect all team external IDs from `apiMatches` (home + away)
- `ToDictionaryAsync(t => t.ExternalId)` into `teamsDict`
- Collect all match external IDs, pre-fetch into `matchesDict`
- Use dictionary lookups in the loop instead of per-item queries

Converts **2N + N queries** to **3 queries** (teams, matches, season).

### Step 4.2 — Pre-fetch teams in SyncStandingsForCompetitionAsync
**File:** same file (line 344)

Before the standings loop:
- Collect all team external IDs from all `standingsResponse.Standings[].Table[].Team.Id`
- Pre-fetch into `teamsDict`
- Pre-fetch existing standings for the season into a lookup
- For teams not in dict (new from standings data): add to context AND to the dict, call `SaveChangesAsync` once after all new teams are added, before processing standing rows

### Step 4.3 — Pre-fetch teams in SyncTeamsForCompetitionAsync(int)
**File:** same file (line 144)

Before the loop:
- Collect external IDs from `apiTeams`
- Pre-fetch existing teams into dict
- Pre-fetch existing `SeasonTeams` for the season into a HashSet

### Step 4.4 — Remove SaveChangesAsync from loops
**File:** same file

- **int overload (line 190):** Remove `SaveChangesAsync` inside foreach. The one at line 210 handles the final save.
- **Guid overload:** Already replaced in Phase 3 — delegates to int overload.

---

## Phase 5: Refit API Client + Error Handling (Items 9 & 10)

### Step 5.1 — Add Refit package
**File:** `Directory.Packages.props` — add `<PackageVersion Include="Refit.HttpClientFactory" Version="8.0.0" />`
**File:** `src/ExtraTime.Infrastructure/ExtraTime.Infrastructure.csproj` — add `<PackageReference Include="Refit.HttpClientFactory" />`

### Step 5.2 — Create IFootballDataApi Refit interface
**New file:** `src/ExtraTime.Infrastructure/Services/Football/IFootballDataApi.cs`

```csharp
using Refit;

internal interface IFootballDataApi
{
    [Get("/competitions/{externalId}")]
    Task<CompetitionApiDto> GetCompetitionAsync(int externalId, CancellationToken ct = default);

    [Get("/competitions/{competitionExternalId}/teams")]
    Task<TeamsApiResponse> GetTeamsForCompetitionAsync(int competitionExternalId, CancellationToken ct = default);

    [Get("/competitions/{competitionExternalId}/matches")]
    Task<MatchesApiResponse> GetMatchesForCompetitionAsync(
        int competitionExternalId,
        [Query(Format = "yyyy-MM-dd")] DateTime? dateFrom = null,
        [Query(Format = "yyyy-MM-dd")] DateTime? dateTo = null,
        CancellationToken ct = default);

    [Get("/matches")]
    Task<MatchesApiResponse> GetMatchesAsync(
        [Query] string status,
        [Query] string competitions,
        CancellationToken ct = default);

    [Get("/competitions/{competitionExternalId}/standings")]
    Task<StandingsApiResponse> GetStandingsAsync(int competitionExternalId, CancellationToken ct = default);
}
```

`internal` — infrastructure detail only. Application-layer `IFootballDataService` remains the port.

### Step 5.3 — Refactor FootballDataService as adapter
**File:** `src/ExtraTime.Infrastructure/Services/Football/FootballDataService.cs`

- Constructor takes `IFootballDataApi` instead of `HttpClient`
- Each method wraps the Refit call with: `catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)`
- Returns `null` / `[]` on failure (preserves existing contract)
- Passes `DateTime?` directly to Refit (Refit handles formatting via `[Query(Format = "yyyy-MM-dd")]`)

### Step 5.4 — Update DI registration
**File:** `src/ExtraTime.Infrastructure/DependencyInjection.cs` (lines 112-121)

Replace:
```csharp
services.AddRefitClient<IFootballDataApi>()
    .ConfigureHttpClient((sp, client) =>
    {
        var s = configuration.GetSection(FootballDataSettings.SectionName).Get<FootballDataSettings>();
        client.BaseAddress = new Uri(s?.BaseUrl ?? "https://api.football-data.org/v4/");
        client.DefaultRequestHeaders.Add("X-Auth-Token", s?.ApiKey ?? string.Empty);
    })
    .AddHttpMessageHandler<RateLimitingHandler>();

services.AddScoped<IFootballDataService, FootballDataService>();
```

Standard resilience handler from ServiceDefaults applies automatically to all HttpClients (including Refit's). RateLimitingHandler is preserved.

---

## Phase 6: Orchestrator Batch Executor (Item 11)

### Step 6.1 — Extract generic ExecuteInBatchesAsync
**File:** `src/ExtraTime.Functions/Orchestrators/SyncFootballDataOrchestrator.cs`

Add two overloads:
```csharp
// For activities that return a result
private static async Task<List<TResult>> ExecuteInBatchesAsync<TResult>(
    TaskOrchestrationContext context, List<int> ids, string activityName)

// For activities with no return value
private static async Task ExecuteInBatchesAsync(
    TaskOrchestrationContext context, List<int> ids, string activityName)
```

Both follow the same pattern: chunk by `RateLimitConfig.CompetitionsPerBatch`, parallel execute, timer between batches.

### Step 6.2 — Replace three private methods
Delete `SyncMatchesForCompetitionsAsync`, `SyncStandingsForCompetitionsAsync`, `SyncTeamsForCompetitionsAsync`. Replace calls in `RunOrchestrator`:

```csharp
var matchResults = await ExecuteInBatchesAsync<MatchSyncResult>(
    context, competitionIds, nameof(Activities.SyncCompetitionMatchesActivity));

var standingsResults = await ExecuteInBatchesAsync<StandingsSyncResult>(
    context, competitionsNeedingStandings, nameof(Activities.SyncCompetitionStandingsActivity));

await ExecuteInBatchesAsync(
    context, competitionsWithNewSeasons, nameof(Activities.SyncCompetitionTeamsActivity));
```

---

## Tasks

### Phase 1 Tasks
- [x] **1.1** Add `ExtraTime = 8`, `PenaltyShootout = 9`, `Awarded = 10` to `MatchStatus` enum in `src/ExtraTime.Domain/Enums/MatchStatus.cs`
- [x] **1.2** Add `"EXTRA_TIME"`, `"PENALTY_SHOOTOUT"`, `"AWARDED"` cases to `ParseMatchStatus` switch in `FootballSyncService.cs:550`
- [x] **1.3** Change live match status filter in `FootballDataService.cs:93` from `IN_PLAY,PAUSED` to `IN_PLAY,PAUSED,EXTRA_TIME,PENALTY_SHOOTOUT`
- [x] **1.4** Build and run tests to verify no regressions

### Phase 2 Tasks
- [x] **2.1** Create `CompetitionType` enum file at `src/ExtraTime.Domain/Enums/CompetitionType.cs` with values `League = 0, LeagueCup = 1, Cup = 2, Playoffs = 3`
- [x] **2.2** Add `public CompetitionType Type { get; private set; }` to `Competition` entity; add `CompetitionType type = CompetitionType.League` param to `Create()` and `UpdateDetails()`
- [x] **2.3** Add `string? Type` to `CompetitionApiDto`; add `CompetitionType Type` to `CompetitionDto` and `CompetitionSummaryDto`
- [x] **2.4** Add `ParseCompetitionType` static method to `FootballSyncService`; wire into `SyncCompetitionsAsync` for both create and update paths
- [x] **2.5** Add `Type` property mapping in `CompetitionConfiguration.cs`: `.HasConversion<string>().HasMaxLength(20).HasDefaultValue(CompetitionType.League)`
- [x] **2.6** Generate EF migration: `dotnet ef migrations add AddCompetitionType`
- [x] **2.7** Build and run tests to verify

### Phase 3 Tasks
- [x] **3.1** Replace Guid overload of `SyncTeamsForCompetitionAsync` (lines 72-141) with delegation to int overload (lookup competition, call `SyncTeamsForCompetitionAsync(competition.ExternalId, ct)`)
- [x] **3.2** Update unit test `SyncTeamsForCompetitionAsync_NewTeams_AddsToDatabase` to verify `SeasonTeams` are created instead of `CompetitionTeams`
- [x] **3.3** Build and run tests

### Phase 4 Tasks
- [ ] **4.1** In `ProcessMatchesAsync`: before the loop, collect all team external IDs and pre-fetch into `Dictionary<int, Team>`; also pre-fetch existing matches by external ID into `Dictionary<int, Match>`; replace per-item queries with dict lookups
- [ ] **4.2** In `SyncStandingsForCompetitionAsync`: before standings loop, collect all team external IDs from all table rows, pre-fetch into `Dictionary<int, Team>`; pre-fetch existing standings for the season; for new teams, add to context + dict, then `SaveChangesAsync` once before processing standings rows
- [ ] **4.3** In `SyncTeamsForCompetitionAsync(int)`: pre-fetch existing teams by external ID into dict; pre-fetch existing `SeasonTeams` for the season into a `HashSet<(Guid, Guid)>`
- [ ] **4.4** Remove `SaveChangesAsync` at line 190 inside the foreach loop in int overload (line 210 handles the final save)
- [ ] **4.5** Build and run tests

### Phase 5 Tasks
- [ ] **5.1** Add `<PackageVersion Include="Refit.HttpClientFactory" Version="8.0.0" />` to `Directory.Packages.props` and `<PackageReference Include="Refit.HttpClientFactory" />` to `ExtraTime.Infrastructure.csproj`
- [ ] **5.2** Create `IFootballDataApi` Refit interface at `src/ExtraTime.Infrastructure/Services/Football/IFootballDataApi.cs` (internal, 5 methods matching API endpoints, `[Query(Format = "yyyy-MM-dd")]` for date params)
- [ ] **5.3** Refactor `FootballDataService`: replace `HttpClient` with `IFootballDataApi` in constructor; wrap each call with `catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)`; remove manual URL building and `ReadFromJsonAsync` calls
- [ ] **5.4** Update DI in `DependencyInjection.cs`: replace `AddHttpClient<IFootballDataService, FootballDataService>` with `AddRefitClient<IFootballDataApi>()` (keep `ConfigureHttpClient` for base URL + auth header + `AddHttpMessageHandler<RateLimitingHandler>`); add `services.AddScoped<IFootballDataService, FootballDataService>()`
- [ ] **5.5** Build and run tests; verify HttpClient still gets standard resilience from ServiceDefaults

### Phase 6 Tasks
- [ ] **6.1** Add generic `ExecuteInBatchesAsync<TResult>` method to `SyncFootballDataOrchestrator` (chunk, parallel execute, timer between batches); add non-generic `ExecuteInBatchesAsync` overload for void activities
- [ ] **6.2** Replace `SyncMatchesForCompetitionsAsync`, `SyncStandingsForCompetitionsAsync`, `SyncTeamsForCompetitionsAsync` with calls to the generic method; delete the three old methods
- [ ] **6.3** Build and run tests

---

## Verification

1. **Build:** `dotnet build` — zero errors
2. **Tests:** `dotnet test` — all pass
3. **Migration:** Apply to test DB, verify `Competitions.Type` column exists with default `League`
4. **DevTriggers smoke test:** `sync-all` — verify full pipeline works, CompetitionType populated
5. **Spot check DB:** Confirm no `CompetitionTeams` rows created by new syncs (only `SeasonTeams`)

---

## Files Modified

| File | Changes |
|------|---------|
| `src/ExtraTime.Domain/Enums/MatchStatus.cs` | Add 3 enum values |
| `src/ExtraTime.Domain/Enums/CompetitionType.cs` | **New file** |
| `src/ExtraTime.Domain/Entities/Competition.cs` | Add Type property, update Create/UpdateDetails |
| `src/ExtraTime.Application/Features/Football/DTOs/CompetitionDtos.cs` | Add Type to DTOs |
| `src/ExtraTime.Infrastructure/Services/Football/FootballSyncService.cs` | ParseMatchStatus, ParseCompetitionType, overload consolidation, N+1 fixes, SaveChanges fix |
| `src/ExtraTime.Infrastructure/Services/Football/FootballDataService.cs` | Refit adapter, broader exception handling, live status fix |
| `src/ExtraTime.Infrastructure/Services/Football/IFootballDataApi.cs` | **New file** — Refit interface |
| `src/ExtraTime.Infrastructure/DependencyInjection.cs` | Refit registration |
| `src/ExtraTime.Infrastructure/ExtraTime.Infrastructure.csproj` | Add Refit package |
| `Directory.Packages.props` | Add Refit version |
| `src/ExtraTime.Infrastructure/Data/Configurations/CompetitionConfiguration.cs` | Add Type mapping |
| `src/ExtraTime.Functions/Orchestrators/SyncFootballDataOrchestrator.cs` | Extract generic batch executor |
| `tests/ExtraTime.UnitTests/Infrastructure/Services/FootballSyncServiceTests.cs` | Update for SeasonTeams |
| New migration file | AddCompetitionType |
