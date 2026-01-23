# Phase 3: Football Data Integration - Detailed Implementation Plan

## Overview
Fetch and store football match data from Football-Data.org API. This phase adds Competition, Team, and Match entities with synchronization services and public API endpoints.

**Key Constraints:**
- Football-Data.org free tier: 10 requests/minute
- Rate limiting required in HTTP client
- Data sync integrates with existing BackgroundJob tracking

---

## Part 1: Domain Layer

### 1.1 New Enum

**`src/ExtraTime.Domain/Enums/MatchStatus.cs`**
```csharp
namespace ExtraTime.Domain.Enums;

public enum MatchStatus
{
    Scheduled = 0,
    Timed = 1,
    InPlay = 2,
    Paused = 3,
    Finished = 4,
    Postponed = 5,
    Suspended = 6,
    Cancelled = 7
}
```

### 1.2 New Entities

**`src/ExtraTime.Domain/Entities/Competition.cs`**
```csharp
public sealed class Competition : BaseEntity
{
    public required int ExternalId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string Country { get; set; }
    public string? LogoUrl { get; set; }
    public int? CurrentMatchday { get; set; }
    public DateTime? CurrentSeasonStart { get; set; }
    public DateTime? CurrentSeasonEnd { get; set; }
    public DateTime LastSyncedAt { get; set; }

    public ICollection<CompetitionTeam> CompetitionTeams { get; set; } = [];
    public ICollection<Match> Matches { get; set; } = [];
}
```

**`src/ExtraTime.Domain/Entities/Team.cs`**
```csharp
public sealed class Team : BaseEntity
{
    public required int ExternalId { get; set; }
    public required string Name { get; set; }
    public required string ShortName { get; set; }
    public string? Tla { get; set; }  // Three Letter Abbreviation
    public string? LogoUrl { get; set; }
    public string? ClubColors { get; set; }  // e.g., "Red / White"
    public string? Venue { get; set; }
    public DateTime LastSyncedAt { get; set; }

    public ICollection<CompetitionTeam> CompetitionTeams { get; set; } = [];
    public ICollection<Match> HomeMatches { get; set; } = [];
    public ICollection<Match> AwayMatches { get; set; } = [];
}
```

**`src/ExtraTime.Domain/Entities/CompetitionTeam.cs`** (Many-to-many join table)
```csharp
public sealed class CompetitionTeam : BaseEntity
{
    public Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public int Season { get; set; }  // e.g., 2025 for 2025-2026 season
}
```

**`src/ExtraTime.Domain/Entities/Match.cs`**
```csharp
public sealed class Match : BaseEntity
{
    public required int ExternalId { get; set; }

    public Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public Guid HomeTeamId { get; set; }
    public Team HomeTeam { get; set; } = null!;

    public Guid AwayTeamId { get; set; }
    public Team AwayTeam { get; set; } = null!;

    public required DateTime MatchDateUtc { get; set; }
    public required MatchStatus Status { get; set; }
    public int? Matchday { get; set; }
    public string? Stage { get; set; }
    public string? Group { get; set; }

    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public int? HomeHalfTimeScore { get; set; }
    public int? AwayHalfTimeScore { get; set; }

    public string? Venue { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
```

---

## Part 2: Application Layer

### 2.1 Interfaces

**`src/ExtraTime.Application/Common/Interfaces/IFootballDataService.cs`**
```csharp
public interface IFootballDataService
{
    Task<CompetitionApiDto?> GetCompetitionAsync(int externalId, CancellationToken ct = default);
    Task<IReadOnlyList<TeamApiDto>> GetTeamsForCompetitionAsync(int competitionExternalId, CancellationToken ct = default);
    Task<IReadOnlyList<MatchApiDto>> GetMatchesForCompetitionAsync(int competitionExternalId, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default);
    Task<IReadOnlyList<MatchApiDto>> GetLiveMatchesAsync(CancellationToken ct = default);
}
```

**`src/ExtraTime.Application/Common/Interfaces/IFootballSyncService.cs`**
```csharp
public interface IFootballSyncService
{
    Task SyncCompetitionsAsync(CancellationToken ct = default);
    Task SyncTeamsForCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task SyncMatchesAsync(DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default);
    Task SyncLiveMatchResultsAsync(CancellationToken ct = default);
}
```

