'use client';

import { use } from 'react';
import { Settings } from 'lucide-react';
import { LeagueForm } from '@/components/leagues/league-form';
import { useLeague } from '@/hooks/use-leagues';
import { CardSkeleton } from '@/components/shared/loading-skeleton';
import { ErrorMessage } from '@/components/shared/error-message';
import { PageHeader, PageHeaderSkeleton } from '@/components/shared/page-header';

interface PageProps {
  params: Promise<{ id: string }>;
}

function EditLeagueContent({ leagueId }: { leagueId: string }) {
  const { data: league, isLoading, isError, error, refetch } = useLeague(leagueId);

  if (isLoading) {
    return (
      <>
        <PageHeaderSkeleton showIcon />
        <CardSkeleton />
      </>
    );
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

  return (
    <>
      <PageHeader
        title="Edit League"
        subtitle={`Update settings for ${league.name}`}
        icon={Settings}
        backHref={`/leagues/${leagueId}`}
      />
      <LeagueForm league={league} />
    </>
  );
}

export default function EditLeaguePage({ params }: PageProps) {
  const { id } = use(params);

  return (
    <div className="max-w-2xl mx-auto">
      <EditLeagueContent leagueId={id} />
    </div>
  );
}
