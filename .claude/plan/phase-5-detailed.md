# Phase 5: Betting System - Detailed Implementation Plan

## Overview
Users can place, edit, and delete bets on football matches within their leagues. Bets are scored based on league-specific rules when matches complete. Leaderboards track member standings with points, streaks, and statistics. Following Clean Architecture patterns established in previous phases.

**Scope:** Backend only. Frontend UI deferred to Phase 6.

---

## Domain Layer

### Entities

#### Bet (`src/ExtraTime.Domain/Entities/Bet.cs`)
```csharp
public sealed class Bet : BaseAuditableEntity
{
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    // Prediction
    public int PredictedHomeScore { get; set; }
    public int PredictedAwayScore { get; set; }

    // Timestamps
    public DateTime PlacedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }

    // Navigation
    public BetResult? Result { get; set; }
}
```

#### BetResult (`src/ExtraTime.Domain/Entities/BetResult.cs`)
```csharp
public sealed class BetResult
{
    public Guid BetId { get; set; }  // Primary key (one-to-one with Bet)
    public Bet Bet { get; set; } = null!;

    // Scoring
    public int PointsEarned { get; set; }
    public bool IsExactMatch { get; set; }
    public bool IsCorrectResult { get; set; }

    // Calculation metadata
    public DateTime CalculatedAt { get; set; }
}
```

**Note:** `BetResult` uses `BetId` as the primary key (not `BaseEntity.Id`) since it has a one-to-one relationship with `Bet` and cannot exist independently.

#### LeagueStanding (`src/ExtraTime.Domain/Entities/LeagueStanding.cs`)
```csharp
public sealed class LeagueStanding : BaseEntity
{
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Core Stats
    public int TotalPoints { get; set; }
    public int BetsPlaced { get; set; }
    public int ExactMatches { get; set; }
    public int CorrectResults { get; set; }

    // Streak Tracking
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    // Metadata
    public DateTime LastUpdatedAt { get; set; }
}
```

---

## API Endpoints

| Method | Path | Description | Auth | Authorization | Response Codes |
|--------|------|-------------|------|---------------|----------------|
| POST | `/api/leagues/{leagueId}/bets` | Place or update bet | Yes | League member, before deadline | 201 (new), 200 (update), 400, 403 |
| DELETE | `/api/leagues/{leagueId}/bets/{betId}` | Delete bet | Yes | Bet owner, before deadline | 204, 400, 403, 404 |
| GET | `/api/leagues/{leagueId}/bets/my` | Get user's bets in league | Yes | League member | 200, 403 |
| GET | `/api/leagues/{leagueId}/matches/{matchId}/bets` | Get all bets for match (after deadline) | Yes | League member, after deadline passes | 200, 403, 404 |
| GET | `/api/leagues/{leagueId}/standings` | Get league leaderboard | Yes | League member | 200, 403 |
| GET | `/api/leagues/{leagueId}/users/{userId}/stats` | Get user stats in league | Yes | League member | 200, 403, 404 |

---

## Application Layer Structure

```
Features/Bets/
├── BetErrors.cs
├── DTOs/BetDtos.cs
├── Commands/
│   ├── PlaceBet/
│   │   ├── PlaceBetCommand.cs
│   │   ├── PlaceBetCommandHandler.cs
│   │   └── PlaceBetCommandValidator.cs
│   ├── DeleteBet/
│   │   ├── DeleteBetCommand.cs
│   │   └── DeleteBetCommandHandler.cs
│   ├── CalculateBetResults/
│   │   ├── CalculateBetResultsCommand.cs
│   │   └── CalculateBetResultsCommandHandler.cs
│   └── RecalculateLeagueStandings/
│       ├── RecalculateLeagueStandingsCommand.cs
│       └── RecalculateLeagueStandingsCommandHandler.cs
└── Queries/
    ├── GetMyBets/
    │   ├── GetMyBetsQuery.cs
    │   └── GetMyBetsQueryHandler.cs
    ├── GetMatchBets/
    │   ├── GetMatchBetsQuery.cs
    │   └── GetMatchBetsQueryHandler.cs
    ├── GetLeagueStandings/
    │   ├── GetLeagueStandingsQuery.cs
    │   └── GetLeagueStandingsQueryHandler.cs
    └── GetUserStats/
        ├── GetUserStatsQuery.cs
        └── GetUserStatsQueryHandler.cs
```

---

## Interfaces to Add

### IBetCalculator (`src/ExtraTime.Application/Common/Interfaces/IBetCalculator.cs`)
```csharp
public interface IBetCalculator
{
    BetResultDto CalculateResult(Bet bet, Match match, League league);
}
```

**Purpose:** Calculate points earned based on league scoring rules

