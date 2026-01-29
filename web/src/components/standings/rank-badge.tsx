import { Crown, Medal, Award } from 'lucide-react';

interface RankBadgeProps {
  rank: number;
}

/**
 * Visual rank indicator with special styling for top 3
 *
 * Demonstrates:
 * - Conditional styling based on data
 * - Icon selection logic
 */
export function RankBadge({ rank }: RankBadgeProps) {
  // Top 3 get special styling
  if (rank === 1) {
    return (
      <div className="flex items-center justify-center w-8 h-8 rounded-full bg-yellow-100 dark:bg-yellow-900/30">
        <Crown className="h-5 w-5 text-yellow-500" />
      </div>
    );
  }

  if (rank === 2) {
    return (
      <div className="flex items-center justify-center w-8 h-8 rounded-full bg-gray-100 dark:bg-gray-800">
        <Medal className="h-5 w-5 text-gray-400" />
      </div>
    );
  }

  if (rank === 3) {
    return (
      <div className="flex items-center justify-center w-8 h-8 rounded-full bg-orange-100 dark:bg-orange-900/30">
        <Award className="h-5 w-5 text-orange-500" />
      </div>
    );
  }

  // Others get number only
  return (
    <div className="flex items-center justify-center w-8 h-8 text-muted-foreground">
      {rank}
    </div>
  );
}
