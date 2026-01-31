# ExtraTime - Betting App MVP Plan

> **Detailed Plans:** Phase-specific detailed implementation plans are in separate files:
> - `.claude/plan/phase-1-detailed.md` - Project Foundation (Backend + Frontend setup)
> - `.claude/plan/phase-7-detailed.md` - Bot System (Basic AI bots for league activity)
> - `.claude/plan/phase-7.5-detailed.md` - Intelligent Stats-Based Bots (Configurable analytics bots)
> - `.claude/plan/phase-9.5-detailed.md` - External Data Sources (xG, Odds, Injuries)
> - Future phases will be planned iteratively before implementation

## Implementation Progress

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1 | ✅ Complete | Project Foundation (Backend + Frontend + DevOps) |
| Phase 2 | ✅ Complete | Authentication System (Backend + Frontend) |
| Phase 2.1 | ✅ Complete | User Roles (Admin Panel Backend) |
| Phase 2.2 | ✅ Complete | BackgroundJob Tracking System |
| Phase 3 | ✅ Complete | Football Data Integration |
| Phase 4 | ✅ Complete | League System (Backend) |
| Phase 5 | ✅ Complete | Betting System (Backend) |
| Phase 6 | ✅ Complete | Frontend Implementation & Polish |
| Phase 6.1 | ✅ Complete | Foundation & Types |
| Phase 6.2 | ✅ Complete | League System UI |
| Phase 6.3 | ✅ Complete | Betting System Core |
| Phase 6.4 | ✅ Complete | Leaderboard & Statistics |
| Phase 6.5 | ✅ Complete | Gamification System |
| Phase 6.6 | ✅ Complete | UX Polish & Dark Mode |
| Phase 7 | ✅ Complete | Bot System (Basic Bots) |
| Phase 7.5 | ✅ Complete | Intelligent Stats-Based Bots |
| Phase 8 | ⬜ Pending | Deployment & Launch (Azure) |
| Phase 9 | ⬜ Pending | Extended Football Data (Standings, Lineups) |
| Phase 9.5 | ⬜ Pending | External Data Sources (xG, Odds, Injuries) |
| Phase 10 | ⬜ Pending | FastEndpoints Migration & Advanced Tests |


## Project Overview
A social betting app (no real money) where friends create leagues, bet on football matches, and compete for points. Portfolio project showcasing frontend skills.

## Tech Stack

### Backend
- **ASP.NET Core (.NET 10)** with Clean Architecture
- **Entity Framework Core 10** with SQL Server (Microsoft.EntityFrameworkCore.SqlServer)
- **Mediator** (source generator, not MediatR) for CQRS pattern
- **JWT Authentication** (BCrypt + custom JWT)
- **FluentValidation** for request validation

### Frontend
- **Next.js 16** (App Router, React 19)
- **TypeScript**
- **TanStack Query** for server state management
- **Zustand** for client state
- **Tailwind CSS 4** + **shadcn/ui** for styling


### Infrastructure (Budget-Friendly)
- **Frontend**: Azure Static Web Apps (free tier)
- **Backend API**: Azure App Service Free Tier
- **Database**: Azure SQL (free tier - 32GB)
- **Scheduled Jobs**: Azure Functions (free tier - 1M executions/month)
- **Football Data**: Football-Data.org API (free tier - 10 calls/min)

---

## Phase 1: Project Foundation ✅
**Goal**: Set up project structure and development environment
**Status**: Complete

### Backend Setup
- [x] Create solution with Clean Architecture structure:
  ```
  src/
    ExtraTime.Domain/        # Entities, Value Objects, Enums
    ExtraTime.Application/   # Use Cases, DTOs, Interfaces
    ExtraTime.Infrastructure/ # EF Core, External APIs, Identity
    ExtraTime.API/           # Minimal APIs, Middleware
  ```
- [x] Configure EF Core with SQL Server
- [x] Set up dependency injection (Application + Infrastructure)
- [x] Add basic health check endpoint (`/api/health`, `/health`)

### Frontend Setup
- [x] Initialize Next.js 15 with App Router
- [x] Configure TypeScript, ESLint, Prettier
- [x] Set up Tailwind CSS + shadcn/ui (button, card, input, sonner)
- [x] Configure TanStack Query provider
- [x] Set up Zustand store structure (`auth-store.ts`)
- [x] Create base API client (`api-client.ts`)

