'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { apiClient } from '@/lib/api-client';
import { useAuthStore } from '@/stores/auth-store';
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  CurrentUserResponse,
  ApiError,
  User,
} from '@/types';

export function useLogin() {
  const router = useRouter();
  const setAuth = useAuthStore((state) => state.setAuth);

  return useMutation<AuthResponse, ApiError, LoginRequest>({
    mutationFn: (data) => apiClient.post<AuthResponse>('/auth/login', data),
    onSuccess: (data) => {
      setAuth(data.user, data.accessToken, data.refreshToken);
      router.push('/dashboard');
    },
  });
}

export function useRegister() {
  const router = useRouter();
  const setAuth = useAuthStore((state) => state.setAuth);

  return useMutation<AuthResponse, ApiError, RegisterRequest>({
    mutationFn: (data) => apiClient.post<AuthResponse>('/auth/register', data),
    onSuccess: (data) => {
      setAuth(data.user, data.accessToken, data.refreshToken);
      router.push('/dashboard');
    },
  });
}

export function useLogout() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const clearAuth = useAuthStore((state) => state.clearAuth);

  return useMutation<void, Error, void>({
    mutationFn: async () => {
      clearAuth();
      queryClient.clear();
    },
    onSuccess: () => {
      router.push('/login');
    },
  });
}

export function useCurrentUser() {
  const { isAuthenticated, setAuth, clearAuth, setLoading, accessToken, refreshToken, user } =
    useAuthStore();

  return useQuery<CurrentUserResponse | null, ApiError>({
    queryKey: ['currentUser'],
    queryFn: async () => {
      if (!accessToken) {
        setLoading(false);
        return null;
      }

      try {
        const data = await apiClient.get<CurrentUserResponse>('/auth/me');
        const userData: User = {
          id: data.id,
          email: data.email,
          username: data.username,
          role: data.role as 'User' | 'Admin',
        };
        setAuth(userData, accessToken, refreshToken!);
        return data;
      } catch {
        clearAuth();
        return null;
      }
    },
    enabled: isAuthenticated && !!accessToken,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: false,
    initialData: user
      ? {
          id: user.id,
          email: user.email,
          username: user.username,
          role: user.role,
        }
      : undefined,
  });
}
