'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { ApiError, MlModelVersionDto, MlStrategyAccuracyDto } from '@/types';

export const adminMlKeys = {
  all: ['admin', 'ml'] as const,
  models: () => [...adminMlKeys.all, 'models'] as const,
  accuracy: (period: string) => [...adminMlKeys.all, 'accuracy', period] as const,
};

export function useMlModels() {
  return useQuery<MlModelVersionDto[], ApiError>({
    queryKey: adminMlKeys.models(),
    queryFn: () => apiClient.get<MlModelVersionDto[]>('/admin/ml/models'),
    refetchInterval: 30_000,
  });
}

export function useMlAccuracy(period = 'monthly') {
  return useQuery<MlStrategyAccuracyDto[], ApiError>({
    queryKey: adminMlKeys.accuracy(period),
    queryFn: () => apiClient.get<MlStrategyAccuracyDto[]>(`/admin/ml/accuracy?period=${period}`),
    refetchInterval: 60_000,
  });
}

export function useActivateMlModel() {
  const queryClient = useQueryClient();

  return useMutation<unknown, ApiError, { version: string; notes?: string }>({
    mutationFn: ({ version, notes }) =>
      apiClient.post(`/admin/ml/models/${encodeURIComponent(version)}/activate`, notes ? { notes } : undefined),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminMlKeys.all });
    },
  });
}

export function useRecalculateMlAccuracy() {
  const queryClient = useQueryClient();

  return useMutation<
    unknown,
    ApiError,
    { fromDate: string; toDate: string; periodType?: string; accuracyPeriod?: string }
  >({
    mutationFn: ({ fromDate, toDate, periodType }) =>
      apiClient.post('/admin/ml/accuracy/recalculate', {
        fromDate,
        toDate,
        periodType: periodType ?? 'custom',
      }),
    onSuccess: (_, variables) => {
      if (variables.accuracyPeriod) {
        queryClient.invalidateQueries({ queryKey: adminMlKeys.accuracy(variables.accuracyPeriod) });
      }
      queryClient.invalidateQueries({ queryKey: adminMlKeys.all });
    },
  });
}

export function useTriggerMlTraining() {
  const queryClient = useQueryClient();

  return useMutation<unknown, ApiError, { league: string; fromDate?: string }>({
    mutationFn: ({ league, fromDate }) =>
      apiClient.post('/admin/ml/train', {
        league,
        fromDate: fromDate ?? null,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminMlKeys.models() });
    },
  });
}
