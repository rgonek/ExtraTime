'use client';

import { useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { useJoinLeagueByCode } from '@/hooks/use-leagues';

export function JoinLeagueForm() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [code, setCode] = useState(searchParams.get('code') ?? '');
  const [error, setError] = useState('');

  const joinMutation = useJoinLeagueByCode();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!code.trim()) {
      setError('Please enter an invite code');
      return;
    }

    try {
      const league = await joinMutation.mutateAsync({ inviteCode: code.trim() });
      toast.success('Successfully joined the league!');
      router.push(`/leagues/${league.id}`);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Invalid invite code';
      setError(message);
      toast.error(message);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="w-full">
      <Card className="max-w-md mx-auto">
        <CardHeader>
          <CardTitle>Join a League</CardTitle>
          <CardDescription>
            Enter the invite code you received to join a league
          </CardDescription>
        </CardHeader>

        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="code">Invite Code</Label>
            <Input
              id="code"
              value={code}
              onChange={(e) => setCode(e.target.value.toUpperCase())}
              placeholder="ABC123"
              className="font-mono text-lg tracking-wider"
              aria-invalid={!!error}
            />
            {error && (
              <p className="text-sm text-destructive">{error}</p>
            )}
          </div>
        </CardContent>

        <CardFooter className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => router.push('/leagues')}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={joinMutation.isPending}>
            {joinMutation.isPending ? 'Joining...' : 'Join League'}
          </Button>
        </CardFooter>
      </Card>
    </form>
  );
}