### IStandingsCalculator (`src/ExtraTime.Application/Common/Interfaces/IStandingsCalculator.cs`)
```csharp
public interface IStandingsCalculator
{
    Task RecalculateLeagueStandingsAsync(Guid leagueId, CancellationToken cancellationToken = default);
}
```

**Purpose:** Recalculate league standings after bet results are calculated

---

## Infrastructure Services

### BetCalculator (`src/ExtraTime.Infrastructure/Services/BetCalculator.cs`)
```csharp
public sealed class BetCalculator : IBetCalculator
{
    public BetResultDto CalculateResult(Bet bet, Match match, League league)
    {
        // Check if match has final score
        if (!match.HomeScore.HasValue || !match.AwayScore.HasValue)
        {
            return new BetResultDto(0, false, false);
        }

        var actualHome = match.HomeScore.Value;
        var actualAway = match.AwayScore.Value;
        var predictedHome = bet.PredictedHomeScore;
        var predictedAway = bet.PredictedAwayScore;

        // Check exact match
        var isExactMatch = predictedHome == actualHome && predictedAway == actualAway;
        if (isExactMatch)
        {
            return new BetResultDto(league.ScoreExactMatch, true, true);
        }

        // Check correct result (win/draw/loss)
        var actualResult = GetMatchResult(actualHome, actualAway);
        var predictedResult = GetMatchResult(predictedHome, predictedAway);
        var isCorrectResult = actualResult == predictedResult;

        var points = isCorrectResult ? league.ScoreCorrectResult : 0;
        return new BetResultDto(points, false, isCorrectResult);
    }

    private static MatchResult GetMatchResult(int homeScore, int awayScore)
        => homeScore > awayScore ? MatchResult.HomeWin
         : homeScore < awayScore ? MatchResult.AwayWin
         : MatchResult.Draw;

    private enum MatchResult { HomeWin, Draw, AwayWin }
}
```

**Note:** `BetCalculator` uses `BetResultDto` from `ExtraTime.Application.Features.Bets.DTOs` (defined in Application DTOs section below). Do not define a separate DTO in Infrastructure.

### StandingsCalculator (`src/ExtraTime.Infrastructure/Services/StandingsCalculator.cs`)
```csharp
public sealed class StandingsCalculator(IApplicationDbContext context) : IStandingsCalculator
{
    public async Task RecalculateLeagueStandingsAsync(
        Guid leagueId,
        CancellationToken cancellationToken = default)
    {
        // Get all bets with results for this league
        var betsWithResults = await context.Bets
            .Include(b => b.Result)
            .Include(b => b.Match)
            .Where(b => b.LeagueId == leagueId && b.Result != null)
            .OrderBy(b => b.Match.MatchDateUtc)
            .ToListAsync(cancellationToken);

        // Group by user
        var userBets = betsWithResults.GroupBy(b => b.UserId);

        foreach (var userGroup in userBets)
        {
            var userId = userGroup.Key;
            var bets = userGroup.OrderBy(b => b.Match.MatchDateUtc).ToList();

            // Calculate totals
            var totalPoints = bets.Sum(b => b.Result!.PointsEarned);
            var betsPlaced = bets.Count;
            var exactMatches = bets.Count(b => b.Result!.IsExactMatch);
            var correctResults = bets.Count(b => b.Result!.IsCorrectResult);

            // Calculate streaks
            var (currentStreak, bestStreak) = CalculateStreaks(bets);

            // Upsert standing
            var standing = await context.LeagueStandings
                .FirstOrDefaultAsync(s => s.LeagueId == leagueId && s.UserId == userId, cancellationToken);

            if (standing == null)
            {
                standing = new LeagueStanding
                {
                    LeagueId = leagueId,
                    UserId = userId
                };
                context.LeagueStandings.Add(standing);
            }

            standing.TotalPoints = totalPoints;
            standing.BetsPlaced = betsPlaced;
            standing.ExactMatches = exactMatches;
            standing.CorrectResults = correctResults;
            standing.CurrentStreak = currentStreak;
            standing.BestStreak = bestStreak;
            standing.LastUpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    // PRECONDITION: All bets in the list must have non-null Result property
    // (ensured by filtering with 'b.Result != null' in the calling method)
    private static (int CurrentStreak, int BestStreak) CalculateStreaks(List<Bet> bets)
    {
        var currentStreak = 0;
        var bestStreak = 0;
        var tempStreak = 0;

        foreach (var bet in bets)
        {
            // Null-forgiving operator safe due to precondition
            if (bet.Result!.IsCorrectResult)
            {
                tempStreak++;
                bestStreak = Math.Max(bestStreak, tempStreak);
            }
            else
            {
                tempStreak = 0;
            }
        }

        // Current streak = most recent consecutive correct results
        for (var i = bets.Count - 1; i >= 0; i--)
        {
            if (bets[i].Result!.IsCorrectResult)
            {
                currentStreak++;
            }
            else
            {
                break;
            }
        }

        return (currentStreak, bestStreak);
    }
}
```

