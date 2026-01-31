'use client';

import { Trophy, Plus, UserPlus } from 'lucide-react';
import { useLeagues } from '@/hooks/use-leagues';
import { LeagueCard } from './league-card';
import { CardGridSkeleton } from '@/components/shared/loading-skeleton';
import { EmptyState } from '@/components/shared/empty-state';
import { ErrorMessage } from '@/components/shared/error-message';
import { PageHeader, PageHeaderSkeleton } from '@/components/shared/page-header';
import { useRouter } from 'next/navigation';

export function LeagueList() {
  const router = useRouter();
  const { data: leagues, isLoading, isError, error, refetch } = useLeagues();

  if (isLoading) {
    return (
      <div className="space-y-6">
        <PageHeaderSkeleton showIcon />
        <CardGridSkeleton count={6} />
      </div>
    );
  }

  if (isError) {
    return (
      <ErrorMessage
        title="Failed to load leagues"
        message={error?.message ?? 'An unexpected error occurred'}
        onRetry={() => refetch()}
      />
    );
  }

  if (!leagues || leagues.length === 0) {
    return (
      <EmptyState
        icon={Trophy}
        title="No leagues yet"
        description="Create your first league or join one with an invite code"
        action={{
          label: 'Create League',
          onClick: () => router.push('/leagues/create'),
        }}
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Your Leagues"
        subtitle={`${leagues.length} league${leagues.length !== 1 ? 's' : ''}`}
        icon={Trophy}
        actions={[
          {
            label: 'Join League',
            href: '/leagues/join',
            icon: UserPlus,
            variant: 'outline',
          },
          {
            label: 'Create League',
            href: '/leagues/create',
            icon: Plus,
          },
        ]}
      />

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {leagues.map((league) => (
          <LeagueCard key={league.id} league={league} />
        ))}
      </div>
    </div>
  );
}
