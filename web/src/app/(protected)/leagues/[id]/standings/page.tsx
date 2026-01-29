import { ProtectedRoute } from '@/components/auth/protected-route';
import { Leaderboard } from '@/components/standings/leaderboard';
import { UserStatsCard } from '@/components/standings/user-stats-card';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default async function StandingsPage({ params }: PageProps) {
  const { id } = await params;

  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-6xl space-y-6">
          <h1 className="text-2xl font-bold tracking-tight">League Standings</h1>

          {/* User's own stats card */}
          <UserStatsCard leagueId={id} />

          {/* Full leaderboard */}
          <Leaderboard leagueId={id} />
        </div>
      </div>
    </ProtectedRoute>
  );
}
