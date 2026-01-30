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
