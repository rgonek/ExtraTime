# Phase 6.1: Foundation & Types

> **Goal:** Set up TypeScript types and API hooks infrastructure
> **Backend Analogy:** Creating DTOs and Repository interfaces before implementing features
> **Estimated Time:** 2-3 hours

---

## What You'll Learn

| Concept | Backend Analogy | Why It Matters |
|---------|-----------------|----------------|
| TypeScript interfaces | C# DTOs/Records | Type safety across the app |
| Generic types | `Result<T>`, `IRequest<T>` | Reusable type patterns |
| Union types | Enums | Constrained value sets |
| Type inference | `var` keyword | Less typing, same safety |
| Re-exports | `global using` | Clean import paths |

---

## Understanding TypeScript (For a C# Developer)

### Interfaces vs Types

```typescript
// TypeScript interface = C# interface (but for data shape)
interface LeagueDto {
  id: string;
  name: string;
  ownerId: string;
}

// TypeScript type = C# record (more features)
type LeagueSummary = {
  id: string;
  name: string;
};

// When to use which?
// - interface: For DTOs, component props (extendable)
// - type: For unions, computed types, aliases
```

**Backend Analogy:**
```csharp
// C# - You use records for DTOs
public sealed record LeagueDto(
    Guid Id,
    string Name,
    Guid OwnerId
);

// TypeScript - Use interfaces for the same purpose
interface LeagueDto {
  id: string;      // Guid becomes string in JSON
  name: string;
  ownerId: string;
}
```

### Union Types (Like Enums, But Better)

```typescript
// C# enum
// public enum MatchStatus { Scheduled, InPlay, Finished }

// TypeScript equivalent - union of literal types
type MatchStatus =
  | 'Scheduled'
  | 'Timed'
  | 'InPlay'
  | 'Paused'
  | 'Finished'
  | 'Postponed'
  | 'Suspended'
  | 'Cancelled';

// Why unions over enums?
// - Matches JSON string values from your API exactly
// - No enum member access confusion
// - Better tree-shaking (smaller bundles)
```

### Nullable Types

```typescript
// C# nullable
// public string? Description { get; set; }

// TypeScript - use ? or union with null/undefined
interface League {
  description?: string;           // Optional property (may not exist)
  inviteCodeExpiresAt: string | null;  // Exists but can be null
}

// The difference:
// description?: string    → property may be absent entirely
// description: string | null → property exists, value can be null
```

---

## Step 1: Create League Types

**Why:** These match your backend DTOs from `Features/Leagues/DTOs/LeagueDtos.cs`

### File: `web/src/types/league.ts`

```typescript
// ============================================================
// LEAGUE TYPES
// These mirror your backend DTOs exactly
// ============================================================

/**
 * Member role within a league
 * Backend: public enum MemberRole { Member = 0, Owner = 1 }
 */
export type MemberRole = 'Member' | 'Owner';

/**
 * Full league data returned from POST/PUT operations
 * Backend: LeagueDto record
 */
export interface LeagueDto {
  id: string;
  name: string;
  description: string | null;
  ownerId: string;
  ownerUsername: string;
  isPublic: boolean;
  maxMembers: number;
  currentMemberCount: number;
  scoreExactMatch: number;
  scoreCorrectResult: number;
  bettingDeadlineMinutes: number;
  allowedCompetitionIds: string[];
  inviteCode: string;
  inviteCodeExpiresAt: string | null;  // ISO date string
  createdAt: string;                    // ISO date string
}

/**
 * Summary for list views (less data = faster)
 * Backend: LeagueSummaryDto record
 */
export interface LeagueSummaryDto {
  id: string;
  name: string;
  ownerUsername: string;
  memberCount: number;
  isPublic: boolean;
  createdAt: string;
}

/**
 * Detail view with members included
 * Backend: LeagueDetailDto record
 */
export interface LeagueDetailDto extends LeagueDto {
  members: LeagueMemberDto[];
}

/**
 * Single member in a league
 * Backend: LeagueMemberDto record
 */
export interface LeagueMemberDto {
  userId: string;
  username: string;
  role: MemberRole;
  joinedAt: string;
}

// ============================================================
// REQUEST TYPES (What we send TO the API)
// ============================================================

/**
 * Create a new league
 * Backend: CreateLeagueRequest record
 */
export interface CreateLeagueRequest {
  name: string;
  description?: string;
  isPublic?: boolean;
  maxMembers?: number;
  scoreExactMatch?: number;
  scoreCorrectResult?: number;
  bettingDeadlineMinutes?: number;
  allowedCompetitionIds?: string[];
  expiresAt?: string;
}

/**
 * Update existing league
 * Backend: UpdateLeagueRequest record
 */
export interface UpdateLeagueRequest {
  name: string;
  description?: string;
  isPublic?: boolean;
  maxMembers?: number;
  scoreExactMatch?: number;
  scoreCorrectResult?: number;
  bettingDeadlineMinutes?: number;
  allowedCompetitionIds?: string[];
}

/**
 * Join via invite code
 * Backend: JoinLeagueRequest record
 */
export interface JoinLeagueRequest {
  inviteCode: string;
}

/**
 * Regenerate invite code
 * Backend: RegenerateInviteCodeRequest record
 */
export interface RegenerateInviteCodeRequest {
  expiresAt?: string;
}

/**
 * Response from invite code regeneration
 */
export interface InviteCodeResponse {
  inviteCode: string;
  expiresAt: string | null;
}
```

