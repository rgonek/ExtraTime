'use client';

import { Trophy, Plus } from 'lucide-react';
import { useLeagues } from '@/hooks/use-leagues';
import { LeagueCard } from './league-card';
import { CardGridSkeleton } from '@/components/shared/loading-skeleton';
import { EmptyState } from '@/components/shared/empty-state';
import { ErrorMessage } from '@/components/shared/error-message';
import { Button } from '@/components/ui/button';
import Link from 'next/link';
import { useRouter } from 'next/navigation';

export function LeagueList() {
  const router = useRouter();
  const { data: leagues, isLoading, isError, error, refetch } = useLeagues();

  if (isLoading) {
    return <CardGridSkeleton count={6} />;
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
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold tracking-tight">Your Leagues</h2>
        <div className="flex gap-2">
          <Button variant="outline" asChild>
            <Link href="/leagues/join">Join League</Link>
          </Button>
          <Button asChild>
            <Link href="/leagues/create">
              <Plus className="h-4 w-4 mr-2" />
              Create League
            </Link>
          </Button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {leagues.map((league) => (
          <LeagueCard key={league.id} league={league} />
        ))}
      </div>
    </div>
  );
}
