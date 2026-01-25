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
