'use client';

import Link from 'next/link';
import { Users, Calendar, Lock, Globe } from 'lucide-react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import type { LeagueSummaryDto } from '@/types';

interface LeagueCardProps {
  league: LeagueSummaryDto;
}

export function LeagueCard({ league }: LeagueCardProps) {
  const createdDate = new Date(league.createdAt).toLocaleDateString();

  return (
    <Link href={`/leagues/${league.id}`}>
      <Card className="transition-colors hover:bg-muted/50 cursor-pointer">
        <CardHeader className="pb-2">
          <div className="flex items-start justify-between">
            <div>
              <CardTitle className="text-lg">{league.name}</CardTitle>
              <CardDescription>by {league.ownerUsername}</CardDescription>
            </div>
            <Badge variant={league.isPublic ? 'secondary' : 'outline'}>
              {league.isPublic ? (
                <>
                  <Globe className="h-3 w-3 mr-1" />
                  Public
                </>
              ) : (
                <>
                  <Lock className="h-3 w-3 mr-1" />
                  Private
                </>
              )}
            </Badge>
          </div>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-4 text-sm text-muted-foreground">
            <div className="flex items-center gap-1">
              <Users className="h-4 w-4" />
              <span>{league.memberCount} members</span>
            </div>
            <div className="flex items-center gap-1">
              <Calendar className="h-4 w-4" />
              <span>{createdDate}</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}
