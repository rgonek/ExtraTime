'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import {
  Trophy,
  Users,
  Settings,
  Copy,
  LogOut,
  Trash2,
  Target,
  Clock,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
} from '@/components/ui/card';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  useLeague,
  useDeleteLeague,
  useLeaveLeague,
  useRegenerateInviteCode,
} from '@/hooks/use-leagues';
import { useAuthStore } from '@/stores/auth-store';
import { MemberList } from './member-list';
import { InviteShare } from './invite-share';
import { CardSkeleton } from '@/components/shared/loading-skeleton';
import { ErrorMessage } from '@/components/shared/error-message';

interface LeagueDetailProps {
  leagueId: string;
}

function StatsCard({
  icon: Icon,
  label,
  value,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: string;
}) {
  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center gap-2">
          <Icon className="h-4 w-4 text-muted-foreground" />
          <span className="text-sm text-muted-foreground">{label}</span>
        </div>
        <p className="text-2xl font-bold mt-1">{value}</p>
      </CardContent>
    </Card>
  );
}

export function LeagueDetail({ leagueId }: LeagueDetailProps) {
  const router = useRouter();
  const currentUser = useAuthStore((state) => state.user);

  const { data: league, isLoading, isError, error, refetch } = useLeague(leagueId);

  const deleteMutation = useDeleteLeague();
  const leaveMutation = useLeaveLeague();
  const regenerateMutation = useRegenerateInviteCode(leagueId);

  const [showInviteShare, setShowInviteShare] = useState(false);

  const isOwner = league?.ownerId === currentUser?.id;
  const isMember = league?.members.some((m) => m.userId === currentUser?.id);

  const handleRegenerateCode = async () => {
    try {
      await regenerateMutation.mutateAsync(undefined);
      toast.success('New invite code generated');
    } catch {
      toast.error('Failed to generate new code');
    }
  };

  const handleLeaveLeague = async () => {
    if (!confirm('Are you sure you want to leave this league?')) return;

    try {
      await leaveMutation.mutateAsync(leagueId);
      toast.success('You have left the league');
      router.push('/leagues');
    } catch {
      toast.error('Failed to leave league');
    }
  };

  const handleDeleteLeague = async () => {
    if (!confirm('Are you sure? This will permanently delete the league and all bets.')) {
      return;
    }

    try {
      await deleteMutation.mutateAsync(leagueId);
      toast.success('League deleted');
      router.push('/leagues');
    } catch {
      toast.error('Failed to delete league');
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-4">
        <CardSkeleton />
        <CardSkeleton />
      </div>
    );
  }

  if (isError || !league) {
    return (
      <ErrorMessage
        title="Failed to load league"
        message={error?.message ?? 'League not found'}
        onRetry={() => refetch()}
      />
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{league.name}</h1>
          {league.description && (
            <p className="text-muted-foreground mt-1">{league.description}</p>
          )}
        </div>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="icon">
              <Settings className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => setShowInviteShare(true)}>
              <Copy className="h-4 w-4 mr-2" />
              Share Invite
            </DropdownMenuItem>

            {isOwner && (
              <>
                <DropdownMenuItem onClick={() => router.push(`/leagues/${leagueId}/edit`)}>
                  <Settings className="h-4 w-4 mr-2" />
                  Edit Settings
                </DropdownMenuItem>
                <DropdownMenuItem onClick={handleRegenerateCode}>
                  <Copy className="h-4 w-4 mr-2" />
                  Regenerate Invite Code
                </DropdownMenuItem>
              </>
            )}

            <DropdownMenuSeparator />

            {!isOwner && isMember && (
              <DropdownMenuItem
                className="text-destructive"
                onClick={handleLeaveLeague}
              >
                <LogOut className="h-4 w-4 mr-2" />
                Leave League
              </DropdownMenuItem>
            )}

            {isOwner && (
              <DropdownMenuItem
                className="text-destructive"
                onClick={handleDeleteLeague}
              >
                <Trash2 className="h-4 w-4 mr-2" />
                Delete League
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <div className="grid gap-4 md:grid-cols-4">
        <StatsCard
          icon={Users}
          label="Members"
          value={`${league.currentMemberCount}/${league.maxMembers}`}
        />
        <StatsCard
          icon={Target}
          label="Exact Match Points"
          value={league.scoreExactMatch.toString()}
        />
        <StatsCard
          icon={Trophy}
          label="Correct Result Points"
          value={league.scoreCorrectResult.toString()}
        />
        <StatsCard
          icon={Clock}
          label="Betting Deadline"
          value={`${league.bettingDeadlineMinutes} min`}
        />
      </div>

      <div className="flex gap-2">
        <Button onClick={() => router.push(`/leagues/${leagueId}/matches`)}>
          <Target className="h-4 w-4 mr-2" />
          Place Bets
        </Button>
        <Button variant="outline" onClick={() => router.push(`/leagues/${leagueId}/standings`)}>
          <Trophy className="h-4 w-4 mr-2" />
          View Standings
        </Button>
      </div>

      <MemberList
        members={league.members}
        ownerId={league.ownerId}
        leagueId={leagueId}
        isOwner={isOwner}
      />

      {showInviteShare && (
        <InviteShare
          inviteCode={league.inviteCode}
          leagueName={league.name}
          onClose={() => setShowInviteShare(false)}
        />
      )}
    </div>
  );
}
