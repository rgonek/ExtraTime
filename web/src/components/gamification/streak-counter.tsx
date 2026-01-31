'use client';

import { motion, AnimatePresence } from 'framer-motion';
import { Flame, Trophy } from 'lucide-react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/utils';

const streakCounterVariants = cva(
  'inline-flex items-center gap-3 rounded-2xl transition-all duration-200',
  {
    variants: {
      variant: {
        default: 'bg-accent-light border-2 border-accent',
        compact: 'bg-accent/10 border border-accent/30',
        ghost: '',
      },
      size: {
        sm: 'px-3 py-2',
        default: 'px-4 py-3',
        lg: 'px-5 py-4',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
);

interface StreakCounterProps extends VariantProps<typeof streakCounterVariants> {
  currentStreak: number;
  bestStreak?: number;
  showBest?: boolean;
  className?: string;
}

/**
 * Animated streak display with fire icon
 *
 * Features:
 * - Fire animation that intensifies with streak
 * - Accent color theme matching design system
 * - Multiple size and variant options
 * - Best streak comparison
 */
export function StreakCounter({
  currentStreak,
  bestStreak,
  showBest = true,
  variant,
  size,
  className,
}: StreakCounterProps) {
  // Fire intensity and animation based on streak value
  const isOnFire = currentStreak >= 3;
  const isHotStreak = currentStreak >= 5;

  const flameColorClass = isHotStreak
    ? 'text-orange-500'
    : isOnFire
      ? 'text-accent'
      : 'text-muted-foreground';

  const iconSizeClass = size === 'lg' ? 'w-8 h-8' : size === 'sm' ? 'w-5 h-5' : 'w-7 h-7';
  const numberSizeClass = size === 'lg' ? 'text-4xl' : size === 'sm' ? 'text-xl' : 'text-3xl';
  const labelSizeClass = size === 'lg' ? 'text-sm' : 'text-xs';

  return (
    <div className={cn(streakCounterVariants({ variant, size }), className)}>
      {/* Flame icon with animation */}
      <div className={cn('relative', isOnFire && 'animate-fire')}>
        <Flame
          className={cn(
            iconSizeClass,
            flameColorClass,
            isOnFire && 'drop-shadow-[0_0_10px_rgba(245,158,11,0.5)] dark:drop-shadow-[0_0_12px_rgba(251,191,36,0.6)]',
            isHotStreak && 'drop-shadow-[0_0_14px_rgba(249,115,22,0.6)] dark:drop-shadow-[0_0_18px_rgba(251,146,60,0.7)]'
          )}
        />
      </div>

      {/* Current streak */}
      <div className="flex flex-col items-start">
        <AnimatePresence mode="wait">
          <motion.div
            key={currentStreak}
            initial={{ y: -12, opacity: 0, scale: 0.8 }}
            animate={{ y: 0, opacity: 1, scale: 1 }}
            exit={{ y: 12, opacity: 0, scale: 0.8 }}
            transition={{ type: 'spring', stiffness: 400, damping: 25 }}
            className={cn(numberSizeClass, 'font-bold text-accent leading-none')}
          >
            {currentStreak}
          </motion.div>
        </AnimatePresence>
        <span className={cn(labelSizeClass, 'text-accent font-medium')}>
          day streak
        </span>
      </div>

      {/* Best streak (optional) */}
      {showBest && bestStreak !== undefined && (
        <div className="flex items-center gap-1.5 border-l border-accent/30 pl-3 ml-1">
          <Trophy className={cn(
            size === 'lg' ? 'w-4 h-4' : 'w-3.5 h-3.5',
            'text-accent/60'
          )} />
          <div className="flex flex-col items-start">
            <span className={cn(
              size === 'lg' ? 'text-lg' : 'text-base',
              'font-semibold text-accent/80 leading-none'
            )}>
              {bestStreak}
            </span>
            <span className={cn(labelSizeClass, 'text-accent/60')}>best</span>
          </div>
        </div>
      )}
    </div>
  );
}
