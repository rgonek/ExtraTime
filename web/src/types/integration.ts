export type IntegrationHealth = 'Unknown' | 'Healthy' | 'Degraded' | 'Failed' | 'Disabled';

export interface IntegrationStatus {
  integrationName: string;
  health: IntegrationHealth;
  isOperational: boolean;
  lastSuccessfulSync: string | null;
  lastAttemptedSync: string | null;
  lastFailedSync: string | null;
  consecutiveFailures: number;
  totalFailures24h: number;
  lastErrorMessage: string | null;
  dataFreshAsOf: string | null;
  isDataStale: boolean;
  isManuallyDisabled: boolean;
  disabledReason: string | null;
  disabledAt: string | null;
  successRate24h: number;
}

export interface DataAvailability {
  formDataAvailable: boolean;
  xgDataAvailable: boolean;
  oddsDataAvailable: boolean;
  injuryDataAvailable: boolean;
  lineupDataAvailable: boolean;
  eloDataAvailable: boolean;
  standingsDataAvailable: boolean;
  hasAnyExternalData: boolean;
  availableSourceCount: number;
}
