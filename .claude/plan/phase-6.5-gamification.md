# Phase 6.5: Gamification System

> **Goal:** Add achievements, celebrations, and engagement features
> **Backend Analogy:** Domain events, decorators, and cross-cutting concerns that add delight
> **Estimated Time:** 6-8 hours
> **Prerequisites:** Phase 6.4 complete (leaderboard showing stats)

---

## What You'll Learn

| Frontend Concept | Backend Analogy | Example |
|------------------|-----------------|---------|
| Framer Motion | Animation middleware | `<motion.div animate={...}>` |
| CSS transitions | State visualizations | `transition-all duration-300` |
| Custom hooks | Reusable services | `useAchievements()` |
| Local storage | User preferences | Theme, dismissed toasts |
| Canvas API | Drawing/graphics | Confetti particles |

---

## Understanding Animations (For Backend Developers)

### Mental Model: State Machines for Visual Properties

```typescript
// Backend: State machine for order status
enum OrderState { Pending, Processing, Shipped, Delivered }

// Animation: State machine for visual properties
interface AnimationState {
  opacity: number;    // 0 to 1
  scale: number;      // 0.5 to 1
  y: number;          // offset in pixels
}

// Framer Motion defines states and transitions between them
<motion.div
  initial={{ opacity: 0, y: 20 }}    // Starting state
  animate={{ opacity: 1, y: 0 }}     // Target state
  exit={{ opacity: 0, y: -20 }}      // Exit state
  transition={{ duration: 0.3 }}     // How to animate between states
/>
```

### When to Animate

| Animation Type | Purpose | Example |
|----------------|---------|---------|
| Enter | Draw attention to new content | New bet appears |
| Exit | Smooth removal | Delete confirmation |
| Hover | Interactive feedback | Button state |
| State change | Show something happened | Score update |
| Celebration | Reward & delight | Exact match |

### Decision Analysis: Animation Library

| Library | Why Use | Why Not |
|---------|---------|---------|
| **Framer Motion (chosen)** | Declarative, React-native, powerful | Larger bundle |
| CSS transitions | Simple, no dependencies | Limited to CSS properties |
| React Spring | Physics-based, natural | Steeper learning curve |
| GSAP | Most powerful | Overkill, different paradigm |

**We chose Framer Motion because:**
1. Already installed in the project
2. Declarative API matches React patterns
3. Great documentation and examples
4. Handles enter/exit animations well

---

## Step 1: Create Achievements Hook

### File: `web/src/hooks/use-achievements.ts`

```typescript
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
  icon: string;       // Emoji for simplicity
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
    progress: (stats) => ({ current: Math.min(stats.betsPlaced, 100), target: 100 }),
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
    progress: (stats) => ({ current: Math.min(stats.exactMatches, 5), target: 5 }),
  },
  {
    id: 'twenty-exact',
    name: 'Oracle',
    description: 'Get 20 exact score matches',
    icon: '🔮',
    condition: (stats) => stats.exactMatches >= 20,
    progress: (stats) => ({ current: Math.min(stats.exactMatches, 20), target: 20 }),
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
    progress: (stats) => ({ current: Math.min(stats.bestStreak, 10), target: 10 }),
  },

  // Accuracy achievements
  {
    id: 'accuracy-50',
    name: 'Consistent',
    description: 'Achieve 50% accuracy (min 10 bets)',
    icon: '📈',
    condition: (stats) => stats.betsPlaced >= 10 && stats.accuracyPercentage >= 50,
  },
  {
    id: 'accuracy-75',
    name: 'Expert',
    description: 'Achieve 75% accuracy (min 10 bets)',
    icon: '🎖️',
    condition: (stats) => stats.betsPlaced >= 10 && stats.accuracyPercentage >= 75,
  },

  // Points milestones
  {
    id: 'points-50',
    name: 'Rising Star',
    description: 'Earn 50 points',
    icon: '⭐',
    condition: (stats) => stats.totalPoints >= 50,
    progress: (stats) => ({ current: Math.min(stats.totalPoints, 50), target: 50 }),
  },
  {
    id: 'points-100',
    name: 'Point Master',
    description: 'Earn 100 points',
    icon: '🌟',
    condition: (stats) => stats.totalPoints >= 100,
    progress: (stats) => ({ current: Math.min(stats.totalPoints, 100), target: 100 }),
  },
  {
    id: 'points-500',
    name: 'Elite Predictor',
    description: 'Earn 500 points',
    icon: '💫',
    condition: (stats) => stats.totalPoints >= 500,
    progress: (stats) => ({ current: Math.min(stats.totalPoints, 500), target: 500 }),
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
    const nextUp = locked
      .filter((a) => a.progress)
      .slice(0, 3);

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
  const progress = pointsForLevel > 0 ? (pointsInLevel / pointsForLevel) * 100 : 100;

  return {
    level: levelIndex + 1,
    title: currentLevel.title,
    pointsForNext: nextLevel.threshold,
    progress: Math.min(progress, 100),
  };
}
```

