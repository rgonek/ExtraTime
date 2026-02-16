'use client';

import { useState } from 'react';
import { Bot as BotIcon, Plus } from 'lucide-react';
import { PageHeader } from '@/components/shared/page-header';
import { Button } from '@/components/ui/button';
import { BotCard } from '@/components/admin/bot-card';
import { CreateBotModal } from '@/components/admin/create-bot-modal';
import { useBots, useDeleteBot } from '@/hooks/use-admin-bots';

export default function AdminBotsPage() {
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const { data: bots, isLoading } = useBots({ includeInactive: true });
  const deleteBot = useDeleteBot();

  return (
    <div className="space-y-6">
      <PageHeader
        title="Bot Management"
        subtitle="Create and manage AI betting bots"
        icon={BotIcon}
        actions={[
          {
            label: 'Create Bot',
            icon: Plus,
            onClick: () => setIsCreateOpen(true),
          },
        ]}
      />

      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, index) => (
            <div key={index} className="h-56 animate-pulse rounded-lg bg-muted" />
          ))}
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {bots?.map((bot) => (
            <BotCard
              key={bot.id}
              bot={bot}
              onDelete={() => deleteBot.mutate(bot.id)}
              isDeleting={deleteBot.isPending}
            />
          ))}
          {bots?.length === 0 && (
            <div className="col-span-full rounded-lg border border-dashed p-8 text-center text-muted-foreground">
              No bots found.
              <div className="mt-4">
                <Button onClick={() => setIsCreateOpen(true)}>Create your first bot</Button>
              </div>
            </div>
          )}
        </div>
      )}

      <CreateBotModal open={isCreateOpen} onOpenChange={setIsCreateOpen} />
    </div>
  );
}
