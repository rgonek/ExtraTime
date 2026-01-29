import { toast } from 'sonner';

/**
 * Enhanced toast helpers with consistent styling
 *
 * Backend Analogy: These are like logging helpers that
 * format messages consistently across the application.
 */

export const showToast = {
  success: (message: string, description?: string) => {
    toast.success(message, { description });
  },

  error: (message: string, description?: string) => {
    toast.error(message, { description });
  },

  warning: (message: string, description?: string) => {
    toast.warning(message, { description });
  },

  info: (message: string, description?: string) => {
    toast.info(message, { description });
  },

  // For async operations with loading state
  promise: <T>(
    promise: Promise<T>,
    messages: {
      loading: string;
      success: string | ((data: T) => string);
      error: string | ((error: Error) => string);
    }
  ) => {
    return toast.promise(promise, messages);
  },

  // Bet-specific toasts
  betPlaced: (homeScore: number, awayScore: number) => {
    toast.success('Bet placed!', {
      description: `Your prediction: ${homeScore} - ${awayScore}`,
    });
  },

  betUpdated: () => {
    toast.success('Bet updated!');
  },

  betDeleted: () => {
    toast.success('Bet deleted');
  },

  // Achievement unlocked
  achievementUnlocked: (name: string, icon: string) => {
    toast.success('Achievement Unlocked!', {
      description: `${icon} ${name}`,
      duration: 5000,
    });
  },

  // Points earned
  pointsEarned: (points: number, isExact: boolean) => {
    toast.success(`+${points} points!`, {
      description: isExact ? 'Exact match!' : 'Correct result!',
      duration: 4000,
    });
  },
};
