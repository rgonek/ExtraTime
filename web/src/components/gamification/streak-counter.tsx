'use client';

import { motion, AnimatePresence } from 'framer-motion';
import { Flame } from 'lucide-react';

interface StreakCounterProps {
  currentStreak: number;
  bestStreak: number;
}

/**
 * Animated streak display with fire icon
 *
 * Demonstrates:
 * - AnimatePresence for mount/unmount animations
 * - Conditional intensity based on streak value
 * - Number animation
 */
export function StreakCounter({ currentStreak, bestStreak }: StreakCounterProps) {
  // Intensity increases with streak
  const flameColor =
    currentStreak >= 5
      ? 'text-orange-500'
      : currentStreak >= 3
        ? 'text-amber-500'
        : 'text-gray-400';

  return (
    <div className="flex items-center gap-4">
      {/* Current streak */}
      <div className="flex items-center gap-2">
        <motion.div
          animate={{
            scale: currentStreak > 0 ? [1, 1.1, 1] : 1,
          }}
          transition={{
            repeat: currentStreak >= 3 ? Infinity : 0,
            duration: 1.5,
          }}
        >
          <Flame
            className={`w-8 h-8 ${flameColor} ${
              currentStreak >= 3
                ? 'drop-shadow-[0_0_8px_rgba(251,146,60,0.5)]'
                : ''
            }`}
          />
        </motion.div>
        <div>
          <AnimatePresence mode="wait">
            <motion.div
              key={currentStreak}
              initial={{ y: -20, opacity: 0 }}
              animate={{ y: 0, opacity: 1 }}
              exit={{ y: 20, opacity: 0 }}
              className="text-2xl font-bold"
            >
              {currentStreak}
            </motion.div>
          </AnimatePresence>
          <div className="text-xs text-muted-foreground">Current</div>
        </div>
      </div>

      {/* Best streak */}
      <div className="text-center border-l pl-4">
        <div className="text-lg font-semibold text-muted-foreground">
          {bestStreak}
        </div>
        <div className="text-xs text-muted-foreground">Best</div>
      </div>
    </div>
  );
}
