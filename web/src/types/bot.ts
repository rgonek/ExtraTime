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
  matchesAnalyzed: number;
  highStakesBoost: boolean;
  lateSeasonMatchday: number;
  style: 'Conservative' | 'Moderate' | 'Bold';
  randomVariance: number;
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
  avatarUrl: string | null;
  strategy: BotStrategy;
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
