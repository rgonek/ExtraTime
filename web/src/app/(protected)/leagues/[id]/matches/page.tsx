'use client';

import { use } from 'react';
import { Target } from 'lucide-react';
import { ProtectedRoute } from '@/components/auth/protected-route';
import { MatchList } from '@/components/bets/match-list';
import { PageHeader } from '@/components/shared/page-header';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default function MatchesPage({ params }: PageProps) {
  const { id } = use(params);

  return (
    <ProtectedRoute>
      <div className="mx-auto max-w-4xl space-y-6">
        <PageHeader
          title="Place Your Bets"
          subtitle="Predict the scores for upcoming matches"
          icon={Target}
          backHref={`/leagues/${id}`}
        />
        <MatchList leagueId={id} />
      </div>
    </ProtectedRoute>
  );
}