**Key Learning Point:**
- `interface extends` is like C# inheritance: `LeagueDetailDto extends LeagueDto`
- Optional properties with `?` are like nullable parameters with defaults
- String dates because JSON doesn't have a Date type (you'll parse them client-side)

---

## Step 2: Create Bet Types

**Why:** These match your backend DTOs from `Features/Betting/DTOs/BetDtos.cs`

### File: `web/src/types/bet.ts`

```typescript
// ============================================================
// BET TYPES
// ============================================================

/**
 * Result of a scored bet
 * Backend: BetResultDto record
 */
export interface BetResultDto {
  pointsEarned: number;
  isExactMatch: boolean;
  isCorrectResult: boolean;
}

/**
 * Basic bet data
 * Backend: BetDto record
 */
export interface BetDto {
  id: string;
  leagueId: string;
  userId: string;
  matchId: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
  placedAt: string;
  lastUpdatedAt: string | null;
  result: BetResultDto | null;  // null until match is scored
}

/**
 * User's bet with match context (for "My Bets" view)
 * Backend: MyBetDto record
 *
 * Backend Analogy: This is like a "projected DTO" that joins
 * Bet with Match data for display purposes
 */
export interface MyBetDto {
  betId: string;
  matchId: string;
  homeTeamName: string;
  awayTeamName: string;
  matchDateUtc: string;
  matchStatus: MatchStatus;
  actualHomeScore: number | null;
  actualAwayScore: number | null;
  predictedHomeScore: number;
  predictedAwayScore: number;
  result: BetResultDto | null;
  placedAt: string;
}

/**
 * Another user's bet on a match (for reveal after deadline)
 * Backend: MatchBetDto record
 */
export interface MatchBetDto {
  userId: string;
  username: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
  result: BetResultDto | null;
}

/**
 * User's position in league standings
 * Backend: LeagueStandingDto record
 */
export interface LeagueStandingDto {
  userId: string;
  username: string;
  email: string;
  rank: number;
  totalPoints: number;
  betsPlaced: number;
  exactMatches: number;
  correctResults: number;
  currentStreak: number;
  bestStreak: number;
  lastUpdatedAt: string;
}

/**
 * Detailed user statistics in a league
 * Backend: UserStatsDto record
 */
export interface UserStatsDto {
  userId: string;
  username: string;
  totalPoints: number;
  betsPlaced: number;
  exactMatches: number;
  correctResults: number;
  currentStreak: number;
  bestStreak: number;
  accuracyPercentage: number;
  rank: number;
  lastUpdatedAt: string;
}

// ============================================================
// REQUEST TYPES
// ============================================================

/**
 * Place or update a bet
 * Backend: PlaceBetRequest record
 */
export interface PlaceBetRequest {
  matchId: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
}

// ============================================================
// MATCH STATUS (used in bet context)
// ============================================================

/**
 * Match status enum as union type
 * Backend: public enum MatchStatus
 *
 * Why union over enum?
 * Your API returns these as strings. TypeScript unions
 * match the JSON exactly without conversion.
 */
export type MatchStatus =
  | 'Scheduled'
  | 'Timed'
  | 'InPlay'
  | 'Paused'
  | 'Finished'
  | 'Postponed'
  | 'Suspended'
  | 'Cancelled';
```

