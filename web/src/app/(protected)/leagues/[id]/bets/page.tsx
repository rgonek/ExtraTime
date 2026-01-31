'use client';

import { use } from 'react';
import { History } from 'lucide-react';
import { ProtectedRoute } from '@/components/auth/protected-route';
import { MyBetsList } from '@/components/bets/my-bets-list';
import { PageHeader } from '@/components/shared/page-header';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default function MyBetsPage({ params }: PageProps) {
  const { id } = use(params);

  return (
    <ProtectedRoute>
      <div className="mx-auto max-w-4xl space-y-6">
        <PageHeader
          title="My Bets"
          subtitle="View your prediction history"
          icon={History}
          backHref={`/leagues/${id}`}
        />
        <MyBetsList leagueId={id} />
      </div>
    </ProtectedRoute>
  );
}