### DevOps
- [x] Configure Docker Compose for local development
- [x] Set up GitHub Actions for CI (build + lint)

---

## Phase 2: Authentication System ✅
**Goal**: Users can register and login
**Status**: Complete

### Backend
- [x] User entity with BaseAuditableEntity
- [x] RefreshToken entity for token rotation
- [x] Register endpoint (email, username, password)
- [x] Login endpoint (returns JWT + refresh token)
- [x] Refresh token endpoint with rotation and reuse detection
- [x] Current user endpoint
- [x] BCrypt password hashing (work factor 12)
- [x] JWT with 15min access token, 7-day refresh token
- [x] FluentValidation for register/login requests

### API Endpoints
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/register` | No | Create account |
| POST | `/api/auth/login` | No | Get tokens |
| POST | `/api/auth/refresh` | No | Rotate tokens |
| GET | `/api/auth/me` | Yes | Current user |

### Frontend
- [x] Auth pages: Login, Register
- [x] Auth context/store with Zustand
- [x] Protected route wrapper
- [x] Token storage and refresh logic

---

## Phase 2.1: User Roles (Admin Panel) ✅
**Goal**: Role-based authorization for admin functionality
**Status**: Complete

### Backend
- [x] UserRole enum (User, Admin)
- [x] Role property on User entity (stored as string)
- [x] Role claim added to JWT tokens
- [x] Authorization policies: "AdminOnly", "UserOrAdmin"
- [x] ICurrentUserService with Role and IsAdmin properties

### Usage
```csharp
// Admin only endpoint
.RequireAuthorization("AdminOnly")

// Any authenticated user
.RequireAuthorization()

// Check in code
if (currentUserService.IsAdmin) { ... }
```

---

## Phase 2.2: BackgroundJob Tracking System ✅
**Goal**: Track and manage background jobs for admin monitoring
**Status**: Complete

### Backend Entities
- [x] JobStatus enum (Pending, Processing, Completed, Failed, Cancelled)
- [x] BackgroundJob entity with full tracking fields
- [x] IJobDispatcher interface for enqueueing jobs
- [x] InMemoryJobDispatcher stub (ready for Azure Queue integration)

### BackgroundJob Fields
| Field | Type | Description |
|-------|------|-------------|
| JobType | string | Type identifier (e.g., "SendEmail") |
| Status | JobStatus | Current job status |
| Payload | JSON | Job input data |
| Result | JSON | Job output data |
| Error | string | Error message if failed |
| RetryCount | int | Current retry attempt |
| MaxRetries | int | Maximum allowed retries |
| CreatedAt | DateTime | When job was created |
| StartedAt | DateTime? | When processing started |
| CompletedAt | DateTime? | When job finished |
| ScheduledAt | DateTime? | For delayed jobs |
| CorrelationId | string? | For tracking related jobs |

### Admin API Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/admin/jobs` | List jobs (paginated, filterable) |
| GET | `/api/admin/jobs/stats` | Job statistics dashboard |
| GET | `/api/admin/jobs/{id}` | Job details |
| POST | `/api/admin/jobs/{id}/retry` | Retry failed job |
| POST | `/api/admin/jobs/{id}/cancel` | Cancel pending job |

### Future Integration
When adding Azure Functions/Service Bus:
1. Replace InMemoryJobDispatcher with AzureQueueJobDispatcher
2. Azure Function updates job status as it processes
3. Admin panel shows real-time job monitoring

---

## Phase 3: Football Data Integration ✅
**Goal**: Fetch and store match data from Football-Data.org
**Status**: Complete

### Backend
- [x] Competition entity (leagues)
- [x] Team entity
- [x] CompetitionTeam entity (many-to-many join)
- [x] Match entity (with status, score, date)
- [x] MatchStatus enum
- [x] Football-Data.org API client service (with rate limiting)
- [x] Sync service to import competitions, teams, matches

### Background Sync (HostedService for local dev)
- [x] Initial full sync on startup
- [x] Daily sync for upcoming matches (next 14 days)
- [x] Live sync every 5 minutes during match hours (10:00-23:00 UTC)
- [ ] Azure Functions (deferred to Phase 8: Deployment)

### Backend API
- [x] GET /api/competitions - list available competitions
- [x] GET /api/matches - list matches (with filters: date, competition, status)
- [x] GET /api/matches/{id} - match details

