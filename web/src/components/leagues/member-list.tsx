'use client';

import { toast } from 'sonner';
import { Crown, MoreVertical, UserMinus, Users, Calendar } from 'lucide-react';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useKickMember } from '@/hooks/use-leagues';
import { useAuthStore } from '@/stores/auth-store';
import { cn } from '@/lib/utils';
import type { LeagueMemberDto } from '@/types';

interface MemberListProps {
  members: LeagueMemberDto[];
  ownerId: string;
  leagueId: string;
  isOwner: boolean;
}

export function MemberList({ members, ownerId, leagueId, isOwner }: MemberListProps) {
  const currentUser = useAuthStore((state) => state.user);
  const kickMutation = useKickMember(leagueId);

  const handleKick = async (userId: string, username: string) => {
    if (!confirm(`Are you sure you want to remove ${username} from the league?`)) {
      return;
    }

    try {
      await kickMutation.mutateAsync(userId);
      toast.success(`${username} has been removed`);
    } catch {
      toast.error('Failed to remove member');
    }
  };

  const sortedMembers = [...members].sort((a, b) => {
    if (a.role === 'Owner') return -1;
    if (b.role === 'Owner') return 1;
    return a.username.localeCompare(b.username);
  });

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-secondary to-primary flex items-center justify-center shadow-sm">
            <Users className="h-5 w-5 text-white" />
          </div>
          <div>
            <CardTitle className="text-lg">Members</CardTitle>
            <p className="text-sm text-muted-foreground">
              {members.length} {members.length === 1 ? 'person' : 'people'} in this league
            </p>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-1">
          {sortedMembers.map((member, index) => {
            const isCurrentUser = member.userId === currentUser?.id;
            const isMemberOwner = member.role === 'Owner';

            return (
              <div
                key={member.userId}
                className={cn(
                  'flex items-center justify-between p-3 rounded-xl transition-all duration-150',
                  'hover:bg-muted/50',
                  isCurrentUser && 'bg-primary/5 hover:bg-primary/10',
                  isMemberOwner && !isCurrentUser && 'bg-accent/5'
                )}
              >
                <div className="flex items-center gap-3">
                  {/* Avatar with rank-like styling for owner */}
                  <Avatar
                    size="md"
                    className={cn(
                      'ring-2 ring-offset-2 ring-offset-background',
                      isMemberOwner && 'ring-accent',
                      !isMemberOwner && 'ring-border'
                    )}
                  >
                    <AvatarFallback className={cn(
                      isMemberOwner && 'bg-accent/20 text-accent'
                    )}>
                      {member.username.slice(0, 2).toUpperCase()}
                    </AvatarFallback>
                  </Avatar>

                  <div className="min-w-0">
                    <div className="flex items-center gap-2">
                      <span className={cn(
                        'font-medium truncate',
                        isCurrentUser && 'text-primary'
                      )}>
                        {member.username}
                      </span>
                      {isMemberOwner && (
                        <Badge variant="accent" className="gap-1 text-[10px] px-1.5">
                          <Crown className="h-3 w-3" />
                          Owner
                        </Badge>
                      )}
                      {isCurrentUser && !isMemberOwner && (
                        <Badge variant="points" className="text-[10px] px-1.5 py-0">
                          You
                        </Badge>
                      )}
                    </div>
                    <div className="flex items-center gap-1 text-xs text-muted-foreground mt-0.5">
                      <Calendar className="h-3 w-3" />
                      <span>Joined {new Date(member.joinedAt).toLocaleDateString()}</span>
                    </div>
                  </div>
                </div>

                {isOwner && member.userId !== ownerId && (
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 opacity-0 group-hover:opacity-100 hover:opacity-100 focus:opacity-100 transition-opacity"
                      >
                        <MoreVertical className="h-4 w-4" />
                        <span className="sr-only">Member options</span>
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem
                        className="text-destructive focus:text-destructive focus:bg-destructive/10"
                        onClick={() => handleKick(member.userId, member.username)}
                      >
                        <UserMinus className="h-4 w-4 mr-2" />
                        Remove from League
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                )}
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
