'use client';

import { motion } from 'framer-motion';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { AchievementBadge } from './achievement-badge';
import { useAchievements, ACHIEVEMENTS } from '@/hooks/use-achievements';
import { useUserStats } from '@/hooks/use-bets';
import { useAuthStore } from '@/stores/auth-store';
import { CardSkeleton } from '@/components/shared/loading-skeleton';

interface AchievementListProps {
  leagueId: string;
}

/**
 * Full list of achievements with progress tracking
 *
 * Demonstrates:
 * - Staggered animation for list items
 * - Progress visualization
 * - Grouped display (unlocked vs locked)
 */
export function AchievementList({ leagueId }: AchievementListProps) {
  const currentUser = useAuthStore((state) => state.user);
  const { data: stats, isLoading } = useUserStats(
    leagueId,
    currentUser?.id ?? ''
  );
  const { unlocked, nextUp, totalUnlocked, totalPossible } =
    useAchievements(stats);

  if (isLoading) {
    return <CardSkeleton />;
  }

  return (
    <div className="space-y-6">
      {/* Summary */}
      <Card>
        <CardHeader>
          <CardTitle>Achievements</CardTitle>
          <CardDescription>
            {totalUnlocked} of {totalPossible} unlocked
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Progress
            value={(totalUnlocked / totalPossible) * 100}
            className="h-2"
          />
        </CardContent>
      </Card>

      {/* Unlocked achievements */}
      {unlocked.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Unlocked</CardTitle>
          </CardHeader>
          <CardContent>
            <motion.div
              className="flex flex-wrap gap-4"
              initial="hidden"
              animate="visible"
              variants={{
                visible: {
                  transition: { staggerChildren: 0.1 },
                },
              }}
            >
              {unlocked.map((achievement) => (
                <motion.div
                  key={achievement.id}
                  variants={{
                    hidden: { opacity: 0, y: 20 },
                    visible: { opacity: 1, y: 0 },
                  }}
                >
                  <AchievementBadge
                    achievement={achievement}
                    unlocked={true}
                    stats={stats}
                  />
                </motion.div>
              ))}
            </motion.div>
          </CardContent>
        </Card>
      )}

      {/* Next up (with progress) */}
      {nextUp.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Next Up</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {nextUp.map((achievement) => {
              const progress = achievement.progress?.(stats!);
              return (
                <div key={achievement.id} className="flex items-center gap-4">
                  <AchievementBadge
                    achievement={achievement}
                    unlocked={false}
                    stats={stats}
                    size="sm"
                  />
                  <div className="flex-1">
                    <div className="flex items-center justify-between mb-1">
                      <span className="font-medium">{achievement.name}</span>
                      {progress && (
                        <span className="text-sm text-muted-foreground">
                          {progress.current} / {progress.target}
                        </span>
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground mb-2">
                      {achievement.description}
                    </p>
                    {progress && (
                      <Progress
                        value={(progress.current / progress.target) * 100}
                        className="h-1"
                      />
                    )}
                  </div>
                </div>
              );
            })}
          </CardContent>
        </Card>
      )}

      {/* All locked achievements */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">All Achievements</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-3">
            {ACHIEVEMENTS.map((achievement) => {
              const isUnlocked = unlocked.some((u) => u.id === achievement.id);
              return (
                <AchievementBadge
                  key={achievement.id}
                  achievement={achievement}
                  unlocked={isUnlocked}
                  stats={stats}
                  size="sm"
                />
              );
            })}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
