# Phase 9.7: Context Data Sources (Lineups, Suspensions, Weather/Referee)

## Overview
Phase 9.7 adds missing contextual signals that can improve bot quality beyond core 9.5 sources.
This phase productionizes lineup data and introduces additional optional sources (suspensions, weather, referee tendencies).

> **Prerequisite**: `phase-lineups.md` base abstractions and Phase 9.5 integration health foundation
> **Priority**: Recommended after 9.6; can be delivered incrementally

---

## Free-Only Source Strategy

| Data | Primary Source (Free) | Fallback Source (Free) | Notes |
|------|------------------------|------------------------|-------|
| Lineups | API-Football `fixtures/lineups` | TheSportsDB `lookuplineup.php` | API-Football free plan: 100 requests/day; lineup sync gets first quota allocation. |
| Injuries | FPL `bootstrap-static` (EPL-only) | API-Football `injuries` (only with spare quota) | No strong fully free multi-league injury API identified; injuries remain optional. |

- No paid APIs in default implementation.
- If quota is constrained, keep lineups enabled and disable injuries first.

---

## Locked Pre-Implementation Decisions

These decisions are fixed for implementation sessions unless explicitly changed.

### 0.1 Injury/Suspension Coverage Rules

- **EPL injuries**: use FPL `bootstrap-static` player `status/news` as primary free source.
- **Non-EPL injuries**: default to unavailable (`null`/not synced) in zero-cost mode.
- **API-Football injuries**: allowed only when spare API budget exists after lineup reservation.
- **Suspensions**:
  - Primary path: local derivation from available match events/cards data (zero external cost).
  - Optional enhancement: API-Football sidelined/injury-related endpoints only with spare quota.

### 0.2 API-Football Quota Policy (Lineups First)

Use these defaults for daily budgeting:

- `HardDailyLimit = 100`
- `OperationalCap = 95` (leave 5-call guard band)
- `SafetyReserve = 10`
- `MaxInjuryCallsPerDay = 15` (and only after lineup targets are met)

Budget algorithm:
1. Query current remaining requests.
2. Compute `NeededLineupCalls24h = count(upcoming matches in next 24h without lineups)`.
3. Compute `ReservedForLineups = NeededLineupCalls24h + SafetyReserve`.
4. If `Remaining <= ReservedForLineups`, skip injury/suspension external calls.
5. Run lineup sync first; only then allow injury calls up to `MaxInjuryCallsPerDay`.

Stop conditions:
- Stop all non-lineup calls once `Remaining <= ReservedForLineups`.
- Stop all calls at `OperationalCap` usage for the day.

### 0.3 Provider Selection Order

Lineup provider chain:
1. `ApiLineupDataProvider` (primary)
2. `TheSportsDbLineupDataProvider` (fallback when API-Football has no lineup for a fixture)
3. No third fallback; store miss and retry on next schedule.

### 0.4 Quality Gates (Required Before Enabling Injury Sync)

- `LineupCoverageUpcoming24h >= 75%` for supported leagues.
- `LineupCoveragePlayedMatches >= 90%` for supported leagues.
- `LineupParseFailureRate < 5%`.
- `QuotaPolicyBreaches = 0` (no day where injuries consumed reserved lineup budget).

If gates are not met, keep injury sync disabled and iterate lineup quality first.

---

## Scope

### In Scope
- Replace `NullLineupDataProvider` with a real provider.
- Add suspension data ingestion and storage.
- Add optional weather and referee-context enrichment.
- Expose source availability through integration health.

### Out of Scope
- Full player-level tracking platform.
- Paid premium-only data sources as hard dependencies.

---

## Part 1: Real Lineup Provider

### 1.1 Provider Implementation

Create production provider implementing `ILineupDataProvider`:

**File:** `src/ExtraTime.Infrastructure/Services/Football/ApiLineupDataProvider.cs`

Responsibilities:
- Resolve fixtures reliably using `MatchLineupRequest`.
- Fetch formation, coach, starting XI, bench, captain.
- Normalize player naming/position fields.
- Upsert through existing `LineupSyncService`.

Optional fallback provider:
- `TheSportsDbLineupDataProvider` using `lookuplineup.php` for events where mapping is available.

