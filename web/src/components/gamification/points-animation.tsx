'use client';

import { motion, AnimatePresence } from 'framer-motion';
import { useState } from 'react';

interface PointsAnimationProps {
  points: number;
  show: boolean;
  onComplete?: () => void;
}

/**
 * Floating points animation (+3, +1, etc.)
 *
 * Demonstrates:
 * - Exit animations with AnimatePresence
 * - Callback on animation complete
 * - Positioning for overlay effects
 */
export function PointsAnimation({
  points,
  show,
  onComplete,
}: PointsAnimationProps) {
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
            className={`text-6xl font-bold ${
              points > 0 ? 'text-green-500' : 'text-red-500'
            }`}
            initial={{ scale: 0, y: 50 }}
            animate={{ scale: 1, y: 0 }}
            exit={{ scale: 0.5, y: -100, opacity: 0 }}
            transition={{
              type: 'spring',
              stiffness: 500,
              damping: 30,
            }}
          >
            {points > 0 ? '+' : ''}
            {points}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

/**
 * Hook for triggering points animations
 */
export function usePointsAnimation() {
  const [animation, setAnimation] = useState<{
    points: number;
    show: boolean;
  }>({ points: 0, show: false });

  const showPoints = (points: number) => {
    setAnimation({ points, show: true });

    // Auto-hide after animation
    setTimeout(() => {
      setAnimation((prev) => ({ ...prev, show: false }));
    }, 1500);
  };

  return {
    animation,
    showPoints,
    PointsDisplay: () => (
      <PointsAnimation points={animation.points} show={animation.show} />
    ),
  };
}
