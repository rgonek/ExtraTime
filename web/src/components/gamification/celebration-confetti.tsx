'use client';

import { useEffect, useCallback } from 'react';
import confetti from 'canvas-confetti';

interface CelebrationConfettiProps {
  trigger: boolean;
  intensity?: 'low' | 'medium' | 'high';
}

/**
 * Confetti celebration effect
 *
 * Demonstrates:
 * - Canvas API usage via library
 * - Effect cleanup
 * - Configurable intensity
 *
 * Backend Analogy: This is like a decorator that adds
 * celebration behavior to any component.
 */
export function CelebrationConfetti({
  trigger,
  intensity = 'medium',
}: CelebrationConfettiProps) {
  const celebrate = useCallback(() => {
    const config = {
      low: { particleCount: 50, spread: 60 },
      medium: { particleCount: 100, spread: 90 },
      high: { particleCount: 200, spread: 120 },
    };

    const { particleCount, spread } = config[intensity];

    // Fire confetti from both sides
    confetti({
      particleCount: particleCount / 2,
      spread,
      origin: { x: 0.2, y: 0.6 },
      colors: ['#10b981', '#3b82f6', '#f59e0b', '#ef4444', '#8b5cf6'],
    });

    confetti({
      particleCount: particleCount / 2,
      spread,
      origin: { x: 0.8, y: 0.6 },
      colors: ['#10b981', '#3b82f6', '#f59e0b', '#ef4444', '#8b5cf6'],
    });
  }, [intensity]);

  useEffect(() => {
    if (trigger) {
      celebrate();
    }
  }, [trigger, celebrate]);

  // This component doesn't render anything visible
  return null;
}

/**
 * Hook version for programmatic celebration
 */
export function useCelebration() {
  const celebrate = useCallback(
    (intensity: 'low' | 'medium' | 'high' = 'medium') => {
      const config = {
        low: { particleCount: 50, spread: 60 },
        medium: { particleCount: 100, spread: 90 },
        high: { particleCount: 200, spread: 120 },
      };

      const { particleCount, spread } = config[intensity];

      confetti({
        particleCount: particleCount / 2,
        spread,
        origin: { x: 0.2, y: 0.6 },
      });

      confetti({
        particleCount: particleCount / 2,
        spread,
        origin: { x: 0.8, y: 0.6 },
      });
    },
    []
  );

  return { celebrate };
}
