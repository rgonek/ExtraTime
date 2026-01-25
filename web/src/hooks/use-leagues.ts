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
 * Join a league via invite code (requires leagueId)
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
 * Join a league using only the invite code
 *
 * Backend equivalent:
 * POST /api/leagues/join
 * JoinLeagueByCodeCommand
 */
export function useJoinLeagueByCode() {
  const queryClient = useQueryClient();

  return useMutation<LeagueDto, ApiError, JoinLeagueRequest>({
    mutationFn: (data) => apiClient.post<LeagueDto>('/leagues/join', data),
    onSuccess: () => {
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
