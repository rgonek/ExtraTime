'use client';

import { useMemo, useState } from 'react';
import { toast } from 'sonner';
import type { BotStrategy } from '@/types';
import { useBotPresets, useCreateBot } from '@/hooks/use-admin-bots';
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

interface CreateBotModalProps {
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
];

export function CreateBotModal({ open, onOpenChange }: CreateBotModalProps) {
  const createBot = useCreateBot();
  const { data: presets } = useBotPresets();

  const [name, setName] = useState('');
  const [avatarUrl, setAvatarUrl] = useState('');
  const [strategy, setStrategy] = useState<BotStrategy>('Random');
  const [presetName, setPresetName] = useState('');

  const selectedPreset = useMemo(
    () => presets?.find((preset) => preset.name === presetName),
    [presetName, presets]
  );

  const onSubmit = () => {
    if (!name.trim()) {
      toast.error('Bot name is required');
      return;
    }

    createBot.mutate(
      {
        name: name.trim(),
        avatarUrl: avatarUrl.trim() || null,
        strategy,
        configuration: selectedPreset ? selectedPreset.configuration : null,
      },
      {
        onSuccess: () => {
          toast.success('Bot created');
          setName('');
          setAvatarUrl('');
          setStrategy('Random');
          setPresetName('');
          onOpenChange(false);
        },
        onError: () => {
          toast.error('Failed to create bot');
        },
      }
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create Bot</DialogTitle>
          <DialogDescription>Create a new AI betting bot for leagues.</DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="bot-name">Name</Label>
            <Input id="bot-name" value={name} onChange={(event) => setName(event.target.value)} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="bot-avatar">Avatar URL</Label>
            <Input
              id="bot-avatar"
              value={avatarUrl}
              onChange={(event) => setAvatarUrl(event.target.value)}
              placeholder="https://example.com/avatar.png"
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

          {strategy === 'StatsAnalyst' && (
            <div className="space-y-2">
              <Label>Configuration Preset</Label>
              <Select value={presetName} onValueChange={setPresetName}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select preset (optional)" />
                </SelectTrigger>
                <SelectContent>
                  {presets?.map((preset) => (
                    <SelectItem key={preset.name} value={preset.name}>
                      {preset.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {selectedPreset && (
                <p className="text-xs text-muted-foreground">{selectedPreset.description}</p>
              )}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={onSubmit} disabled={createBot.isPending}>
            {createBot.isPending ? 'Creating...' : 'Create Bot'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
