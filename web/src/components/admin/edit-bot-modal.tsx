'use client';

import { useState } from 'react';
import { toast } from 'sonner';
import type { BotDto, BotStrategy } from '@/types';
import { useUpdateBot } from '@/hooks/use-admin-bots';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

interface EditBotModalProps {
  bot: BotDto;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

const STRATEGIES: BotStrategy[] = [
  'Random',
  'HomeFavorer',
  'UnderdogSupporter',
  'DrawPredictor',
  'HighScorer',
  'StatsAnalyst',
  'MachineLearning',
];

export function EditBotModal({ bot, open, onOpenChange }: EditBotModalProps) {
  const updateBot = useUpdateBot();

  const [name, setName] = useState(bot.name);
  const [avatarUrl, setAvatarUrl] = useState(bot.avatarUrl ?? '');
  const [strategy, setStrategy] = useState<BotStrategy>(bot.strategy);

  const onSubmit = () => {
    if (!name.trim()) {
      toast.error('Bot name is required');
      return;
    }

    updateBot.mutate(
      {
        id: bot.id,
        data: {
          name: name.trim(),
          avatarUrl: avatarUrl.trim() || null,
          strategy,
        },
      },
      {
        onSuccess: () => {
          toast.success('Bot updated');
          onOpenChange(false);
        },
        onError: () => {
          toast.error('Failed to update bot');
        },
      }
    );
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(isOpen) => {
        if (isOpen) {
          setName(bot.name);
          setAvatarUrl(bot.avatarUrl ?? '');
          setStrategy(bot.strategy);
        }

        onOpenChange(isOpen);
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit Bot</DialogTitle>
          <DialogDescription>Update bot details and strategy.</DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor={`edit-bot-name-${bot.id}`}>Name</Label>
            <Input
              id={`edit-bot-name-${bot.id}`}
              value={name}
              onChange={(event) => setName(event.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor={`edit-bot-avatar-${bot.id}`}>Avatar URL</Label>
            <Input
              id={`edit-bot-avatar-${bot.id}`}
              value={avatarUrl}
              onChange={(event) => setAvatarUrl(event.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label>Strategy</Label>
            <Select value={strategy} onValueChange={(value) => setStrategy(value as BotStrategy)}>
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {STRATEGIES.map((strategyOption) => (
                  <SelectItem key={strategyOption} value={strategyOption}>
                    {strategyOption}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={onSubmit} disabled={updateBot.isPending}>
            {updateBot.isPending ? 'Saving...' : 'Save changes'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
