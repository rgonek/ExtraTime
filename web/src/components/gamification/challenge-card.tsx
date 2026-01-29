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
      <Card
        className={
          isComplete ? 'border-green-500 bg-green-50 dark:bg-green-900/10' : ''
        }
      >
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
          <p className="text-sm text-muted-foreground">
            {challenge.description}
          </p>

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
