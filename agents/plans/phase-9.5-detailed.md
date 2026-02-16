# Phase 9.5: External Data Sources Integration - Overview

## Overview
Integrate external free data sources to enhance bot prediction accuracy with advanced statistics (xG), market consensus (betting odds), Elo ratings, match statistics, and injury data.
For Phase 7.8 ML training, all external data usage must be leakage-safe: feature values for a historical match must be resolved "as of" that match date (never from future snapshots).

> **Prerequisite**: Phase 9 (Extended Football Data), `phase-9-head-to-head.md`, and `phase-lineups.md` foundations should be complete
> **Data Sources**:
> - Understat (xG statistics) - Primary
> - Football-Data.co.uk (Historical betting odds + match stats) - Primary
> - ClubElo.com (Elo ratings) - Primary
> - API-Football (Injuries) - Optional/Limited
> **Cost Policy**:
> - Free/near-zero only in default architecture.
> - If one free quota is shared between lineups and injuries, reserve quota for lineups first.
> - Injuries are optional and should be skipped before lineups when quotas are tight.

---

## Sub-Plans

| # | Plan File | Description | Priority |
|---|-----------|-------------|----------|
| A | [phase-9.5a-integration-health.md](phase-9.5a-integration-health.md) | Integration health monitoring system | Required first |
| B | [phase-9.5b-understat-xg.md](phase-9.5b-understat-xg.md) | Understat xG data scraping | Primary |
| C | [phase-9.5c-football-data-uk.md](phase-9.5c-football-data-uk.md) | Football-Data.co.uk odds + match stats CSV | Primary |
| D | [phase-9.5d-clubelo.md](phase-9.5d-clubelo.md) | ClubElo.com Elo ratings | Primary |
| E | [phase-9.5e-api-football-injuries.md](phase-9.5e-api-football-injuries.md) | API-Football injury data | Optional |
| F | [phase-9.5f-graceful-degradation.md](phase-9.5f-graceful-degradation.md) | Bot graceful degradation + StatsAnalyst strategy | After B-E |
| G | [phase-9.5g-admin-bot-management.md](phase-9.5g-admin-bot-management.md) | Admin CRUD, endpoints, frontend | After F |
| H | [phase-9.6-historical-snapshots.md](phase-9.6-historical-snapshots.md) | Historical backfill + as-of snapshot retrieval for ML training | Required for ML training |
| I | [phase-9.7-context-data-sources.md](phase-9.7-context-data-sources.md) | Real lineup provider, suspensions, optional weather/referee | Recommended |

## Implementation Order

1. **9.5A** - Integration Health (foundation for all other integrations)
2. **9.5B** - Understat xG (most valuable data source)
3. **9.5C** - Football-Data.co.uk (odds + extended match stats)
4. **9.5D** - ClubElo (simple integration, powerful feature)
5. **9.5E** - API-Football injuries (optional, rate-limited)
6. **9.5F** - Graceful Degradation (after data sources are in place)
7. **9.5G** - Admin Bot Management (after strategies are updated)
8. **9.6** - Historical snapshots/backfill + leakage-safe ML feature extraction contracts
9. **9.7** - Additional context sources (lineups provider, suspensions, weather/referee)

> **Zero-cost sequencing note**: prioritize lineup provider work from Phase 9.7 before enabling Phase 9.5E injuries.

## Phase 7.8 Data Contracts

- Every external data service used by ML must expose an `asOfUtc`/date-effective read method.
- Feature extraction for training must only consume data available on or before each match kickoff.
- `phase-9-head-to-head.md` must expose recent 3-match counters required by `H2HRecentHomeWins` / `H2HRecentAwayWins`.
- `phase-lineups.md` remains optional for Phase 7.8 ML feature vector, but required for enhanced StatsAnalyst context.

## Data Freshness

| Source | Refresh Rate | Staleness Threshold |
|--------|--------------|---------------------|
| Understat | Daily 4 AM UTC | 48 hours |
| Football-Data.co.uk | Weekly Monday 5 AM UTC | 7 days |
| ClubElo | Daily 3 AM UTC | 48 hours |
| API-Football | On-demand (upcoming matches) | 24 hours |

## Rate Limiting

- Understat: No official limit, use 2s delay between requests
- Football-Data.co.uk: No limit (static files)
- ClubElo: No limit (public CSV)
- API-Football: 100/day strict limit - prioritize upcoming matches

## Database Entities Added

| Entity | Source | Table |
|--------|--------|-------|
| IntegrationStatus | Internal | IntegrationStatuses |
| TeamXgStats | Understat | TeamXgStats |
| MatchXgStats | Understat | MatchXgStats |
| MatchOdds | Football-Data.co.uk | MatchOdds |
| MatchStats | Football-Data.co.uk | MatchStats |
| TeamEloRating | ClubElo | TeamEloRatings |
| TeamInjuries | API-Football | TeamInjuries |
| PlayerInjury | API-Football | PlayerInjuries |