**Update `IApplicationDbContext.cs`** - Add:
```csharp
DbSet<Competition> Competitions { get; }
DbSet<Team> Teams { get; }
DbSet<CompetitionTeam> CompetitionTeams { get; }
DbSet<Match> Matches { get; }
```

### 2.2 DTOs

**`src/ExtraTime.Application/Features/Football/DTOs/`**
- `CompetitionDtos.cs` - API DTOs (from Football-Data.org) + Application DTOs (for our API)
- `TeamDtos.cs` - TeamApiDto (with ClubColors), TeamDto, TeamSummaryDto
- `MatchDtos.cs` - MatchApiDto, MatchDto, MatchDetailDto, MatchesPagedResponse, MatchesFilterRequest

### 2.3 Queries

| Query | Handler | Description |
|-------|---------|-------------|
| GetCompetitionsQuery | Returns list of all competitions | No filters needed |
| GetMatchesQuery | Paginated, filterable by competition/date/status | Follow GetJobsQuery pattern |
| GetMatchByIdQuery | Returns MatchDetailDto or 404 | Single match lookup |

### 2.4 Errors

**`src/ExtraTime.Application/Features/Football/FootballErrors.cs`**
```csharp
public static class FootballErrors
{
    public const string MatchNotFound = "Match not found";
    public const string CompetitionNotFound = "Competition not found";
    public const string SyncFailed = "Football data synchronization failed";
}
```

---

## Part 3: Infrastructure Layer

### 3.1 Configuration

**`src/ExtraTime.Infrastructure/Configuration/FootballDataSettings.cs`**
```csharp
public sealed class FootballDataSettings
{
    public const string SectionName = "FootballData";

    public required string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.football-data.org/v4";
    public int RequestsPerMinute { get; set; } = 10;
    public int[] SupportedCompetitionIds { get; set; } = [2021, 2014, 2002, 2019, 2015];
}
```

**Competition IDs:** 2021=Premier League, 2014=La Liga, 2002=Bundesliga, 2019=Serie A, 2015=Ligue 1

### 3.2 Services

| Service | File | Purpose |
|---------|------|---------|
| RateLimitingHandler | `Services/Football/RateLimitingHandler.cs` | Limits to 10 req/min |
| FootballDataService | `Services/Football/FootballDataService.cs` | HTTP client for Football-Data.org |
| FootballSyncService | `Services/Football/FootballSyncService.cs` | Maps API data to domain entities |
| FootballSyncHostedService | `Services/Football/FootballSyncHostedService.cs` | Background sync for local dev |

### 3.3 EF Core Configurations

| File | Entity | Key Features |
|------|--------|--------------|
| CompetitionConfiguration.cs | Competition | Unique ExternalId index |
| TeamConfiguration.cs | Team | Unique ExternalId index |
| CompetitionTeamConfiguration.cs | CompetitionTeam | Composite unique index (CompetitionId, TeamId, Season) |
| MatchConfiguration.cs | Match | ExternalId unique, indexes on MatchDateUtc/Status |

### 3.4 DependencyInjection Updates

```csharp
// Add to DependencyInjection.cs
services.Configure<FootballDataSettings>(configuration.GetSection(FootballDataSettings.SectionName));
services.AddTransient<RateLimitingHandler>();
services.AddHttpClient<IFootballDataService, FootballDataService>(...)
    .AddHttpMessageHandler<RateLimitingHandler>();
services.AddScoped<IFootballSyncService, FootballSyncService>();
services.AddHostedService<FootballSyncHostedService>();  // For local dev
```

---

## Part 4: API Layer

### 4.1 Public Endpoints

**`src/ExtraTime.API/Features/Football/FootballEndpoints.cs`**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/competitions` | No | List all competitions |
| GET | `/api/matches` | No | Paginated matches with filters |
| GET | `/api/matches/{id}` | No | Match details |

**Query Parameters for `/api/matches`:**
- `competitionId` (Guid, optional)
- `dateFrom` (DateTime, optional)
- `dateTo` (DateTime, optional)
- `status` (string, optional)
- `page` (int, default 1)
- `pageSize` (int, default 20)

### 4.2 Admin Sync Endpoints

**`src/ExtraTime.API/Features/Admin/FootballSyncEndpoints.cs`**

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/admin/sync/competitions` | Trigger competition sync |
| POST | `/api/admin/sync/teams/{competitionId}` | Trigger team sync |
| POST | `/api/admin/sync/matches` | Trigger match sync |
| POST | `/api/admin/sync/live` | Trigger live match sync |

