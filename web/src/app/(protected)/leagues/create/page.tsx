import { ProtectedRoute } from '@/components/auth/protected-route';
import { LeagueForm } from '@/components/leagues/league-form';

export default function CreateLeaguePage() {
  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-2xl">
          <LeagueForm />
        </div>
      </div>
    </ProtectedRoute>
  );
}
