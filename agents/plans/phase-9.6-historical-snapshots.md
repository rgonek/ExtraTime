# Phase 9.6: Historical Snapshots & Leakage-Safe ML Features

## Overview
Phase 9.5 integrates external sources, but Phase 7.8 ML training also needs **historically correct, date-effective data retrieval**.
This phase adds snapshot/backfill mechanisms and enforces "as-of match date" feature extraction to prevent training data leakage.

> **Required before production ML training** in `phase-7.8-ml-bots-detailed.md`
> **Prerequisite**: Phase 9.5B/C/D/E completed (interfaces + base sync)

---

## Locked Pre-Implementation Decisions

### 0.1 Default Backfill Windows

- Understat xG snapshots: backfill last **4 completed seasons** per supported league.
- Football-Data.co.uk odds/stats: backfill same 4-season window.
- Elo ratings: backfill daily from `(earliest training match date - 30 days)` to current date.
- Injuries: no synthetic historical reconstruction; only persist snapshots from integration start date onward.

### 0.2 Snapshot Keys (Idempotency)

- `TeamXgSnapshot`: unique `(TeamId, CompetitionId, Season, SnapshotDateUtc.Date)`.
- `TeamInjurySnapshot`: unique `(TeamId, SnapshotDateUtc.Date)`.
- Upserts must be deterministic: rerun same input window -> same stored values.

### 0.3 Backfill Run Order

1. Understat xG seasonal snapshots
2. Football-Data odds/stats seasonal imports
3. ClubElo daily ratings
4. Injury snapshots (from rollout date onward)
5. Rebuild ML training set using as-of retrieval

---

## Problem Statement

Without historical snapshots and as-of lookups, historical training rows can accidentally use future data:
- `TeamXgStats` is season-level and gets overwritten over time.
- `TeamInjuries` stores current state unless archived.
- Feature extraction paths may default to `DateTime.UtcNow` instead of `match.MatchDate`.

This inflates offline metrics and weakens real-world prediction quality.

---

## Part 1: Domain Layer - Snapshot Entities

### 1.1 TeamXgSnapshot

**File:** `src/ExtraTime.Domain/Entities/TeamXgSnapshot.cs`

```csharp
namespace ExtraTime.Domain.Entities;

public sealed class TeamXgSnapshot : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public required Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public required string Season { get; set; }
    public DateTime SnapshotDateUtc { get; set; }   // Date snapshot was captured

    public double XgPerMatch { get; set; }
    public double XgAgainstPerMatch { get; set; }
    public double XgOverperformance { get; set; }
    public double RecentXgPerMatch { get; set; }
}
```

### 1.2 TeamInjurySnapshot

**File:** `src/ExtraTime.Domain/Entities/TeamInjurySnapshot.cs`

```csharp
namespace ExtraTime.Domain.Entities;

public sealed class TeamInjurySnapshot : BaseEntity
{
    public required Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public DateTime SnapshotDateUtc { get; set; }
    public int TotalInjured { get; set; }
    public int KeyPlayersInjured { get; set; }
    public double InjuryImpactScore { get; set; }
    public string InjuredPlayerNames { get; set; } = "[]";
}
```

---

## Part 2: Service Contracts - Date-Effective Retrieval

Use as-of methods across all external sources in ML training paths:

- `IUnderstatService.GetTeamXgAsOfAsync(...)`
- `IOddsDataService.GetOddsForMatchAsOfAsync(...)`
- `IEloRatingService.GetTeamEloAtDateAsync(...)`
- `IInjuryService.GetTeamInjuriesAsOfAsync(...)`

Add orchestrator interface:

**File:** `src/ExtraTime.Application/Common/Interfaces/IExternalDataBackfillService.cs`

```csharp
namespace ExtraTime.Application.Common.Interfaces;

public interface IExternalDataBackfillService
{
    Task BackfillForLeagueAsync(
        string leagueCode,
        int fromSeason,
        int toSeason,
        CancellationToken cancellationToken = default);

    Task BackfillGlobalEloAsync(
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default);
}
```

---

## Part 3: Infrastructure - Backfill & Snapshot Jobs

### 3.1 Backfill Service

**File:** `src/ExtraTime.Infrastructure/Services/ExternalData/ExternalDataBackfillService.cs`

Responsibilities:
- Understat: run multi-season backfill and persist `TeamXgSnapshot` time series.
- Football-Data.co.uk: import historical seasons for odds + match stats.
- ClubElo: import ratings for every date in range.
- Injuries: snapshot available team injury state daily (from feature rollout date onward).

### 3.2 Snapshot Retention

- Keep raw snapshots for at least 3 seasons.
- Make backfill idempotent (unique keys on source + date + team/match).
- Log progress checkpoints per source/league/date range.
- Persist checkpoint state after each league-season/date chunk to support resume-after-failure.

---

## Part 4: Phase 7.8 Integration - Leakage-Safe Feature Extraction

Update ML feature extraction paths so every row uses:
- `asOfUtc = match.MatchDate` (or `MatchDateUtc`)
- Source calls that read the newest snapshot **<= asOfUtc**

Rules:
- No `DateTime.UtcNow` inside historical training feature computation.
- If a source lacks historical data for that date, return null/default and continue.
- Track missing-source rates in training logs to monitor data quality.

---

## Part 5: Verification

- Unit test: as-of lookup never returns data newer than match date.
- Integration test: training sample built for an old match is deterministic over re-runs.
- Regression test: disabling one source still produces valid feature vectors.

Acceptance criteria:
- `LeakageViolations = 0` in as-of retrieval tests.
- `DeterminismMismatches = 0` for repeated training-data extraction of the same match set.
- Backfill job resume test passes after forced interruption.
- Data quality report is generated per source (coverage + missing-rate by league/season/date range).

---

## Implementation Checklist

- [x] Create `TeamXgSnapshot` entity + EF configuration
- [x] Create `TeamInjurySnapshot` entity + EF configuration
- [x] Add snapshot DbSets to `IApplicationDbContext` and `ApplicationDbContext`
- [ ] Create `IExternalDataBackfillService`
- [ ] Implement `ExternalDataBackfillService`
- [ ] Wire Understat/Odds/Elo/Injury historical backfill entrypoints
- [ ] Implement checkpointed backfill execution (resume from last successful chunk)
- [ ] Update ML feature extraction to use as-of retrieval for training rows
- [x] Add migration(s) for snapshot tables
- [ ] Add tests for leakage-safe retrieval and deterministic training data
- [ ] Add data-quality report output for each backfill run

---

## Files Summary

| Action | File |
|--------|------|
| **Create** | `Domain/Entities/TeamXgSnapshot.cs` |
| **Create** | `Domain/Entities/TeamInjurySnapshot.cs` |
| **Create** | `Infrastructure/Data/Configurations/TeamXgSnapshotConfiguration.cs` |
| **Create** | `Infrastructure/Data/Configurations/TeamInjurySnapshotConfiguration.cs` |
| **Create** | `Application/Common/Interfaces/IExternalDataBackfillService.cs` |
| **Create** | `Infrastructure/Services/ExternalData/ExternalDataBackfillService.cs` |
| **Modify** | `Application/Common/Interfaces/IApplicationDbContext.cs` |
| **Modify** | `Infrastructure/Data/ApplicationDbContext.cs` |
| **Modify** | `Infrastructure/Services/MlFeatureExtractor.cs` |
| **Modify** | `Infrastructure/DependencyInjection.cs` |
| **New migration** | `AddExternalDataSnapshots` |
