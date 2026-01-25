'use client';

import { Check, Copy, Link as LinkIcon } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';

interface InviteShareProps {
  inviteCode: string;
  leagueName: string;
  onClose: () => void;
}

export function InviteShare({ inviteCode, leagueName, onClose }: InviteShareProps) {
  const [copied, setCopied] = useState(false);

  const inviteUrl = `${typeof window !== 'undefined' ? window.location.origin : ''}/leagues/join?code=${inviteCode}`;

  const handleCopyCode = async () => {
    await navigator.clipboard.writeText(inviteCode);
    setCopied(true);
    toast.success('Invite code copied!');

    setTimeout(() => setCopied(false), 2000);
  };

  const handleCopyLink = async () => {
    await navigator.clipboard.writeText(inviteUrl);
    toast.success('Invite link copied!');
  };

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Invite to {leagueName}</DialogTitle>
          <DialogDescription>
            Share this code or link with friends to invite them
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div>
            <label className="text-sm font-medium">Invite Code</label>
            <div className="flex gap-2 mt-1">
              <Input
                value={inviteCode}
                readOnly
                className="font-mono text-lg tracking-wider"
              />
              <Button onClick={handleCopyCode} variant="outline">
                {copied ? (
                  <Check className="h-4 w-4" />
                ) : (
                  <Copy className="h-4 w-4" />
                )}
              </Button>
            </div>
          </div>

          <div>
            <label className="text-sm font-medium">Invite Link</label>
            <div className="flex gap-2 mt-1">
              <Input value={inviteUrl} readOnly className="text-sm" />
              <Button onClick={handleCopyLink} variant="outline">
                <LinkIcon className="h-4 w-4" />
              </Button>
            </div>
          </div>

          <p className="text-sm text-muted-foreground">
            Anyone with this code can join your league
          </p>
        </div>
      </DialogContent>
    </Dialog>
  );
}
