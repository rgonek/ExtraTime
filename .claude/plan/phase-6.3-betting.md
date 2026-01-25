# Phase 6.3: Betting System Core

> **Goal:** Build the core betting experience with match lists and bet placement
> **Backend Analogy:** Complex command handling with business rules and real-time considerations
> **Estimated Time:** 8-10 hours
> **Prerequisites:** Phase 6.2 complete (league system working)

---

## What You'll Learn

| Frontend Concept | Backend Analogy | Example |
|------------------|-----------------|---------|
| Optimistic updates | Write-through cache | Update UI before API confirms |
| Query invalidation | Cache busting | Refresh data after mutation |
| Derived state | Computed properties | Calculate from existing state |
| useCallback | Cached delegates | Memoize event handlers |
| Complex mutations | Command handlers with side effects | Place bet with validation |
| Date/time handling | DateTime manipulation | Deadline calculations |

---

## Understanding Optimistic Updates (Critical Concept)

### The Problem Without Optimistic Updates

```
1. User clicks "Place Bet"
2. Show loading spinner
3. Wait 500ms-2000ms for API response
4. Update UI

Result: App feels slow and unresponsive
```

### With Optimistic Updates

```
1. User clicks "Place Bet"
2. Immediately update UI (assume success)
3. Send API request in background
4. If success: Do nothing (UI already correct)
5. If fail: Rollback UI to previous state

Result: App feels instant
```

### Backend Analogy: Write-Through Cache

```csharp
// Backend: Write-through cache pattern
public async Task<BetDto> PlaceBet(PlaceBetRequest request)
{
    // 1. Update cache immediately (optimistic)
    _cache.Set($"bet:{request.MatchId}", request);

    try
    {
        // 2. Persist to database
        var bet = await _repository.SaveAsync(request);
        return bet;
    }
    catch
    {
        // 3. Rollback cache on failure
        _cache.Remove($"bet:{request.MatchId}");
        throw;
    }
}

// Frontend equivalent with TanStack Query:
useMutation({
  mutationFn: placeBet,
  onMutate: async (newBet) => {
    // 1. Cancel in-flight queries (prevent race conditions)
    await queryClient.cancelQueries(['myBets']);

    // 2. Snapshot current state (for potential rollback)
    const previousBets = queryClient.getQueryData(['myBets']);

    // 3. Optimistically update cache
    queryClient.setQueryData(['myBets'], old => [...old, newBet]);

    // 4. Return context for rollback
    return { previousBets };
  },
  onError: (err, newBet, context) => {
    // 5. Rollback on error
    queryClient.setQueryData(['myBets'], context.previousBets);
  },
  onSettled: () => {
    // 6. Refetch to ensure consistency (eventual consistency)
    queryClient.invalidateQueries(['myBets']);
  },
});
```

---

## Understanding useCallback (Memoization)

### The Problem

```typescript
// This creates a NEW function on every render
function BetCard({ bet }) {
  const handleDelete = () => {
    deleteBet(bet.id);
  };

  // Child components that receive handleDelete will re-render
  // every time BetCard renders (even if nothing changed)
  return <DeleteButton onClick={handleDelete} />;
}
```

### The Solution

```typescript
// useCallback memoizes the function
function BetCard({ bet }) {
  const handleDelete = useCallback(() => {
    deleteBet(bet.id);
  }, [bet.id]); // Only recreate if bet.id changes

  // Now DeleteButton only re-renders if handleDelete actually changes
  return <DeleteButton onClick={handleDelete} />;
}
```

### Backend Analogy: Cached Delegate

```csharp
// C# - You wouldn't create new delegates unnecessarily
private readonly Action<Guid> _cachedDeleteHandler;

public BetService()
{
    _cachedDeleteHandler = DeleteBet; // Cached once
}

// vs creating new delegate each time
public void ProcessBet()
{
    Action<Guid> delete = DeleteBet; // New delegate each call - wasteful
}
```

### When to Use useCallback

| Situation | Use useCallback? |
|-----------|------------------|
| Passing callback to child component | Yes |
| Inline event handler | No |
| Callback in dependency array | Yes |
| Simple click handler | Usually no |

---

## Step 1: Update Bet Hooks with Optimistic Updates

### File: `web/src/hooks/use-bets.ts` (Update)

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
  myBets: (leagueId: string) => [...betKeys.all, 'my', leagueId] as const,
  matchBets: (leagueId: string, matchId: string) =>
    [...betKeys.all, 'match', leagueId, matchId] as const,
  standings: (leagueId: string) =>
    [...betKeys.all, 'standings', leagueId] as const,
  userStats: (leagueId: string, userId: string) =>
    [...betKeys.all, 'stats', leagueId, userId] as const,
};

