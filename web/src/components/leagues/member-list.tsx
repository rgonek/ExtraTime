'use client';

import { toast } from 'sonner';
import { Crown, MoreVertical, UserMinus } from 'lucide-react';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
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
import type { LeagueMemberDto } from '@/types';

interface MemberListProps {
  members: LeagueMemberDto[];
  ownerId: string;
  leagueId: string;
  isOwner: boolean;
}

export function MemberList({ members, ownerId, leagueId, isOwner }: MemberListProps) {
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
        <CardTitle>Members ({members.length})</CardTitle>
        <CardDescription>People in this league</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-2">
          {sortedMembers.map((member) => (
            <div
              key={member.userId}
              className="flex items-center justify-between p-2 rounded-lg hover:bg-muted/50"
            >
              <div className="flex items-center gap-3">
                <Avatar>
                  <AvatarFallback>
                    {member.username.slice(0, 2).toUpperCase()}
                  </AvatarFallback>
                </Avatar>

                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-medium">{member.username}</span>
                    {member.role === 'Owner' && (
                      <Badge variant="secondary" className="gap-1">
                        <Crown className="h-3 w-3" />
                        Owner
                      </Badge>
                    )}
                  </div>
                  <span className="text-sm text-muted-foreground">
                    Joined {new Date(member.joinedAt).toLocaleDateString()}
                  </span>
                </div>
              </div>

              {isOwner && member.userId !== ownerId && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon">
                      <MoreVertical className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem
                      className="text-destructive"
                      onClick={() => handleKick(member.userId, member.username)}
                    >
                      <UserMinus className="h-4 w-4 mr-2" />
                      Remove from League
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              )}
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
