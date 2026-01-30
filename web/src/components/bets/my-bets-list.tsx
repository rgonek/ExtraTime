'use client';

import { useMemo } from 'react';
import { Target, Clock, CheckCircle, XCircle, Trophy } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { useMyBets } from '@/hooks/use-bets';
import { CardListSkeleton } from '@/components/shared/loading-skeleton';
import { EmptyState } from '@/components/shared/empty-state';
import { ErrorMessage } from '@/components/shared/error-message';
import type { MyBetDto } from '@/types';

interface MyBetsListProps {
  leagueId: string;
}

/**
 * List of user's bets in a league
 *
 * Demonstrates:
 * - Grouping data by status
 * - Summary statistics calculation
 * - Complex conditional rendering
 */
export function MyBetsList({ leagueId }: MyBetsListProps) {
  const { data: bets, isLoading, isError, error, refetch } = useMyBets(leagueId);

  // Calculate statistics
  const stats = useMemo(() => {
    if (!bets) return { total: 0, pending: 0, correct: 0, exact: 0, points: 0 };

    return bets.reduce(
      (acc, bet) => {
        acc.total++;
        if (!bet.result) {
          acc.pending++;
        } else {
          if (bet.result.isExactMatch) acc.exact++;
          else if (bet.result.isCorrectResult) acc.correct++;
          acc.points += bet.result.pointsEarned;
        }
        return acc;
      },
      { total: 0, pending: 0, correct: 0, exact: 0, points: 0 }
    );
  }, [bets]);

  // Group bets by status
  const { pendingBets, resultedBets } = useMemo(() => {
    if (!bets) return { pendingBets: [], resultedBets: [] };

    const pending: MyBetDto[] = [];
    const resulted: MyBetDto[] = [];

    bets.forEach((bet) => {
      if (bet.result) {
        resulted.push(bet);
      } else {
        pending.push(bet);
      }
    });

    // Sort pending by match date (soonest first)
    pending.sort(
      (a, b) =>
        new Date(a.matchDateUtc).getTime() - new Date(b.matchDateUtc).getTime()
    );

    // Sort resulted by match date (most recent first)
    resulted.sort(
      (a, b) =>
        new Date(b.matchDateUtc).getTime() - new Date(a.matchDateUtc).getTime()
    );

    return { pendingBets: pending, resultedBets: resulted };
  }, [bets]);

  if (isLoading) {
    return <CardListSkeleton count={5} />;
  }

  if (isError) {
    return (
      <ErrorMessage
        title="Failed to load bets"
        message={error?.message ?? 'Something went wrong'}
        onRetry={() => refetch()}
      />
    );
  }

  if (!bets || bets.length === 0) {
    return (
      <EmptyState
        icon={Target}
        title="No bets yet"
        description="Place your first bet on an upcoming match"
      />
    );
  }

  return (
    <div className="space-y-6">
      {/* Stats summary */}
      <div className="grid gap-4 grid-cols-2 md:grid-cols-4">
        <StatCard label="Total Bets" value={stats.total} icon={Target} />
        <StatCard label="Pending" value={stats.pending} icon={Clock} />
        <StatCard
          label="Exact Matches"
          value={stats.exact}
          icon={Trophy}
          highlight
        />
        <StatCard label="Total Points" value={stats.points} icon={CheckCircle} />
      </div>

      {/* Pending bets */}
      {pendingBets.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold mb-3">Pending Results</h3>
          <div className="space-y-2">
            {pendingBets.map((bet) => (
              <BetCard key={bet.betId} bet={bet} />
            ))}
          </div>
        </div>
      )}

      {/* Resulted bets */}
      {resultedBets.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold mb-3">Past Results</h3>
          <div className="space-y-2">
            {resultedBets.map((bet) => (
              <BetCard key={bet.betId} bet={bet} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function StatCard({
  label,
  value,
  icon: Icon,
  highlight,
}: {
  label: string;
  value: number;
  icon: React.ComponentType<{ className?: string }>;
  highlight?: boolean;
}) {
  return (
    <Card className={highlight ? 'border-primary' : ''}>
      <CardContent className="p-4">
        <div className="flex items-center gap-2">
          <Icon className={`h-4 w-4 ${highlight ? 'text-primary' : 'text-muted-foreground'}`} />
          <span className="text-sm text-muted-foreground">{label}</span>
        </div>
        <p className={`text-2xl font-bold mt-1 ${highlight ? 'text-primary' : ''}`}>
          {value}
        </p>
      </CardContent>
    </Card>
  );
}

function BetCard({ bet }: { bet: MyBetDto }) {
  const matchDate = new Date(bet.matchDateUtc).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  });

  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          {/* Match info */}
          <div>
            <div className="font-medium">
              {bet.homeTeamName} vs {bet.awayTeamName}
            </div>
            <div className="text-sm text-muted-foreground">{matchDate}</div>
          </div>

          {/* Scores */}
          <div className="flex items-center gap-4">
            {/* Your prediction */}
            <div className="text-center">
              <div className="text-xs text-muted-foreground">Your bet</div>
              <div className="font-semibold">
                {bet.predictedHomeScore} - {bet.predictedAwayScore}
              </div>
            </div>

            {/* Actual score (if finished) */}
            {bet.matchStatus === 'Finished' && (
              <div className="text-center">
                <div className="text-xs text-muted-foreground">Actual</div>
                <div className="font-semibold">
                  {bet.actualHomeScore} - {bet.actualAwayScore}
                </div>
              </div>
            )}

            {/* Result badge */}
            {bet.result && (
              <div className="text-center">
                {bet.result.isExactMatch ? (
                  <Badge className="bg-green-500">
                    <Trophy className="h-3 w-3 mr-1" />
                    +{bet.result.pointsEarned}
                  </Badge>
                ) : bet.result.isCorrectResult ? (
                  <Badge variant="secondary">+{bet.result.pointsEarned}</Badge>
                ) : (
                  <Badge variant="outline">
                    <XCircle className="h-3 w-3 mr-1" />0
                  </Badge>
                )}
              </div>
            )}

            {!bet.result && (
              <Badge variant="outline">
                <Clock className="h-3 w-3 mr-1" />
                Pending
              </Badge>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
