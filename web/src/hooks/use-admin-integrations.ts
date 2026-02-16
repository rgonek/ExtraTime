'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { ApiError, IntegrationStatus, DataAvailability } from '@/types';

export const adminIntegrationKeys = {
  all: ['admin', 'integrations'] as const,
  statuses: () => [...adminIntegrationKeys.all, 'statuses'] as const,
  availability: () => [...adminIntegrationKeys.all, 'availability'] as const,
};

export function useIntegrationStatuses() {
  return useQuery<IntegrationStatus[], ApiError>({
    queryKey: adminIntegrationKeys.statuses(),
    queryFn: () => apiClient.get<IntegrationStatus[]>('/admin/integrations'),
    refetchInterval: 30_000,
  });
}

export function useDataAvailability() {
  return useQuery<DataAvailability, ApiError>({
    queryKey: adminIntegrationKeys.availability(),
    queryFn: () => apiClient.get<DataAvailability>('/admin/integrations/availability'),
    refetchInterval: 60_000,
  });
}

export function useTriggerSync() {
  const queryClient = useQueryClient();

  return useMutation<unknown, ApiError, string>({
    mutationFn: (type) => apiClient.post(`/admin/integrations/${type}/sync`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminIntegrationKeys.all });
    },
  });
}

export function useToggleIntegration() {
  const queryClient = useQueryClient();

  return useMutation<unknown, ApiError, { type: string; enable: boolean; reason?: string }>({
    mutationFn: ({ type, enable, reason }) =>
      enable
        ? apiClient.post(`/admin/integrations/${type}/enable`)
        : apiClient.post(`/admin/integrations/${type}/disable`, {
            reason: reason ?? 'Manually disabled by admin',
          }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminIntegrationKeys.all });
    },
  });
}
