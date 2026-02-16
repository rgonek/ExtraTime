'use client';

import { ShieldCheck } from 'lucide-react';
import { PageHeader } from '@/components/shared/page-header';
import { DataAvailabilityCard } from '@/components/admin/data-availability-card';
import { IntegrationCard } from '@/components/admin/integration-card';
import { useDataAvailability, useIntegrationStatuses } from '@/hooks/use-admin-integrations';

export default function AdminIntegrationsPage() {
  const { data: statuses, isLoading } = useIntegrationStatuses();
  const { data: availability } = useDataAvailability();

  return (
    <div className="space-y-6">
      <PageHeader
        title="Integration Health"
        subtitle="Monitor and control external data integrations"
        icon={ShieldCheck}
      />

      {availability && <DataAvailabilityCard availability={availability} />}

      <div className="grid gap-4 md:grid-cols-2">
        {isLoading
          ? Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="h-52 animate-pulse rounded-lg bg-muted" />
            ))
          : statuses?.map((status) => (
              <IntegrationCard key={status.integrationName} status={status} />
            ))}
      </div>
    </div>
  );
}