### 1.2 Integration Health Alignment

Add dedicated integration type for lineups:

```csharp
public enum IntegrationType
{
    FootballDataOrg = 0,
    Understat = 1,
    FootballDataUk = 2,
    ApiFootball = 3,
    ClubElo = 4,
    LineupProvider = 5,
    SuspensionProvider = 6
}
```

Update `GetDataAvailabilityAsync()` so `LineupDataAvailable` maps to `LineupProvider` (not `FootballDataOrg`).

---

## Part 2: Suspensions Data

### 2.1 Domain Entities

**File:** `src/ExtraTime.Domain/Entities/TeamSuspensions.cs`
**File:** `src/ExtraTime.Domain/Entities/PlayerSuspension.cs`

Track:
- active suspended players
- reason (cards/disciplinary)
- expected return date
- suspension impact score

### 2.2 Service Contract

**File:** `src/ExtraTime.Application/Common/Interfaces/ISuspensionService.cs`

```csharp
public interface ISuspensionService
{
    Task SyncSuspensionsForUpcomingMatchesAsync(
        int daysAhead = 3,
        CancellationToken cancellationToken = default);

    Task<TeamSuspensions?> GetTeamSuspensionsAsOfAsync(
        Guid teamId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}
```

---

## Part 3: Optional Enrichment Sources

### 3.1 Weather Context (Optional)

Source example: Open-Meteo (free).

Use for:
- extreme conditions (heavy rain/wind)
- temperature/humidity adjustments
- model feature flags only when confidence is high

### 3.2 Referee Profile (Optional)

Build from existing `MatchStats.Referee` history:
- cards per match
- foul rate
- penalty frequency (if available later)

This can be derived locally even before adding a new external source.

---

## Part 4: Bot Strategy & ML Integration

- Extend `PredictionContext` with suspension/weather/referee availability flags.
- Keep all new sources optional with graceful fallback.
- For ML training, only include features with stable historical coverage.

Recommended rollout:
1. Lineups provider (required first)
2. Suspensions
3. Referee profile (local derivation)
4. Weather (optional)

---

## Implementation Checklist

- [x] Implement real `ILineupDataProvider` and replace `NullLineupDataProvider` in DI
- [x] Implement lineup quota reservation (lineups first, injuries second)
- [x] Add configurable `ExternalDataQuotaPolicy` options (limit, reserve, injury cap)
- [x] Add `LineupProvider`/`SuspensionProvider` to `IntegrationType`
- [x] Update integration health mapping for `LineupDataAvailable`
- [x] Create `TeamSuspensions` and `PlayerSuspension` entities + configurations
- [x] Create `ISuspensionService` and implementation
- [x] Add optional EPL-only `FplInjuryStatusProvider` adapter for free injury status
- [x] Add league-scoped behavior: EPL injury sync on, non-EPL injury sync off by default
- [x] Add suspension sync trigger (timer or orchestrator phase)
- [x] Add optional weather/referee enrichment service interfaces
- [x] Expose new availability flags in bot prediction context
- [x] Add migration(s) for suspension tables
- [x] Add coverage metrics and quality-gate checks before enabling injury sync

---

## Files Summary

| Action | File |
|--------|------|
| **Create** | `Infrastructure/Services/Football/ApiLineupDataProvider.cs` |
| **Create** | `Domain/Entities/TeamSuspensions.cs` |
| **Create** | `Domain/Entities/PlayerSuspension.cs` |
| **Create** | `Infrastructure/Data/Configurations/TeamSuspensionsConfiguration.cs` |
| **Create** | `Infrastructure/Data/Configurations/PlayerSuspensionConfiguration.cs` |
| **Create** | `Application/Common/Interfaces/ISuspensionService.cs` |
| **Create** | `Infrastructure/Services/ExternalData/SuspensionService.cs` |
| **Modify** | `Domain/Enums/IntegrationType.cs` |
| **Modify** | `Infrastructure/Services/IntegrationHealthService.cs` |
| **Modify** | `Infrastructure/DependencyInjection.cs` |
| **New migration** | `AddSuspensionsData` |
