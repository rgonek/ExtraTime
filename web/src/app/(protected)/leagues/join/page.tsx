import { Suspense } from 'react';
import { JoinLeagueForm } from '@/components/leagues/join-league-form';
import { CardSkeleton } from '@/components/shared/loading-skeleton';

export default function JoinLeaguePage() {
  return (
    <div className="flex items-center justify-center min-h-[50vh]">
      <Suspense fallback={<CardSkeleton />}>
        <JoinLeagueForm />
      </Suspense>
    </div>
  );
}
