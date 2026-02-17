'use client';

import { toast } from 'sonner';
import type { IntegrationStatus } from '@/types';
import { useToggleIntegration, useTriggerSync } from '@/hooks/use-admin-integrations';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface IntegrationCardProps {
  status: IntegrationStatus;
}

const healthVariant: Record<IntegrationStatus['health'], 'default' | 'secondary' | 'destructive' | 'outline'> = {
  Healthy: 'default',
  Degraded: 'secondary',
  Failed: 'destructive',
  Disabled: 'outline',
  Unknown: 'secondary',
};

export function IntegrationCard({ status }: IntegrationCardProps) {
  const triggerSync = useTriggerSync();
  const toggleIntegration = useToggleIntegration();

  const sync = () => {
    triggerSync.mutate(status.integrationName, {
      onSuccess: () => toast.success(`${status.integrationName} sync triggered`),
      onError: () => toast.error('Failed to trigger sync'),
    });
  };

  const toggle = () => {
    toggleIntegration.mutate(
      {
        type: status.integrationName,
        enable: status.isManuallyDisabled,
      },
      {
        onSuccess: () =>
          toast.success(
            status.isManuallyDisabled
              ? `${status.integrationName} enabled`
              : `${status.integrationName} disabled`
          ),
        onError: () => toast.error('Failed to update integration state'),
      }
    );
  };

  return (
    <Card>
      <CardHeader className="space-y-2">
        <CardTitle className="flex items-center justify-between text-lg">
          {status.integrationName}
          <Badge variant={healthVariant[status.health]}>{status.health}</Badge>
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4 text-sm">
        <div className="space-y-1 text-muted-foreground">
          <p>Operational: {status.isOperational ? 'Yes' : 'No'}</p>
          <p>Consecutive failures: {status.consecutiveFailures}</p>
          <p>
            Last successful sync:{' '}
            {status.lastSuccessfulSync
              ? new Date(status.lastSuccessfulSync).toLocaleString()
              : 'Never'}
          </p>
          {status.lastErrorMessage && (
            <p className="text-destructive">Last error: {status.lastErrorMessage}</p>
          )}
        </div>

        <div className="flex gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={sync}
            disabled={triggerSync.isPending}
          >
            {triggerSync.isPending ? 'Syncing...' : 'Sync'}
          </Button>
          <Button
            variant={status.isManuallyDisabled ? 'default' : 'secondary'}
            size="sm"
            onClick={toggle}
            disabled={toggleIntegration.isPending}
          >
            {status.isManuallyDisabled ? 'Enable' : 'Disable'}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