---

## Step 3: Create Match/Football Types

### File: `web/src/types/match.ts`

```typescript
// ============================================================
// FOOTBALL DATA TYPES
// These are read-only (no requests, just responses)
// ============================================================

import type { MatchStatus } from './bet';

/**
 * Competition (league/tournament) summary
 * Backend: CompetitionSummaryDto record
 */
export interface CompetitionSummaryDto {
  id: string;
  name: string;
  code: string;
  country: string;
  logoUrl: string | null;
}

/**
 * Full competition details
 * Backend: CompetitionDto record
 */
export interface CompetitionDto extends CompetitionSummaryDto {
  externalId: number;
  currentMatchday: number | null;
  currentSeasonStart: string | null;
  currentSeasonEnd: string | null;
  lastSyncedAt: string;
}

/**
 * Team summary for match display
 * Backend: TeamSummaryDto record
 */
export interface TeamSummaryDto {
  id: string;
  name: string;
  shortName: string;
  crest: string | null;  // Team logo URL
}

/**
 * Match for list views
 * Backend: MatchDto record
 */
export interface MatchDto {
  id: string;
  competition: CompetitionSummaryDto;
  homeTeam: TeamSummaryDto;
  awayTeam: TeamSummaryDto;
  matchDateUtc: string;
  status: MatchStatus;
  matchday: number | null;
  homeScore: number | null;
  awayScore: number | null;
}

/**
 * Match with full details
 * Backend: MatchDetailDto record
 */
export interface MatchDetailDto extends MatchDto {
  stage: string | null;
  group: string | null;
  homeHalfTimeScore: number | null;
  awayHalfTimeScore: number | null;
  venue: string | null;
  lastSyncedAt: string;
}

// ============================================================
// PAGINATED RESPONSE
// ============================================================

/**
 * Generic paginated response wrapper
 * Backend: You use this pattern for all list endpoints
 *
 * TypeScript generics work like C# generics:
 * PagedResponse<MatchDto> = { items: MatchDto[], ... }
 */
export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Specific alias for matches (for clarity)
 */
export type MatchesPagedResponse = PagedResponse<MatchDto>;

// ============================================================
// QUERY PARAMETERS
// ============================================================

/**
 * Filters for match list endpoint
 * These become URL query params: ?page=1&pageSize=20&competitionId=xxx
 */
export interface MatchFilters {
  page?: number;
  pageSize?: number;
  competitionId?: string;
  dateFrom?: string;
  dateTo?: string;
  status?: MatchStatus;
}
```

---

## Step 4: Update Index Re-exports

**Why:** Single import point, like C# `global using` statements

### File: `web/src/types/index.ts`

```typescript
// ============================================================
// TYPES INDEX
// Central export point for all types
//
// Usage: import { LeagueDto, BetDto, MatchDto } from '@/types';
// Instead of: import { LeagueDto } from '@/types/league';
// ============================================================

// Auth types (existing)
export type {
  User,
  RegisterRequest,
  LoginRequest,
  RefreshTokenRequest,
  AuthResponse,
  CurrentUserResponse,
  ApiError
} from './auth';

// League types
export type {
  MemberRole,
  LeagueDto,
  LeagueSummaryDto,
  LeagueDetailDto,
  LeagueMemberDto,
  CreateLeagueRequest,
  UpdateLeagueRequest,
  JoinLeagueRequest,
  RegenerateInviteCodeRequest,
  InviteCodeResponse,
} from './league';

// Bet types
export type {
  MatchStatus,
  BetResultDto,
  BetDto,
  MyBetDto,
  MatchBetDto,
  LeagueStandingDto,
  UserStatsDto,
  PlaceBetRequest,
} from './bet';

// Match/Football types
export type {
  CompetitionSummaryDto,
  CompetitionDto,
  TeamSummaryDto,
  MatchDto,
  MatchDetailDto,
  PagedResponse,
  MatchesPagedResponse,
  MatchFilters,
} from './match';
```

**Note:** We need to move existing auth types to a separate file first.

### File: `web/src/types/auth.ts` (move existing types here)

```typescript
// ============================================================
// AUTH TYPES (moved from index.ts)
// ============================================================

export interface User {
  id: string;
  email: string;
  username: string;
  role: 'User' | 'Admin';
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface AuthResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
}

export interface CurrentUserResponse {
  id: string;
  email: string;
  username: string;
  role: string;
}

export interface ApiError {
  message: string;
  statusCode?: number;
  errors?: Record<string, string[]>;
}
```

