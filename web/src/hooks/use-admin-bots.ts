'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type {
  BotDto,
  CreateBotRequest,
  UpdateBotRequest,
  ConfigurationPreset,
  ApiError,
} from '@/types';

export const adminBotKeys = {
  all: ['admin', 'bots'] as const,
  list: (options?: { includeInactive?: boolean; strategy?: string }) =>
    [...adminBotKeys.all, 'list', options] as const,
  presets: () => [...adminBotKeys.all, 'presets'] as const,
};

export function useBots(options?: { includeInactive?: boolean; strategy?: string }) {
  return useQuery<BotDto[], ApiError>({
    queryKey: adminBotKeys.list(options),
    queryFn: () => {
      const params = new URLSearchParams();

      if (options?.includeInactive) {
        params.set('includeInactive', 'true');
      }

      if (options?.strategy) {
        params.set('strategy', options.strategy);
      }

      const queryString = params.toString();
      return apiClient.get<BotDto[]>(`/admin/bots${queryString ? `?${queryString}` : ''}`);
    },
  });
}

export function useBotPresets() {
  return useQuery<ConfigurationPreset[], ApiError>({
    queryKey: adminBotKeys.presets(),
    queryFn: () => apiClient.get<ConfigurationPreset[]>('/admin/bots/presets'),
    staleTime: 5 * 60 * 1000,
  });
}

export function useCreateBot() {
  const queryClient = useQueryClient();

  return useMutation<BotDto, ApiError, CreateBotRequest>({
    mutationFn: (data) => apiClient.post<BotDto>('/admin/bots', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminBotKeys.all });
      queryClient.invalidateQueries({ queryKey: ['bots'] });
    },
  });
}

export function useUpdateBot() {
  const queryClient = useQueryClient();

  return useMutation<BotDto, ApiError, { id: string; data: UpdateBotRequest }>({
    mutationFn: ({ id, data }) => apiClient.put<BotDto>(`/admin/bots/${id}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminBotKeys.all });
      queryClient.invalidateQueries({ queryKey: ['bots'] });
    },
  });
}

export function useDeleteBot() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, string>({
    mutationFn: (id) => apiClient.delete(`/admin/bots/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminBotKeys.all });
      queryClient.invalidateQueries({ queryKey: ['bots'] });
    },
  });
}
