# Phase 6.4: Leaderboard & Statistics

> **Goal:** Build standings display and user statistics views
> **Backend Analogy:** Query handlers with sorting, projections, and data aggregation
> **Estimated Time:** 4-6 hours
> **Prerequisites:** Phase 6.3 complete (betting system working)

---

## What You'll Learn

| Frontend Concept | Backend Analogy | Example |
|------------------|-----------------|---------|
| Client-side sorting | LINQ OrderBy | `standings.sort((a, b) => ...)` |
| Filtering UI state | Query parameters | `?sortBy=points&order=desc` |
| Table components | Data grids | Sortable, scrollable tables |
| Number formatting | ToString("N0") | `Intl.NumberFormat` |
| Relative time | TimeSpan display | "5 minutes ago" |

---

## Understanding Client-Side vs Server-Side Operations

### When to Sort/Filter on Client

```typescript
// Client-side: Data already loaded, small dataset (<1000 items)
const sortedStandings = useMemo(() => {
  return [...standings].sort((a, b) => b.totalPoints - a.totalPoints);
}, [standings]);
```

### When to Sort/Filter on Server

```typescript
// Server-side: Large dataset, pagination needed
const { data } = useQuery({
  queryKey: ['matches', { sortBy: 'date', page: 1 }],
  queryFn: () => api.get(`/matches?sortBy=date&page=1`),
});
```

**Decision Analysis:**

| Factor | Client-Side | Server-Side |
|--------|-------------|-------------|
| Data size | < 100 items | > 100 items |
| Already loaded | Yes | No |
| Complex filtering | Simple | Complex |
| Real-time updates | No | Yes |

**For our leaderboard:** Client-side is fine because:
- Leagues have max 100 members
- Data is already fetched
- Sorting is simple (by one field)

---

## Step 1: Create Leaderboard Component

### File: `web/src/components/standings/leaderboard.tsx`

```typescript
'use client';

import { useMemo, useState } from 'react';
import { Trophy, Flame, Target, TrendingUp, ArrowUpDown } from 'lucide-react';
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
import type { LeagueStandingDto } from '@/types';

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
```

---

## Step 2: Create Rank Badge Component

### File: `web/src/components/standings/rank-badge.tsx`

```typescript
import { Crown, Medal, Award } from 'lucide-react';

interface RankBadgeProps {
  rank: number;
}

/**
 * Visual rank indicator with special styling for top 3
 *
 * Demonstrates:
 * - Conditional styling based on data
 * - Icon selection logic
 */
export function RankBadge({ rank }: RankBadgeProps) {
  // Top 3 get special styling
  if (rank === 1) {
    return (
      <div className="flex items-center justify-center w-8 h-8 rounded-full bg-yellow-100 dark:bg-yellow-900/30">
        <Crown className="h-5 w-5 text-yellow-500" />
      </div>
    );
  }

  if (rank === 2) {
    return (
      <div className="flex items-center justify-center w-8 h-8 rounded-full bg-gray-100 dark:bg-gray-800">
        <Medal className="h-5 w-5 text-gray-400" />
      </div>
    );
  }

  if (rank === 3) {
    return (
      <div className="flex items-center justify-center w-8 h-8 rounded-full bg-orange-100 dark:bg-orange-900/30">
        <Award className="h-5 w-5 text-orange-500" />
      </div>
    );
  }

  // Others get number only
  return (
    <div className="flex items-center justify-center w-8 h-8 text-muted-foreground">
      {rank}
    </div>
  );
}
```

---

## Step 3: Create User Stats Card

### File: `web/src/components/standings/user-stats-card.tsx`