---

## Step 5: Create League Hooks

**Backend Analogy:** This is like creating your `LeagueEndpoints.cs` handlers, but for the frontend.

### File: `web/src/hooks/use-leagues.ts`

```typescript
'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type {
  LeagueSummaryDto,
  LeagueDetailDto,
  LeagueDto,
  CreateLeagueRequest,
  UpdateLeagueRequest,
  JoinLeagueRequest,
  InviteCodeResponse,
  RegenerateInviteCodeRequest,
  ApiError,
} from '@/types';

// ============================================================
// QUERY KEYS
// ============================================================
// Query keys are like cache keys in your backend.
// They identify what data is cached and when to invalidate.
//
// Pattern: ['resource', ...identifiers]
// - ['leagues'] = all leagues
// - ['leagues', id] = specific league
// ============================================================

export const leagueKeys = {
  all: ['leagues'] as const,
  lists: () => [...leagueKeys.all, 'list'] as const,
  list: () => [...leagueKeys.lists()] as const,
  details: () => [...leagueKeys.all, 'detail'] as const,
  detail: (id: string) => [...leagueKeys.details(), id] as const,
};

// ============================================================
// QUERIES (GET operations)
// ============================================================

/**
 * Get all leagues for current user
 *
 * Backend equivalent:
 * GET /api/leagues
 * LeagueEndpoints.GetUserLeagues()
 */
export function useLeagues() {
  return useQuery<LeagueSummaryDto[], ApiError>({
    queryKey: leagueKeys.list(),
    queryFn: () => apiClient.get<LeagueSummaryDto[]>('/leagues'),
    // staleTime: How long data is considered fresh (no refetch)
    // Think of it like cache expiration
    staleTime: 30 * 1000, // 30 seconds
  });
}

/**
 * Get single league with details
 *
 * Backend equivalent:
 * GET /api/leagues/{id}
 * LeagueEndpoints.GetLeague(Guid id)
 */
export function useLeague(id: string) {
  return useQuery<LeagueDetailDto, ApiError>({
    queryKey: leagueKeys.detail(id),
    queryFn: () => apiClient.get<LeagueDetailDto>(`/leagues/${id}`),
    // enabled: Only fetch when id exists (like a guard clause)
    enabled: !!id,
    staleTime: 30 * 1000,
  });
}

// ============================================================
// MUTATIONS (POST/PUT/DELETE operations)
// ============================================================

/**
 * Create a new league
 *
 * Backend equivalent:
 * POST /api/leagues
 * CreateLeagueCommand + CreateLeagueCommandHandler
 *
 * The mutation pattern:
 * 1. mutationFn: The actual API call (like Handle method)
 * 2. onSuccess: What to do after success (invalidate cache)
 * 3. onError: Handle errors (shown in UI)
 */
export function useCreateLeague() {
  const queryClient = useQueryClient();

  return useMutation<LeagueDto, ApiError, CreateLeagueRequest>({
    mutationFn: (data) => apiClient.post<LeagueDto>('/leagues', data),
    onSuccess: () => {
      // Invalidate cache = mark data as stale, triggering refetch
      // Like clearing a cache entry so fresh data loads
      queryClient.invalidateQueries({ queryKey: leagueKeys.lists() });
    },
  });
}

/**
 * Update existing league
 *
 * Backend equivalent:
 * PUT /api/leagues/{id}
 * UpdateLeagueCommand + UpdateLeagueCommandHandler
 */
export function useUpdateLeague(id: string) {
  const queryClient = useQueryClient();

  return useMutation<LeagueDto, ApiError, UpdateLeagueRequest>({
    mutationFn: (data) => apiClient.put<LeagueDto>(`/leagues/${id}`, data),
    onSuccess: (updatedLeague) => {
      // Update the specific league in cache
      queryClient.setQueryData(leagueKeys.detail(id), updatedLeague);
      // Also refresh the list
      queryClient.invalidateQueries({ queryKey: leagueKeys.lists() });
    },
  });
}

/**
 * Delete a league
 *
 * Backend equivalent:
 * DELETE /api/leagues/{id}
 */
export function useDeleteLeague() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, string>({
    mutationFn: (id) => apiClient.delete(`/leagues/${id}`),
    onSuccess: (_, id) => {
      // Remove from cache and refresh list
      queryClient.removeQueries({ queryKey: leagueKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: leagueKeys.lists() });
    },
  });
}

/**
 * Join a league via invite code
 *
 * Backend equivalent:
 * POST /api/leagues/{id}/join
 * JoinLeagueCommand
 */
export function useJoinLeague(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation<{ message: string }, ApiError, JoinLeagueRequest>({
    mutationFn: (data) => apiClient.post(`/leagues/${leagueId}/join`, data),
    onSuccess: () => {
      // Refresh leagues list (user now has a new league)
      queryClient.invalidateQueries({ queryKey: leagueKeys.all });
    },
  });
}

/**
 * Leave a league
 *
 * Backend equivalent:
 * DELETE /api/leagues/{id}/leave
 */
export function useLeaveLeague() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, string>({
    mutationFn: (leagueId) => apiClient.delete(`/leagues/${leagueId}/leave`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: leagueKeys.all });
    },
  });
}

/**
 * Kick a member from league
 *
 * Backend equivalent:
 * DELETE /api/leagues/{id}/members/{userId}
 */
export function useKickMember(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, string>({
    mutationFn: (userId) =>
      apiClient.delete(`/leagues/${leagueId}/members/${userId}`),
    onSuccess: () => {
      // Refresh league detail to update member list
      queryClient.invalidateQueries({ queryKey: leagueKeys.detail(leagueId) });
    },
  });
}

/**
 * Regenerate invite code
 *
 * Backend equivalent:
 * POST /api/leagues/{id}/invite-code/regenerate
 */
export function useRegenerateInviteCode(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation<InviteCodeResponse, ApiError, RegenerateInviteCodeRequest | undefined>({
    mutationFn: (data) =>
      apiClient.post<InviteCodeResponse>(
        `/leagues/${leagueId}/invite-code/regenerate`,
        data ?? {}
      ),
    onSuccess: () => {
      // Refresh league detail to show new invite code
      queryClient.invalidateQueries({ queryKey: leagueKeys.detail(leagueId) });
    },
  });
}
```

