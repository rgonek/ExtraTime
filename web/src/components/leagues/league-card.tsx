'use client';

import Link from 'next/link';
import { Users, Calendar, Lock, Globe, Trophy } from 'lucide-react';
import {
  Card,
  CardContent,
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
      <Card interactive className="group">
        <CardHeader>
          <div className="flex items-start gap-4">
            {/* League Icon */}
            <div className="flex-shrink-0 w-12 h-12 rounded-xl bg-gradient-to-br from-primary to-secondary flex items-center justify-center shadow-md group-hover:shadow-lg transition-shadow">
              <Trophy className="h-6 w-6 text-white" />
            </div>

            {/* League Info */}
            <div className="flex-1 min-w-0">
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0">
                  <CardTitle className="text-lg truncate group-hover:text-primary transition-colors">
                    {league.name}
                  </CardTitle>
                  <p className="text-sm text-muted-foreground mt-0.5">
                    by {league.ownerUsername}
                  </p>
                </div>
                <Badge variant={league.isPublic ? 'info' : 'outline'}>
                  {league.isPublic ? (
                    <>
                      <Globe className="h-3 w-3" />
                      Public
                    </>
                  ) : (
                    <>
                      <Lock className="h-3 w-3" />
                      Private
                    </>
                  )}
                </Badge>
              </div>
            </div>
          </div>
        </CardHeader>

        <CardContent className="pt-0">
          {/* Stats Row */}
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-1.5 text-sm">
              <div className="w-7 h-7 rounded-lg bg-primary/10 flex items-center justify-center">
                <Users className="h-4 w-4 text-primary" />
              </div>
              <span className="text-muted-foreground">
                <span className="font-medium text-foreground">{league.memberCount}</span> members
              </span>
            </div>
            <div className="flex items-center gap-1.5 text-sm">
              <div className="w-7 h-7 rounded-lg bg-secondary/10 flex items-center justify-center">
                <Calendar className="h-4 w-4 text-secondary" />
              </div>
              <span className="text-muted-foreground">{createdDate}</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}