```typescript
'use client';

import {
  Trophy,
  Target,
  Flame,
  TrendingUp,
  Award,
  BarChart3,
} from 'lucide-react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { useUserStats } from '@/hooks/use-bets';
import { useAuthStore } from '@/stores/auth-store';
import { CardSkeleton } from '@/components/shared/loading-skeleton';
import { RankBadge } from './rank-badge';

interface UserStatsCardProps {
  leagueId: string;
  userId?: string; // If not provided, show current user
}

/**
 * Detailed statistics card for a user
 *
 * Demonstrates:
 * - Progress bar for visual representation
 * - Calculated percentages
 * - Grid layout for stats
 */
export function UserStatsCard({ leagueId, userId }: UserStatsCardProps) {
  const currentUser = useAuthStore((state) => state.user);
  const targetUserId = userId ?? currentUser?.id ?? '';

  const { data: stats, isLoading } = useUserStats(leagueId, targetUserId);

  if (isLoading) {
    return <CardSkeleton />;
  }

  if (!stats) {
    return null;
  }

  // Calculate additional metrics
  const wrongBets = stats.betsPlaced - stats.exactMatches - stats.correctResults;
  const exactPercentage = stats.betsPlaced > 0
    ? (stats.exactMatches / stats.betsPlaced) * 100
    : 0;
  const correctPercentage = stats.betsPlaced > 0
    ? (stats.correctResults / stats.betsPlaced) * 100
    : 0;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              {stats.username}
              {targetUserId === currentUser?.id && (
                <span className="text-sm font-normal text-muted-foreground">
                  (You)
                </span>
              )}
            </CardTitle>
            <CardDescription>League Statistics</CardDescription>
          </div>
          <RankBadge rank={stats.rank} />
        </div>
      </CardHeader>

      <CardContent className="space-y-6">
        {/* Primary stats grid */}
        <div className="grid grid-cols-2 gap-4">
          <StatItem
            icon={Trophy}
            label="Total Points"
            value={stats.totalPoints.toLocaleString()}
            highlight
          />
          <StatItem
            icon={Target}
            label="Bets Placed"
            value={stats.betsPlaced.toString()}
          />
          <StatItem
            icon={Award}
            label="Exact Matches"
            value={stats.exactMatches.toString()}
            subtext={`${exactPercentage.toFixed(1)}% of bets`}
          />
          <StatItem
            icon={TrendingUp}
            label="Correct Results"
            value={stats.correctResults.toString()}
            subtext={`${correctPercentage.toFixed(1)}% of bets`}
          />
        </div>

        {/* Accuracy progress bar */}
        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span className="text-muted-foreground">Accuracy</span>
            <span className="font-medium">{stats.accuracyPercentage}%</span>
          </div>
          <Progress value={stats.accuracyPercentage} className="h-2" />
        </div>

        {/* Streaks */}
        <div className="grid grid-cols-2 gap-4 pt-4 border-t">
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-orange-100 dark:bg-orange-900/30">
              <Flame className="h-5 w-5 text-orange-500" />
            </div>
            <div>
              <div className="text-2xl font-bold">{stats.currentStreak}</div>
              <div className="text-xs text-muted-foreground">Current Streak</div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-purple-100 dark:bg-purple-900/30">
              <BarChart3 className="h-5 w-5 text-purple-500" />
            </div>
            <div>
              <div className="text-2xl font-bold">{stats.bestStreak}</div>
              <div className="text-xs text-muted-foreground">Best Streak</div>
            </div>
          </div>
        </div>

        {/* Bet breakdown */}
        <div className="pt-4 border-t">
          <div className="text-sm text-muted-foreground mb-2">Bet Breakdown</div>
          <div className="flex gap-2 h-4 rounded-full overflow-hidden">
            {stats.exactMatches > 0 && (
              <div
                className="bg-green-500"
                style={{ width: `${exactPercentage}%` }}
                title={`Exact: ${stats.exactMatches}`}
              />
            )}
            {stats.correctResults > 0 && (
              <div
                className="bg-blue-500"
                style={{ width: `${correctPercentage}%` }}
                title={`Correct: ${stats.correctResults}`}
              />
            )}
            {wrongBets > 0 && (
              <div
                className="bg-gray-300 dark:bg-gray-600"
                style={{ width: `${100 - exactPercentage - correctPercentage}%` }}
                title={`Wrong: ${wrongBets}`}
              />
            )}
          </div>
          <div className="flex gap-4 mt-2 text-xs text-muted-foreground">
            <span className="flex items-center gap-1">
              <div className="w-2 h-2 rounded-full bg-green-500" />
              Exact ({stats.exactMatches})
            </span>
            <span className="flex items-center gap-1">
              <div className="w-2 h-2 rounded-full bg-blue-500" />
              Correct ({stats.correctResults})
            </span>
            <span className="flex items-center gap-1">
              <div className="w-2 h-2 rounded-full bg-gray-300 dark:bg-gray-600" />
              Wrong ({wrongBets})
            </span>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function StatItem({
  icon: Icon,
  label,
  value,
  subtext,
  highlight,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: string;
  subtext?: string;
  highlight?: boolean;
}) {
  return (
    <div className="flex items-start gap-3">
      <div
        className={`flex items-center justify-center w-10 h-10 rounded-lg ${
          highlight
            ? 'bg-primary/10'
            : 'bg-muted'
        }`}
      >
        <Icon
          className={`h-5 w-5 ${
            highlight ? 'text-primary' : 'text-muted-foreground'
          }`}
        />
      </div>
      <div>
        <div className={`text-xl font-bold ${highlight ? 'text-primary' : ''}`}>
          {value}
        </div>
        <div className="text-xs text-muted-foreground">{label}</div>
        {subtext && (
          <div className="text-xs text-muted-foreground/70">{subtext}</div>
        )}
      </div>
    </div>
  );
}
```