---

## Step 6: Create Bet Hooks (Skeleton)

### File: `web/src/hooks/use-bets.ts`

```typescript
'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type {
  BetDto,
  MyBetDto,
  MatchBetDto,
  LeagueStandingDto,
  UserStatsDto,
  PlaceBetRequest,
  ApiError,
} from '@/types';

// ============================================================
// QUERY KEYS
// ============================================================

export const betKeys = {
  all: ['bets'] as const,
  // My bets in a specific league
  myBets: (leagueId: string) => [...betKeys.all, 'my', leagueId] as const,
  // All bets on a specific match
  matchBets: (leagueId: string, matchId: string) =>
    [...betKeys.all, 'match', leagueId, matchId] as const,
  // League standings
  standings: (leagueId: string) =>
    [...betKeys.all, 'standings', leagueId] as const,
  // User stats in league
  userStats: (leagueId: string, userId: string) =>
    [...betKeys.all, 'stats', leagueId, userId] as const,
};

// ============================================================
// QUERIES
// ============================================================

/**
 * Get current user's bets in a league
 * GET /api/leagues/{leagueId}/bets/my
 */
export function useMyBets(leagueId: string) {
  return useQuery<MyBetDto[], ApiError>({
    queryKey: betKeys.myBets(leagueId),
    queryFn: () => apiClient.get<MyBetDto[]>(`/leagues/${leagueId}/bets/my`),
    enabled: !!leagueId,
    staleTime: 30 * 1000,
  });
}

/**
 * Get all bets on a specific match (visible after deadline)
 * GET /api/leagues/{leagueId}/matches/{matchId}/bets
 */
export function useMatchBets(leagueId: string, matchId: string) {
  return useQuery<MatchBetDto[], ApiError>({
    queryKey: betKeys.matchBets(leagueId, matchId),
    queryFn: () =>
      apiClient.get<MatchBetDto[]>(`/leagues/${leagueId}/matches/${matchId}/bets`),
    enabled: !!leagueId && !!matchId,
    staleTime: 60 * 1000, // 1 minute (bets don't change often)
  });
}

/**
 * Get league standings
 * GET /api/leagues/{leagueId}/standings
 */
export function useLeagueStandings(leagueId: string) {
  return useQuery<LeagueStandingDto[], ApiError>({
    queryKey: betKeys.standings(leagueId),
    queryFn: () =>
      apiClient.get<LeagueStandingDto[]>(`/leagues/${leagueId}/standings`),
    enabled: !!leagueId,
    staleTime: 60 * 1000,
  });
}

/**
 * Get user's stats in a league
 * GET /api/leagues/{leagueId}/users/{userId}/stats
 */
export function useUserStats(leagueId: string, userId: string) {
  return useQuery<UserStatsDto, ApiError>({
    queryKey: betKeys.userStats(leagueId, userId),
    queryFn: () =>
      apiClient.get<UserStatsDto>(`/leagues/${leagueId}/users/${userId}/stats`),
    enabled: !!leagueId && !!userId,
    staleTime: 60 * 1000,
  });
}

// ============================================================
// MUTATIONS
// ============================================================

/**
 * Place or update a bet
 * POST /api/leagues/{leagueId}/bets
 *
 * Note: This endpoint creates or updates depending on whether
 * a bet already exists for this match
 */
export function usePlaceBet(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation<BetDto, ApiError, PlaceBetRequest>({
    mutationFn: (data) =>
      apiClient.post<BetDto>(`/leagues/${leagueId}/bets`, data),
    onSuccess: () => {
      // Refresh my bets list
      queryClient.invalidateQueries({ queryKey: betKeys.myBets(leagueId) });
    },
  });
}

/**
 * Delete a bet
 * DELETE /api/leagues/{leagueId}/bets/{betId}
 */
export function useDeleteBet(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, string>({
    mutationFn: (betId) =>
      apiClient.delete(`/leagues/${leagueId}/bets/${betId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: betKeys.myBets(leagueId) });
    },
  });
}
```

---

## Step 7: Create Match Hooks

### File: `web/src/hooks/use-matches.ts`

```typescript
'use client';

