import { Suspense } from 'react';
import { ProtectedRoute } from '@/components/auth/protected-route';
import { JoinLeagueForm } from '@/components/leagues/join-league-form';
import { CardSkeleton } from '@/components/shared/loading-skeleton';

export default function JoinLeaguePage() {
  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4 flex items-center">
        <Suspense fallback={<CardSkeleton />}>
          <JoinLeagueForm />
        </Suspense>
      </div>
    </ProtectedRoute>
  );
}
