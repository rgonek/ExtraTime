// ============================================================
// BET TYPES
// ============================================================

/**
 * Match status enum as union type
 * Backend: public enum MatchStatus
 *
 * Why union over enum?
 * Your API returns these as strings. TypeScript unions
 * match the JSON exactly without conversion.
 */
export type MatchStatus =
  | 'Scheduled'
  | 'Timed'
  | 'InPlay'
  | 'Paused'
  | 'Finished'
  | 'Postponed'
  | 'Suspended'
  | 'Cancelled';

/**
 * Result of a scored bet
 * Backend: BetResultDto record
 */
export interface BetResultDto {
  pointsEarned: number;
  isExactMatch: boolean;
  isCorrectResult: boolean;
}

/**
 * Basic bet data
 * Backend: BetDto record
 */
export interface BetDto {
  id: string;
  leagueId: string;
  userId: string;
  matchId: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
  placedAt: string;
  lastUpdatedAt: string | null;
  result: BetResultDto | null;  // null until match is scored
}

/**
 * User's bet with match context (for "My Bets" view)
 * Backend: MyBetDto record
 *
 * Backend Analogy: This is like a "projected DTO" that joins
 * Bet with Match data for display purposes
 */
export interface MyBetDto {
  betId: string;
  matchId: string;
  homeTeamName: string;
  awayTeamName: string;
  matchDateUtc: string;
  matchStatus: MatchStatus;
  actualHomeScore: number | null;
  actualAwayScore: number | null;
  predictedHomeScore: number;
  predictedAwayScore: number;
  result: BetResultDto | null;
  placedAt: string;
}

/**
 * Another user's bet on a match (for reveal after deadline)
 * Backend: MatchBetDto record
 */
export interface MatchBetDto {
  userId: string;
  username: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
  result: BetResultDto | null;
}

/**
 * User's position in league standings
 * Backend: LeagueStandingDto record
 */
export interface LeagueStandingDto {
  userId: string;
  username: string;
  email: string;
  rank: number;
  totalPoints: number;
  betsPlaced: number;
  exactMatches: number;
  correctResults: number;
  currentStreak: number;
  bestStreak: number;
  lastUpdatedAt: string;
}

/**
 * Detailed user statistics in a league
 * Backend: UserStatsDto record
 */
export interface UserStatsDto {
  userId: string;
  username: string;
  totalPoints: number;
  betsPlaced: number;
  exactMatches: number;
  correctResults: number;
  currentStreak: number;
  bestStreak: number;
  accuracyPercentage: number;
  rank: number;
  lastUpdatedAt: string;
}

// ============================================================
// REQUEST TYPES
// ============================================================

/**
 * Place or update a bet
 * Backend: PlaceBetRequest record
 */
export interface PlaceBetRequest {
  matchId: string;
  predictedHomeScore: number;
  predictedAwayScore: number;
}
