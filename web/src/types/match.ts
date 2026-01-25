// ============================================================
// FOOTBALL DATA TYPES
// These are read-only (no requests, just responses)
// ============================================================

import type { MatchStatus } from './bet';

/**
 * Competition (league/tournament) summary
 * Backend: CompetitionSummaryDto record
 */
export interface CompetitionSummaryDto {
  id: string;
  name: string;
  code: string;
  country: string;
  logoUrl: string | null;
}

/**
 * Full competition details
 * Backend: CompetitionDto record
 */
export interface CompetitionDto extends CompetitionSummaryDto {
  externalId: number;
  currentMatchday: number | null;
  currentSeasonStart: string | null;
  currentSeasonEnd: string | null;
  lastSyncedAt: string;
}

/**
 * Team summary for match display
 * Backend: TeamSummaryDto record
 */
export interface TeamSummaryDto {
  id: string;
  name: string;
  shortName: string;
  crest: string | null;  // Team logo URL
}

/**
 * Match for list views
 * Backend: MatchDto record
 */
export interface MatchDto {
  id: string;
  competition: CompetitionSummaryDto;
  homeTeam: TeamSummaryDto;
  awayTeam: TeamSummaryDto;
  matchDateUtc: string;
  status: MatchStatus;
  matchday: number | null;
  homeScore: number | null;
  awayScore: number | null;
}

/**
 * Match with full details
 * Backend: MatchDetailDto record
 */
export interface MatchDetailDto extends MatchDto {
  stage: string | null;
  group: string | null;
  homeHalfTimeScore: number | null;
  awayHalfTimeScore: number | null;
  venue: string | null;
  lastSyncedAt: string;
}

// ============================================================
// PAGINATED RESPONSE
// ============================================================

/**
 * Generic paginated response wrapper
 * Backend: You use this pattern for all list endpoints
 *
 * TypeScript generics work like C# generics:
 * PagedResponse<MatchDto> = { items: MatchDto[], ... }
 */
export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Specific alias for matches (for clarity)
 */
export type MatchesPagedResponse = PagedResponse<MatchDto>;

// ============================================================
// QUERY PARAMETERS
// ============================================================

/**
 * Filters for match list endpoint
 * These become URL query params: ?page=1&pageSize=20&competitionId=xxx
 */
export interface MatchFilters {
  page?: number;
  pageSize?: number;
  competitionId?: string;
  dateFrom?: string;
  dateTo?: string;
  status?: MatchStatus;
}
