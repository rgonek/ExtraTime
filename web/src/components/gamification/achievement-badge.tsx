'use client';

import { motion } from 'framer-motion';
import { Lock } from 'lucide-react';
import { cva, type VariantProps } from 'class-variance-authority';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { Progress } from '@/components/ui/progress';
import { cn } from '@/lib/utils';
import type { Achievement } from '@/hooks/use-achievements';
import type { UserStatsDto } from '@/types';

const achievementBadgeVariants = cva(
  'relative rounded-full flex items-center justify-center cursor-pointer select-none transition-all duration-200',
  {
    variants: {
      tier: {
        default: '',
        gold: '',
        silver: '',
        bronze: '',
      },
      size: {
        xs: 'w-10 h-10 text-lg',
        sm: 'w-12 h-12 text-xl',
        md: 'w-16 h-16 text-2xl',
        lg: 'w-20 h-20 text-3xl',
        xl: 'w-24 h-24 text-4xl',
      },
    },
    defaultVariants: {
      tier: 'default',
      size: 'md',
    },
  }
);

// Gradient backgrounds for each tier
const tierGradients = {
  default: 'bg-gradient-to-br from-primary to-secondary',
  gold: 'bg-gradient-to-br from-yellow-400 via-amber-400 to-yellow-500',
  silver: 'bg-gradient-to-br from-slate-300 via-gray-200 to-slate-400',
  bronze: 'bg-gradient-to-br from-orange-400 via-amber-600 to-orange-500',
};

// Glow colors for each tier
const tierGlows = {
  default: 'shadow-[0_0_20px_rgba(16,185,129,0.4)]',
  gold: 'shadow-[0_0_20px_rgba(251,191,36,0.5)]',
  silver: 'shadow-[0_0_20px_rgba(148,163,184,0.5)]',
  bronze: 'shadow-[0_0_20px_rgba(251,146,60,0.5)]',
};

// Ring colors for outer decoration
const tierRings = {
  default: 'border-primary/50',
  gold: 'border-yellow-400/50',
  silver: 'border-slate-400/50',
  bronze: 'border-orange-400/50',
};

interface AchievementBadgeProps extends VariantProps<typeof achievementBadgeVariants> {
  achievement: Achievement;
  unlocked: boolean;
  stats?: UserStatsDto;
  showRing?: boolean;
  className?: string;
}

/**
 * Achievement badge with tier-based styling and animations
 *
 * Features:
 * - Gold/Silver/Bronze tier variants
 * - Glow effect for unlocked achievements
 * - Locked state with grayscale and lock icon
 * - Outer ring decoration
 * - Hover scale/rotate animation
 * - Progress tooltip for locked achievements
 */
export function AchievementBadge({
  achievement,
  unlocked,
  stats,
  tier = 'default',
  size = 'md',
  showRing = true,
  className,
}: AchievementBadgeProps) {
  const progress =
    achievement.progress && stats ? achievement.progress(stats) : null;

  const resolvedTier = tier ?? 'default';

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <motion.div
            className={cn(
              achievementBadgeVariants({ size }),
              unlocked
                ? cn(tierGradients[resolvedTier], tierGlows[resolvedTier])
                : 'bg-muted grayscale opacity-60',
              className
            )}
            whileHover={
              unlocked
                ? { scale: 1.1, rotate: 5 }
                : { scale: 1.05 }
            }
            whileTap={{ scale: 0.95 }}
            initial={unlocked ? { scale: 0, rotate: -10 } : { scale: 1 }}
            animate={{ scale: 1, rotate: 0 }}
            transition={{ type: 'spring', stiffness: 400, damping: 25 }}
          >
            {/* Outer ring decoration */}
            {showRing && unlocked && (
              <div
                className={cn(
                  'absolute inset-[-4px] rounded-full border-2',
                  tierRings[resolvedTier],
                  'pointer-events-none'
                )}
              />
            )}

            {/* Icon or lock */}
            {unlocked ? (
              <span className="drop-shadow-sm">{achievement.icon}</span>
            ) : (
              <div className="relative">
                <span className="opacity-40">{achievement.icon}</span>
                <Lock
                  className={cn(
                    'absolute bottom-0 right-0 translate-x-1/4 translate-y-1/4',
                    'text-muted-foreground bg-muted rounded-full p-0.5',
                    size === 'xs' || size === 'sm' ? 'w-3 h-3' : 'w-4 h-4'
                  )}
                />
              </div>
            )}
          </motion.div>
        </TooltipTrigger>
        <TooltipContent side="top" className="max-w-[220px]">
          <div className="space-y-2">
            <div className="flex items-center gap-2">
              {unlocked && tier && tier !== 'default' && (
                <span
                  className={cn(
                    'w-2 h-2 rounded-full',
                    tier === 'gold' && 'bg-yellow-400',
                    tier === 'silver' && 'bg-slate-400',
                    tier === 'bronze' && 'bg-orange-400'
                  )}
                />
              )}
              <p className="font-semibold">{achievement.name}</p>
            </div>
            <p className="text-xs text-muted-foreground">
              {achievement.description}
            </p>
            {!unlocked && progress && (
              <div className="pt-1 space-y-1">
                <Progress
                  value={(progress.current / progress.target) * 100}
                  className="h-1.5"
                  variant="muted"
                />
                <p className="text-xs text-muted-foreground">
                  {progress.current} / {progress.target}
                </p>
              </div>
            )}
            {unlocked && (
              <p className="text-xs text-success font-medium">✓ Unlocked</p>
            )}
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}