### Admin Sync Endpoints
- [x] POST /api/admin/sync/competitions - trigger competition sync
- [x] POST /api/admin/sync/teams/{competitionId} - trigger team sync
- [x] POST /api/admin/sync/matches - trigger match sync
- [x] POST /api/admin/sync/live - trigger live match sync

---

## Phase 4: League System ✅
**Goal**: Users can create and join betting leagues
**Status**: Complete (Backend)

### Backend Entities
- [x] League entity (name, code, settings, owner)
- [x] LeagueMember entity (user, league, role, joined date)
- [x] League invitation system (unique codes)
- [x] MemberRole enum (Member, Owner)

### Backend API
- [x] POST /api/leagues - create league
- [x] GET /api/leagues - list user's leagues
- [x] GET /api/leagues/{id} - league details with members
- [x] PUT /api/leagues/{id} - update league settings
- [x] DELETE /api/leagues/{id} - delete league
- [x] POST /api/leagues/{id}/join - join via invite code
- [x] DELETE /api/leagues/{id}/leave - leave league
- [x] DELETE /api/leagues/{id}/members/{userId} - kick member
- [x] POST /api/leagues/{id}/invite-code/regenerate - regenerate invite code

---

## Phase 5: Betting System (Core MVP) ✅
**Goal**: Users can place bets and earn points
**Status**: Complete (Backend)

### Backend Entities
- [x] Bet entity (user, match, predicted score, timestamps)
- [x] BetResult entity (points earned, exact match, correct result)
- [x] LeagueStanding entity (cached leaderboard with stats, streaks)
- [x] Scoring rules (configurable per league):
  - Exact score: league.ScoreExactMatch (default 3)
  - Correct result: league.ScoreCorrectResult (default 1)
  - Wrong: 0 points

### Backend API
- [x] POST /api/leagues/{leagueId}/bets - place/update bet
- [x] DELETE /api/leagues/{leagueId}/bets/{betId} - delete bet
- [x] GET /api/leagues/{leagueId}/bets/my - user's bets in league
- [x] GET /api/leagues/{leagueId}/matches/{matchId}/bets - all bets for a match (after deadline)
- [x] GET /api/leagues/{leagueId}/standings - leaderboard
- [x] GET /api/leagues/{leagueId}/users/{userId}/stats - user stats

### Backend Services
- [x] BetCalculator service (calculates points based on league rules)
- [x] StandingsCalculator service (recalculates leaderboard)
- [x] Background job: CalculateBetResults (triggered when match finishes)
- [x] Background job: RecalculateLeagueStandings (after bet results calculated)
- [x] Streak tracking (current streak, best streak)
- [x] FootballSyncService integration (auto-triggers bet calculation)

---

## Phase 6: Frontend Implementation & Polish (Portfolio Focus) ✅
**Goal**: Build complete frontend UI with impressive, polished UX
**Status**: Complete

### League System UI (from Phase 4) ✅
- [x] Create league form (`league-form.tsx`)
- [x] League list/dashboard (`league-list.tsx`, `league-card.tsx`)
- [x] League detail page with member list (`league-detail.tsx`, `member-list.tsx`)
- [x] Join league via code flow (`join-league-form.tsx`)
- [x] Share invite link functionality (`invite-share.tsx`)

