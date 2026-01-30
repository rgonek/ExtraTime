'use client';

import { useState, useCallback } from 'react';
import { toast } from 'sonner';
import { Minus, Plus, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { usePlaceBet, useDeleteBet } from '@/hooks/use-bets';
import type { MyBetDto } from '@/types';

interface BetFormProps {
  matchId: string;
  leagueId: string;
  existingBet?: MyBetDto;
  homeTeamName: string;
  awayTeamName: string;
  onSuccess?: () => void;
}

/**
 * Form for placing/editing a bet
 *
 * Demonstrates:
 * - Controlled number inputs
 * - useCallback for memoized handlers
 * - Optimistic mutations
 * - Form state management
 */
export function BetForm({
  matchId,
  leagueId,
  existingBet,
  homeTeamName,
  awayTeamName,
  onSuccess,
}: BetFormProps) {
  // Initialize with existing bet or 0-0
  const [homeScore, setHomeScore] = useState(existingBet?.predictedHomeScore ?? 0);
  const [awayScore, setAwayScore] = useState(existingBet?.predictedAwayScore ?? 0);

  const placeMutation = usePlaceBet(leagueId);
  const deleteMutation = useDeleteBet(leagueId);

  // ============================================================
  // MEMOIZED HANDLERS
  // ============================================================
  // useCallback prevents creating new function references
  // on every render. Only recreate when dependencies change.
  // ============================================================

  const incrementHome = useCallback(() => {
    setHomeScore((prev) => Math.min(prev + 1, 20)); // Max 20 goals
  }, []);

  const decrementHome = useCallback(() => {
    setHomeScore((prev) => Math.max(prev - 1, 0)); // Min 0 goals
  }, []);

  const incrementAway = useCallback(() => {
    setAwayScore((prev) => Math.min(prev + 1, 20));
  }, []);

  const decrementAway = useCallback(() => {
    setAwayScore((prev) => Math.max(prev - 1, 0));
  }, []);

  const handleSubmit = async () => {
    try {
      await placeMutation.mutateAsync({
        matchId,
        predictedHomeScore: homeScore,
        predictedAwayScore: awayScore,
      });

      toast.success(existingBet ? 'Bet updated!' : 'Bet placed!');
      onSuccess?.();
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to place bet';
      toast.error(message);
    }
  };

  const handleDelete = async () => {
    if (!existingBet) return;

    if (!confirm('Are you sure you want to delete this bet?')) return;

    try {
      await deleteMutation.mutateAsync(existingBet.betId);
      toast.success('Bet deleted');
      onSuccess?.();
    } catch {
      toast.error('Failed to delete bet');
    }
  };

  const isPending = placeMutation.isPending || deleteMutation.isPending;

  return (
    <div className="space-y-4">
      {/* Score inputs */}
      <div className="flex items-center justify-between gap-4">
        {/* Home team score */}
        <div className="flex-1">
          <div className="text-sm text-muted-foreground mb-1">{homeTeamName}</div>
          <div className="flex items-center justify-center gap-2">
            <ScoreButton onClick={decrementHome} disabled={homeScore === 0 || isPending}>
              <Minus className="h-4 w-4" />
            </ScoreButton>
            <span className="text-3xl font-bold w-12 text-center">{homeScore}</span>
            <ScoreButton onClick={incrementHome} disabled={homeScore >= 20 || isPending}>
              <Plus className="h-4 w-4" />
            </ScoreButton>
          </div>
        </div>

        {/* Separator */}
        <div className="text-2xl font-bold text-muted-foreground">-</div>

        {/* Away team score */}
        <div className="flex-1">
          <div className="text-sm text-muted-foreground mb-1 text-right">
            {awayTeamName}
          </div>
          <div className="flex items-center justify-center gap-2">
            <ScoreButton onClick={decrementAway} disabled={awayScore === 0 || isPending}>
              <Minus className="h-4 w-4" />
            </ScoreButton>
            <span className="text-3xl font-bold w-12 text-center">{awayScore}</span>
            <ScoreButton onClick={incrementAway} disabled={awayScore >= 20 || isPending}>
              <Plus className="h-4 w-4" />
            </ScoreButton>
          </div>
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center justify-between">
        {existingBet && (
          <Button
            variant="ghost"
            size="sm"
            onClick={handleDelete}
            disabled={isPending}
            className="text-destructive hover:text-destructive"
          >
            <Trash2 className="h-4 w-4 mr-1" />
            Delete
          </Button>
        )}

        <div className="flex-1" />

        <Button onClick={handleSubmit} disabled={isPending}>
          {isPending
            ? 'Saving...'
            : existingBet
              ? 'Update Bet'
              : 'Place Bet'}
        </Button>
      </div>
    </div>
  );
}

/**
 * Score increment/decrement button
 */
function ScoreButton({
  children,
  onClick,
  disabled,
}: {
  children: React.ReactNode;
  onClick: () => void;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className="w-10 h-10 rounded-full bg-muted hover:bg-muted/80 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center transition-colors"
    >
      {children}
    </button>
  );
}
