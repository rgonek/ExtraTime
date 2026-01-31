'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Card,
  CardContent,
  CardFooter,
} from '@/components/ui/card';
import { useCreateLeague, useUpdateLeague } from '@/hooks/use-leagues';
import type { LeagueDto, CreateLeagueRequest, UpdateLeagueRequest } from '@/types';

interface LeagueFormProps {
  league?: LeagueDto;
}

export function LeagueForm({ league }: LeagueFormProps) {
  const router = useRouter();
  const isEditing = !!league;

  const [name, setName] = useState(league?.name ?? '');
  const [description, setDescription] = useState(league?.description ?? '');
  const [isPublic, setIsPublic] = useState(league?.isPublic ?? false);
  const [maxMembers, setMaxMembers] = useState(league?.maxMembers ?? 20);
  const [scoreExactMatch, setScoreExactMatch] = useState(league?.scoreExactMatch ?? 3);
  const [scoreCorrectResult, setScoreCorrectResult] = useState(league?.scoreCorrectResult ?? 1);
  const [bettingDeadlineMinutes, setBettingDeadlineMinutes] = useState(
    league?.bettingDeadlineMinutes ?? 60
  );

  const [errors, setErrors] = useState<Record<string, string>>({});

  const createMutation = useCreateLeague();
  const updateMutation = useUpdateLeague(league?.id ?? '');

  const mutation = isEditing ? updateMutation : createMutation;

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!name.trim()) {
      newErrors.name = 'Name is required';
    } else if (name.length < 3) {
      newErrors.name = 'Name must be at least 3 characters';
    } else if (name.length > 100) {
      newErrors.name = 'Name must be less than 100 characters';
    }

    if (maxMembers < 2 || maxMembers > 100) {
      newErrors.maxMembers = 'Must be between 2 and 100';
    }

    if (scoreExactMatch < 1) {
      newErrors.scoreExactMatch = 'Must be at least 1';
    }

    if (scoreCorrectResult < 0) {
      newErrors.scoreCorrectResult = 'Cannot be negative';
    }

    if (bettingDeadlineMinutes < 0) {
      newErrors.bettingDeadlineMinutes = 'Cannot be negative';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validate()) return;

    const data: CreateLeagueRequest | UpdateLeagueRequest = {
      name: name.trim(),
      description: description.trim() || undefined,
      isPublic,
      maxMembers,
      scoreExactMatch,
      scoreCorrectResult,
      bettingDeadlineMinutes,
    };

    try {
      await mutation.mutateAsync(data);
      toast.success(isEditing ? 'League updated' : 'League created');
      router.push('/leagues');
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Something went wrong';
      toast.error(message);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <Card>
        <CardContent className="pt-6 space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name">League Name *</Label>
            <Input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Premier League Predictors"
              aria-invalid={!!errors.name}
            />
            {errors.name && (
              <p className="text-sm text-destructive">{errors.name}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Input
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="A friendly competition among friends"
            />
          </div>

          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="isPublic"
              checked={isPublic}
              onChange={(e) => setIsPublic(e.target.checked)}
              className="h-4 w-4 rounded border-gray-300"
            />
            <Label htmlFor="isPublic">Make league public (visible to everyone)</Label>
          </div>

          <div className="grid gap-4 sm:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="maxMembers">Max Members</Label>
              <Input
                id="maxMembers"
                type="number"
                value={maxMembers}
                onChange={(e) => setMaxMembers(Number(e.target.value))}
                min={2}
                max={100}
                aria-invalid={!!errors.maxMembers}
              />
              {errors.maxMembers && (
                <p className="text-sm text-destructive">{errors.maxMembers}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="scoreExactMatch">Points for Exact Match</Label>
              <Input
                id="scoreExactMatch"
                type="number"
                value={scoreExactMatch}
                onChange={(e) => setScoreExactMatch(Number(e.target.value))}
                min={1}
                aria-invalid={!!errors.scoreExactMatch}
              />
              {errors.scoreExactMatch && (
                <p className="text-sm text-destructive">{errors.scoreExactMatch}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="scoreCorrectResult">Points for Correct Result</Label>
              <Input
                id="scoreCorrectResult"
                type="number"
                value={scoreCorrectResult}
                onChange={(e) => setScoreCorrectResult(Number(e.target.value))}
                min={0}
                aria-invalid={!!errors.scoreCorrectResult}
              />
              {errors.scoreCorrectResult && (
                <p className="text-sm text-destructive">{errors.scoreCorrectResult}</p>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="bettingDeadlineMinutes">
              Betting Deadline (minutes before match)
            </Label>
            <Input
              id="bettingDeadlineMinutes"
              type="number"
              value={bettingDeadlineMinutes}
              onChange={(e) => setBettingDeadlineMinutes(Number(e.target.value))}
              min={0}
              aria-invalid={!!errors.bettingDeadlineMinutes}
            />
            <p className="text-sm text-muted-foreground">
              Users must place bets at least this many minutes before kick-off
            </p>
          </div>
        </CardContent>

        <CardFooter className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => router.back()}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending
              ? isEditing
                ? 'Saving...'
                : 'Creating...'
              : isEditing
                ? 'Save Changes'
                : 'Create League'}
          </Button>
        </CardFooter>
      </Card>
    </form>
  );
}
