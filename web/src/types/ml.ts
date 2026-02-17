export interface MlModelVersionDto {
  id: string;
  modelType: string;
  version: string;
  isActive: boolean;
  trainedAt: string;
  trainingSamples: number;
  rsquared: number;
  meanAbsoluteError: number;
  rootMeanSquaredError: number;
  algorithmUsed: string;
}

export interface MlStrategyAccuracyDto {
  strategy: string;
  avgExactAccuracy: number;
  avgResultAccuracy: number;
  avgMAE: number;
  totalPredictions: number;
  latestPeriod: string;
}