---

## Step 2: Create Achievement Badge Component

### File: `web/src/components/gamification/achievement-badge.tsx`

```typescript
'use client';

import { motion } from 'framer-motion';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { Progress } from '@/components/ui/progress';
import type { Achievement } from '@/hooks/use-achievements';
import type { UserStatsDto } from '@/types';

interface AchievementBadgeProps {
  achievement: Achievement;
  unlocked: boolean;
  stats?: UserStatsDto;
  size?: 'sm' | 'md' | 'lg';
}

/**
 * Single achievement badge with animation
 *
 * Demonstrates:
 * - Framer Motion hover animations
 * - Conditional styling (unlocked vs locked)
 * - Tooltips for additional info
 */
export function AchievementBadge({
  achievement,
  unlocked,
  stats,
  size = 'md',
}: AchievementBadgeProps) {
  const progress = achievement.progress && stats
    ? achievement.progress(stats)
    : null;

  const sizeClasses = {
    sm: 'w-10 h-10 text-lg',
    md: 'w-14 h-14 text-2xl',
    lg: 'w-20 h-20 text-4xl',
  };

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <motion.div
            className={`
              ${sizeClasses[size]}
              rounded-full flex items-center justify-center
              ${unlocked
                ? 'bg-gradient-to-br from-yellow-100 to-yellow-200 dark:from-yellow-900/50 dark:to-yellow-800/50 shadow-lg'
                : 'bg-muted opacity-50 grayscale'}
              cursor-pointer select-none
            `}
            whileHover={unlocked ? { scale: 1.1, rotate: 5 } : { scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            initial={unlocked ? { scale: 0 } : { scale: 1 }}
            animate={{ scale: 1 }}
            transition={{ type: 'spring', stiffness: 500, damping: 30 }}
          >
            {achievement.icon}
          </motion.div>
        </TooltipTrigger>
        <TooltipContent side="top" className="max-w-[200px]">
          <div className="space-y-1">
            <p className="font-semibold">{achievement.name}</p>
            <p className="text-xs text-muted-foreground">
              {achievement.description}
            </p>
            {!unlocked && progress && (
              <div className="pt-1">
                <Progress
                  value={(progress.current / progress.target) * 100}
                  className="h-1"
                />
                <p className="text-xs text-muted-foreground mt-1">
                  {progress.current} / {progress.target}
                </p>
              </div>
            )}
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}
```

---

## Step 3: Create Achievement List Component

### File: `web/src/components/gamification/achievement-list.tsx`