---

## Background Jobs

### BetCalculationJob
**Trigger:** When match status changes to `Finished` (via Football-Data.org sync)

**Job Type:** `"CalculateBetResults"`

**Payload:**
```json
{
  "matchId": "guid",
  "competitionId": "guid"
}
```

**Workflow:**
1. Fetch match with final score
2. Find all bets for this match across all leagues
3. For each bet:
   - Calculate result using `IBetCalculator`
   - Create or update `BetResult` entity
4. Determine affected leagues by collecting distinct `LeagueId` values from the bets processed in step 2-3
5. Enqueue a `RecalculateLeagueStandingsJob` with payload `{ "leagueIds": [...] }` for those leagues (executed as a separate background job)
6. Update `CalculateBetResults` job status to `Completed` once bet results are persisted and the standings job is enqueued

**Handler:** `CalculateBetResultsCommandHandler`

### RecalculateLeagueStandingsJob
**Trigger:** After bet results are calculated for a match

**Job Type:** `"RecalculateLeagueStandings"`

**Payload:**
```json
{
  "leagueIds": ["guid1", "guid2", "guid3"]
}
```

**Workflow:**
1. For each league ID:
   - Call `IStandingsCalculator.RecalculateLeagueStandingsAsync()`
   - Recalculate all user standings
   - Update streaks
2. Update job status to `Completed`

**Handler:** `RecalculateLeagueStandingsCommandHandler`

---

## Application DTOs

### Request DTOs

**PlaceBetRequest:**
```csharp
public sealed record PlaceBetRequest(
    Guid MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore
);
```

### Response DTOs

**BetDto:**
```csharp
public sealed record BetDto(
    Guid Id,
    Guid LeagueId,
    Guid UserId,
    Guid MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore,
    DateTime PlacedAt,
    DateTime? LastUpdatedAt,
    BetResultDto? Result
);
```

**BetResultDto:**
```csharp
public sealed record BetResultDto(
    int PointsEarned,
    bool IsExactMatch,
    bool IsCorrectResult
);
```

**MyBetDto:** (includes match info)
```csharp
public sealed record MyBetDto(
    Guid BetId,
    Guid MatchId,
    string HomeTeamName,
    string AwayTeamName,
    DateTime MatchDateUtc,
    MatchStatus MatchStatus,
    int? ActualHomeScore,
    int? ActualAwayScore,
    int PredictedHomeScore,
    int PredictedAwayScore,
    BetResultDto? Result,
    DateTime PlacedAt
);
```