### Betting System UI (from Phase 5) ✅
- [x] Match list with betting UI (`match-list.tsx`, `match-card.tsx`)
- [x] Bet placement component (score inputs) (`bet-form.tsx`)
- [x] My bets view (`my-bets-list.tsx`)
- [x] Leaderboard page with avatars and ranks (`leaderboard.tsx`, `rank-badge.tsx`)
- [x] Match result reveal (show others' bets after deadline) (`match-bets-reveal.tsx`)
- [x] Points earned animation/celebration (`points-animation.tsx`)

### Core Pages & Components ✅
- [x] Dashboard (upcoming matches, recent results, quick stats)
- [x] Match day view (grouped matches by date)
- [x] League standings with animations (`standings/page.tsx`)
- [x] Responsive design (mobile-first)
- [x] App shell with navigation (`app-shell.tsx`)

### UX Enhancements ✅
- [x] Loading skeletons (`loading-skeleton.tsx`)
- [x] Toast notifications (Sonner)
- [x] Optimistic updates for bets (`use-bets.ts`)
- [x] Dark/light mode toggle (`theme-toggle.tsx`)
- [x] Micro-animations (`animated-container.tsx`)

### Gamification Features ✅
- [x] Achievement badge system (`achievement-badge.tsx`, `achievement-list.tsx`)
- [x] User levels/ranks based on total points (`level-indicator.tsx`)
- [x] Streak counter component (`streak-counter.tsx`)
- [x] Confetti/celebration animations for wins (`celebration-confetti.tsx`)
- [x] Weekly challenge cards (`challenge-card.tsx`)
- [x] Progress bars for achievements
- [x] User stats card (`user-stats-card.tsx`)

### Accessibility & Polish ✅
- [x] Error boundaries (`error-boundary.tsx`)
- [x] Skip links (`skip-link.tsx`)
- [x] Visually hidden elements (`visually-hidden.tsx`)
- [x] Global loading indicator (`global-loading.tsx`)
- [x] Empty state components (`empty-state.tsx`)
- [x] Error message components (`error-message.tsx`)

---

## Phase 7: Bot System (Basic Bots) ✅
**Goal**: Add AI bots to make leagues feel active
**Detailed Plan**: `.claude/plan/phase-7-detailed.md`
**Status**: Complete

### Backend
- [x] Bot entity (name, avatar, betting strategy)
- [x] LeagueBotMember join table
- [x] Predefined basic bot strategies:
  - Random bot
  - Home team favorer
  - Underdog supporter
  - Draw predictor
  - High scorer
- [x] BotBettingService to place bets
- [x] Azure Function (runs every 30 min during active hours)
- [x] Bot seeder (11 initial bots including stats-based)

### Frontend
- [x] Bot indicator on leaderboard
- [x] Option to add bots when creating/editing league
- [x] League bots management tab

---

## Phase 7.5: Intelligent Stats-Based Bots ✅
**Goal**: Add configurable bots that use statistical analysis
**Detailed Plan**: `.claude/plan/phase-7.5-detailed.md`
**Status**: Complete

### Backend
- [x] TeamFormCache entity (cached team performance stats)
- [x] StatsAnalystConfig value object (configurable weights)
- [x] TeamFormCalculator service (calculates form from match history)
- [x] StatsAnalystStrategy implementation:
  - Form analysis (last N matches)
  - Home/away performance trends
  - Goal scoring/conceding patterns
  - Streak tracking
  - High stakes detection (late season)
- [x] Form cache background service (Azure Function)
- [x] 6 intelligent bot personalities:
  - Stats Genius (balanced)
  - Form Master (recent results focused)
  - Fortress Fred (home advantage focused)
  - Goal Hunter (high-scoring focused)
  - Safe Steve (conservative)
  - Chaos Carl (unpredictable)

### Frontend
- [x] Bot strategy descriptions
- [x] Bot performance comparison view


### Future (Phase 9)
- Enhanced with standings data for position-based analysis
- Head-to-head historical records
- Derby/rivalry detection
- **Lineup analysis** - detect weakened squads:
  - Goalkeeper/defender changes
  - Top scorer absence (-12% strength)
  - Captain absence (-5% strength)
  - Formation changes
  - New bot: "Lineup Larry" (lineup-focused predictions)

---

## Phase 8: Deployment & Launch
**Goal**: Deploy MVP to production

### Infrastructure Setup
- [ ] Provision Azure resources (App Service, Functions, Azure SQL)
- [ ] Set up Azure SQL database
- [ ] Configure environment variables
- [ ] Set up Azure Static Web Apps for frontend

### Deployment
- [ ] Deploy backend API to Azure App Service
- [ ] Deploy Azure Functions
- [ ] Deploy frontend to Azure Static Web Apps
- [ ] Configure custom domain (optional)
- [ ] Set up monitoring/logging (Application Insights)

### Testing
- [ ] End-to-end testing of critical flows
- [ ] Load testing for API
- [ ] Mobile responsiveness testing

---

## Phase 9: Extended Football Data

**Goal**: Sync additional Football-Data.org subresources for richer match display and smarter bot predictions.

### New Entities
- **Standing** - League table (position, team, points, wins, draws, losses, goals)
- **Scorer** - Top scorers (player name, team, goals, assists, penalties)
- **MatchLineup** - Match lineups and substitutes
- **TeamUsualLineup** - Cached typical starting XI for lineup comparison

### New Endpoints (Football-Data.org)
- `GET /competitions/{id}/standings` - League standings
- `GET /competitions/{id}/scorers` - Top scorers
- `GET /matches/{id}` - Match details with lineups (homeTeam.lineup, awayTeam.lineup)

### New API Endpoints
- `GET /api/competitions/{id}/standings` - League table
- `GET /api/competitions/{id}/scorers` - Top scorers list
- `GET /api/matches/{id}/lineups` - Match squad/lineup data

### Bot Integration (StatsAnalyst Enhancement)
When this phase is complete, intelligent bots gain new analysis capabilities:

**Standings-based analysis:**
- League position gap detection (top 4 vs relegation)
- Title race / relegation battle high stakes boost
- Derby/rivalry detection (same city teams)

**Lineup analysis (new):**
- Goalkeeper change detection (-8% strength)
- Central defender changes (-6% each)
- Midfielder/forward rotation impact
- Top scorer absence detection (-12% strength)
- Captain absence detection (-5% strength)
- Formation change detection (-3% strength)
- New bot personality: "Lineup Larry" (heavy lineup weight)

**Head-to-head analysis:**
- Historical matchup records
- Home/away dominance patterns

### Notes
- Standings and scorers are subresources of the competition resource
- Match lineups available via expanded match endpoint
- This phase enables displaying league tables, top scorers, and match lineups in the UI
- Bot lineup analysis requires TeamUsualLineup cache (calculated from recent matches)

---

## Phase 9.5: External Data Sources Integration

**Goal**: Integrate free external data sources for enhanced bot predictions.
**Detailed Plan**: `.claude/plan/phase-9.5-detailed.md`
**Prerequisite**: Phase 9 complete

### Data Sources

| Source | Data Type | Sync Schedule | Limit |
|--------|-----------|---------------|-------|
| Understat | xG statistics | Daily 4 AM | None (scraping) |
| Football-Data.co.uk | Betting odds | Weekly Monday | None (CSV files) |
| API-Football | Injuries | On-demand | 100/day free |

### New Entities
- **TeamXgStats** - Team expected goals (xG, xGA, overperformance)
- **MatchXgStats** - Per-match xG data
- **MatchOdds** - Historical betting odds with implied probabilities
- **TeamInjuries** - Aggregated injury status
- **PlayerInjury** - Individual player injuries

### Understat Integration (Primary)
- [ ] Scrape xG data for supported leagues (PL, La Liga, Bundesliga, Serie A, Ligue 1)
- [ ] Track: xG per match, xGA per match, xG overperformance
- [ ] Calculate recent xG form (last 5 matches)
- [ ] Background sync daily at 4 AM UTC

### Football-Data.co.uk Integration (Primary)
- [ ] Import historical betting odds from CSV files
- [ ] Calculate implied probabilities (remove bookmaker margin)
- [ ] Detect market favorite and confidence level
- [ ] Track Over/Under 2.5 odds
- [ ] Weekly sync on Monday

### API-Football Integration (Optional - Limited)
- [ ] Fetch injuries for teams in upcoming matches
- [ ] Track key player injuries (top scorer, captain, GK)
- [ ] Calculate injury impact score (0-100)
- [ ] Respect 100 requests/day limit

### Enhanced Bot Strategy
New configuration weights for StatsAnalyst bots:
- `XgWeight` (0.20) - Expected goals trend
- `XgDefensiveWeight` (0.10) - xGA trend
- `OddsWeight` (0.05) - Market consensus
- `InjuryWeight` (0.05) - Squad availability

### New Bot Personalities
- 🔬 **Data Scientist** - Uses all available data sources
- 📊 **xG Expert** - Heavy xG weighting (40%)
- 💰 **Market Follower** - Follows betting odds (50%)
- 🏥 **Injury Tracker** - Focuses on squad availability

### Integration Health Monitoring
- [ ] Track status of each external data source
- [ ] Record sync success/failure with timestamps
- [ ] Calculate success rate and consecutive failures
- [ ] Detect stale data based on configurable thresholds
- [ ] Allow manual enable/disable of integrations

### Graceful Degradation
- [ ] Bots check data availability before predictions
- [ ] Redistribute weights when data sources unavailable
- [ ] Fall back to simpler strategy if data quality < 50%
- [ ] Log degradation warnings for monitoring

### Admin Bot Management
- [ ] CRUD operations for bots from admin panel
- [ ] Configuration presets for easy bot creation
- [ ] Custom weight configuration with sliders
- [ ] Bot statistics (bets placed, avg points, leagues joined)
- [ ] Activate/deactivate bots

---

## Database Schema (Core Entities)

```
Users ✅
- Id, Email, Username, PasswordHash, Role, CreatedAt, LastLoginAt
- CreatedBy, UpdatedAt, UpdatedBy (audit fields)

RefreshTokens ✅
- Id, Token, ExpiresAt, CreatedAt, RevokedAt, ReplacedByToken
- UserId (FK to Users)

BackgroundJobs ✅
- Id, JobType, Status, Payload(JSON), Result(JSON), Error
- RetryCount, MaxRetries, CreatedAt, StartedAt, CompletedAt
- ScheduledAt, CreatedByUserId, CorrelationId

Competitions ✅
- Id, ExternalId, Name, Code, Country, LogoUrl
- CurrentMatchday, CurrentSeasonStart, CurrentSeasonEnd, LastSyncedAt

Teams ✅
- Id, ExternalId, Name, ShortName, Tla, LogoUrl
- ClubColors, Venue, LastSyncedAt

CompetitionTeams (many-to-many) ✅
- Id, CompetitionId, TeamId, Season

Matches ✅
- Id, ExternalId, CompetitionId, HomeTeamId, AwayTeamId
- MatchDateUtc, Status, Matchday, Stage, Group
- HomeScore, AwayScore, HomeHalfTimeScore, AwayHalfTimeScore
- Venue, LastSyncedAt

Leagues ✅
- Id, Name, Description, OwnerId, IsPublic, MaxMembers
- ScoreExactMatch, ScoreCorrectResult, BettingDeadlineMinutes
- AllowedCompetitionIds (JSON), InviteCode, InviteCodeExpiresAt
- CreatedAt, CreatedBy, UpdatedAt, UpdatedBy (audit fields)

LeagueMembers ✅
- Id, LeagueId, UserId, Role, JoinedAt

Bets ✅
- Id, LeagueId, UserId, MatchId
- PredictedHomeScore, PredictedAwayScore
- PlacedAt, LastUpdatedAt
- CreatedAt, CreatedBy, UpdatedAt, UpdatedBy (audit fields)

BetResults ✅
- BetId (PK, FK to Bets - one-to-one)
- PointsEarned, IsExactMatch, IsCorrectResult, CalculatedAt

LeagueStandings ✅
- Id, LeagueId, UserId
- TotalPoints, BetsPlaced, ExactMatches, CorrectResults
- CurrentStreak, BestStreak, LastUpdatedAt

Bots (Phase 7)
- Id, UserId (FK), Name, AvatarUrl, Strategy, Configuration (JSON)
- IsActive, CreatedAt, LastBetPlacedAt

LeagueBotMembers (Phase 7)
- Id, LeagueId (FK), BotId (FK), AddedAt

TeamFormCaches (Phase 7.5)
- Id, TeamId (FK), CompetitionId (FK)
- MatchesPlayed, Wins, Draws, Losses, GoalsScored, GoalsConceded
- HomeMatchesPlayed, HomeWins, HomeGoalsScored, HomeGoalsConceded
- AwayMatchesPlayed, AwayWins, AwayGoalsScored, AwayGoalsConceded
- PointsPerMatch, GoalsPerMatch, GoalsConcededPerMatch
- HomeWinRate, AwayWinRate, CurrentStreak, RecentForm
- MatchesAnalyzed, CalculatedAt, LastMatchDate

Standings (Phase 9)
- Id, CompetitionId (FK), TeamId (FK), Season
- Position, PlayedGames, Won, Draw, Lost
- Points, GoalsFor, GoalsAgainst, GoalDifference
- LastUpdatedAt

Scorers (Phase 9)
- Id, CompetitionId (FK), TeamId (FK), Season
- PlayerName, PlayerId (external), Goals, Assists, Penalties
- LastUpdatedAt

MatchLineups (Phase 9)
- Id, MatchId (FK), TeamId (FK)
- Formation, GoalkeeperId, DefenderIds (JSON), MidfielderIds (JSON), ForwardIds (JSON)
- SubstituteIds (JSON), Coach

TeamUsualLineups (Phase 9 - for bot analysis)
- Id, TeamId (FK), CompetitionId (FK)
- UsualFormation, UsualGoalkeeper
- UsualDefenders (JSON), UsualMidfielders (JSON), UsualForwards (JSON)
- TopScorerId, TopScorerName, TopScorerGoals
- CaptainId, CaptainName
- MatchesAnalyzed, CalculatedAt

TeamXgStats (Phase 9.5 - Understat)
- Id, TeamId (FK), CompetitionId (FK), Season
- XgFor, XgAgainst, XgDiff
- XgPerMatch, XgAgainstPerMatch
- GoalsScored, GoalsConceded
- XgOverperformance, XgaOverperformance
- RecentXgPerMatch, RecentXgAgainstPerMatch
- MatchesPlayed, UnderstatTeamId, LastSyncedAt

MatchXgStats (Phase 9.5 - Understat)
- Id, MatchId (FK)
- HomeXg, HomeShots, HomeShotsOnTarget
- AwayXg, AwayShots, AwayShotsOnTarget
- HomeXgWin, ActualHomeWin, XgMatchedResult
- UnderstatMatchId, SyncedAt

MatchOdds (Phase 9.5 - Football-Data.co.uk)
- Id, MatchId (FK)
- HomeWinOdds, DrawOdds, AwayWinOdds
- HomeWinProbability, DrawProbability, AwayWinProbability
- Over25Odds, Under25Odds, BttsYesOdds, BttsNoOdds
- MarketFavorite, FavoriteConfidence
- DataSource, ImportedAt

TeamInjuries (Phase 9.5 - API-Football)
- Id, TeamId (FK)
- TotalInjured, KeyPlayersInjured
- LongTermInjuries, ShortTermInjuries, Doubtful
- InjuredPlayerNames (JSON)
- TopScorerInjured, CaptainInjured, FirstChoiceGkInjured
- InjuryImpactScore, LastSyncedAt

PlayerInjuries (Phase 9.5 - API-Football)
- Id, TeamId (FK)
- ExternalPlayerId, PlayerName, Position, IsKeyPlayer
- InjuryType, InjurySeverity, InjuryDate, ExpectedReturn
- IsDoubtful, IsActive, LastUpdatedAt

IntegrationStatuses (Phase 9.5 - Health Monitoring)
- Id, IntegrationName
- Health (enum: Unknown, Healthy, Degraded, Failed, Disabled)
- LastSuccessfulSync, LastAttemptedSync, LastFailedSync
- ConsecutiveFailures, TotalFailures24h, SuccessfulSyncs24h
- LastErrorMessage, LastErrorDetails
- DataFreshAsOf, StaleThreshold
- IsManuallyDisabled, DisabledReason, DisabledBy, DisabledAt
- CreatedAt, UpdatedAt
```

---

## Estimated Hosting Costs

| Service | Provider | Cost |
|---------|----------|------|
| Frontend | Azure Static Web Apps | Free |
| Backend API | Azure App Service F1 | Free |
| Database | Azure SQL | Free (32GB limit) |
| Functions | Azure Functions | Free (1M/month) |
| Football API | Football-Data.org | Free (10/min) |

**Total: $0/month** for MVP scale

---

## Supported Competitions (MVP)

- Premier League (England)
- La Liga (Spain)
- Bundesliga (Germany)
- Serie A (Italy)
- Ligue 1 (France)

---

## UI Design Direction: Playful & Gamified

### Visual Style
- Colorful palette with team colors as accents
- Card-based layouts with depth/shadows
- Achievement badges and icons throughout
- Progress bars and stat visualizations
- Celebratory animations for wins/achievements

### Gamification Elements
- Points earned animations
- Streak counters (correct predictions in a row)
- Achievement badges (First bet, Perfect score, etc.)
- Level/rank system based on total points
- Weekly/monthly challenges

### Inspiration References
- Duolingo (gamification, streaks, celebrations)
- FotMob (sports data presentation)
- Discord (playful but functional)

---

## Success Criteria for Portfolio

1. Playful, engaging UI with delightful animations
2. Strong gamification elements (badges, streaks, levels)
3. Responsive design working on all devices
4. Real-time feel (optimistic updates, celebrations)
5. Clean code architecture (showcases best practices)
6. Good TypeScript usage
7. Accessible (basic a11y compliance)
