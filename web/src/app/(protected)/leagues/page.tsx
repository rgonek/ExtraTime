import { ProtectedRoute } from '@/components/auth/protected-route';
import { LeagueList } from '@/components/leagues/league-list';

export default function LeaguesPage() {
  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-6xl">
          <LeagueList />
        </div>
      </div>
    </ProtectedRoute>
  );
}
