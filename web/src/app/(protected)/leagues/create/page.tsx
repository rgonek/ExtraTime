'use client';

import { Plus } from 'lucide-react';
import { LeagueForm } from '@/components/leagues/league-form';
import { PageHeader } from '@/components/shared/page-header';

export default function CreateLeaguePage() {
  return (
    <div className="max-w-2xl mx-auto">
      <PageHeader
        title="Create League"
        subtitle="Set up a new league for you and your friends"
        icon={Plus}
        backHref="/leagues"
      />
      <LeagueForm />
    </div>
  );
}