---

## Step 4: Create Match Bets Reveal Component

### File: `web/src/components/standings/match-bets-reveal.tsx`

```typescript
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
```

---

## Step 5: Create Standings Page

### File: `web/src/app/(protected)/leagues/[id]/standings/page.tsx`

```typescript
import { ProtectedRoute } from '@/components/auth/protected-route';
import { Leaderboard } from '@/components/standings/leaderboard';
import { UserStatsCard } from '@/components/standings/user-stats-card';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default async function StandingsPage({ params }: PageProps) {
  const { id } = await params;

  return (
    <ProtectedRoute>
      <div className="min-h-screen bg-gradient-to-br from-background to-muted p-4">
        <div className="mx-auto max-w-6xl space-y-6">
          <h1 className="text-2xl font-bold tracking-tight">League Standings</h1>

          {/* User's own stats card */}
          <UserStatsCard leagueId={id} />

          {/* Full leaderboard */}
          <Leaderboard leagueId={id} />
        </div>
      </div>
    </ProtectedRoute>
  );
}
```

---

## Step 6: Add shadcn/ui Components

```bash
cd web
npx shadcn@latest add table
npx shadcn@latest add progress
```

---

## Verification Checklist

After completing Phase 6.4:

- [ ] Leaderboard displays all league members
- [ ] Sorting by rank/points/streak/accuracy works
- [ ] Top 3 have special rank badges (crown, medal, award)
- [ ] Current user row is highlighted
- [ ] User stats card shows detailed statistics
- [ ] Progress bar shows accuracy visually
- [ ] Streak display works correctly
- [ ] Bet breakdown bar shows proportions
- [ ] Match bets reveal shows after deadline
- [ ] Winners highlighted in reveal view
- [ ] Responsive design on mobile (hidden columns)
- [ ] `npm run build` passes

---

## Key Learnings from This Phase

1. **Client-side sorting** - Use useMemo for sorted data, track sort state
2. **Table components** - Use proper Table structure for accessibility
3. **Data visualization** - Progress bars and color coding convey information
4. **Responsive tables** - Hide less important columns on mobile
5. **Highlight current user** - Help users find themselves in lists
6. **Conditional styling** - Use Tailwind classes based on data values

---

## Next Step

Proceed to **Phase 6.5: Gamification System** (`phase-6.5-gamification.md`)
