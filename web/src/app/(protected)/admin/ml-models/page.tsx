'use client';

import { useMemo, useState } from 'react';
import { BrainCircuit, RefreshCw, Rocket } from 'lucide-react';
import { toast } from 'sonner';
import { PageHeader } from '@/components/shared/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  useActivateMlModel,
  useMlAccuracy,
  useMlModels,
  useRecalculateMlAccuracy,
  useTriggerMlTraining,
} from '@/hooks/use-admin-ml';

const PERIOD_OPTIONS = ['weekly', 'monthly', 'yearly'] as const;

export default function AdminMlModelsPage() {
  const [period, setPeriod] = useState<(typeof PERIOD_OPTIONS)[number]>('monthly');
  const { data: models, isLoading: modelsLoading } = useMlModels();
  const { data: accuracy, isLoading: accuracyLoading } = useMlAccuracy(period);
  const activateModel = useActivateMlModel();
  const recalculateAccuracy = useRecalculateMlAccuracy();
  const triggerTraining = useTriggerMlTraining();

  const isMutating =
    activateModel.isPending || recalculateAccuracy.isPending || triggerTraining.isPending;

  const accuracyRange = useMemo(() => {
    const toDate = new Date();
    const fromDate = new Date(toDate);

    if (period === 'weekly') {
      fromDate.setDate(toDate.getDate() - 7);
    } else if (period === 'yearly') {
      fromDate.setDate(toDate.getDate() - 365);
    } else {
      fromDate.setDate(toDate.getDate() - 30);
    }

    return { fromDate, toDate };
  }, [period]);

  const onActivateModel = (version: string) => {
    activateModel.mutate(
      { version, notes: 'Activated from admin dashboard' },
      {
        onSuccess: () => toast.success(`Model ${version} activated`),
        onError: () => toast.error('Failed to activate model version'),
      }
    );
  };

  const onRecalculateAccuracy = () => {
    recalculateAccuracy.mutate(
      {
        fromDate: accuracyRange.fromDate.toISOString(),
        toDate: accuracyRange.toDate.toISOString(),
        periodType: period,
        accuracyPeriod: period,
      },
      {
        onSuccess: () => toast.success('Accuracy metrics recalculated'),
        onError: () => toast.error('Failed to recalculate accuracy metrics'),
      }
    );
  };

  const onTriggerTraining = () => {
    triggerTraining.mutate(
      { league: 'premier-league' },
      {
        onSuccess: () => toast.success('Training request submitted'),
        onError: () => toast.error('Failed to submit training request'),
      }
    );
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="ML Model Management"
        subtitle="Manage model versions and monitor strategy accuracy"
        icon={BrainCircuit}
        actions={[
          {
            label: 'Trigger Training',
            icon: Rocket,
            onClick: onTriggerTraining,
            variant: 'default',
          },
        ]}
      />

      <Card>
        <CardHeader>
          <CardTitle>Model Versions</CardTitle>
        </CardHeader>
        <CardContent>
          {modelsLoading ? (
            <div className="h-60 animate-pulse rounded-lg bg-muted" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Version</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Trained</TableHead>
                  <TableHead className="text-right">Samples</TableHead>
                  <TableHead className="text-right">R²</TableHead>
                  <TableHead className="text-right">MAE</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {models?.map((model) => (
                  <TableRow key={model.id}>
                    <TableCell className="font-mono text-xs">{model.version}</TableCell>
                    <TableCell>{model.modelType}</TableCell>
                    <TableCell>
                      <Badge variant={model.isActive ? 'accent' : 'secondary'}>
                        {model.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </TableCell>
                    <TableCell>{new Date(model.trainedAt).toLocaleDateString()}</TableCell>
                    <TableCell className="text-right">{model.trainingSamples.toLocaleString()}</TableCell>
                    <TableCell className="text-right">{model.rsquared.toFixed(3)}</TableCell>
                    <TableCell className="text-right">{model.meanAbsoluteError.toFixed(3)}</TableCell>
                    <TableCell className="text-right">
                      {!model.isActive && (
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => onActivateModel(model.version)}
                          disabled={isMutating}
                        >
                          Activate
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
                {!models?.length && (
                  <TableRow>
                    <TableCell colSpan={8} className="text-center text-muted-foreground py-8">
                      No model versions found.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0">
          <CardTitle>Strategy Accuracy Comparison</CardTitle>
          <div className="flex items-center gap-2">
            <Select value={period} onValueChange={(value) => setPeriod(value as typeof period)}>
              <SelectTrigger className="w-32">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {PERIOD_OPTIONS.map((option) => (
                  <SelectItem key={option} value={option}>
                    {option}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button
              variant="outline"
              size="sm"
              onClick={onRecalculateAccuracy}
              disabled={isMutating}
            >
              <RefreshCw className="h-4 w-4 mr-2" />
              Recalculate
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {accuracyLoading ? (
            <div className="h-48 animate-pulse rounded-lg bg-muted" />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Strategy</TableHead>
                  <TableHead className="text-right">Total Predictions</TableHead>
                  <TableHead className="text-right">Exact Score %</TableHead>
                  <TableHead className="text-right">Result %</TableHead>
                  <TableHead className="text-right">MAE</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {accuracy?.map((entry) => (
                  <TableRow key={entry.strategy}>
                    <TableCell className="font-medium">{entry.strategy}</TableCell>
                    <TableCell className="text-right">{entry.totalPredictions.toLocaleString()}</TableCell>
                    <TableCell className="text-right">
                      {(entry.avgExactAccuracy * 100).toFixed(1)}%
                    </TableCell>
                    <TableCell className="text-right">
                      {(entry.avgResultAccuracy * 100).toFixed(1)}%
                    </TableCell>
                    <TableCell className="text-right">{entry.avgMAE.toFixed(3)}</TableCell>
                  </TableRow>
                ))}
                {!accuracy?.length && (
                  <TableRow>
                    <TableCell colSpan={5} className="text-center text-muted-foreground py-8">
                      No accuracy data available for selected period.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
