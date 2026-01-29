'use client';

import { motion } from 'framer-motion';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { Progress } from '@/components/ui/progress';
import type { Achievement } from '@/hooks/use-achievements';
import type { UserStatsDto } from '@/types';

interface AchievementBadgeProps {
  achievement: Achievement;
  unlocked: boolean;
  stats?: UserStatsDto;
  size?: 'sm' | 'md' | 'lg';
}

/**
 * Single achievement badge with animation
 *
 * Demonstrates:
 * - Framer Motion hover animations
 * - Conditional styling (unlocked vs locked)
 * - Tooltips for additional info
 */
export function AchievementBadge({
  achievement,
  unlocked,
  stats,
  size = 'md',
}: AchievementBadgeProps) {
  const progress =
    achievement.progress && stats ? achievement.progress(stats) : null;

  const sizeClasses = {
    sm: 'w-10 h-10 text-lg',
    md: 'w-14 h-14 text-2xl',
    lg: 'w-20 h-20 text-4xl',
  };

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <motion.div
            className={`
              ${sizeClasses[size]}
              rounded-full flex items-center justify-center
              ${
                unlocked
                  ? 'bg-gradient-to-br from-yellow-100 to-yellow-200 dark:from-yellow-900/50 dark:to-yellow-800/50 shadow-lg'
                  : 'bg-muted opacity-50 grayscale'
              }
              cursor-pointer select-none
            `}
            whileHover={unlocked ? { scale: 1.1, rotate: 5 } : { scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            initial={unlocked ? { scale: 0 } : { scale: 1 }}
            animate={{ scale: 1 }}
            transition={{ type: 'spring', stiffness: 500, damping: 30 }}
          >
            {achievement.icon}
          </motion.div>
        </TooltipTrigger>
        <TooltipContent side="top" className="max-w-[200px]">
          <div className="space-y-1">
            <p className="font-semibold">{achievement.name}</p>
            <p className="text-xs text-muted-foreground">
              {achievement.description}
            </p>
            {!unlocked && progress && (
              <div className="pt-1">
                <Progress
                  value={(progress.current / progress.target) * 100}
                  className="h-1"
                />
                <p className="text-xs text-muted-foreground mt-1">
                  {progress.current} / {progress.target}
                </p>
              </div>
            )}
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}
