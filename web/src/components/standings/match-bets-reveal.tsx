'use client';

import { Trophy, Eye } from 'lucide-react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { useMatchBets } from '@/hooks/use-bets';
import { useMatch } from '@/hooks/use-matches';
import { CardSkeleton } from '@/components/shared/loading-skeleton';
import type { MatchBetDto } from '@/types';

interface MatchBetsRevealProps {
  leagueId: string;
  matchId: string;
}

/**
 * Shows all members' bets after betting deadline passes
 *
 * Demonstrates:
 * - Conditional data fetching
 * - Sorting by result
 * - Comparison display
 */
export function MatchBetsReveal({ leagueId, matchId }: MatchBetsRevealProps) {
  const { data: match, isLoading: matchLoading } = useMatch(matchId);
  const { data: bets, isLoading: betsLoading } = useMatchBets(leagueId, matchId);

  const isLoading = matchLoading || betsLoading;

  if (isLoading) {
    return <CardSkeleton />;
  }

  if (!match || !bets || bets.length === 0) {
    return null;
  }

  // Sort bets: winners first, then by points descending
  const sortedBets = [...bets].sort((a, b) => {
    // Those with results come first
    if (a.result && !b.result) return -1;
    if (!a.result && b.result) return 1;

    // Sort by points earned
    const pointsA = a.result?.pointsEarned ?? 0;
    const pointsB = b.result?.pointsEarned ?? 0;
    return pointsB - pointsA;
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Eye className="h-5 w-5" />
          Everyone's Predictions
        </CardTitle>
        <CardDescription>
          {match.homeTeam.name} vs {match.awayTeam.name}
          {match.status === 'Finished' && (
            <span className="ml-2 font-semibold">
              ({match.homeScore} - {match.awayScore})
            </span>
          )}
        </CardDescription>
      </CardHeader>

      <CardContent>
        <div className="space-y-2">
          {sortedBets.map((bet) => (
            <BetRevealRow
              key={bet.userId}
              bet={bet}
              actualHomeScore={match.homeScore}
              actualAwayScore={match.awayScore}
              isFinished={match.status === 'Finished'}
            />
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

function BetRevealRow({
  bet,
  actualHomeScore,
  actualAwayScore,
  isFinished,
}: {
  bet: MatchBetDto;
  actualHomeScore: number | null;
  actualAwayScore: number | null;
  isFinished: boolean;
}) {
  // Calculate if prediction was exact
  const isExact =
    isFinished &&
    bet.predictedHomeScore === actualHomeScore &&
    bet.predictedAwayScore === actualAwayScore;

  // Calculate if prediction got the result right (win/draw/lose)
  const isCorrectResult =
    isFinished &&
    actualHomeScore !== null &&
    actualAwayScore !== null &&
    Math.sign(bet.predictedHomeScore - bet.predictedAwayScore) ===
      Math.sign(actualHomeScore - actualAwayScore);

  return (
    <div
      className={`flex items-center justify-between p-3 rounded-lg ${
        isExact
          ? 'bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800'
          : isCorrectResult
            ? 'bg-blue-50 dark:bg-blue-900/10 border border-blue-200 dark:border-blue-800'
            : 'bg-muted/50'
      }`}
    >
      {/* User */}
      <div className="flex items-center gap-2">
        <Avatar className="h-8 w-8">
          <AvatarFallback>{bet.username.slice(0, 2).toUpperCase()}</AvatarFallback>
        </Avatar>
        <span className="font-medium">{bet.username}</span>
      </div>

      {/* Prediction */}
      <div className="flex items-center gap-3">
        <div className="text-lg font-bold">
          {bet.predictedHomeScore} - {bet.predictedAwayScore}
        </div>

        {/* Result badge */}
        {isFinished && bet.result && (
          <>
            {bet.result.isExactMatch ? (
              <Badge className="bg-green-500">
                <Trophy className="h-3 w-3 mr-1" />
                +{bet.result.pointsEarned}
              </Badge>
            ) : bet.result.isCorrectResult ? (
              <Badge variant="secondary">+{bet.result.pointsEarned}</Badge>
            ) : (
              <Badge variant="outline">0</Badge>
            )}
          </>
        )}
      </div>
    </div>
  );
}
