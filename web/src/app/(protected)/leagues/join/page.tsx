'use client';

import { Suspense } from 'react';
import { UserPlus } from 'lucide-react';
import { JoinLeagueForm } from '@/components/leagues/join-league-form';
import { PageHeader } from '@/components/shared/page-header';
import { CardSkeleton } from '@/components/shared/loading-skeleton';

export default function JoinLeaguePage() {
  return (
    <div className="max-w-md mx-auto">
      <PageHeader
        title="Join League"
        subtitle="Enter an invite code to join a league"
        icon={UserPlus}
        backHref="/leagues"
      />
      <Suspense fallback={<CardSkeleton />}>
        <JoinLeagueForm />
      </Suspense>
    </div>
  );
}