```typescript
'use client';

import { motion } from 'framer-motion';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { AchievementBadge } from './achievement-badge';
import { useAchievements, ACHIEVEMENTS } from '@/hooks/use-achievements';
import { useUserStats } from '@/hooks/use-bets';
import { useAuthStore } from '@/stores/auth-store';
import { CardSkeleton } from '@/components/shared/loading-skeleton';

interface AchievementListProps {
  leagueId: string;
}

/**
 * Full list of achievements with progress tracking
 *
 * Demonstrates:
 * - Staggered animation for list items
 * - Progress visualization
 * - Grouped display (unlocked vs locked)
 */
export function AchievementList({ leagueId }: AchievementListProps) {
  const currentUser = useAuthStore((state) => state.user);
  const { data: stats, isLoading } = useUserStats(leagueId, currentUser?.id ?? '');
  const { unlocked, nextUp, totalUnlocked, totalPossible } = useAchievements(stats);

  if (isLoading) {
    return <CardSkeleton />;
  }

  return (
    <div className="space-y-6">
      {/* Summary */}
      <Card>
        <CardHeader>
          <CardTitle>Achievements</CardTitle>
          <CardDescription>
            {totalUnlocked} of {totalPossible} unlocked
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Progress
            value={(totalUnlocked / totalPossible) * 100}
            className="h-2"
          />
        </CardContent>
      </Card>

      {/* Unlocked achievements */}
      {unlocked.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Unlocked</CardTitle>
          </CardHeader>
          <CardContent>
            <motion.div
              className="flex flex-wrap gap-4"
              initial="hidden"
              animate="visible"
              variants={{
                visible: {
                  transition: { staggerChildren: 0.1 },
                },
              }}
            >
              {unlocked.map((achievement) => (
                <motion.div
                  key={achievement.id}
                  variants={{
                    hidden: { opacity: 0, y: 20 },
                    visible: { opacity: 1, y: 0 },
                  }}
                >
                  <AchievementBadge
                    achievement={achievement}
                    unlocked={true}
                    stats={stats}
                  />
                </motion.div>
              ))}
            </motion.div>
          </CardContent>
        </Card>
      )}

      {/* Next up (with progress) */}
      {nextUp.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Next Up</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {nextUp.map((achievement) => {
              const progress = achievement.progress?.(stats!);
              return (
                <div key={achievement.id} className="flex items-center gap-4">
                  <AchievementBadge
                    achievement={achievement}
                    unlocked={false}
                    stats={stats}
                    size="sm"
                  />
                  <div className="flex-1">
                    <div className="flex items-center justify-between mb-1">
                      <span className="font-medium">{achievement.name}</span>
                      {progress && (
                        <span className="text-sm text-muted-foreground">
                          {progress.current} / {progress.target}
                        </span>
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground mb-2">
                      {achievement.description}
                    </p>
                    {progress && (
                      <Progress
                        value={(progress.current / progress.target) * 100}
                        className="h-1"
                      />
                    )}
                  </div>
                </div>
              );
            })}
          </CardContent>
        </Card>
      )}

      {/* All locked achievements */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">All Achievements</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-3">
            {ACHIEVEMENTS.map((achievement) => {
              const isUnlocked = unlocked.some((u) => u.id === achievement.id);
              return (
                <AchievementBadge
                  key={achievement.id}
                  achievement={achievement}
                  unlocked={isUnlocked}
                  stats={stats}
                  size="sm"
                />
              );
            })}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
```

---

## Step 4: Create Level Indicator Component

### File: `web/src/components/gamification/level-indicator.tsx`

```typescript
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
```

---

## Step 5: Create Streak Counter Component

### File: `web/src/components/gamification/streak-counter.tsx`

```typescript
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
  const intensity = Math.min(currentStreak / 10, 1);
  const flameColor = currentStreak >= 5
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
              currentStreak >= 3 ? 'drop-shadow-[0_0_8px_rgba(251,146,60,0.5)]' : ''
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
```

---

## Step 6: Create Celebration Confetti Component

### File: `web/src/components/gamification/celebration-confetti.tsx`

```typescript
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
  const celebrate = useCallback((intensity: 'low' | 'medium' | 'high' = 'medium') => {
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
  }, []);

  return { celebrate };
}
```

---

## Step 7: Create Points Animation Component

### File: `web/src/components/gamification/points-animation.tsx`

```typescript
'use client';

import { motion, AnimatePresence } from 'framer-motion';
import { useState, useEffect } from 'react';

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
            {points > 0 ? '+' : ''}{points}
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
      <PointsAnimation
        points={animation.points}
        show={animation.show}
      />
    ),
  };
}
```

---

## Step 8: Create Challenge Card Component

### File: `web/src/components/gamification/challenge-card.tsx`

