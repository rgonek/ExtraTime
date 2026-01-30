'use client';

import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { LeagueBotDto, BotDto, ApiError } from '@/types';

export const botKeys = {
  all: ['bots'] as const,
  lists: () => [...botKeys.all, 'list'] as const,
  list: () => [...botKeys.lists()] as const,
  league: (leagueId: string) => [...botKeys.all, 'league', leagueId] as const,
};

export function useBots() {
  return useQuery<BotDto[], ApiError>({
    queryKey: botKeys.list(),
    queryFn: () => apiClient.get<BotDto[]>('/bots'),
    staleTime: 60 * 1000,
  });
}

export function useLeagueBots(leagueId: string) {
  return useQuery<LeagueBotDto[], ApiError>({
    queryKey: botKeys.league(leagueId),
    queryFn: () => apiClient.get<LeagueBotDto[]>(`/leagues/${leagueId}/bots`),
    enabled: !!leagueId,
    staleTime: 30 * 1000,
  });
}
