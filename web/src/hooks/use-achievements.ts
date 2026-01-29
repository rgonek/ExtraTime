'use client';

import { useMemo } from 'react';
import type { UserStatsDto } from '@/types';

/**
 * Achievement definitions
 *
 * Backend Analogy: These are like domain rules/specifications
 * that determine if a user qualifies for an achievement.
 */
export interface Achievement {
  id: string;
  name: string;
  description: string;
  icon: string; // Emoji for simplicity
  condition: (stats: UserStatsDto) => boolean;
  progress?: (stats: UserStatsDto) => { current: number; target: number };
}

/**
 * All possible achievements
 *
 * Think of this as a specification pattern:
 * Each achievement has a condition (specification) that
 * checks if the user meets the criteria.
 */
export const ACHIEVEMENTS: Achievement[] = [
  // First steps
  {
    id: 'first-bet',
    name: 'First Steps',
    description: 'Place your first bet',
    icon: '🎯',
    condition: (stats) => stats.betsPlaced >= 1,
  },

  // Betting milestones
  {
    id: 'ten-bets',
    name: 'Getting Serious',
    description: 'Place 10 bets',
    icon: '📊',
    condition: (stats) => stats.betsPlaced >= 10,
    progress: (stats) => ({ current: Math.min(stats.betsPlaced, 10), target: 10 }),
  },
  {
    id: 'fifty-bets',
    name: 'Dedicated Predictor',
    description: 'Place 50 bets',
    icon: '🏆',
    condition: (stats) => stats.betsPlaced >= 50,
    progress: (stats) => ({ current: Math.min(stats.betsPlaced, 50), target: 50 }),
  },
  {
    id: 'hundred-bets',
    name: 'Centurion',
    description: 'Place 100 bets',
    icon: '💯',
    condition: (stats) => stats.betsPlaced >= 100,
    progress: (stats) => ({
      current: Math.min(stats.betsPlaced, 100),
      target: 100,
    }),
  },

  // Exact match achievements
  {
    id: 'first-exact',
    name: 'Perfect Prediction',
    description: 'Get your first exact score match',
    icon: '✨',
    condition: (stats) => stats.exactMatches >= 1,
  },
  {
    id: 'five-exact',
    name: 'Sharp Shooter',
    description: 'Get 5 exact score matches',
    icon: '🎯',
    condition: (stats) => stats.exactMatches >= 5,
    progress: (stats) => ({
      current: Math.min(stats.exactMatches, 5),
      target: 5,
    }),
  },
  {
    id: 'twenty-exact',
    name: 'Oracle',
    description: 'Get 20 exact score matches',
    icon: '🔮',
    condition: (stats) => stats.exactMatches >= 20,
    progress: (stats) => ({
      current: Math.min(stats.exactMatches, 20),
      target: 20,
    }),
  },

  // Streak achievements
  {
    id: 'streak-3',
    name: 'On Fire',
    description: 'Get a 3-match winning streak',
    icon: '🔥',
    condition: (stats) => stats.bestStreak >= 3,
    progress: (stats) => ({ current: Math.min(stats.bestStreak, 3), target: 3 }),
  },
  {
    id: 'streak-5',
    name: 'Unstoppable',
    description: 'Get a 5-match winning streak',
    icon: '⚡',
    condition: (stats) => stats.bestStreak >= 5,
    progress: (stats) => ({ current: Math.min(stats.bestStreak, 5), target: 5 }),
  },
  {
    id: 'streak-10',
    name: 'Legendary',
    description: 'Get a 10-match winning streak',
    icon: '👑',
    condition: (stats) => stats.bestStreak >= 10,
    progress: (stats) => ({
      current: Math.min(stats.bestStreak, 10),
      target: 10,
    }),
  },

  // Accuracy achievements
  {
    id: 'accuracy-50',
    name: 'Consistent',
    description: 'Achieve 50% accuracy (min 10 bets)',
    icon: '📈',
    condition: (stats) =>
      stats.betsPlaced >= 10 && stats.accuracyPercentage >= 50,
  },
  {
    id: 'accuracy-75',
    name: 'Expert',
    description: 'Achieve 75% accuracy (min 10 bets)',
    icon: '🎖️',
    condition: (stats) =>
      stats.betsPlaced >= 10 && stats.accuracyPercentage >= 75,
  },

  // Points milestones
  {
    id: 'points-50',
    name: 'Rising Star',
    description: 'Earn 50 points',
    icon: '⭐',
    condition: (stats) => stats.totalPoints >= 50,
    progress: (stats) => ({
      current: Math.min(stats.totalPoints, 50),
      target: 50,
    }),
  },
  {
    id: 'points-100',
    name: 'Point Master',
    description: 'Earn 100 points',
    icon: '🌟',
    condition: (stats) => stats.totalPoints >= 100,
    progress: (stats) => ({
      current: Math.min(stats.totalPoints, 100),
      target: 100,
    }),
  },
  {
    id: 'points-500',
    name: 'Elite Predictor',
    description: 'Earn 500 points',
    icon: '💫',
    condition: (stats) => stats.totalPoints >= 500,
    progress: (stats) => ({
      current: Math.min(stats.totalPoints, 500),
      target: 500,
    }),
  },
];