```typescript
'use client';

import { motion } from 'framer-motion';
import { Target, Clock, Trophy } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { Badge } from '@/components/ui/badge';

interface Challenge {
  id: string;
  title: string;
  description: string;
  target: number;
  current: number;
  reward: string;
  expiresAt?: string;
}

interface ChallengeCardProps {
  challenge: Challenge;
}

/**
 * Weekly/daily challenge card with progress
 *
 * Note: Challenges are UI-only for now (no backend).
 * Could be extended with backend support later.
 */
export function ChallengeCard({ challenge }: ChallengeCardProps) {
  const progress = (challenge.current / challenge.target) * 100;
  const isComplete = challenge.current >= challenge.target;

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      whileHover={{ scale: 1.02 }}
    >
      <Card className={isComplete ? 'border-green-500 bg-green-50 dark:bg-green-900/10' : ''}>
        <CardHeader className="pb-2">
          <div className="flex items-center justify-between">
            <CardTitle className="text-base flex items-center gap-2">
              <Target className="h-4 w-4 text-primary" />
              {challenge.title}
            </CardTitle>
            {isComplete ? (
              <Badge className="bg-green-500">
                <Trophy className="h-3 w-3 mr-1" />
                Complete!
              </Badge>
            ) : challenge.expiresAt ? (
              <Badge variant="outline">
                <Clock className="h-3 w-3 mr-1" />
                {formatTimeLeft(challenge.expiresAt)}
              </Badge>
            ) : null}
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <p className="text-sm text-muted-foreground">{challenge.description}</p>

          <div>
            <div className="flex justify-between text-sm mb-1">
              <span>Progress</span>
              <span className="font-medium">
                {challenge.current} / {challenge.target}
              </span>
            </div>
            <Progress value={progress} className="h-2" />
          </div>

          <div className="flex items-center gap-2 text-sm">
            <Trophy className="h-4 w-4 text-yellow-500" />
            <span>Reward: {challenge.reward}</span>
          </div>
        </CardContent>
      </Card>
    </motion.div>
  );
}

function formatTimeLeft(expiresAt: string): string {
  const now = new Date();
  const expiry = new Date(expiresAt);
  const diff = expiry.getTime() - now.getTime();

  if (diff <= 0) return 'Expired';

  const hours = Math.floor(diff / (1000 * 60 * 60));
  const days = Math.floor(hours / 24);

  if (days > 0) return `${days}d left`;
  if (hours > 0) return `${hours}h left`;
  return 'Soon';
}

/**
 * Sample challenges for demonstration
 */
export const SAMPLE_CHALLENGES: Challenge[] = [
  {
    id: 'weekly-5-bets',
    title: 'Weekly Warrior',
    description: 'Place 5 bets this week',
    target: 5,
    current: 0,
    reward: '+10 bonus points',
    expiresAt: getNextSunday().toISOString(),
  },
  {
    id: 'exact-this-week',
    title: 'Sharp Eye',
    description: 'Get an exact match prediction this week',
    target: 1,
    current: 0,
    reward: 'Sharp Eye badge',
    expiresAt: getNextSunday().toISOString(),
  },
  {
    id: 'streak-3',
    title: 'Hot Streak',
    description: 'Build a 3-match winning streak',
    target: 3,
    current: 0,
    reward: 'Streak bonus x2',
  },
];

function getNextSunday(): Date {
  const now = new Date();
  const daysUntilSunday = (7 - now.getDay()) % 7 || 7;
  const nextSunday = new Date(now);
  nextSunday.setDate(now.getDate() + daysUntilSunday);
  nextSunday.setHours(23, 59, 59, 999);
  return nextSunday;
}
```

---

## Step 9: Install canvas-confetti

```bash
cd web
npm install canvas-confetti
npm install -D @types/canvas-confetti
```

---

## Step 10: Integrate Gamification into Existing Components

### Update `UserStatsCard` to include level and achievements

Add to `web/src/components/standings/user-stats-card.tsx`:

```typescript
// Add imports
import { LevelIndicator } from '@/components/gamification/level-indicator';
import { StreakCounter } from '@/components/gamification/streak-counter';

// Add to CardHeader, after username:
<LevelIndicator totalPoints={stats.totalPoints} />

// Replace streak section with:
<StreakCounter
  currentStreak={stats.currentStreak}
  bestStreak={stats.bestStreak}
/>
```

### Add celebration to bet result

In match-card.tsx, add celebration when showing exact match result:

```typescript
import { CelebrationConfetti, useCelebration } from '@/components/gamification/celebration-confetti';

// In component:
const { celebrate } = useCelebration();

// When showing exact match result:
useEffect(() => {
  if (bet?.result?.isExactMatch) {
    celebrate('high');
  }
}, [bet?.result?.isExactMatch]);
```

---

## Verification Checklist

After completing Phase 6.5:

- [ ] Achievement badges display with correct styling
- [ ] Locked achievements show progress
- [ ] Unlocked achievements animate on mount
- [ ] Hover effects work on badges
- [ ] Level indicator shows current level and progress
- [ ] Streak counter animates with fire icon
- [ ] Confetti fires on exact match results
- [ ] Points animation floats up
- [ ] Challenge cards show progress
- [ ] All animations are smooth (60fps)
- [ ] `npm run build` passes

---

## Key Learnings from This Phase

1. **Framer Motion basics** - initial, animate, exit, transition
2. **AnimatePresence** - Required for exit animations
3. **Staggered animations** - Use variants with staggerChildren
4. **Custom hooks** - Encapsulate animation logic for reuse
5. **Canvas effects** - Use libraries like canvas-confetti
6. **Performance** - Avoid animating expensive properties (use transform)

---

## Animation Performance Tips

1. **Only animate transform and opacity** - These are GPU-accelerated
2. **Use will-change sparingly** - Only for complex animations
3. **Avoid layout thrashing** - Don't read and write DOM in quick succession
4. **Use requestAnimationFrame** - For custom animations
5. **Test on low-end devices** - Animations can be jarring if slow

---

## Next Step

Proceed to **Phase 6.6: UX Polish** (`phase-6.6-polish.md`)
