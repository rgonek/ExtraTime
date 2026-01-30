'use client';

import { useState } from 'react';
import { Clock, Trophy } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { BetForm } from './bet-form';
import type { MatchDto, MyBetDto, MatchStatus } from '@/types';

interface MatchCardProps {
  match: MatchDto;
  leagueId: string;
  existingBet?: MyBetDto;
  bettingDeadlineMinutes: number;
}

/**
 * Card displaying a match with betting functionality
 *
 * Demonstrates:
 * - Derived state (canBet calculation)
 * - Conditional rendering based on match status
 * - Date/time formatting and calculations
 */
export function MatchCard({
  match,
  leagueId,
  existingBet,
  bettingDeadlineMinutes,
}: MatchCardProps) {
  const [isExpanded, setIsExpanded] = useState(false);

  // ============================================================
  // DERIVED STATE
  // ============================================================
  // Derived state = calculated from existing state/props
  // Like computed properties in C#
  // Don't store in useState - calculate on render
  // ============================================================

  const matchDate = new Date(match.matchDateUtc);
  const now = new Date();

  // Calculate deadline (match time - deadline minutes)
  const deadlineDate = new Date(
    matchDate.getTime() - bettingDeadlineMinutes * 60 * 1000
  );

  // Can bet if: before deadline AND match not started/finished
  const canBet =
    now < deadlineDate &&
    (match.status === 'Scheduled' || match.status === 'Timed');

  // Time remaining until deadline (for countdown)
  const timeUntilDeadline = deadlineDate.getTime() - now.getTime();
  const minutesUntilDeadline = Math.floor(
    (timeUntilDeadline % (1000 * 60 * 60)) / (1000 * 60)
  );

  // Format match time for display
  const matchTimeFormatted = matchDate.toLocaleString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });

  return (
    <Card
      className={`transition-all ${isExpanded ? 'ring-2 ring-primary' : ''}`}
    >
      <CardContent className="p-4">
        {/* Match header - teams and score */}
        <div
          className="flex items-center justify-between cursor-pointer"
          onClick={() => canBet && setIsExpanded(!isExpanded)}
        >
          {/* Teams */}
          <div className="flex-1 space-y-2">
            {/* Home team */}
            <div className="flex items-center gap-2">
              {match.homeTeam.crest && (
                <img
                  src={match.homeTeam.crest}
                  alt=""
                  className="w-6 h-6 object-contain"
                />
              )}
              <span className="font-medium">{match.homeTeam.name}</span>
            </div>

            {/* Away team */}
            <div className="flex items-center gap-2">
              {match.awayTeam.crest && (
                <img
                  src={match.awayTeam.crest}
                  alt=""
                  className="w-6 h-6 object-contain"
                />
              )}
              <span className="font-medium">{match.awayTeam.name}</span>
            </div>
          </div>

          {/* Score or status */}
          <div className="text-center min-w-[80px]">
            {match.status === 'Finished' && (
              <div className="text-2xl font-bold">
                {match.homeScore} - {match.awayScore}
              </div>
            )}

            {match.status === 'InPlay' && (
              <Badge variant="destructive" className="animate-pulse">
                LIVE
              </Badge>
            )}

            {(match.status === 'Scheduled' || match.status === 'Timed') && (
              <div className="text-sm text-muted-foreground">
                <Clock className="h-4 w-4 inline mr-1" />
                {matchTimeFormatted}
              </div>
            )}
          </div>

          {/* User's bet indicator */}
          {existingBet && (
            <div className="text-right min-w-[60px]">
              <div className="text-lg font-semibold text-primary">
                {existingBet.predictedHomeScore} - {existingBet.predictedAwayScore}
              </div>
              <div className="text-xs text-muted-foreground">Your bet</div>
            </div>
          )}
        </div>

        {/* Deadline warning */}
        {canBet && timeUntilDeadline < 60 * 60 * 1000 && ( // Less than 1 hour
          <div className="mt-2 text-sm text-amber-500 flex items-center gap-1">
            <Clock className="h-4 w-4" />
            Deadline in {minutesUntilDeadline} min
          </div>
        )}

        {/* Expanded bet form */}
        {isExpanded && canBet && (
          <div className="mt-4 pt-4 border-t">
            <BetForm
              matchId={match.id}
              leagueId={leagueId}
              existingBet={existingBet}
              homeTeamName={match.homeTeam.name}
              awayTeamName={match.awayTeam.name}
              onSuccess={() => setIsExpanded(false)}
            />
          </div>
        )}

        {/* Results section (after match finished) */}
        {match.status === 'Finished' && existingBet?.result && (
          <div className="mt-4 pt-4 border-t">
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">Result:</span>
              <div className="flex items-center gap-2">
                {existingBet.result.isExactMatch && (
                  <Badge variant="default" className="bg-green-500">
                    <Trophy className="h-3 w-3 mr-1" />
                    Exact!
                  </Badge>
                )}
                {!existingBet.result.isExactMatch && existingBet.result.isCorrectResult && (
                  <Badge variant="secondary">Correct Result</Badge>
                )}
                {!existingBet.result.isExactMatch && !existingBet.result.isCorrectResult && (
                  <Badge variant="outline">Wrong</Badge>
                )}
                <span className="font-bold text-primary">
                  +{existingBet.result.pointsEarned} pts
                </span>
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

/**
 * Helper to get status badge
 */
function getStatusBadge(status: MatchStatus) {
  switch (status) {
    case 'Scheduled':
    case 'Timed':
      return null;
    case 'InPlay':
      return <Badge variant="destructive">LIVE</Badge>;
    case 'Finished':
      return <Badge variant="secondary">FT</Badge>;
    case 'Postponed':
      return <Badge variant="outline">Postponed</Badge>;
    case 'Cancelled':
      return <Badge variant="outline">Cancelled</Badge>;
    default:
      return <Badge variant="outline">{status}</Badge>;
  }
}
