import { ProtectedRoute } from '@/components/auth/protected-route';
import { MyBetsList } from '@/components/bets/my-bets-list';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default async function MyBetsPage({ params }: PageProps) {
  const { id } = await params;

  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-4xl space-y-4">
          <h1 className="text-2xl font-bold tracking-tight">My Bets</h1>
          <MyBetsList leagueId={id} />
        </div>
      </div>
    </ProtectedRoute>
  );
}
