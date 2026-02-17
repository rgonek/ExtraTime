'use client';

import type { DataAvailability } from '@/types';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface DataAvailabilityCardProps {
  availability: DataAvailability;
}

interface AvailabilityRow {
  label: string;
  value: boolean;
}

export function DataAvailabilityCard({ availability }: DataAvailabilityCardProps) {
  const rows: AvailabilityRow[] = [
    { label: 'Form', value: availability.formDataAvailable },
    { label: 'xG', value: availability.xgDataAvailable },
    { label: 'Odds', value: availability.oddsDataAvailable },
    { label: 'Injuries', value: availability.injuryDataAvailable },
    { label: 'Lineups', value: availability.lineupDataAvailable },
    { label: 'Elo', value: availability.eloDataAvailable },
    { label: 'Standings', value: availability.standingsDataAvailable },
  ];

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">Data Availability</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex items-center justify-between">
          <span className="text-sm text-muted-foreground">Available sources</span>
          <span className="font-semibold">{availability.availableSourceCount}</span>
        </div>
        <div className="flex flex-wrap gap-2">
          {rows.map((row) => (
            <Badge key={row.label} variant={row.value ? 'default' : 'secondary'}>
              {row.label}
            </Badge>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
