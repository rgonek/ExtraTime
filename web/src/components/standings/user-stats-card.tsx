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
