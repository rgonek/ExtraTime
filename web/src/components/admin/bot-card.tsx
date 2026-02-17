'use client';

import { useState } from 'react';
import { Edit, MoreVertical, Power, PowerOff, Trash2 } from 'lucide-react';
import type { BotDto } from '@/types';
import { useUpdateBot } from '@/hooks/use-admin-bots';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { EditBotModal } from './edit-bot-modal';

interface BotCardProps {
  bot: BotDto;
  onDelete: () => void;
  isDeleting?: boolean;
}

export function BotCard({ bot, onDelete, isDeleting = false }: BotCardProps) {
  const [isEditOpen, setIsEditOpen] = useState(false);
  const updateBot = useUpdateBot();

  const toggleActive = () => {
    updateBot.mutate({
      id: bot.id,
      data: { isActive: !bot.isActive },
    });
  };

  return (
    <>
      <Card className={bot.isActive ? '' : 'opacity-70'}>
        <CardHeader className="flex flex-row items-center justify-between pb-2">
          <CardTitle className="text-lg">{bot.name}</CardTitle>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon">
                <MoreVertical className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => setIsEditOpen(true)}>
                <Edit className="mr-2 h-4 w-4" />
                Edit
              </DropdownMenuItem>
              <DropdownMenuItem onClick={toggleActive}>
                {bot.isActive ? (
                  <>
                    <PowerOff className="mr-2 h-4 w-4" />
                    Deactivate
                  </>
                ) : (
                  <>
                    <Power className="mr-2 h-4 w-4" />
                    Activate
                  </>
                )}
              </DropdownMenuItem>
              <DropdownMenuItem className="text-destructive" onClick={onDelete} disabled={isDeleting}>
                <Trash2 className="mr-2 h-4 w-4" />
                Delete
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </CardHeader>
        <CardContent className="space-y-3 text-sm">
          <div className="flex gap-2">
            <Badge variant={bot.isActive ? 'default' : 'secondary'}>
              {bot.isActive ? 'Active' : 'Inactive'}
            </Badge>
            <Badge variant="outline">{bot.strategy}</Badge>
          </div>

          {bot.stats && (
            <div className="grid grid-cols-2 gap-2 text-muted-foreground">
              <div>
                Bets: <span className="font-medium text-foreground">{bot.stats.totalBetsPlaced}</span>
              </div>
              <div>
                Leagues: <span className="font-medium text-foreground">{bot.stats.leaguesJoined}</span>
              </div>
              <div>
                Avg pts:{' '}
                <span className="font-medium text-foreground">
                  {bot.stats.averagePointsPerBet.toFixed(2)}
                </span>
              </div>
              <div>
                Exact: <span className="font-medium text-foreground">{bot.stats.exactPredictions}</span>
              </div>
            </div>
          )}

          {bot.lastBetPlacedAt && (
            <p className="text-xs text-muted-foreground">
              Last bet: {new Date(bot.lastBetPlacedAt).toLocaleDateString()}
            </p>
          )}
        </CardContent>
      </Card>

      <EditBotModal bot={bot} open={isEditOpen} onOpenChange={setIsEditOpen} />
    </>
  );
}
