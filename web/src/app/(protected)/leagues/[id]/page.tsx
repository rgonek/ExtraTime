import { LeagueDetail } from '@/components/leagues/league-detail';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default async function LeagueDetailPage({ params }: PageProps) {
  const { id } = await params;

  return (
    <div className="max-w-4xl mx-auto">
      <LeagueDetail leagueId={id} />
    </div>
  );
}
