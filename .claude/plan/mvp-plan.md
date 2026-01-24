# ExtraTime - Betting App MVP Plan

> **Detailed Plans:** Phase-specific detailed implementation plans are in separate files:
> - `.claude/phase-1-detailed.md` - Project Foundation (Backend + Frontend setup)
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

## Project Overview
A social betting app (no real money) where friends create leagues, bet on football matches, and compete for points. Portfolio project showcasing frontend skills.

## Tech Stack

### Backend
- **ASP.NET Core (.NET 9)** with Clean Architecture
- **Entity Framework Core 9** with PostgreSQL (Npgsql)
- **Mediator** (source generator, not MediatR) for CQRS pattern
- **JWT Authentication** (BCrypt + custom JWT)
- **FluentValidation** for request validation

### Frontend
- **Next.js 15** (App Router)
- **TypeScript**
- **TanStack Query** for server state management
- **Zustand** for client state
- **Tailwind CSS** + **shadcn/ui** for styling

### Infrastructure (Budget-Friendly)
- **Frontend**: Vercel (free tier)
- **Backend API**: Azure App Service Free Tier or Railway
- **Database**: PostgreSQL on Supabase (free) or Neon
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
- [x] Configure EF Core with PostgreSQL (Npgsql)
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

## Phase 6: Frontend Implementation & Polish (Portfolio Focus)
**Goal**: Build complete frontend UI with impressive, polished UX

### League System UI (from Phase 4)
- [ ] Create league form
- [ ] League list/dashboard
- [ ] League detail page with member list
- [ ] Join league via code flow
- [ ] Share invite link functionality

### Betting System UI (from Phase 5)
- [ ] Match list with betting UI
- [ ] Bet placement component (score inputs)
- [ ] My bets view
- [ ] Leaderboard page with avatars and ranks
- [ ] Match result reveal (show others' bets after deadline)
- [ ] Points earned animation/celebration

### Core Pages & Components
- [ ] Landing page (marketing/hero)
- [ ] Dashboard (upcoming matches, recent results, quick stats)
- [ ] Match day view (grouped matches by date)
- [ ] User profile page
- [ ] League standings with animations
- [ ] Responsive design (mobile-first)

### UX Enhancements
- [ ] Loading skeletons
- [ ] Toast notifications
- [ ] Optimistic updates for bets
- [ ] Dark/light mode toggle
- [ ] Micro-animations (Framer Motion)

### Gamification Features
- [ ] Achievement badge system (First Bet, Perfect Score, etc.)
- [ ] User levels/ranks based on total points
- [ ] Streak counter component
- [ ] Confetti/celebration animations for wins
- [ ] Weekly challenge cards
- [ ] Progress bars for achievements

---

## Phase 7: Bot System
**Goal**: Add AI bots to make leagues feel active

### Backend
- [ ] Bot entity (name, avatar, betting strategy)
- [ ] Predefined bot strategies:
  - Random bot
  - Home team favorer
  - Underdog supporter
  - Stats-based predictor
- [ ] Azure Function to place bot bets before deadline

### Frontend
- [ ] Bot indicator on leaderboard
- [ ] Option to add bots when creating league

---

## Phase 8: Deployment & Launch
**Goal**: Deploy MVP to production

### Infrastructure Setup
- [ ] Provision Azure resources (App Service, Functions, or alternatives)
- [ ] Set up Supabase/Neon PostgreSQL
- [ ] Configure environment variables
- [ ] Set up Vercel project for frontend

### Deployment
- [ ] Deploy backend API
- [ ] Deploy Azure Functions
- [ ] Deploy frontend to Vercel
- [ ] Configure custom domain (optional)
- [ ] Set up monitoring/logging

### Testing
- [ ] End-to-end testing of critical flows
- [ ] Load testing for API
- [ ] Mobile responsiveness testing

---

## Phase 9: Extended Football Data

**Goal**: Sync additional Football-Data.org subresources for richer match display.

### New Entities
- **Standing** - League table (position, team, points, wins, draws, losses, goals)
- **Scorer** - Top scorers (player name, team, goals, assists, penalties)
- **MatchSquad** / **MatchLineup** - Match lineups and substitutes

### New Endpoints (Football-Data.org)
- `GET /competitions/{id}/standings` - League standings
- `GET /competitions/{id}/scorers` - Top scorers
- `GET /matches/{id}` - Match details with lineups (homeTeam.lineup, awayTeam.lineup)

### New API Endpoints
- `GET /api/competitions/{id}/standings` - League table
- `GET /api/competitions/{id}/scorers` - Top scorers list
- `GET /api/matches/{id}/lineups` - Match squad/lineup data

### Notes
- Standings and scorers are subresources of the competition resource
- Match lineups available via expanded match endpoint
- This phase enables displaying league tables, top scorers, and match lineups in the UI

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
- Id, Name, AvatarUrl, Strategy
```

---

## Estimated Hosting Costs

| Service | Provider | Cost |
|---------|----------|------|
| Frontend | Vercel | Free |
| Backend API | Azure App Service F1 / Railway | Free |
| Database | Supabase / Neon | Free (500MB) |
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
