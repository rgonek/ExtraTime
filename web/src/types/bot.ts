// Bot Strategy Types
export type BotStrategy =
  | 'Random'
  | 'HomeFavorer'
  | 'UnderdogSupporter'
  | 'DrawPredictor'
  | 'HighScorer'
  | 'StatsAnalyst';

export interface StatsAnalystConfig {
  formWeight: number;
  homeAdvantageWeight: number;
  goalTrendWeight: number;
  streakWeight: number;
  xgWeight?: number;
  xgDefensiveWeight?: number;
  oddsWeight?: number;
  injuryWeight?: number;
  lineupAnalysisWeight?: number;
  eloWeight?: number;
  matchesAnalyzed: number;
  highStakesBoost: boolean;
  lateSeasonMatchday: number;
  style: 'Conservative' | 'Moderate' | 'Bold';
  randomVariance: number;
  useXgData?: boolean;
  useOddsData?: boolean;
  useInjuryData?: boolean;
  useLineupData?: boolean;
  useEloData?: boolean;
}

export interface BotStats {
  totalBetsPlaced: number;
  leaguesJoined: number;
  averagePointsPerBet: number;
  exactPredictions: number;
  correctResults: number;
}

export interface BotDto {
  id: string;
  name: string;
  avatarUrl: string | null;
  strategy: BotStrategy;
  configuration: string | null;
  isActive: boolean;
  createdAt: string;
  lastBetPlacedAt: string | null;
  stats?: BotStats | null;
}

export interface BotSummaryDto {
  id: string;
  name: string;
  avatarUrl: string | null;
  strategy: BotStrategy;
}

export interface LeagueBotDto {
  id: string;
  name: string;
  avatarUrl: string | null;
  strategy: BotStrategy;
  addedAt: string;
}

export interface CreateBotRequest {
  name: string;
  avatarUrl?: string | null;
  strategy: BotStrategy;
  configuration?: Record<string, unknown> | null;
}

export interface UpdateBotRequest {
  name?: string;
  avatarUrl?: string | null;
  strategy?: BotStrategy;
  configuration?: Record<string, unknown> | null;
  isActive?: boolean;
}

export interface CreateStatsAnalystBotRequest {
  name: string;
  avatarUrl: string | null;
  formWeight: number;
  homeAdvantageWeight: number;
  goalTrendWeight: number;
  streakWeight: number;
  matchesAnalyzed: number;
  highStakesBoost: boolean;
  style: 'Conservative' | 'Moderate' | 'Bold';
  randomVariance: number;
}

export interface AddBotToLeagueRequest {
  botId: string;
}

export interface BotConfigurationDto {
  formWeight: number;
  homeAdvantageWeight: number;
  goalTrendWeight: number;
  streakWeight: number;
  xgWeight: number;
  xgDefensiveWeight: number;
  oddsWeight: number;
  injuryWeight: number;
  lineupAnalysisWeight: number;
  matchesAnalyzed: number;
  highStakesBoost: boolean;
  style: string;
  randomVariance: number;
  useXgData: boolean;
  useOddsData: boolean;
  useInjuryData: boolean;
  useLineupData: boolean;
  useEloData: boolean;
}

export interface ConfigurationPreset {
  name: string;
  description: string;
  configuration: BotConfigurationDto;
}
