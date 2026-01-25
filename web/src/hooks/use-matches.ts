'use client';

import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type {
  CompetitionDto,
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
