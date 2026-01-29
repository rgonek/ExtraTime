import { Leaderboard } from '@/components/standings/leaderboard';
import { UserStatsCard } from '@/components/standings/user-stats-card';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default async function StandingsPage({ params }: PageProps) {
  const { id } = await params;

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold tracking-tight">League Standings</h1>

      {/* User's own stats card */}
      <UserStatsCard leagueId={id} />

      {/* Full leaderboard */}
      <Leaderboard leagueId={id} />
    </div>
  );
}
