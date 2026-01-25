'use client';

import { use } from 'react';
import { ProtectedRoute } from '@/components/auth/protected-route';
import { LeagueForm } from '@/components/leagues/league-form';
import { useLeague } from '@/hooks/use-leagues';
import { CardSkeleton } from '@/components/shared/loading-skeleton';
import { ErrorMessage } from '@/components/shared/error-message';

interface PageProps {
  params: Promise<{ id: string }>;
}

function EditLeagueContent({ leagueId }: { leagueId: string }) {
  const { data: league, isLoading, isError, error, refetch } = useLeague(leagueId);

  if (isLoading) {
    return <CardSkeleton />;
  }

  if (isError || !league) {
    return (
      <ErrorMessage
        title="Failed to load league"
        message={error?.message ?? 'League not found'}
        onRetry={() => refetch()}
      />
    );
  }

  return <LeagueForm league={league} />;
}

export default function EditLeaguePage({ params }: PageProps) {
  const { id } = use(params);

  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-2xl">
          <EditLeagueContent leagueId={id} />
        </div>
      </div>
    </ProtectedRoute>
  );
}