**MatchBetDto:** (for viewing other users' bets after deadline)
```csharp
public sealed record MatchBetDto(
    Guid UserId,
    string Username,
    int PredictedHomeScore,
    int PredictedAwayScore,
    BetResultDto? Result
);
```

**LeagueStandingDto:**
```csharp
public sealed record LeagueStandingDto(
    Guid UserId,
    string Username,
    string Email,
    int Rank,
    int TotalPoints,
    int BetsPlaced,
    int ExactMatches,
    int CorrectResults,
    int CurrentStreak,
    int BestStreak,
    DateTime LastUpdatedAt
);
```

**UserStatsDto:**
```csharp
public sealed record UserStatsDto(
    Guid UserId,
    string Username,
    int TotalPoints,
    int BetsPlaced,
    int ExactMatches,
    int CorrectResults,
    int CurrentStreak,
    int BestStreak,
    double AccuracyPercentage,  // CorrectResults / BetsPlaced * 100 (CorrectResults already includes exact matches)
    int Rank,
    DateTime LastUpdatedAt
);
```

---

## Validation Rules

### PlaceBet
- **LeagueId:** Must exist and user must be a member
- **MatchId:** Must exist
- **Match Competition:** Must be in league's `AllowedCompetitionIds` (if set)
  - Note: `AllowedCompetitionIds` is stored as a JSON string in the `League` entity
  - Implementation must deserialize the JSON array to check if match's `CompetitionId` is in the allowed list
  - If `AllowedCompetitionIds` is null, all competitions are allowed
- **Match Deadline:** Current time must be before `match.MatchDateUtc - league.BettingDeadlineMinutes`
- **Match Status:** Must be `Scheduled` or `Timed`
- **PredictedHomeScore:** Must be >= 0 and <= 99
- **PredictedAwayScore:** Must be >= 0 and <= 99

### DeleteBet
- **BetId:** Must exist
- **Ownership:** User must own the bet
- **Match Deadline:** Current time must be before betting deadline
- **Match Status:** Must be `Scheduled` or `Timed` (not started)

---

## Business Rules

### Bet Placement
1. **League Membership Required:**
   - User must be a member of the league
   - Non-members cannot place bets

2. **Competition Restrictions:**
   - If league has `AllowedCompetitionIds` set, match must be in that list
   - If `AllowedCompetitionIds` is null, all competitions allowed

3. **Deadline Enforcement:**
   - Betting deadline = `match.MatchDateUtc - league.BettingDeadlineMinutes`
   - Bets can only be placed/edited/deleted before deadline
   - After deadline, bets are locked

4. **Upsert Behavior:**
   - If user has existing bet for match in league, update it
   - Otherwise, create new bet
   - Track both `PlacedAt` (first bet) and `LastUpdatedAt` (last edit):
     - On initial bet placement: set `PlacedAt` to current time and leave `LastUpdatedAt` as `null`
     - On subsequent bet updates: do not change `PlacedAt`; set `LastUpdatedAt` to current time

5. **Match Status Check:**
   - Only allow bets on matches with status `Scheduled` or `Timed`
   - No bets on `InPlay`, `Paused`, `Finished`, `Postponed`, `Cancelled`, or `Suspended`

### Bet Visibility
1. **Before Deadline:**
   - Users can only see their own bets
   - Other users' bets are hidden

2. **After Deadline:**
   - All league members can view all bets for a match
   - Bets revealed via `GET /api/leagues/{leagueId}/matches/{matchId}/bets`

3. **My Bets View:**
   - Users can always view all their own bets in a league
   - Shows past, present, and future bets

### Bet Calculation
1. **Automatic Trigger:**
   - Triggered when match status changes to `Finished`
   - Runs as background job via `CalculateBetResultsCommand`

2. **Scoring Logic:**
   - **Exact Match:** Predicted score exactly matches actual score
     - Points = `league.ScoreExactMatch` (default 3)
   - **Correct Result:** Predicted result (win/draw/loss) matches actual
     - Points = `league.ScoreCorrectResult` (default 1)
   - **Wrong:** Neither exact nor correct result
     - Points = 0

3. **Result Storage:**
   - Create `BetResult` entity for each bet
   - Store `PointsEarned`, `IsExactMatch`, `IsCorrectResult`, `CalculatedAt`

### Leaderboard Calculation
1. **Trigger:**
   - Runs after bet results are calculated
   - Background job via `RecalculateLeagueStandingsCommand`

2. **Standings Update:**
   - Recalculate all user stats in affected leagues
   - Update `LeagueStanding` entity (cached leaderboard)

3. **Streak Rules:**
   - **Current Streak:** Most recent consecutive correct results on placed bets
   - **Best Streak:** Longest ever consecutive correct results on placed bets
   - Streaks only count correct results (exact match or correct result)
   - Missing a bet does not affect the streak; only placed bets are considered
   - Wrong prediction breaks the streak

4. **Ranking:**
   - Primary sort: `TotalPoints` descending
   - Tiebreaker 1: `ExactMatches` descending
   - Tiebreaker 2: `BetsPlaced` ascending (fewer bets = better)
   - Tiebreaker 3: `UserId` ascending (stable sort)

---

## Error Messages

### BetErrors.cs
```csharp
public static class BetErrors
{
    public const string NotALeagueMember = "You must be a member of this league to place bets";
    public const string LeagueNotFound = "League not found";
    public const string MatchNotFound = "Match not found";
    public const string BetNotFound = "Bet not found";
    public const string DeadlinePassed = "Betting deadline has passed for this match";
    public const string MatchAlreadyStarted = "Match has already started";
    public const string MatchNotAllowed = "This match is not allowed in this league";
    public const string InvalidScore = "Score predictions must be between 0 and 99";
    public const string NotBetOwner = "You can only modify your own bets";
    public const string StandingsNotFound = "Standings not found for this league";
}
```

---

## Infrastructure EF Core Configurations

### BetConfiguration.cs
```csharp
public sealed class BetConfiguration : IEntityTypeConfiguration<Bet>
{
    public void Configure(EntityTypeBuilder<Bet> builder)
    {
        builder.ToTable("bets");

        builder.HasOne(b => b.League)
            .WithMany()
            .HasForeignKey(b => b.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Match)
            .WithMany()
            .HasForeignKey(b => b.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one bet per user per match per league
        builder.HasIndex(b => new { b.LeagueId, b.UserId, b.MatchId })
            .IsUnique();

        // Performance indexes
        builder.HasIndex(b => new { b.LeagueId, b.MatchId });
        builder.HasIndex(b => new { b.UserId, b.LeagueId });
    }
}
```

### BetResultConfiguration.cs
```csharp
public sealed class BetResultConfiguration : IEntityTypeConfiguration<BetResult>
{
    public void Configure(EntityTypeBuilder<BetResult> builder)
    {
        builder.ToTable("bet_results");

        // Use BetId as the primary key for the one-to-one dependent
        builder.HasKey(br => br.BetId);

        builder.HasOne(br => br.Bet)
            .WithOne(b => b.Result)
            .HasForeignKey<BetResult>(br => br.BetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### LeagueStandingConfiguration.cs
```csharp
public sealed class LeagueStandingConfiguration : IEntityTypeConfiguration<LeagueStanding>
{
    public void Configure(EntityTypeBuilder<LeagueStanding> builder)
    {
        builder.ToTable("league_standings");

        builder.HasOne(ls => ls.League)
            .WithMany()
            .HasForeignKey(ls => ls.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ls => ls.User)
            .WithMany()
            .HasForeignKey(ls => ls.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one standing per user per league
        builder.HasIndex(ls => new { ls.LeagueId, ls.UserId })
            .IsUnique();

        // Performance index for leaderboard queries
        // Includes UserId to support the full ORDER BY clause: TotalPoints DESC, ExactMatches DESC, BetsPlaced ASC, UserId ASC
        builder.HasIndex(ls => new { ls.LeagueId, ls.TotalPoints, ls.ExactMatches, ls.BetsPlaced, ls.UserId });
    }
}
```

---

## Triggering Background Jobs from Match Sync

### Update MatchSyncService
When match status changes to `Finished`, enqueue bet calculation job:

```csharp
// In MatchSyncService.cs
private async Task UpdateMatchAsync(Match existingMatch, FootballDataMatch apiMatch)
{
    var previousStatus = existingMatch.Status;

    // Update match fields...
    existingMatch.Status = apiMatch.Status;
    existingMatch.HomeScore = apiMatch.Score?.FullTime?.Home;
    existingMatch.AwayScore = apiMatch.Score?.FullTime?.Away;
    // ... other fields

    await _context.SaveChangesAsync();

    // If match just finished, trigger bet calculation
    if (previousStatus != MatchStatus.Finished && existingMatch.Status == MatchStatus.Finished)
    {
        await _jobDispatcher.EnqueueAsync(
            jobType: "CalculateBetResults",
            payload: new { matchId = existingMatch.Id, competitionId = existingMatch.CompetitionId },
            correlationId: $"match-result-{existingMatch.Id}"
        );
    }
}
```

---

## Implementation Order

### Step 1: Domain Layer
1. Create `Bet` entity
2. Create `BetResult` entity
3. Create `LeagueStanding` entity

### Step 2: Application Common
1. Create `IBetCalculator` interface
2. Create `IStandingsCalculator` interface
3. Update `IApplicationDbContext` with `Bets`, `BetResults`, `LeagueStandings` DbSets

### Step 3: Application Features - DTOs & Errors
1. Create `BetDtos.cs` (all request/response DTOs)
2. Create `BetErrors.cs`

### Step 4: Application Features - Commands
1. **PlaceBet** (command + handler + validator)
2. **DeleteBet** (command + handler)
3. **CalculateBetResults** (command + handler) - Background job
4. **RecalculateLeagueStandings** (command + handler) - Background job

### Step 5: Application Features - Queries
1. **GetMyBets** (query + handler) - returns `MyBetDto[]`
2. **GetMatchBets** (query + handler) - returns `MatchBetDto[]` (checks deadline)
3. **GetLeagueStandings** (query + handler) - returns `LeagueStandingDto[]`
4. **GetUserStats** (query + handler) - returns `UserStatsDto`

### Step 6: Infrastructure Services
1. Create `BetCalculator` service
2. Create `StandingsCalculator` service
3. Update `DependencyInjection.cs` to register services

### Step 7: Infrastructure EF Core
1. Create `BetConfiguration`
2. Create `BetResultConfiguration`
3. Create `LeagueStandingConfiguration`
4. Update `ApplicationDbContext` with new DbSets and configurations

### Step 8: Infrastructure Match Sync
1. Update `MatchSyncService` to enqueue `CalculateBetResults` job when match finishes

### Step 9: API Layer
1. Create `BetEndpoints.cs` with all 6 endpoints
2. Update `Program.cs` to map endpoints

### Step 10: Database Migration
1. Create migration: `dotnet ef migrations add AddBettingSystem`
2. Apply migration: `dotnet ef database update`

---

## Files to Create (New)

### Domain
- `src/ExtraTime.Domain/Entities/Bet.cs`
- `src/ExtraTime.Domain/Entities/BetResult.cs`
- `src/ExtraTime.Domain/Entities/LeagueStanding.cs`

### Application - Interfaces
- `src/ExtraTime.Application/Common/Interfaces/IBetCalculator.cs`
- `src/ExtraTime.Application/Common/Interfaces/IStandingsCalculator.cs`

### Application - Features
- `src/ExtraTime.Application/Features/Bets/BetErrors.cs`
- `src/ExtraTime.Application/Features/Bets/DTOs/BetDtos.cs`
- `src/ExtraTime.Application/Features/Bets/Commands/PlaceBet/*` (3 files)
- `src/ExtraTime.Application/Features/Bets/Commands/DeleteBet/*` (2 files)
- `src/ExtraTime.Application/Features/Bets/Commands/CalculateBetResults/*` (2 files)
- `src/ExtraTime.Application/Features/Bets/Commands/RecalculateLeagueStandings/*` (2 files)
- `src/ExtraTime.Application/Features/Bets/Queries/GetMyBets/*` (2 files)
- `src/ExtraTime.Application/Features/Bets/Queries/GetMatchBets/*` (2 files)
- `src/ExtraTime.Application/Features/Bets/Queries/GetLeagueStandings/*` (2 files)
- `src/ExtraTime.Application/Features/Bets/Queries/GetUserStats/*` (2 files)

### Infrastructure
- `src/ExtraTime.Infrastructure/Services/BetCalculator.cs`
- `src/ExtraTime.Infrastructure/Services/StandingsCalculator.cs`
- `src/ExtraTime.Infrastructure/Data/Configurations/BetConfiguration.cs`
- `src/ExtraTime.Infrastructure/Data/Configurations/BetResultConfiguration.cs`
- `src/ExtraTime.Infrastructure/Data/Configurations/LeagueStandingConfiguration.cs`

### API
- `src/ExtraTime.API/Features/Bets/BetEndpoints.cs`

---

## Files to Modify (Existing)

- `src/ExtraTime.Application/Common/Interfaces/IApplicationDbContext.cs` - Add DbSets
- `src/ExtraTime.Infrastructure/Data/ApplicationDbContext.cs` - Add DbSets
- `src/ExtraTime.Infrastructure/DependencyInjection.cs` - Register `IBetCalculator`, `IStandingsCalculator`
- `src/ExtraTime.Infrastructure/Football/Services/MatchSyncService.cs` - Trigger bet calculation on match finish
- `src/ExtraTime.API/Program.cs` - Map BetEndpoints

---

## Verification Steps

### Build & Migration
```bash
dotnet build
dotnet ef migrations add AddBettingSystem --project src/ExtraTime.Infrastructure --startup-project src/ExtraTime.API
dotnet ef database update --project src/ExtraTime.Infrastructure --startup-project src/ExtraTime.API
```

### API Testing (Swagger)

#### Prerequisites
1. Login as `user1` and `user2` to get JWT tokens
2. Create a league as `user1` (save league ID and invite code)
3. Join league as `user2` using invite code
4. Ensure there are upcoming matches in the database

#### Test 1: Place Bet (Happy Path)
```json
POST /api/leagues/{leagueId}/bets
Authorization: Bearer {user1_token}
{
  "matchId": "guid",
  "predictedHomeScore": 2,
  "predictedAwayScore": 1
}
```
- Verify 201 Created with `BetDto`
- Verify `PlacedAt` is set
- Verify `LastUpdatedAt` is null

#### Test 2: Update Existing Bet
```json
POST /api/leagues/{leagueId}/bets
Authorization: Bearer {user1_token}
{
  "matchId": "same-guid",
  "predictedHomeScore": 3,
  "predictedAwayScore": 1
}
```
- Verify 200 OK (not 201)
- Verify `PredictedHomeScore` updated to 3
- Verify `LastUpdatedAt` is now set
- Verify `PlacedAt` unchanged

#### Test 3: Get My Bets
```
GET /api/leagues/{leagueId}/bets/my
Authorization: Bearer {user1_token}
```
- Verify returns array of `MyBetDto`
- Verify includes match details (team names, date, status)
- Verify only shows user1's bets

#### Test 4: Delete Bet (Before Deadline)
```
DELETE /api/leagues/{leagueId}/bets/{betId}
Authorization: Bearer {user1_token}
```
- Verify 204 No Content
- Verify bet no longer appears in "my bets"

#### Test 5: Place Bet as user2
```json
POST /api/leagues/{leagueId}/bets
Authorization: Bearer {user2_token}
{
  "matchId": "guid",
  "predictedHomeScore": 1,
  "predictedAwayScore": 1
}
```
- Verify 201 Created

#### Test 6: View Match Bets (Before Deadline)
```
GET /api/leagues/{leagueId}/matches/{matchId}/bets
Authorization: Bearer {user1_token}
```
- Verify 200 OK with an empty array response
- Bets are intentionally hidden before the bet-visibility deadline; this endpoint MUST return an empty list (rather than an error) until the deadline has passed
- (Optional extension) If response metadata is added, include fields such as `betsVisible: false` and `betsVisibleAfter: <timestamp>` to indicate why the array is empty and when bets will become visible

#### Test 7: Simulate Match Finish
1. Manually update match in database:
   ```sql
   UPDATE "matches"
   SET "status" = 4,  -- Finished
       "home_score" = 2,
       "away_score" = 1
   WHERE "id" = '{matchId}';
   ```
2. Trigger bet calculation job:
   ```json
   POST /api/admin/jobs (manually enqueue CalculateBetResults job)
   {
     "jobType": "CalculateBetResults",
     "payload": { "matchId": "guid" }
   }
   ```
3. Wait for job to complete
4. Check `BackgroundJobs` table for status

> **Note:** In a real scenario, the match status would be updated by `MatchSyncService` when it detects that a match has finished, and that service would automatically enqueue the `CalculateBetResults` background job. The direct database update and manual `/api/admin/jobs` call in this test are only needed when simulating a finished match by bypassing the normal sync process.

#### Test 8: View Match Bets (After Match Finishes)
```
GET /api/leagues/{leagueId}/matches/{matchId}/bets
Authorization: Bearer {user1_token}
```
- Verify returns array of `MatchBetDto` with both user1 and user2 bets
- Verify includes `Result` with points earned
- Verify user2's bet (1-1) shows 0 points (wrong)
- Verify user1's bet depends on their prediction

#### Test 9: Get League Standings
```
GET /api/leagues/{leagueId}/standings
Authorization: Bearer {user1_token}
```
- Verify returns `LeagueStandingDto[]` ordered by points
- Verify includes `TotalPoints`, `BetsPlaced`, `ExactMatches`, `CorrectResults`
- Verify includes `CurrentStreak`, `BestStreak`
- Verify includes `Rank` (1-based ranking)

#### Test 10: Get User Stats
```
GET /api/leagues/{leagueId}/users/{user1Id}/stats
Authorization: Bearer {user1_token}
```
- Verify returns `UserStatsDto`
- Verify includes `AccuracyPercentage` calculated correctly
- Verify includes `Rank` in league

#### Test 11: Validation Errors

**Invalid Score (negative):**
```json
POST /api/leagues/{leagueId}/bets
{
  "matchId": "guid",
  "predictedHomeScore": -1,
  "predictedAwayScore": 2
}
```
- Verify 400 Bad Request with "InvalidScore" error

**Invalid Score (too high):**
```json
POST /api/leagues/{leagueId}/bets
{
  "matchId": "guid",
  "predictedHomeScore": 100,
  "predictedAwayScore": 2
}
```
- Verify 400 Bad Request

**Match Not in League's Allowed Competitions:**
1. Update league to only allow specific competition
2. Try to bet on match from different competition
3. Verify 400 Bad Request with "MatchNotAllowed"

**Not a League Member:**
1. Login as `user3` (not in league)
2. Try to place bet
3. Verify 403 Forbidden with "NotALeagueMember"

#### Test 12: Deadline Enforcement

**Place bet after deadline (respecting league betting deadline):**
1. Configure the league's `BettingDeadlineMinutes` to a known value (e.g., 5 minutes)
2. Update match `MatchDateUtc` to 2 minutes from now (so the betting deadline = `MatchDateUtc - BettingDeadlineMinutes` = -3 minutes, which is in the past)
3. Try to place bet immediately (current time is past the computed deadline)
4. Verify 400 Bad Request with "DeadlinePassed"

**Delete bet after deadline (respecting league betting deadline):**
1. Place bet on match while its betting deadline (`MatchDateUtc - league.BettingDeadlineMinutes`) is still in the future
2. Update match `MatchDateUtc` so that its betting deadline is now in the past (e.g., using the same setup as above: `BettingDeadlineMinutes = 5`, `MatchDateUtc = 2 minutes from now`)
3. Try to delete bet
4. Verify 400 Bad Request with "DeadlinePassed"

#### Test 13: Streak Calculation

**Scenario: user1 makes 5 bets**
1. Match 1: Correct result → CurrentStreak = 1, BestStreak = 1
2. Match 2: Correct result → CurrentStreak = 2, BestStreak = 2
3. Match 3: Wrong → CurrentStreak = 0, BestStreak = 2
4. Match 4: Exact match → CurrentStreak = 1, BestStreak = 2
5. Match 5: Correct result → CurrentStreak = 2, BestStreak = 2

Verify standings show:
- `CurrentStreak = 2`
- `BestStreak = 2`

#### Test 14: Leaderboard

**Create scenario:**
- user1: 10 points (3 exact, 1 correct result)
- user2: 10 points (2 exact, 4 correct results)
- user3: 8 points (2 exact, 2 correct results)

Verify ranking:
1. user1 (more exact matches)
2. user2 (same points, fewer exact)
3. user3 (fewer points)

---

## Edge Cases to Handle

1. **Concurrent Bet Placement:**
   - Two users bet on same match simultaneously
   - Use unique index to prevent duplicates per user/match/league
   - For concurrent updates to the same bet, consider implementing proper concurrency handling:
     - Option 1: Use optimistic concurrency with a version/timestamp token in the `Bet` entity
     - Option 2: Use database-level locking to ensure sequential updates
     - Without concurrency control, concurrent updates may result in one update overwriting the other

2. **Match Status Changes:**
   - Match postponed after bets placed
   - Keep bets, don't calculate results until match actually finishes

3. **Match Cancelled:**
   - Don't calculate results
   - Bets remain in database for history

4. **League Deletion:**
   - Cascade delete all bets, results, standings
   - Already handled by EF Core configuration

5. **User Kicked from League:**
   - Keep historical bets and results
   - User cannot place new bets
   - User removed from standings query results:
     - `GetLeagueStandingsQueryHandler` MUST filter standings by current league membership
     - Implement via a JOIN (or equivalent) with `LeagueMembers` so only active/current members are returned
     - Historical bets/results for kicked users remain in the database but do not appear in standings
   - **User-facing behavior:** When a user is kicked, their points and stats disappear from the visible leaderboard. If they rejoin later, their historical bets are still in the database but standings are recalculated only from bets placed while they were a member. This behavior should be documented in user-facing help text if needed.

6. **Missing Match Score:**
   - Don't calculate results if `HomeScore` or `AwayScore` is null
   - Edge case: match status is `Finished` but scores not synced yet

7. **Recalculation Idempotency:**
   - `RecalculateLeagueStandings` should be idempotent
   - Can be run multiple times safely

8. **Partial Match Data:**
   - If match has half-time score but not full-time, don't calculate
   - Only calculate when `Status = Finished` AND scores present

9. **Missing Standings Entries:**
   - `StandingsCalculator.RecalculateLeagueStandingsAsync` only updates standings for users who have placed bets
   - League members who haven't placed any bets won't have standings entries created
   - Query handlers should handle the case where standings don't exist (return empty/zero stats)
   - Alternative: Consider creating/updating standings entries for all league members with zero stats for those without bets

---

## Security Considerations

1. **Authorization Checks:**
   - PlaceBet: Verify user is league member
   - DeleteBet: Verify user owns the bet
   - GetMatchBets: Verify deadline passed before returning bets
   - GetLeagueStandings: Verify user is league member
   - GetUserStats: Verify user is league member

2. **Input Validation:**
   - Sanitize all user inputs
   - Validate score predictions (0-99 range)
   - Validate match exists and is eligible for betting

3. **Rate Limiting:**
   - Consider rate limiting bet placement (prevent spam)
   - Background jobs should have max retry limits

4. **Data Privacy:**
   - Don't expose bets before deadline
   - Only league members can view standings

---

## Performance Considerations

1. **Indexes:**
   - Composite index on `(LeagueId, UserId, MatchId)` for bet lookups
   - Index on `(LeagueId, MatchId)` for match bet queries
   - Index on `(LeagueId, TotalPoints, ExactMatches, BetsPlaced)` for leaderboard

2. **Caching:**
   - Leaderboard stored in `LeagueStandings` table (pre-computed)
   - No need to recalculate on every request
   - Only recalculate when matches finish

3. **Batch Processing:**
   - When multiple matches finish simultaneously, batch standings recalculation
   - Single job can handle multiple leagues

4. **Query Optimization:**
   - Use `.AsNoTracking()` for read-only queries
   - Project to DTOs to avoid loading full entities
   - Paginate leaderboard for large leagues (future enhancement)

---

## Future Enhancements (Not in Phase 5)

- Weekly/monthly leaderboard snapshots
- Bet editing history/audit log
- Confidence points (allocate points to bets)
- Head-to-head matchups between users
- League achievements/badges
- Export standings to CSV
- Push notifications for bet reminders
- Social features (comments on bets after reveal)
- Machine learning bet suggestions

---

## Notes

- Frontend implementation deferred to Phase 6
- Bet visibility strictly enforced (hidden until deadline)
- Streaks only count consecutive matches with bets placed
- Background jobs use existing `IJobDispatcher` from Phase 2.2
- All entities use soft-delete via `BaseAuditableEntity`
- Scoring rules are league-specific, stored in `League` entity
- Match sync service automatically triggers bet calculation when matches finish