All admin endpoints require `AdminOnly` authorization.

### 4.3 Configuration

**Update `appsettings.json`:**
```json
{
  "FootballData": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "BaseUrl": "https://api.football-data.org/v4",
    "RequestsPerMinute": 10,
    "SupportedCompetitionIds": [2021, 2014, 2002, 2019, 2015]
  }
}
```

---

## Part 5: Background Sync (HostedService)

For local development, use a BackgroundService that:
1. Runs initial full sync on startup
2. Daily sync for upcoming matches (next 14 days)
3. Live sync every 5 minutes during match hours (10:00-23:00 UTC)

**Note:** Azure Functions will be added in Phase 8 (Deployment) for production.

---

## Implementation Order

### Step 1: Domain Layer
- [x] Create MatchStatus enum
- [x] Create Competition entity
- [x] Create Team entity
- [x] Create CompetitionTeam entity
- [x] Create Match entity

### Step 2: Application Interfaces & DTOs
- [x] Create IFootballDataService interface
- [x] Create IFootballSyncService interface
- [x] Update IApplicationDbContext with new DbSets
- [x] Create CompetitionDtos.cs
- [x] Create TeamDtos.cs
- [x] Create MatchDtos.cs
- [x] Create FootballErrors.cs

### Step 3: Application Queries
- [x] Create GetCompetitionsQuery + Handler
- [x] Create GetMatchesQuery + Handler (with pagination)
- [x] Create GetMatchByIdQuery + Handler

### Step 4: Infrastructure Configuration
- [x] Create FootballDataSettings.cs
- [x] Update appsettings.json

### Step 5: Infrastructure EF Core
- [x] Create CompetitionConfiguration.cs
- [x] Create TeamConfiguration.cs
- [x] Create CompetitionTeamConfiguration.cs
- [x] Create MatchConfiguration.cs
- [x] Update ApplicationDbContext with new DbSets

### Step 6: Infrastructure Services
- [x] Create RateLimitingHandler.cs
- [x] Create FootballDataService.cs
- [x] Create FootballSyncService.cs
- [x] Create FootballSyncHostedService.cs
- [x] Update DependencyInjection.cs

### Step 7: API Layer
- [x] Create FootballEndpoints.cs
- [x] Create FootballSyncEndpoints.cs
- [x] Update Program.cs with endpoint mappings

### Step 8: Database Migration
- [x] Create migration: `dotnet ef migrations add AddFootballEntities`
- [ ] Apply migration: `dotnet ef database update`

---

## Verification Steps

### Build & Migration
```powershell
dotnet build
dotnet ef migrations add AddFootballEntities --project src/ExtraTime.Infrastructure --startup-project src/ExtraTime.API
dotnet ef database update --project src/ExtraTime.Infrastructure --startup-project src/ExtraTime.API
```

### API Testing (Swagger)
1. Start API: `dotnet run --project src/ExtraTime.API`
2. Admin sync (requires admin JWT):
   - POST `/api/admin/sync/competitions` → 202 Accepted
   - POST `/api/admin/sync/teams/{competitionId}` → 202 Accepted
   - POST `/api/admin/sync/matches` → 202 Accepted
3. Public endpoints:
   - GET `/api/competitions` → Returns competition list
   - GET `/api/matches?page=1&pageSize=10` → Returns paginated matches
   - GET `/api/matches/{id}` → Returns match details or 404

### Rate Limiting Verification
- Monitor logs for "Rate limit reached, waiting..." messages
- Verify no 429 errors from Football-Data.org

---

## Critical Files Summary

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `Entities/Match.cs` | Core match entity with scores/status |
| Application | `Interfaces/IFootballDataService.cs` | API client contract |
| Application | `Features/Football/Queries/GetMatches/*` | Pagination pattern |
| Infrastructure | `Services/Football/FootballDataService.cs` | HTTP client implementation |
| Infrastructure | `Services/Football/FootballSyncService.cs` | Data sync logic |
| API | `Features/Football/FootballEndpoints.cs` | Public endpoints |

---

## Scope Decisions

- **Frontend**: Backend only for Phase 3. Matches UI deferred to Phase 6 (Frontend Polish).
- **Background Sync**: HostedService for local dev. Azure Functions added in Phase 8 (Deployment).

