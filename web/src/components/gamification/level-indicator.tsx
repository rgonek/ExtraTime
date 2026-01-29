'use client';

import { motion } from 'framer-motion';
import { Star } from 'lucide-react';
import { Progress } from '@/components/ui/progress';
import { getUserLevel } from '@/hooks/use-achievements';

interface LevelIndicatorProps {
  totalPoints: number;
  showProgress?: boolean;
}

/**
 * User level badge with progress to next level
 *
 * Demonstrates:
 * - Animated number changes
 * - Level progression visualization
 */
export function LevelIndicator({
  totalPoints,
  showProgress = true,
}: LevelIndicatorProps) {
  const { level, title, pointsForNext, progress } = getUserLevel(totalPoints);

  return (
    <div className="flex items-center gap-3">
      {/* Level badge */}
      <motion.div
        className="relative flex items-center justify-center w-12 h-12 rounded-full bg-gradient-to-br from-primary/20 to-primary/40"
        whileHover={{ scale: 1.1 }}
      >
        <Star className="w-6 h-6 text-primary fill-primary" />
        <div className="absolute -bottom-1 -right-1 w-5 h-5 rounded-full bg-primary text-primary-foreground text-xs font-bold flex items-center justify-center">
          {level}
        </div>
      </motion.div>

      {/* Level info */}
      <div className="flex-1">
        <div className="flex items-center justify-between">
          <span className="font-semibold">{title}</span>
          {showProgress && progress < 100 && (
            <span className="text-xs text-muted-foreground">
              {totalPoints} / {pointsForNext} pts
            </span>
          )}
        </div>
        {showProgress && progress < 100 && (
          <Progress value={progress} className="h-1 mt-1" />
        )}
      </div>
    </div>
  );
}
