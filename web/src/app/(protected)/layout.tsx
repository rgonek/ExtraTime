import { AppShell } from '@/components/layout/app-shell';
import { ProtectedRoute } from '@/components/auth/protected-route';
import { GlobalLoadingIndicator } from '@/components/shared/global-loading';
import { SkipLink } from '@/components/shared/skip-link';
import { ErrorBoundary } from '@/components/shared/error-boundary';

export default function ProtectedLayout({ children }: { children: React.ReactNode }) {
  return (
    <ProtectedRoute>
      <SkipLink />
      <GlobalLoadingIndicator />
      <ErrorBoundary>
        <AppShell>{children}</AppShell>
      </ErrorBoundary>
    </ProtectedRoute>
  );
}
