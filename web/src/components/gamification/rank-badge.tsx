'use client';

import { motion } from 'framer-motion';
import { Crown, Medal, Award, Hash } from 'lucide-react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/utils';

const rankBadgeVariants = cva(
  'flex items-center justify-center font-bold text-white shrink-0 transition-all duration-200',
  {
    variants: {
      size: {
        xs: 'w-6 h-6 text-xs rounded-md',
        sm: 'w-8 h-8 text-sm rounded-lg',
        default: 'w-10 h-10 text-base rounded-xl',
        lg: 'w-12 h-12 text-lg rounded-xl',
        xl: 'w-14 h-14 text-xl rounded-2xl',
      },
    },
    defaultVariants: {
      size: 'default',
    },
  }
);

interface RankBadgeProps extends VariantProps<typeof rankBadgeVariants> {
  rank: number;
  showIcon?: boolean;
  animated?: boolean;
  className?: string;
}

/**
 * Rank badge with gradient backgrounds for top positions
 *
 * Features:
 * - Gold gradient for rank 1
 * - Silver gradient for rank 2
 * - Bronze gradient for rank 3
 * - Neutral style for ranks 4+
 * - Optional crown/medal icons
 * - Hover and mount animations
 */
export function RankBadge({
  rank,
  size,
  showIcon = false,
  animated = true,
  className,
}: RankBadgeProps) {
  // Styling based on rank
  const getRankStyles = (rank: number) => {
    switch (rank) {
      case 1:
        return {
          gradient: 'bg-gradient-to-br from-yellow-400 via-amber-400 to-yellow-500 dark:from-yellow-300 dark:via-amber-400 dark:to-yellow-500',
          shadow: 'shadow-[0_2px_12px_rgba(251,191,36,0.4)] dark:shadow-[0_2px_16px_rgba(251,191,36,0.5),0_0_8px_rgba(251,191,36,0.3)]',
          icon: <Crown className="w-3/5 h-3/5" />,
          ring: 'ring-2 ring-yellow-300/50 dark:ring-yellow-400/40',
        };
      case 2:
        return {
          gradient: 'bg-gradient-to-br from-slate-300 via-gray-200 to-slate-400 dark:from-slate-200 dark:via-slate-300 dark:to-slate-400',
          shadow: 'shadow-[0_2px_12px_rgba(148,163,184,0.4)] dark:shadow-[0_2px_16px_rgba(203,213,225,0.4),0_0_8px_rgba(203,213,225,0.2)]',
          icon: <Medal className="w-3/5 h-3/5" />,
          ring: 'ring-2 ring-slate-300/50 dark:ring-slate-300/40',
        };
      case 3:
        return {
          gradient: 'bg-gradient-to-br from-orange-400 via-amber-500 to-orange-500 dark:from-orange-300 dark:via-amber-400 dark:to-orange-500',
          shadow: 'shadow-[0_2px_12px_rgba(251,146,60,0.4)] dark:shadow-[0_2px_16px_rgba(251,146,60,0.5),0_0_8px_rgba(251,146,60,0.3)]',
          icon: <Award className="w-3/5 h-3/5" />,
          ring: 'ring-2 ring-orange-300/50 dark:ring-orange-400/40',
        };
      default:
        return {
          gradient: 'bg-muted',
          shadow: '',
          icon: null,
          ring: 'ring-1 ring-border',
          textColor: 'text-muted-foreground',
        };
    }
  };

  const styles = getRankStyles(rank);
  const isTopThree = rank <= 3;

  const content = showIcon && styles.icon ? (
    styles.icon
  ) : (
    <span className="flex items-center">
      {!isTopThree && size !== 'xs' && size !== 'sm' && (
        <Hash className="w-3 h-3 opacity-60" />
      )}
      {rank}
    </span>
  );

  const baseClasses = cn(
    rankBadgeVariants({ size }),
    styles.gradient,
    styles.shadow,
    styles.ring,
    !isTopThree && 'text-muted-foreground',
    className
  );

  if (animated) {
    return (
      <motion.div
        className={baseClasses}
        initial={{ scale: 0.8, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        whileHover={{ scale: 1.1 }}
        transition={{ type: 'spring', stiffness: 400, damping: 25 }}
      >
        {content}
      </motion.div>
    );
  }

  return (
    <div className={baseClasses}>
      {content}
    </div>
  );
}

/**
 * Rank change indicator showing movement in standings
 */
export function RankChange({
  change,
  className,
}: {
  change: number;
  className?: string;
}) {
  if (change === 0) return null;

  const isUp = change > 0;

  return (
    <motion.div
      initial={{ opacity: 0, y: isUp ? 4 : -4 }}
      animate={{ opacity: 1, y: 0 }}
      className={cn(
        'flex items-center gap-0.5 text-xs font-semibold',
        isUp ? 'text-success' : 'text-destructive',
        className
      )}
    >
      <span className={cn('text-[10px]', isUp ? 'rotate-0' : 'rotate-180')}>
        {isUp ? '▲' : '▼'}
      </span>
      <span>{Math.abs(change)}</span>
    </motion.div>
  );
}

/**
 * Compact rank display for tables/lists
 */
export function RankCell({
  rank,
  previousRank,
  showChange = true,
  className,
}: {
  rank: number;
  previousRank?: number;
  showChange?: boolean;
  className?: string;
}) {
  const change = previousRank !== undefined ? previousRank - rank : 0;

  return (
    <div className={cn('flex items-center gap-2', className)}>
      <RankBadge rank={rank} size="sm" />
      {showChange && change !== 0 && <RankChange change={change} />}
    </div>
  );
}
