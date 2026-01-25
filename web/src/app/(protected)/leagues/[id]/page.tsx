import { ProtectedRoute } from '@/components/auth/protected-route';
import { LeagueDetail } from '@/components/leagues/league-detail';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default async function LeagueDetailPage({ params }: PageProps) {
  const { id } = await params;

  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-4xl">
          <LeagueDetail leagueId={id} />
        </div>
      </div>
    </ProtectedRoute>
  );
}
