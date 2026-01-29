'use client';

import { useMemo, useState } from 'react';
import { Trophy, Flame, ArrowUpDown } from 'lucide-react';
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
import { useLeagueStandings } from '@/hooks/use-bets';
import { useAuthStore } from '@/stores/auth-store';
import { CardSkeleton } from '@/components/shared/loading-skeleton';
import { EmptyState } from '@/components/shared/empty-state';
import { ErrorMessage } from '@/components/shared/error-message';
import { RankBadge } from './rank-badge';

type SortField = 'rank' | 'points' | 'streak' | 'accuracy';
type SortOrder = 'asc' | 'desc';

interface LeaderboardProps {
  leagueId: string;
}

/**
 * League leaderboard with sortable columns
 *
 * Demonstrates:
 * - Sortable table with UI state
 * - Client-side sorting with useMemo
 * - Highlighting current user
 * - Rank visualization
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
      // Toggle order if same field
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      // New field, default to descending (highest first) except for rank
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
          // Calculate accuracy: (exact + correct) / total bets
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
    <div className="rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
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
          {sortedStandings.map((standing) => {
            const isCurrentUser = standing.userId === currentUser?.id;
            const accuracy = standing.betsPlaced > 0
              ? Math.round(
                  ((standing.exactMatches + standing.correctResults) /
                    standing.betsPlaced) *
                    100
                )
              : 0;

            return (
              <TableRow
                key={standing.userId}
                className={isCurrentUser ? 'bg-primary/5 font-medium' : ''}
              >
                {/* Rank */}
                <TableCell>
                  <RankBadge rank={standing.rank} />
                </TableCell>

                {/* Player */}
                <TableCell>
                  <div className="flex items-center gap-2">
                    <Avatar className="h-8 w-8">
                      <AvatarFallback>
                        {standing.username.slice(0, 2).toUpperCase()}
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <div className="flex items-center gap-1">
                        {standing.username}
                        {isCurrentUser && (
                          <Badge variant="outline" className="text-xs">
                            You
                          </Badge>
                        )}
                      </div>
                    </div>
                  </div>
                </TableCell>

                {/* Points */}
                <TableCell className="text-right font-bold text-primary">
                  {standing.totalPoints.toLocaleString()}
                </TableCell>

                {/* Streak */}
                <TableCell className="text-right hidden sm:table-cell">
                  {standing.currentStreak > 0 && (
                    <div className="flex items-center justify-end gap-1">
                      <Flame
                        className={`h-4 w-4 ${
                          standing.currentStreak >= 3
                            ? 'text-orange-500'
                            : 'text-muted-foreground'
                        }`}
                      />
                      {standing.currentStreak}
                    </div>
                  )}
                </TableCell>

                {/* Bets */}
                <TableCell className="text-right hidden md:table-cell">
                  {standing.betsPlaced}
                </TableCell>

                {/* Exact */}
                <TableCell className="text-right hidden md:table-cell">
                  {standing.exactMatches > 0 && (
                    <div className="flex items-center justify-end gap-1">
                      <Trophy className="h-4 w-4 text-yellow-500" />
                      {standing.exactMatches}
                    </div>
                  )}
                </TableCell>

                {/* Accuracy */}
                <TableCell className="text-right hidden lg:table-cell">
                  {accuracy}%
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
      className="-ml-3 h-8 data-[state=open]:bg-accent"
      onClick={() => onSort(field)}
    >
      {label}
      <ArrowUpDown
        className={`ml-2 h-4 w-4 ${
          isActive ? 'text-primary' : 'text-muted-foreground'
        }`}
      />
    </Button>
  );
}
