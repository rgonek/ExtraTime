'use client';

import { motion, AnimatePresence } from 'framer-motion';
import { Star, Sparkles, TrendingUp } from 'lucide-react';
import { cva, type VariantProps } from 'class-variance-authority';
import { Progress } from '@/components/ui/progress';
import { cn } from '@/lib/utils';
import { getUserLevel } from '@/hooks/use-achievements';

const levelIndicatorVariants = cva(
  'flex items-center gap-4',
  {
    variants: {
      variant: {
        default: '',
        card: 'p-5 bg-card rounded-xl shadow-md',
        compact: '',
      },
      size: {
        sm: '',
        default: '',
        lg: '',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
);

interface LevelIndicatorProps extends VariantProps<typeof levelIndicatorVariants> {
  totalPoints: number;
  showProgress?: boolean;
  showXpGain?: number;
  className?: string;
}

/**
 * User level badge with progress to next level
 *
 * Features:
 * - Gradient background with glow effect
 * - Animated level number
 * - Gradient progress bar
 * - XP gain animation
 * - Multiple size and variant options
 */
export function LevelIndicator({
  totalPoints,
  showProgress = true,
  showXpGain,
  variant,
  size,
  className,
}: LevelIndicatorProps) {
  const { level, title, pointsForNext, progress } = getUserLevel(totalPoints);

  const badgeSizeClasses = {
    sm: 'w-12 h-12',
    default: 'w-16 h-16',
    lg: 'w-20 h-20',
  };

  const levelTextClasses = {
    sm: 'text-lg',
    default: 'text-2xl',
    lg: 'text-3xl',
  };

  const resolvedSize = size ?? 'default';

  return (
    <div className={cn(levelIndicatorVariants({ variant, size }), className)}>
      {/* Level badge with gradient and glow */}
      <motion.div
        className={cn(
          'relative flex items-center justify-center rounded-full',
          'bg-gradient-to-br from-primary via-primary to-secondary',
          'shadow-[0_0_20px_rgba(16,185,129,0.3)] dark:shadow-[0_0_24px_rgba(52,211,153,0.4),0_0_8px_rgba(52,211,153,0.25)]',
          badgeSizeClasses[resolvedSize]
        )}
        whileHover={{ scale: 1.05 }}
        transition={{ type: 'spring', stiffness: 400, damping: 25 }}
      >
        {/* Glow ring */}
        <div className="absolute inset-[-2px] rounded-full bg-gradient-to-br from-primary/50 to-secondary/50 blur-sm" />

        {/* Inner circle */}
        <div className={cn(
          'relative flex items-center justify-center rounded-full',
          'bg-gradient-to-br from-primary to-secondary',
          resolvedSize === 'lg' ? 'w-[76px] h-[76px]' : resolvedSize === 'sm' ? 'w-[44px] h-[44px]' : 'w-[60px] h-[60px]'
        )}>
          <AnimatePresence mode="wait">
            <motion.span
              key={level}
              initial={{ scale: 0.5, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 1.5, opacity: 0 }}
              className={cn(
                'font-bold text-white drop-shadow-sm',
                levelTextClasses[resolvedSize]
              )}
            >
              {level}
            </motion.span>
          </AnimatePresence>
        </div>

        {/* Star decoration */}
        <div className="absolute -top-1 -right-1">
          <Sparkles className={cn(
            'text-yellow-400 drop-shadow-sm',
            resolvedSize === 'lg' ? 'w-5 h-5' : 'w-4 h-4'
          )} />
        </div>
      </motion.div>

      {/* Level info */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <span className={cn(
              'font-bold text-foreground',
              resolvedSize === 'lg' ? 'text-xl' : resolvedSize === 'sm' ? 'text-sm' : 'text-base'
            )}>
              {title}
            </span>
            {showXpGain && showXpGain > 0 && (
              <motion.span
                initial={{ opacity: 0, y: 10, scale: 0.8 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                className="text-xs font-semibold text-success flex items-center gap-0.5"
              >
                <TrendingUp className="w-3 h-3" />
                +{showXpGain} XP
              </motion.span>
            )}
          </div>
          {showProgress && progress < 100 && (
            <span className={cn(
              'text-muted-foreground font-medium',
              resolvedSize === 'lg' ? 'text-sm' : 'text-xs'
            )}>
              {totalPoints.toLocaleString()} / {pointsForNext.toLocaleString()} pts
            </span>
          )}
        </div>

        {showProgress && progress < 100 && (
          <div className="mt-2 space-y-1">
            <Progress
              value={progress}
              size={resolvedSize === 'lg' ? 'lg' : 'default'}
              indicatorVariant="gradient"
              className="w-full"
            />
            <p className="text-xs text-muted-foreground">
              {Math.round(pointsForNext - totalPoints)} pts to next level
            </p>
          </div>
        )}

        {progress >= 100 && (
          <p className="text-xs text-success font-medium mt-1 flex items-center gap-1">
            <Star className="w-3 h-3 fill-current" />
            Max level reached!
          </p>
        )}
      </div>
    </div>
  );
}