// ============================================================
// QUERIES
// ============================================================

export function useMyBets(leagueId: string) {
  return useQuery<MyBetDto[], ApiError>({
    queryKey: betKeys.myBets(leagueId),
    queryFn: () => apiClient.get<MyBetDto[]>(`/leagues/${leagueId}/bets/my`),
    enabled: !!leagueId,
    staleTime: 30 * 1000,
  });
}

export function useMatchBets(leagueId: string, matchId: string) {
  return useQuery<MatchBetDto[], ApiError>({
    queryKey: betKeys.matchBets(leagueId, matchId),
    queryFn: () =>
      apiClient.get<MatchBetDto[]>(`/leagues/${leagueId}/matches/${matchId}/bets`),
    enabled: !!leagueId && !!matchId,
    staleTime: 60 * 1000,
  });
}

export function useLeagueStandings(leagueId: string) {
  return useQuery<LeagueStandingDto[], ApiError>({
    queryKey: betKeys.standings(leagueId),
    queryFn: () =>
      apiClient.get<LeagueStandingDto[]>(`/leagues/${leagueId}/standings`),
    enabled: !!leagueId,
    staleTime: 60 * 1000,
  });
}

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
// MUTATIONS WITH OPTIMISTIC UPDATES
// ============================================================

/**
 * Place or update a bet with optimistic update
 *
 * This demonstrates the full optimistic update pattern:
 * 1. onMutate: Update cache optimistically BEFORE API call
 * 2. onError: Rollback on failure
 * 3. onSettled: Always refetch to ensure consistency
 */
export function usePlaceBet(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation<BetDto, ApiError, PlaceBetRequest, { previousBets?: MyBetDto[] }>({
    mutationFn: (data) =>
      apiClient.post<BetDto>(`/leagues/${leagueId}/bets`, data),

    // OPTIMISTIC UPDATE: Run BEFORE the API call
    onMutate: async (newBet) => {
      // 1. Cancel any outgoing refetches
      // This prevents race conditions where refetch returns old data
      await queryClient.cancelQueries({ queryKey: betKeys.myBets(leagueId) });

      // 2. Snapshot the previous value
      const previousBets = queryClient.getQueryData<MyBetDto[]>(
        betKeys.myBets(leagueId)
      );

      // 3. Optimistically update to the new value
      if (previousBets) {
        const existingIndex = previousBets.findIndex(
          (b) => b.matchId === newBet.matchId
        );

        if (existingIndex >= 0) {
          // Update existing bet
          const updated = [...previousBets];
          updated[existingIndex] = {
            ...updated[existingIndex],
            predictedHomeScore: newBet.predictedHomeScore,
            predictedAwayScore: newBet.predictedAwayScore,
          };
          queryClient.setQueryData(betKeys.myBets(leagueId), updated);
        }
        // Note: For new bets, we don't add optimistically because
        // we don't have all the required MyBetDto fields (team names, etc.)
      }

      // 4. Return context with previous value for rollback
      return { previousBets };
    },

    // ROLLBACK: If mutation fails, restore previous state
    onError: (_err, _newBet, context) => {
      if (context?.previousBets) {
        queryClient.setQueryData(betKeys.myBets(leagueId), context.previousBets);
      }
    },

    // ALWAYS REFETCH: Ensure cache matches server
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: betKeys.myBets(leagueId) });
    },
  });
}

/**
 * Delete a bet with optimistic update
 */
export function useDeleteBet(leagueId: string) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, string, { previousBets?: MyBetDto[] }>({
    mutationFn: (betId) =>
      apiClient.delete(`/leagues/${leagueId}/bets/${betId}`),

    onMutate: async (betId) => {
      await queryClient.cancelQueries({ queryKey: betKeys.myBets(leagueId) });

      const previousBets = queryClient.getQueryData<MyBetDto[]>(
        betKeys.myBets(leagueId)
      );

      // Optimistically remove the bet
      if (previousBets) {
        queryClient.setQueryData(
          betKeys.myBets(leagueId),
          previousBets.filter((b) => b.betId !== betId)
        );
      }

      return { previousBets };
    },

    onError: (_err, _betId, context) => {
      if (context?.previousBets) {
        queryClient.setQueryData(betKeys.myBets(leagueId), context.previousBets);
      }
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: betKeys.myBets(leagueId) });
    },
  });
}
```

---

## Step 2: Create Match Card Component

### File: `web/src/components/bets/match-card.tsx`

```typescript
'use client';