import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type {
  CompetitionDto,
  MatchDto,
  MatchDetailDto,
  MatchesPagedResponse,
  MatchFilters,
  ApiError,
} from '@/types';

// ============================================================
// QUERY KEYS
// ============================================================

export const matchKeys = {
  all: ['matches'] as const,
  lists: () => [...matchKeys.all, 'list'] as const,
  list: (filters?: MatchFilters) => [...matchKeys.lists(), filters] as const,
  details: () => [...matchKeys.all, 'detail'] as const,
  detail: (id: string) => [...matchKeys.details(), id] as const,
  competitions: ['competitions'] as const,
};

// ============================================================
// HELPER: Build query string from filters
// ============================================================

function buildQueryString(filters?: MatchFilters): string {
  if (!filters) return '';

  const params = new URLSearchParams();

  if (filters.page) params.set('page', String(filters.page));
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize));
  if (filters.competitionId) params.set('competitionId', filters.competitionId);
  if (filters.dateFrom) params.set('dateFrom', filters.dateFrom);
  if (filters.dateTo) params.set('dateTo', filters.dateTo);
  if (filters.status) params.set('status', filters.status);

  const queryString = params.toString();
  return queryString ? `?${queryString}` : '';
}

// ============================================================
// QUERIES
// ============================================================

/**
 * Get all available competitions
 * GET /api/competitions
 *
 * Note: This is a public endpoint (no auth required)
 */
