'use client';

import { useState } from 'react';
import { Clock, Trophy, ChevronDown, Timer } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { BetForm } from './bet-form';
import { cn } from '@/lib/utils';
import type { MatchDto, MyBetDto } from '@/types';

interface MatchCardProps {
  match: MatchDto;
  leagueId: string;
  existingBet?: MyBetDto;
  bettingDeadlineMinutes: number;
}

/**
 * Card displaying a match with betting functionality
 */
export function MatchCard({
  match,
  leagueId,
  existingBet,
  bettingDeadlineMinutes,
}: MatchCardProps) {
  const [isExpanded, setIsExpanded] = useState(false);

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
  const hoursUntilDeadline = Math.floor(timeUntilDeadline / (1000 * 60 * 60));
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

  const isLive = match.status === 'InPlay';
  const isFinished = match.status === 'Finished';
  const isUpcoming = match.status === 'Scheduled' || match.status === 'Timed';
  const isUrgent = canBet && timeUntilDeadline < 60 * 60 * 1000; // Less than 1 hour

  return (
    <Card
      interactive={canBet}
      className={cn(
        'group relative overflow-hidden',
        isExpanded && 'ring-2 ring-primary shadow-lg shadow-primary/10',
        isLive && 'ring-2 ring-destructive/50 bg-destructive/5',
        isUrgent && !isLive && 'ring-1 ring-warning/50'
      )}
    >
      {/* Competition badge */}
      {match.competition && (
        <div className="absolute top-0 left-0 right-0">
          <Badge variant="ghost" className="rounded-none rounded-br-lg text-[10px] uppercase tracking-wide">
            {match.competition.name}
          </Badge>
        </div>
      )}

      <CardContent className={cn('p-4', match.competition && 'pt-8')}>
        {/* Match header - teams and score */}
        <div
          className={cn(
            'flex items-center gap-4',
            canBet && 'cursor-pointer'
          )}
          onClick={() => canBet && setIsExpanded(!isExpanded)}
        >
          {/* Teams */}
          <div className="flex-1 space-y-3">
            {/* Home team */}
            <TeamRow
              crest={match.homeTeam.crest}
              name={match.homeTeam.name}
              score={isFinished || isLive ? match.homeScore : undefined}
              isWinner={isFinished && match.homeScore !== null && match.awayScore !== null && match.homeScore > match.awayScore}
            />

            {/* Away team */}
            <TeamRow
              crest={match.awayTeam.crest}
              name={match.awayTeam.name}
              score={isFinished || isLive ? match.awayScore : undefined}
              isWinner={isFinished && match.homeScore !== null && match.awayScore !== null && match.awayScore > match.homeScore}
            />
          </div>

          {/* Status area */}
          <div className="flex flex-col items-center gap-2 min-w-[100px]">
            {isLive && (
              <Badge variant="live" className="text-xs">
                LIVE
              </Badge>
            )}

            {isFinished && (
              <Badge variant="ghost" className="text-xs">
                FT
              </Badge>
            )}

            {isUpcoming && (
              <div className="text-center">
                <div className="text-sm font-medium">{matchTimeFormatted}</div>
                {canBet && (
                  <div className={cn(
                    'flex items-center gap-1 text-xs mt-1',
                    isUrgent ? 'text-warning font-medium' : 'text-muted-foreground'
                  )}>
                    <Timer className="h-3 w-3" />
                    {hoursUntilDeadline > 0
                      ? `${hoursUntilDeadline}h ${minutesUntilDeadline}m`
                      : `${minutesUntilDeadline}m`}
                  </div>
                )}
              </div>
            )}
          </div>

          {/* User's bet indicator */}
          {existingBet && (
            <div className="flex flex-col items-center px-3 py-2 rounded-xl bg-primary/10 border border-primary/20">
              <div className="text-lg font-bold text-primary">
                {existingBet.predictedHomeScore} - {existingBet.predictedAwayScore}
              </div>
              <div className="text-[10px] uppercase tracking-wide text-primary/70 font-medium">
                Your bet
              </div>
            </div>
          )}

          {/* Expand indicator */}
          {canBet && (
            <ChevronDown
              className={cn(
                'h-5 w-5 text-muted-foreground transition-transform duration-200',
                isExpanded && 'rotate-180',
                'group-hover:text-primary'
              )}
            />
          )}
        </div>

        {/* Deadline warning */}
        {isUrgent && !isExpanded && (
          <div className="mt-3 px-3 py-2 rounded-lg bg-warning/10 border border-warning/20 flex items-center gap-2">
            <Clock className="h-4 w-4 text-warning" />
            <span className="text-sm text-warning font-medium">
              Deadline in {minutesUntilDeadline} min - Place your bet now!
            </span>
          </div>
        )}

        {/* Expanded bet form */}
        {isExpanded && canBet && (
          <div className="mt-4 pt-4 border-t border-border/50">
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
        {isFinished && existingBet?.result && (
          <div className="mt-4 pt-4 border-t border-border/50">
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">Your result:</span>
              <div className="flex items-center gap-2">
                {existingBet.result.isExactMatch && (
                  <Badge variant="success">
                    <Trophy className="h-3 w-3" />
                    Exact!
                  </Badge>
                )}
                {!existingBet.result.isExactMatch && existingBet.result.isCorrectResult && (
                  <Badge variant="info">Correct Result</Badge>
                )}
                {!existingBet.result.isExactMatch && !existingBet.result.isCorrectResult && (
                  <Badge variant="outline">Wrong</Badge>
                )}
                <Badge variant="points">
                  +{existingBet.result.pointsEarned} pts
                </Badge>
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

/**
 * Team row with logo and name
 */
function TeamRow({
  crest,
  name,
  score,
  isWinner,
}: {
  crest?: string | null;
  name: string;
  score?: number | null;
  isWinner?: boolean;
}) {
  return (
    <div className="flex items-center gap-3">
      {/* Team crest */}
      <div className="w-8 h-8 rounded-full bg-muted flex items-center justify-center overflow-hidden shadow-sm ring-1 ring-border/50">
        {crest ? (
          <img
            src={crest}
            alt=""
            className="w-6 h-6 object-contain"
          />
        ) : (
          <div className="w-4 h-4 rounded-full bg-muted-foreground/20" />
        )}
      </div>

      {/* Team name */}
      <span className={cn(
        'font-medium flex-1 truncate',
        isWinner && 'text-primary'
      )}>
        {name}
      </span>

      {/* Score */}
      {score !== undefined && score !== null && (
        <span className={cn(
          'text-xl font-bold tabular-nums min-w-[24px] text-right',
          isWinner && 'text-primary'
        )}>
          {score}
        </span>
      )}
    </div>
  );
}