/**
 * Hook to calculate which achievements a user has unlocked
 *
 * Backend Analogy: This is like a service that evaluates
 * specifications/rules against an entity.
 */
export function useAchievements(stats: UserStatsDto | undefined) {
  return useMemo(() => {
    if (!stats) {
      return {
        unlocked: [],
        locked: ACHIEVEMENTS,
        nextUp: ACHIEVEMENTS.slice(0, 3), // First 3 as "next up"
        totalUnlocked: 0,
        totalPossible: ACHIEVEMENTS.length,
      };
    }

    const unlocked: Achievement[] = [];
    const locked: Achievement[] = [];

    ACHIEVEMENTS.forEach((achievement) => {
      if (achievement.condition(stats)) {
        unlocked.push(achievement);
      } else {
        locked.push(achievement);
      }
    });

    // Next up: first 3 locked achievements with progress
    const nextUp = locked.filter((a) => a.progress).slice(0, 3);

    return {
      unlocked,
      locked,
      nextUp,
      totalUnlocked: unlocked.length,
      totalPossible: ACHIEVEMENTS.length,
    };
  }, [stats]);
}

/**
 * Get user level based on total points
 *
 * Simple leveling system:
 * Level 1: 0-24 points
 * Level 2: 25-49 points
 * etc.
 */
export function getUserLevel(totalPoints: number): {
  level: number;
  title: string;
  pointsForNext: number;
  progress: number;
} {
  const levels = [
    { threshold: 0, title: 'Rookie' },
    { threshold: 25, title: 'Amateur' },
    { threshold: 50, title: 'Semi-Pro' },
    { threshold: 100, title: 'Professional' },
    { threshold: 200, title: 'Expert' },
    { threshold: 350, title: 'Master' },
    { threshold: 500, title: 'Grandmaster' },
    { threshold: 750, title: 'Legend' },
    { threshold: 1000, title: 'Immortal' },
  ];

  let currentLevel = levels[0];
  let nextLevel = levels[1];

  for (let i = 0; i < levels.length; i++) {
    if (totalPoints >= levels[i].threshold) {
      currentLevel = levels[i];
      nextLevel = levels[i + 1] || levels[i];
    } else {
      break;
    }
  }

  const levelIndex = levels.indexOf(currentLevel);
  const pointsInLevel = totalPoints - currentLevel.threshold;
  const pointsForLevel = nextLevel.threshold - currentLevel.threshold;
  const progress =
    pointsForLevel > 0 ? (pointsInLevel / pointsForLevel) * 100 : 100;

  return {
    level: levelIndex + 1,
    title: currentLevel.title,
    pointsForNext: nextLevel.threshold,
    progress: Math.min(progress, 100),
  };
}
