'use client';

import { useLeagueStandings } from '@/hooks/use-bets';
import { useLeagueBots } from '@/hooks/use-bots';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { BotIndicator } from './bot-strategy-info';
import { Trophy, Target } from 'lucide-react';

interface BotPerformanceProps {
  leagueId: string;
}

export function BotPerformance({ leagueId }: BotPerformanceProps) {
  const { data: standings, isLoading: standingsLoading } = useLeagueStandings(leagueId);
  const { data: bots, isLoading: botsLoading } = useLeagueBots(leagueId);

  if (standingsLoading || botsLoading) {
    return (
      <Card className="border-2 border-dashed border-muted-foreground/20">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <Trophy className="w-5 h-5 text-primary" />
            Bot Performance
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="h-12 bg-muted rounded animate-pulse" />
            ))}
          </div>
        </CardContent>
      </Card>
    );
  }

  const botStandings = standings?.filter(s =>
    bots?.some(b => b.id === s.userId)
  ) ?? [];

  if (botStandings.length === 0) {
    return (
      <Card className="border-2 border-dashed border-muted-foreground/20">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <Trophy className="w-5 h-5 text-primary" />
            Bot Performance
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground text-center py-4">
            No bots in this league yet
          </p>
        </CardContent>
      </Card>
    );
  }

  const sortedStandings = [...botStandings].sort((a, b) => b.totalPoints - a.totalPoints);

  return (
    <Card className="border-2 border-dashed border-muted-foreground/20">
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Trophy className="w-5 h-5 text-primary" />
          Bot Performance
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-3">
          {sortedStandings.map((standing, index) => {
            const bot = bots?.find(b => b.id === standing.userId);
            if (!bot) return null;

            return (
              <div
                key={standing.userId}
                className="flex items-center justify-between p-3 rounded-lg bg-muted/50 hover:bg-muted transition-colors"
              >
                <div className="flex items-center gap-3">
                  <span className="text-sm font-medium text-muted-foreground w-6">
                    {index + 1}
                  </span>
                  <BotIndicator strategy={bot.strategy} />
                  <span className="font-medium">{bot.name}</span>
                </div>
                <div className="flex items-center gap-3">
                  <div className="flex items-center gap-1 text-sm text-muted-foreground">
                    <Target className="w-4 h-4" />
                    <span>{standing.exactMatches}</span>
                  </div>
                  <span className="font-bold text-primary">{standing.totalPoints} pts</span>
                </div>
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
