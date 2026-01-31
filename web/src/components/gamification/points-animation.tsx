'use client';

import { motion, AnimatePresence } from 'framer-motion';
import { useState } from 'react';
import { Target, CheckCircle2, XCircle } from 'lucide-react';
import { cn } from '@/lib/utils';

type PointsVariant = 'exact' | 'correct' | 'incorrect' | 'default';

interface PointsAnimationProps {
  points: number;
  show: boolean;
  variant?: PointsVariant;
  label?: string;
  onComplete?: () => void;
}

// Styling for each variant (with enhanced dark mode shadows)
const variantStyles: Record<PointsVariant, {
  gradient: string;
  shadow: string;
  icon: React.ReactNode;
  iconBg: string;
}> = {
  exact: {
    gradient: 'bg-gradient-to-br from-success via-emerald-500 to-green-600',
    shadow: 'shadow-[0_4px_20px_rgba(34,197,94,0.5)] dark:shadow-[0_4px_24px_rgba(74,222,128,0.6),0_0_8px_rgba(74,222,128,0.4)]',
    icon: <Target className="w-5 h-5" />,
    iconBg: 'bg-white/20',
  },
  correct: {
    gradient: 'bg-gradient-to-br from-primary via-emerald-500 to-teal-500',
    shadow: 'shadow-[0_4px_20px_rgba(16,185,129,0.5)] dark:shadow-[0_4px_24px_rgba(52,211,153,0.6),0_0_8px_rgba(52,211,153,0.4)]',
    icon: <CheckCircle2 className="w-5 h-5" />,
    iconBg: 'bg-white/20',
  },
  incorrect: {
    gradient: 'bg-gradient-to-br from-destructive to-red-600',
    shadow: 'shadow-[0_4px_20px_rgba(239,68,68,0.5)] dark:shadow-[0_4px_24px_rgba(248,113,113,0.6),0_0_8px_rgba(248,113,113,0.4)]',
    icon: <XCircle className="w-5 h-5" />,
    iconBg: 'bg-white/20',
  },
  default: {
    gradient: 'bg-gradient-to-br from-primary to-secondary',
    shadow: 'shadow-[0_4px_20px_rgba(16,185,129,0.4)] dark:shadow-[0_4px_24px_rgba(52,211,153,0.5),0_0_8px_rgba(52,211,153,0.3)]',
    icon: null,
    iconBg: '',
  },
};

// Labels for each variant
const defaultLabels: Record<PointsVariant, string> = {
  exact: 'Exact Score!',
  correct: 'Correct Result',
  incorrect: '',
  default: '',
};

/**
 * Floating points animation with variant styling
 *
 * Features:
 * - Bounce animation on mount
 * - Distinct styling for exact vs correct predictions
 * - Gradient backgrounds with glow
 * - Optional label display
 */
export function PointsAnimation({
  points,
  show,
  variant = 'default',
  label,
  onComplete,
}: PointsAnimationProps) {
  const styles = variantStyles[variant];
  const displayLabel = label ?? defaultLabels[variant];

  return (
    <AnimatePresence onExitComplete={onComplete}>
      {show && (
        <motion.div
          className="fixed inset-0 pointer-events-none flex items-center justify-center z-50"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
        >
          <motion.div
            className={cn(
              'flex flex-col items-center gap-2 px-8 py-5 rounded-2xl text-white',
              styles.gradient,
              styles.shadow
            )}
            initial={{ scale: 0, y: 60, rotate: -10 }}
            animate={{ scale: 1, y: 0, rotate: 0 }}
            exit={{ scale: 0.5, y: -80, opacity: 0, rotate: 5 }}
            transition={{
              type: 'spring',
              stiffness: 400,
              damping: 20,
            }}
          >
            {/* Icon and label row */}
            {(styles.icon || displayLabel) && (
              <div className="flex items-center gap-2">
                {styles.icon && (
                  <motion.div
                    className={cn('p-1 rounded-full', styles.iconBg)}
                    initial={{ scale: 0 }}
                    animate={{ scale: 1 }}
                    transition={{ delay: 0.1 }}
                  >
                    {styles.icon}
                  </motion.div>
                )}
                {displayLabel && (
                  <motion.span
                    className="text-sm font-semibold opacity-90"
                    initial={{ opacity: 0, x: -10 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ delay: 0.15 }}
                  >
                    {displayLabel}
                  </motion.span>
                )}
              </div>
            )}

            {/* Points number */}
            <motion.div
              className="text-5xl font-bold tracking-tight"
              initial={{ scale: 0.5 }}
              animate={{ scale: [0.5, 1.15, 1] }}
              transition={{
                times: [0, 0.6, 1],
                duration: 0.4,
              }}
            >
              {points > 0 ? '+' : ''}
              {points}
              <span className="text-2xl ml-1 opacity-80">pts</span>
            </motion.div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

/**
 * Inline points badge for displaying within content
 */
export function PointsBadge({
  points,
  variant = 'default',
  size = 'default',
  animated = true,
  className,
}: {
  points: number;
  variant?: PointsVariant;
  size?: 'sm' | 'default' | 'lg';
  animated?: boolean;
  className?: string;
}) {
  const styles = variantStyles[variant];

  const sizeClasses = {
    sm: 'px-2 py-1 text-xs rounded-lg',
    default: 'px-3 py-1.5 text-sm rounded-xl',
    lg: 'px-4 py-2 text-base rounded-xl',
  };

  const content = (
    <>
      {styles.icon && <span className="opacity-80">{styles.icon}</span>}
      <span>
        {points > 0 ? '+' : ''}
        {points}
      </span>
    </>
  );

  const baseClasses = cn(
    'inline-flex items-center gap-1.5 font-bold text-white',
    styles.gradient,
    styles.shadow,
    sizeClasses[size],
    className
  );

  if (animated) {
    return (
      <motion.div
        className={baseClasses}
        initial={{ scale: 0.8, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        transition={{ type: 'spring', stiffness: 400, damping: 25 }}
      >
        {content}
      </motion.div>
    );
  }

  return <div className={baseClasses}>{content}</div>;
}

/**
 * Hook for triggering points animations
 */
export function usePointsAnimation() {
  const [animation, setAnimation] = useState<{
    points: number;
    show: boolean;
    variant: PointsVariant;
    label?: string;
  }>({ points: 0, show: false, variant: 'default' });

  const showPoints = (
    points: number,
    variant: PointsVariant = 'default',
    label?: string
  ) => {
    setAnimation({ points, show: true, variant, label });

    // Auto-hide after animation
    setTimeout(() => {
      setAnimation((prev) => ({ ...prev, show: false }));
    }, 1800);
  };

  const showExactScore = (points: number = 3) => showPoints(points, 'exact');
  const showCorrectResult = (points: number = 1) => showPoints(points, 'correct');
  const showIncorrect = () => showPoints(0, 'incorrect');

  return {
    animation,
    showPoints,
    showExactScore,
    showCorrectResult,
    showIncorrect,
    PointsDisplay: () => (
      <PointsAnimation
        points={animation.points}
        show={animation.show}
        variant={animation.variant}
        label={animation.label}
      />
    ),
  };
}