export function useCompetitions() {
  return useQuery<CompetitionDto[], ApiError>({
    queryKey: matchKeys.competitions,
    queryFn: () => apiClient.get<CompetitionDto[]>('/competitions'),
    // Competitions rarely change, cache for longer
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get matches with pagination and filters
 * GET /api/matches?page=1&pageSize=20&...
 *
 * Note: This is a public endpoint
 */
export function useMatches(filters?: MatchFilters) {
  return useQuery<MatchesPagedResponse, ApiError>({
    queryKey: matchKeys.list(filters),
    queryFn: () =>
      apiClient.get<MatchesPagedResponse>(`/matches${buildQueryString(filters)}`),
    staleTime: 30 * 1000, // 30 seconds (matches update frequently)
  });
}

/**
 * Get single match details
 * GET /api/matches/{id}
 */
export function useMatch(id: string) {
  return useQuery<MatchDetailDto, ApiError>({
    queryKey: matchKeys.detail(id),
    queryFn: () => apiClient.get<MatchDetailDto>(`/matches/${id}`),
    enabled: !!id,
    staleTime: 30 * 1000,
  });
}
```

---

## Step 8: Create Shared Components

### File: `web/src/components/shared/loading-skeleton.tsx`

```typescript
import { Skeleton } from '@/components/ui/skeleton';

/**
 * Card skeleton for loading states
 *
 * Why skeletons?
 * - Better perceived performance than spinners
 * - User sees the layout before data loads
 * - Reduces layout shift when data arrives
 */
export function CardSkeleton() {
  return (
    <div className="rounded-lg border bg-card p-4">
      <Skeleton className="h-5 w-2/3 mb-2" />
      <Skeleton className="h-4 w-1/2 mb-4" />
      <Skeleton className="h-4 w-full" />
    </div>
  );
}

/**
 * List of card skeletons
 */
export function CardListSkeleton({ count = 3 }: { count?: number }) {
  return (
    <div className="space-y-4">
      {Array.from({ length: count }).map((_, i) => (
        <CardSkeleton key={i} />
      ))}
    </div>
  );
}

/**
 * Grid of card skeletons
 */
export function CardGridSkeleton({ count = 6 }: { count?: number }) {
  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: count }).map((_, i) => (
        <CardSkeleton key={i} />
      ))}
    </div>
  );
}
```

### File: `web/src/components/shared/empty-state.tsx`

```typescript
import { LucideIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface EmptyStateProps {
  icon: LucideIcon;
  title: string;
  description: string;
  action?: {
    label: string;
    onClick: () => void;
  };
}

/**
 * Empty state component for when lists have no items
 *
 * Usage:
 * <EmptyState
 *   icon={Trophy}
 *   title="No leagues yet"
 *   description="Create your first league to get started"
 *   action={{ label: "Create League", onClick: () => ... }}
 * />
 */
export function EmptyState({ icon: Icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-12 text-center">
      <div className="rounded-full bg-muted p-4 mb-4">
        <Icon className="h-8 w-8 text-muted-foreground" />
      </div>
      <h3 className="text-lg font-semibold mb-1">{title}</h3>
      <p className="text-muted-foreground mb-4 max-w-sm">{description}</p>
      {action && (
        <Button onClick={action.onClick}>{action.label}</Button>
      )}
    </div>
  );
}
```

### File: `web/src/components/shared/error-message.tsx`

```typescript
import { AlertCircle } from 'lucide-react';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';

interface ErrorMessageProps {
  title?: string;
  message: string;
  onRetry?: () => void;
}

/**
 * Error message component with optional retry
 */
export function ErrorMessage({
  title = 'Something went wrong',
  message,
  onRetry
}: ErrorMessageProps) {
  return (
    <Alert variant="destructive">
      <AlertCircle className="h-4 w-4" />
      <AlertTitle>{title}</AlertTitle>
      <AlertDescription className="flex items-center justify-between">
        <span>{message}</span>
        {onRetry && (
          <Button variant="outline" size="sm" onClick={onRetry}>
            Try again
          </Button>
        )}
      </AlertDescription>
    </Alert>
  );
}
```

---

## Step 9: Add shadcn/ui Components

Run these commands to add required UI components:

```bash
cd web
npx shadcn@latest add skeleton
npx shadcn@latest add alert
```

---

## Verification Checklist

After completing Phase 6.1:

- [ ] All type files created and match backend DTOs
- [ ] Types are exported from `@/types` index
- [ ] League hooks created with all CRUD operations
- [ ] Bet hooks created with queries and mutations
- [ ] Match hooks created for football data
- [ ] Skeleton components work (test in dashboard)
- [ ] Empty state component renders correctly
- [ ] Error message component renders correctly
- [ ] `npm run build` passes with no TypeScript errors
- [ ] `npm run dev` starts without errors

---

## Key Learnings from This Phase

1. **TypeScript interfaces mirror C# DTOs** - Same structure, different syntax
2. **Query keys are like cache keys** - They identify what data to cache/invalidate
3. **useQuery = read operations** (like GET endpoints)
4. **useMutation = write operations** (like POST/PUT/DELETE endpoints)
5. **staleTime = cache duration** - How long data is considered fresh
6. **enabled = conditional fetching** - Like guard clauses in backend
7. **Invalidation = cache busting** - Force refetch of stale data

---

## Next Step

Proceed to **Phase 6.2: League System UI** (`phase-6.2-leagues.md`)