import { useState } from 'react';
import { Calendar, Clock, Trophy } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { BetForm } from './bet-form';
import type { MatchDto, MyBetDto, MatchStatus } from '@/types';

interface MatchCardProps {
  match: MatchDto;
  leagueId: string;
  existingBet?: MyBetDto;
  bettingDeadlineMinutes: number;
}

/**
 * Card displaying a match with betting functionality
 *
 * Demonstrates:
 * - Derived state (canBet calculation)
 * - Conditional rendering based on match status
 * - Date/time formatting and calculations
 */
export function MatchCard({
  match,
  leagueId,
  existingBet,
  bettingDeadlineMinutes,
}: MatchCardProps) {
  const [isExpanded, setIsExpanded] = useState(false);

  // ============================================================
  // DERIVED STATE
  // ============================================================
  // Derived state = calculated from existing state/props
  // Like computed properties in C#
  // Don't store in useState - calculate on render
  // ============================================================

  const matchDate = new Date(match.matchDateUtc);
  const now = new Date();

  // Calculate deadline (match time - deadline minutes)
  const deadlineDate = new Date(
    matchDate.getTime() - bettingDeadlineMinutes * 60 * 1000
  );

  // Can bet if: before deadline AND match not started/finished
  const canBet =
    now < deadlineDate &&
    (match.status === 'Scheduled' || match.status === 'Timed');

  // Time remaining until deadline (for countdown)
  const timeUntilDeadline = deadlineDate.getTime() - now.getTime();
  const hoursUntilDeadline = Math.floor(timeUntilDeadline / (1000 * 60 * 60));
  const minutesUntilDeadline = Math.floor(
    (timeUntilDeadline % (1000 * 60 * 60)) / (1000 * 60)
  );

  // Format match time for display
  const matchTimeFormatted = matchDate.toLocaleString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });

  return (
    <Card
      className={`transition-all ${isExpanded ? 'ring-2 ring-primary' : ''}`}
    >
      <CardContent className="p-4">
        {/* Match header - teams and score */}
        <div
          className="flex items-center justify-between cursor-pointer"
          onClick={() => canBet && setIsExpanded(!isExpanded)}
        >
          {/* Teams */}
          <div className="flex-1 space-y-2">
            {/* Home team */}
            <div className="flex items-center gap-2">
              {match.homeTeam.crest && (
                <img
                  src={match.homeTeam.crest}
                  alt=""
                  className="w-6 h-6 object-contain"
                />
              )}
              <span className="font-medium">{match.homeTeam.name}</span>
            </div>

            {/* Away team */}
            <div className="flex items-center gap-2">
              {match.awayTeam.crest && (
                <img
                  src={match.awayTeam.crest}
                  alt=""
                  className="w-6 h-6 object-contain"
                />
              )}
              <span className="font-medium">{match.awayTeam.name}</span>
            </div>
          </div>

          {/* Score or status */}
          <div className="text-center min-w-[80px]">
            {match.status === 'Finished' && (
              <div className="text-2xl font-bold">
                {match.homeScore} - {match.awayScore}
              </div>
            )}

            {match.status === 'InPlay' && (
              <Badge variant="destructive" className="animate-pulse">
                LIVE
              </Badge>
            )}

            {(match.status === 'Scheduled' || match.status === 'Timed') && (
              <div className="text-sm text-muted-foreground">
                <Clock className="h-4 w-4 inline mr-1" />
                {matchTimeFormatted}
              </div>
            )}
          </div>

          {/* User's bet indicator */}
          {existingBet && (
            <div className="text-right min-w-[60px]">
              <div className="text-lg font-semibold text-primary">
                {existingBet.predictedHomeScore} - {existingBet.predictedAwayScore}
              </div>
              <div className="text-xs text-muted-foreground">Your bet</div>
            </div>
          )}
        </div>

        {/* Deadline warning */}
        {canBet && timeUntilDeadline < 60 * 60 * 1000 && ( // Less than 1 hour
          <div className="mt-2 text-sm text-amber-500 flex items-center gap-1">
            <Clock className="h-4 w-4" />
            Deadline in {minutesUntilDeadline} min
          </div>
        )}

        {/* Expanded bet form */}
        {isExpanded && canBet && (
          <div className="mt-4 pt-4 border-t">
            <BetForm
              matchId={match.id}
              leagueId={leagueId}
              existingBet={existingBet}
              homeTeamName={match.homeTeam.name}
              awayTeamName={match.awayTeam.name}
              onSuccess={() => setIsExpanded(false)}
            />
          </div>
        )}

        {/* Results section (after match finished) */}
        {match.status === 'Finished' && existingBet?.result && (
          <div className="mt-4 pt-4 border-t">
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">Result:</span>
              <div className="flex items-center gap-2">
                {existingBet.result.isExactMatch && (
                  <Badge variant="default" className="bg-green-500">
                    <Trophy className="h-3 w-3 mr-1" />
                    Exact!
                  </Badge>
                )}
                {!existingBet.result.isExactMatch && existingBet.result.isCorrectResult && (
                  <Badge variant="secondary">Correct Result</Badge>
                )}
                {!existingBet.result.isExactMatch && !existingBet.result.isCorrectResult && (
                  <Badge variant="outline">Wrong</Badge>
                )}
                <span className="font-bold text-primary">
                  +{existingBet.result.pointsEarned} pts
                </span>
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

/**
 * Helper to get status badge
 */
function getStatusBadge(status: MatchStatus) {
  switch (status) {
    case 'Scheduled':
    case 'Timed':
      return null;
    case 'InPlay':
      return <Badge variant="destructive">LIVE</Badge>;
    case 'Finished':
      return <Badge variant="secondary">FT</Badge>;
    case 'Postponed':
      return <Badge variant="outline">Postponed</Badge>;
    case 'Cancelled':
      return <Badge variant="outline">Cancelled</Badge>;
    default:
      return <Badge variant="outline">{status}</Badge>;
  }
}
```

---

## Step 3: Create Bet Form Component

### File: `web/src/components/bets/bet-form.tsx`

```typescript
'use client';

import { useState, useCallback } from 'react';
import { toast } from 'sonner';
import { Minus, Plus, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { usePlaceBet, useDeleteBet } from '@/hooks/use-bets';
import type { MyBetDto } from '@/types';

interface BetFormProps {
  matchId: string;
  leagueId: string;
  existingBet?: MyBetDto;
  homeTeamName: string;
  awayTeamName: string;
  onSuccess?: () => void;
}

/**
 * Form for placing/editing a bet
 *
 * Demonstrates:
 * - Controlled number inputs
 * - useCallback for memoized handlers
 * - Optimistic mutations
 * - Form state management
 */
export function BetForm({
  matchId,
  leagueId,
  existingBet,
  homeTeamName,
  awayTeamName,
  onSuccess,
}: BetFormProps) {
  // Initialize with existing bet or 0-0
  const [homeScore, setHomeScore] = useState(existingBet?.predictedHomeScore ?? 0);
  const [awayScore, setAwayScore] = useState(existingBet?.predictedAwayScore ?? 0);

  const placeMutation = usePlaceBet(leagueId);
  const deleteMutation = useDeleteBet(leagueId);

  // ============================================================
  // MEMOIZED HANDLERS
  // ============================================================
  // useCallback prevents creating new function references
  // on every render. Only recreate when dependencies change.
  // ============================================================

  const incrementHome = useCallback(() => {
    setHomeScore((prev) => Math.min(prev + 1, 20)); // Max 20 goals
  }, []);

  const decrementHome = useCallback(() => {
    setHomeScore((prev) => Math.max(prev - 1, 0)); // Min 0 goals
  }, []);

  const incrementAway = useCallback(() => {
    setAwayScore((prev) => Math.min(prev + 1, 20));
  }, []);

  const decrementAway = useCallback(() => {
    setAwayScore((prev) => Math.max(prev - 1, 0));
  }, []);

  const handleSubmit = async () => {
    try {
      await placeMutation.mutateAsync({
        matchId,
        predictedHomeScore: homeScore,
        predictedAwayScore: awayScore,
      });

      toast.success(existingBet ? 'Bet updated!' : 'Bet placed!');
      onSuccess?.();
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to place bet';
      toast.error(message);
    }
  };

  const handleDelete = async () => {
    if (!existingBet) return;

    if (!confirm('Are you sure you want to delete this bet?')) return;

    try {
      await deleteMutation.mutateAsync(existingBet.betId);
      toast.success('Bet deleted');
      onSuccess?.();
    } catch {
      toast.error('Failed to delete bet');
    }
  };

  const isPending = placeMutation.isPending || deleteMutation.isPending;

  return (
    <div className="space-y-4">
      {/* Score inputs */}
      <div className="flex items-center justify-between gap-4">
        {/* Home team score */}
        <div className="flex-1">
          <div className="text-sm text-muted-foreground mb-1">{homeTeamName}</div>
          <div className="flex items-center justify-center gap-2">
            <ScoreButton onClick={decrementHome} disabled={homeScore === 0 || isPending}>
              <Minus className="h-4 w-4" />
            </ScoreButton>
            <span className="text-3xl font-bold w-12 text-center">{homeScore}</span>
            <ScoreButton onClick={incrementHome} disabled={homeScore >= 20 || isPending}>
              <Plus className="h-4 w-4" />
            </ScoreButton>
          </div>
        </div>

        {/* Separator */}
        <div className="text-2xl font-bold text-muted-foreground">-</div>

        {/* Away team score */}
        <div className="flex-1">
          <div className="text-sm text-muted-foreground mb-1 text-right">
            {awayTeamName}
          </div>
          <div className="flex items-center justify-center gap-2">
            <ScoreButton onClick={decrementAway} disabled={awayScore === 0 || isPending}>
              <Minus className="h-4 w-4" />
            </ScoreButton>
            <span className="text-3xl font-bold w-12 text-center">{awayScore}</span>
            <ScoreButton onClick={incrementAway} disabled={awayScore >= 20 || isPending}>
              <Plus className="h-4 w-4" />
            </ScoreButton>
          </div>
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center justify-between">
        {existingBet && (
          <Button
            variant="ghost"
            size="sm"
            onClick={handleDelete}
            disabled={isPending}
            className="text-destructive hover:text-destructive"
          >
            <Trash2 className="h-4 w-4 mr-1" />
            Delete
          </Button>
        )}

        <div className="flex-1" />

        <Button onClick={handleSubmit} disabled={isPending}>
          {isPending
            ? 'Saving...'
            : existingBet
              ? 'Update Bet'
              : 'Place Bet'}
        </Button>
      </div>
    </div>
  );
}

/**
 * Score increment/decrement button
 */
function ScoreButton({
  children,
  onClick,
  disabled,
}: {
  children: React.ReactNode;
  onClick: () => void;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className="w-10 h-10 rounded-full bg-muted hover:bg-muted/80 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center transition-colors"
    >
      {children}
    </button>
  );
}
```

---

## Step 4: Create Match List Component

### File: `web/src/components/bets/match-list.tsx`

```typescript
'use client';

import { useMemo, useState } from 'react';
import { Calendar, Filter } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useMatches, useCompetitions } from '@/hooks/use-matches';
import { useMyBets } from '@/hooks/use-bets';
import { useLeague } from '@/hooks/use-leagues';
import { MatchCard } from './match-card';
import { CardListSkeleton } from '@/components/shared/loading-skeleton';
import { EmptyState } from '@/components/shared/empty-state';
import { ErrorMessage } from '@/components/shared/error-message';
import type { MatchDto, MyBetDto } from '@/types';

interface MatchListProps {
  leagueId: string;
}

/**
 * List of matches with filtering and grouping
 *
 * Demonstrates:
 * - useMemo for expensive calculations
 * - Filtering and grouping data
 * - Combining multiple queries
 */
export function MatchList({ leagueId }: MatchListProps) {
  const [selectedCompetition, setSelectedCompetition] = useState<string>('all');
  const [showTab, setShowTab] = useState<'upcoming' | 'past'>('upcoming');

  // Fetch data from multiple sources
  const { data: league, isLoading: leagueLoading } = useLeague(leagueId);
  const { data: myBets, isLoading: betsLoading } = useMyBets(leagueId);
  const { data: matchesResponse, isLoading: matchesLoading, isError, error, refetch } =
    useMatches({ pageSize: 100 }); // Get more matches
  const { data: competitions } = useCompetitions();

  const isLoading = leagueLoading || betsLoading || matchesLoading;

  // ============================================================
  // useMemo: MEMOIZE EXPENSIVE CALCULATIONS
  // ============================================================
  // useMemo caches the result of a calculation.
  // Only recalculates when dependencies change.
  //
  // Backend analogy: Cached computed property that recalculates
  // only when underlying data changes.
  //
  // private List<MatchGroup>? _cachedGroups;
  // private string? _lastFilter;
  // public List<MatchGroup> Groups {
  //   get {
  //     if (_cachedGroups == null || _lastFilter != filter) {
  //       _cachedGroups = CalculateGroups();
  //       _lastFilter = filter;
  //     }
  //     return _cachedGroups;
  //   }
  // }
  // ============================================================

  // Create bet lookup map for O(1) access
  const betsByMatchId = useMemo(() => {
    if (!myBets) return new Map<string, MyBetDto>();
    return new Map(myBets.map((bet) => [bet.matchId, bet]));
  }, [myBets]);

  // Filter matches by allowed competitions and selected filter
  const filteredMatches = useMemo(() => {
    if (!matchesResponse?.items || !league) return [];

    let matches = matchesResponse.items;

    // Filter by league's allowed competitions
    if (league.allowedCompetitionIds.length > 0) {
      matches = matches.filter((m) =>
        league.allowedCompetitionIds.includes(m.competition.id)
      );
    }

    // Filter by selected competition
    if (selectedCompetition !== 'all') {
      matches = matches.filter((m) => m.competition.id === selectedCompetition);
    }

    return matches;
  }, [matchesResponse?.items, league, selectedCompetition]);

  // Split into upcoming and past matches
  const { upcomingMatches, pastMatches } = useMemo(() => {
    const now = new Date();
    const upcoming: MatchDto[] = [];
    const past: MatchDto[] = [];

    filteredMatches.forEach((match) => {
      if (match.status === 'Finished' || new Date(match.matchDateUtc) < now) {
        past.push(match);
      } else {
        upcoming.push(match);
      }
    });

    // Sort: upcoming by date ascending, past by date descending
    upcoming.sort(
      (a, b) =>
        new Date(a.matchDateUtc).getTime() - new Date(b.matchDateUtc).getTime()
    );
    past.sort(
      (a, b) =>
        new Date(b.matchDateUtc).getTime() - new Date(a.matchDateUtc).getTime()
    );

    return { upcomingMatches: upcoming, pastMatches: past };
  }, [filteredMatches]);

  // Group matches by date for display
  const groupMatchesByDate = (matches: MatchDto[]) => {
    const groups = new Map<string, MatchDto[]>();

    matches.forEach((match) => {
      const dateKey = new Date(match.matchDateUtc).toLocaleDateString(undefined, {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric',
      });

      if (!groups.has(dateKey)) {
        groups.set(dateKey, []);
      }
      groups.get(dateKey)!.push(match);
    });

    return Array.from(groups.entries());
  };

  // Loading state
  if (isLoading) {
    return <CardListSkeleton count={5} />;
  }

  // Error state
  if (isError) {
    return (
      <ErrorMessage
        title="Failed to load matches"
        message={error?.message ?? 'Something went wrong'}
        onRetry={() => refetch()}
      />
    );
  }

  const currentMatches = showTab === 'upcoming' ? upcomingMatches : pastMatches;
  const groupedMatches = groupMatchesByDate(currentMatches);

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <Tabs value={showTab} onValueChange={(v) => setShowTab(v as 'upcoming' | 'past')}>
          <TabsList>
            <TabsTrigger value="upcoming">
              Upcoming ({upcomingMatches.length})
            </TabsTrigger>
            <TabsTrigger value="past">
              Past ({pastMatches.length})
            </TabsTrigger>
          </TabsList>
        </Tabs>

        <Select value={selectedCompetition} onValueChange={setSelectedCompetition}>
          <SelectTrigger className="w-[200px]">
            <SelectValue placeholder="All competitions" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All competitions</SelectItem>
            {competitions?.map((comp) => (
              <SelectItem key={comp.id} value={comp.id}>
                {comp.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Matches grouped by date */}
      {groupedMatches.length === 0 ? (
        <EmptyState
          icon={Calendar}
          title={showTab === 'upcoming' ? 'No upcoming matches' : 'No past matches'}
          description={
            showTab === 'upcoming'
              ? 'Check back later for new matches'
              : 'Past matches will appear here'
          }
        />
      ) : (
        <div className="space-y-6">
          {groupedMatches.map(([date, matches]) => (
            <div key={date}>
              <h3 className="text-sm font-medium text-muted-foreground mb-2">
                {date}
              </h3>
              <div className="space-y-2">
                {matches.map((match) => (
                  <MatchCard
                    key={match.id}
                    match={match}
                    leagueId={leagueId}
                    existingBet={betsByMatchId.get(match.id)}
                    bettingDeadlineMinutes={league?.bettingDeadlineMinutes ?? 60}
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
```

---

## Step 5: Create My Bets Component

### File: `web/src/components/bets/my-bets-list.tsx`

```typescript
'use client';

import { useMemo } from 'react';
import { Target, Clock, CheckCircle, XCircle, Trophy } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { useMyBets } from '@/hooks/use-bets';
import { CardListSkeleton } from '@/components/shared/loading-skeleton';
import { EmptyState } from '@/components/shared/empty-state';
import { ErrorMessage } from '@/components/shared/error-message';
import type { MyBetDto } from '@/types';

interface MyBetsListProps {
  leagueId: string;
}

/**
 * List of user's bets in a league
 *
 * Demonstrates:
 * - Grouping data by status
 * - Summary statistics calculation
 * - Complex conditional rendering
 */
export function MyBetsList({ leagueId }: MyBetsListProps) {
  const { data: bets, isLoading, isError, error, refetch } = useMyBets(leagueId);

  // Calculate statistics
  const stats = useMemo(() => {
    if (!bets) return { total: 0, pending: 0, correct: 0, exact: 0, points: 0 };

    return bets.reduce(
      (acc, bet) => {
        acc.total++;
        if (!bet.result) {
          acc.pending++;
        } else {
          if (bet.result.isExactMatch) acc.exact++;
          else if (bet.result.isCorrectResult) acc.correct++;
          acc.points += bet.result.pointsEarned;
        }
        return acc;
      },
      { total: 0, pending: 0, correct: 0, exact: 0, points: 0 }
    );
  }, [bets]);

  // Group bets by status
  const { pendingBets, resultedBets } = useMemo(() => {
    if (!bets) return { pendingBets: [], resultedBets: [] };

    const pending: MyBetDto[] = [];
    const resulted: MyBetDto[] = [];

    bets.forEach((bet) => {
      if (bet.result) {
        resulted.push(bet);
      } else {
        pending.push(bet);
      }
    });

    // Sort pending by match date (soonest first)
    pending.sort(
      (a, b) =>
        new Date(a.matchDateUtc).getTime() - new Date(b.matchDateUtc).getTime()
    );

    // Sort resulted by match date (most recent first)
    resulted.sort(
      (a, b) =>
        new Date(b.matchDateUtc).getTime() - new Date(a.matchDateUtc).getTime()
    );

    return { pendingBets: pending, resultedBets: resulted };
  }, [bets]);

  if (isLoading) {
    return <CardListSkeleton count={5} />;
  }

  if (isError) {
    return (
      <ErrorMessage
        title="Failed to load bets"
        message={error?.message ?? 'Something went wrong'}
        onRetry={() => refetch()}
      />
    );
  }

  if (!bets || bets.length === 0) {
    return (
      <EmptyState
        icon={Target}
        title="No bets yet"
        description="Place your first bet on an upcoming match"
      />
    );
  }

  return (
    <div className="space-y-6">
      {/* Stats summary */}
      <div className="grid gap-4 grid-cols-2 md:grid-cols-4">
        <StatCard label="Total Bets" value={stats.total} icon={Target} />
        <StatCard label="Pending" value={stats.pending} icon={Clock} />
        <StatCard
          label="Exact Matches"
          value={stats.exact}
          icon={Trophy}
          highlight
        />
        <StatCard label="Total Points" value={stats.points} icon={CheckCircle} />
      </div>

      {/* Pending bets */}
      {pendingBets.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold mb-3">Pending Results</h3>
          <div className="space-y-2">
            {pendingBets.map((bet) => (
              <BetCard key={bet.betId} bet={bet} />
            ))}
          </div>
        </div>
      )}

      {/* Resulted bets */}
      {resultedBets.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold mb-3">Past Results</h3>
          <div className="space-y-2">
            {resultedBets.map((bet) => (
              <BetCard key={bet.betId} bet={bet} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function StatCard({
  label,
  value,
  icon: Icon,
  highlight,
}: {
  label: string;
  value: number;
  icon: React.ComponentType<{ className?: string }>;
  highlight?: boolean;
}) {
  return (
    <Card className={highlight ? 'border-primary' : ''}>
      <CardContent className="p-4">
        <div className="flex items-center gap-2">
          <Icon className={`h-4 w-4 ${highlight ? 'text-primary' : 'text-muted-foreground'}`} />
          <span className="text-sm text-muted-foreground">{label}</span>
        </div>
        <p className={`text-2xl font-bold mt-1 ${highlight ? 'text-primary' : ''}`}>
          {value}
        </p>
      </CardContent>
    </Card>
  );
}

function BetCard({ bet }: { bet: MyBetDto }) {
  const matchDate = new Date(bet.matchDateUtc).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  });

  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          {/* Match info */}
          <div>
            <div className="font-medium">
              {bet.homeTeamName} vs {bet.awayTeamName}
            </div>
            <div className="text-sm text-muted-foreground">{matchDate}</div>
          </div>

          {/* Scores */}
          <div className="flex items-center gap-4">
            {/* Your prediction */}
            <div className="text-center">
              <div className="text-xs text-muted-foreground">Your bet</div>
              <div className="font-semibold">
                {bet.predictedHomeScore} - {bet.predictedAwayScore}
              </div>
            </div>

            {/* Actual score (if finished) */}
            {bet.matchStatus === 'Finished' && (
              <div className="text-center">
                <div className="text-xs text-muted-foreground">Actual</div>
                <div className="font-semibold">
                  {bet.actualHomeScore} - {bet.actualAwayScore}
                </div>
              </div>
            )}

            {/* Result badge */}
            {bet.result && (
              <div className="text-center">
                {bet.result.isExactMatch ? (
                  <Badge className="bg-green-500">
                    <Trophy className="h-3 w-3 mr-1" />
                    +{bet.result.pointsEarned}
                  </Badge>
                ) : bet.result.isCorrectResult ? (
                  <Badge variant="secondary">+{bet.result.pointsEarned}</Badge>
                ) : (
                  <Badge variant="outline">
                    <XCircle className="h-3 w-3 mr-1" />0
                  </Badge>
                )}
              </div>
            )}

            {!bet.result && (
              <Badge variant="outline">
                <Clock className="h-3 w-3 mr-1" />
                Pending
              </Badge>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
```

---

## Step 6: Create Page Files

### File: `web/src/app/(protected)/leagues/[id]/matches/page.tsx`

```typescript
import { ProtectedRoute } from '@/components/auth/protected-route';
import { MatchList } from '@/components/bets/match-list';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default async function MatchesPage({ params }: PageProps) {
  const { id } = await params;

  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-4xl space-y-4">
          <h1 className="text-2xl font-bold tracking-tight">Place Your Bets</h1>
          <MatchList leagueId={id} />
        </div>
      </div>
    </ProtectedRoute>
  );
}
```

### File: `web/src/app/(protected)/leagues/[id]/bets/page.tsx`

```typescript
import { ProtectedRoute } from '@/components/auth/protected-route';
import { MyBetsList } from '@/components/bets/my-bets-list';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default async function MyBetsPage({ params }: PageProps) {
  const { id } = await params;

  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-4xl space-y-4">
          <h1 className="text-2xl font-bold tracking-tight">My Bets</h1>
          <MyBetsList leagueId={id} />
        </div>
      </div>
    </ProtectedRoute>
  );
}
```

---

## Step 7: Add shadcn/ui Components

```bash
cd web
npx shadcn@latest add tabs
npx shadcn@latest add select
npx shadcn@latest add tooltip
```

---

## Verification Checklist

After completing Phase 6.3:

- [ ] Match list displays with correct grouping by date
- [ ] Upcoming/Past tabs filter correctly
- [ ] Competition filter works
- [ ] Click on match expands bet form
- [ ] Score increment/decrement works
- [ ] Placing bet shows optimistic update (instant)
- [ ] Updating existing bet works
- [ ] Deleting bet works with confirmation
- [ ] Deadline warning shows for soon matches
- [ ] My Bets page shows all user bets
- [ ] Stats summary calculates correctly
- [ ] Results show correct badges (Exact, Correct, Wrong)
- [ ] `npm run build` passes

---

## Key Learnings from This Phase

1. **Optimistic updates** - Update UI before API confirms for instant feel
2. **useMemo** - Cache expensive calculations (sorting, filtering, grouping)
3. **useCallback** - Memoize event handlers passed to child components
4. **Derived state** - Calculate from props/state, don't store redundantly
5. **Query composition** - Combine multiple queries in one component
6. **Error handling** - Always handle loading, error, and empty states

---

## Common Pitfalls to Avoid

1. **Don't forget rollback** - Optimistic updates need error handling
2. **Don't over-memoize** - Only use useMemo/useCallback when needed
3. **Don't store derived state** - Calculate on render instead
4. **Don't mutate state directly** - Always use setter functions
5. **Date timezone issues** - Always work with UTC, format for display only

---

## Next Step

Proceed to **Phase 6.4: Leaderboard & Statistics** (`phase-6.4-leaderboard.md`)
