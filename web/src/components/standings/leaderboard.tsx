'use client';

import { useMemo, useState } from 'react';
import { Trophy, Flame, ArrowUpDown, TrendingUp, Target } from 'lucide-react';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { useLeagueStandings } from '@/hooks/use-bets';
import { useAuthStore } from '@/stores/auth-store';
import { CardSkeleton } from '@/components/shared/loading-skeleton';
import { EmptyState } from '@/components/shared/empty-state';
import { ErrorMessage } from '@/components/shared/error-message';
import { RankBadge } from '@/components/gamification/rank-badge';
import { cn } from '@/lib/utils';

type SortField = 'rank' | 'points' | 'streak' | 'accuracy';
type SortOrder = 'asc' | 'desc';

interface LeaderboardProps {
  leagueId: string;
}

/**
 * League leaderboard with sortable columns
 */
export function Leaderboard({ leagueId }: LeaderboardProps) {
  const currentUser = useAuthStore((state) => state.user);
  const { data: standings, isLoading, isError, error, refetch } =
    useLeagueStandings(leagueId);

  // Sorting state
  const [sortField, setSortField] = useState<SortField>('rank');
  const [sortOrder, setSortOrder] = useState<SortOrder>('asc');

  // Handle column header click to toggle sort
  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortOrder(field === 'rank' ? 'asc' : 'desc');
    }
  };

  // Sort standings based on current state
  const sortedStandings = useMemo(() => {
    if (!standings) return [];

    return [...standings].sort((a, b) => {
      let comparison = 0;

      switch (sortField) {
        case 'rank':
          comparison = a.rank - b.rank;
          break;
        case 'points':
          comparison = a.totalPoints - b.totalPoints;
          break;
        case 'streak':
          comparison = a.currentStreak - b.currentStreak;
          break;
        case 'accuracy':
          const accA = a.betsPlaced > 0
            ? (a.exactMatches + a.correctResults) / a.betsPlaced
            : 0;
          const accB = b.betsPlaced > 0
            ? (b.exactMatches + b.correctResults) / b.betsPlaced
            : 0;
          comparison = accA - accB;
          break;
      }

      return sortOrder === 'asc' ? comparison : -comparison;
    });
  }, [standings, sortField, sortOrder]);

  // Calculate max points for progress bars
  const maxPoints = useMemo(() => {
    if (!standings || standings.length === 0) return 1;
    return Math.max(...standings.map((s) => s.totalPoints));
  }, [standings]);

  if (isLoading) {
    return <CardSkeleton />;
  }

  if (isError) {
    return (
      <ErrorMessage
        title="Failed to load standings"
        message={error?.message ?? 'Something went wrong'}
        onRetry={() => refetch()}
      />
    );
  }

  if (!standings || standings.length === 0) {
    return (
      <EmptyState
        icon={Trophy}
        title="No standings yet"
        description="Place some bets to start competing"
      />
    );
  }

  return (
    <div className="rounded-xl border bg-card overflow-hidden">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/30 hover:bg-muted/30">
            <TableHead className="w-16">
              <SortableHeader
                label="Rank"
                field="rank"
                currentField={sortField}
                order={sortOrder}
                onSort={handleSort}
              />
            </TableHead>
            <TableHead>Player</TableHead>
            <TableHead className="text-right">
              <SortableHeader
                label="Points"
                field="points"
                currentField={sortField}
                order={sortOrder}
                onSort={handleSort}
              />
            </TableHead>
            <TableHead className="text-right hidden sm:table-cell">
              <SortableHeader
                label="Streak"
                field="streak"
                currentField={sortField}
                order={sortOrder}
                onSort={handleSort}
              />
            </TableHead>
            <TableHead className="text-right hidden md:table-cell">Bets</TableHead>
            <TableHead className="text-right hidden md:table-cell">Exact</TableHead>
            <TableHead className="text-right hidden lg:table-cell">
              <SortableHeader
                label="Accuracy"
                field="accuracy"
                currentField={sortField}
                order={sortOrder}
                onSort={handleSort}
              />
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {sortedStandings.map((standing, index) => {
            const isCurrentUser = standing.userId === currentUser?.id;
            const accuracy = standing.betsPlaced > 0
              ? Math.round(
                  ((standing.exactMatches + standing.correctResults) /
                    standing.betsPlaced) *
                    100
                )
              : 0;
            const pointsProgress = maxPoints > 0 ? (standing.totalPoints / maxPoints) * 100 : 0;
            const isTopThree = standing.rank <= 3;

            return (
              <TableRow
                key={standing.userId}
                className={cn(
                  'transition-colors duration-150',
                  isCurrentUser && 'bg-primary/5 hover:bg-primary/10',
                  isTopThree && !isCurrentUser && 'bg-muted/20',
                  'hover:bg-muted/50'
                )}
              >
                {/* Rank */}
                <TableCell className="py-3">
                  <RankBadge rank={standing.rank} size="sm" animated={false} />
                </TableCell>

                {/* Player */}
                <TableCell className="py-3">
                  <div className="flex items-center gap-3">
                    <Avatar size="md" className={cn(
                      'ring-2 ring-offset-2 ring-offset-background',
                      standing.rank === 1 && 'ring-yellow-400',
                      standing.rank === 2 && 'ring-slate-400',
                      standing.rank === 3 && 'ring-orange-400',
                      standing.rank > 3 && 'ring-border'
                    )}>
                      <AvatarFallback className={cn(
                        standing.rank === 1 && 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/50 dark:text-yellow-400',
                        standing.rank === 2 && 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
                        standing.rank === 3 && 'bg-orange-100 text-orange-700 dark:bg-orange-900/50 dark:text-orange-400'
                      )}>
                        {standing.username.slice(0, 2).toUpperCase()}
                      </AvatarFallback>
                    </Avatar>
                    <div className="min-w-0">
                      <div className="flex items-center gap-2">
                        <span className={cn(
                          'font-medium truncate',
                          isCurrentUser && 'text-primary'
                        )}>
                          {standing.username}
                        </span>
                        {isCurrentUser && (
                          <Badge variant="points" className="text-[10px] px-1.5 py-0">
                            You
                          </Badge>
                        )}
                      </div>
                    </div>
                  </div>
                </TableCell>

                {/* Points */}
                <TableCell className="text-right py-3">
                  <div className="flex flex-col items-end gap-1">
                    <span className={cn(
                      'font-bold tabular-nums',
                      isTopThree ? 'text-primary text-lg' : 'text-foreground'
                    )}>
                      {standing.totalPoints.toLocaleString()}
                    </span>
                    <Progress
                      value={pointsProgress}
                      variant={isTopThree ? 'default' : 'muted'}
                      size="sm"
                      className="w-16 h-1"
                    />
                  </div>
                </TableCell>

                {/* Streak */}
                <TableCell className="text-right hidden sm:table-cell py-3">
                  {standing.currentStreak > 0 ? (
                    <div className={cn(
                      'inline-flex items-center gap-1 px-2 py-1 rounded-lg',
                      standing.currentStreak >= 5 && 'bg-orange-500/10',
                      standing.currentStreak >= 3 && standing.currentStreak < 5 && 'bg-accent/10'
                    )}>
                      <Flame
                        className={cn(
                          'h-4 w-4',
                          standing.currentStreak >= 5 && 'text-orange-500 animate-fire',
                          standing.currentStreak >= 3 && standing.currentStreak < 5 && 'text-accent',
                          standing.currentStreak < 3 && 'text-muted-foreground'
                        )}
                      />
                      <span className={cn(
                        'font-semibold tabular-nums',
                        standing.currentStreak >= 5 && 'text-orange-500',
                        standing.currentStreak >= 3 && standing.currentStreak < 5 && 'text-accent',
                        standing.currentStreak < 3 && 'text-muted-foreground'
                      )}>
                        {standing.currentStreak}
                      </span>
                    </div>
                  ) : (
                    <span className="text-muted-foreground">-</span>
                  )}
                </TableCell>

                {/* Bets */}
                <TableCell className="text-right hidden md:table-cell py-3">
                  <span className="tabular-nums text-muted-foreground">
                    {standing.betsPlaced}
                  </span>
                </TableCell>

                {/* Exact */}
                <TableCell className="text-right hidden md:table-cell py-3">
                  {standing.exactMatches > 0 ? (
                    <div className="inline-flex items-center gap-1 px-2 py-1 rounded-lg bg-yellow-500/10">
                      <Trophy className="h-4 w-4 text-yellow-500" />
                      <span className="font-semibold text-yellow-600 dark:text-yellow-400 tabular-nums">
                        {standing.exactMatches}
                      </span>
                    </div>
                  ) : (
                    <span className="text-muted-foreground">-</span>
                  )}
                </TableCell>

                {/* Accuracy */}
                <TableCell className="text-right hidden lg:table-cell py-3">
                  <div className="inline-flex items-center gap-1.5">
                    <Target className={cn(
                      'h-3.5 w-3.5',
                      accuracy >= 70 && 'text-success',
                      accuracy >= 50 && accuracy < 70 && 'text-secondary',
                      accuracy < 50 && 'text-muted-foreground'
                    )} />
                    <span className={cn(
                      'font-medium tabular-nums',
                      accuracy >= 70 && 'text-success',
                      accuracy >= 50 && accuracy < 70 && 'text-secondary',
                      accuracy < 50 && 'text-muted-foreground'
                    )}>
                      {accuracy}%
                    </span>
                  </div>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}

/**
 * Sortable column header button
 */
function SortableHeader({
  label,
  field,
  currentField,
  order,
  onSort,
}: {
  label: string;
  field: SortField;
  currentField: SortField;
  order: SortOrder;
  onSort: (field: SortField) => void;
}) {
  const isActive = currentField === field;

  return (
    <Button
      variant="ghost"
      size="sm"
      className={cn(
        '-ml-3 h-8 hover:bg-muted',
        isActive && 'text-primary'
      )}
      onClick={() => onSort(field)}
    >
      {label}
      <ArrowUpDown
        className={cn(
          'ml-2 h-4 w-4 transition-colors',
          isActive ? 'text-primary' : 'text-muted-foreground'
        )}
      />
    </Button>
  );
}
