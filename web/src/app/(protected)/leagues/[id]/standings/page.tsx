'use client';

import { use } from 'react';
import { Trophy } from 'lucide-react';
import { Leaderboard } from '@/components/standings/leaderboard';
import { UserStatsCard } from '@/components/standings/user-stats-card';
import { PageHeader } from '@/components/shared/page-header';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default function StandingsPage({ params }: PageProps) {
  const { id } = use(params);

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <PageHeader
        title="League Standings"
        subtitle="See how you rank against other members"
        icon={Trophy}
        backHref={`/leagues/${id}`}
      />

      <UserStatsCard leagueId={id} />

      <Leaderboard leagueId={id} />
    </div>
  );
}
