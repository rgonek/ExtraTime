// ============================================================
// LEAGUE TYPES
// These mirror your backend DTOs exactly
// ============================================================

/**
 * Member role within a league
 * Backend: public enum MemberRole { Member = 0, Owner = 1 }
 */
export type MemberRole = 'Member' | 'Owner';

/**
 * Full league data returned from POST/PUT operations
 * Backend: LeagueDto record
 */
export interface LeagueDto {
  id: string;
  name: string;
  description: string | null;
  ownerId: string;
  ownerUsername: string;
  isPublic: boolean;
  maxMembers: number;
  currentMemberCount: number;
  scoreExactMatch: number;
  scoreCorrectResult: number;
  bettingDeadlineMinutes: number;
  allowedCompetitionIds: string[];
  inviteCode: string;
  inviteCodeExpiresAt: string | null;  // ISO date string
  createdAt: string;                    // ISO date string
}

/**
 * Summary for list views (less data = faster)
 * Backend: LeagueSummaryDto record
 */
export interface LeagueSummaryDto {
  id: string;
  name: string;
  ownerUsername: string;
  memberCount: number;
  isPublic: boolean;
  createdAt: string;
}

/**
 * Detail view with members included
 * Backend: LeagueDetailDto record
 */
export interface LeagueDetailDto extends LeagueDto {
  members: LeagueMemberDto[];
}

/**
 * Single member in a league
 * Backend: LeagueMemberDto record
 */
export interface LeagueMemberDto {
  userId: string;
  username: string;
  role: MemberRole;
  joinedAt: string;
}

// ============================================================
// REQUEST TYPES (What we send TO the API)
// ============================================================

/**
 * Create a new league
 * Backend: CreateLeagueRequest record
 */
export interface CreateLeagueRequest {
  name: string;
  description?: string;
  isPublic?: boolean;
  maxMembers?: number;
  scoreExactMatch?: number;
  scoreCorrectResult?: number;
  bettingDeadlineMinutes?: number;
  allowedCompetitionIds?: string[];
  expiresAt?: string;
}

/**
 * Update existing league
 * Backend: UpdateLeagueRequest record
 */
export interface UpdateLeagueRequest {
  name: string;
  description?: string;
  isPublic?: boolean;
  maxMembers?: number;
  scoreExactMatch?: number;
  scoreCorrectResult?: number;
  bettingDeadlineMinutes?: number;
  allowedCompetitionIds?: string[];
}

/**
 * Join via invite code
 * Backend: JoinLeagueRequest record
 */
export interface JoinLeagueRequest {
  inviteCode: string;
}

/**
 * Regenerate invite code
 * Backend: RegenerateInviteCodeRequest record
 */
export interface RegenerateInviteCodeRequest {
  expiresAt?: string;
}

/**
 * Response from invite code regeneration
 */
export interface InviteCodeResponse {
  inviteCode: string;
  expiresAt: string | null;
}
