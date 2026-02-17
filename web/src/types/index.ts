// ============================================================
// TYPES INDEX
// Central export point for all types
//
// Usage: import { LeagueDto, BetDto, MatchDto } from '@/types';
// Instead of: import { LeagueDto } from '@/types/league';
// ============================================================

// Auth types
export type {
  User,
  RegisterRequest,
  LoginRequest,
  RefreshTokenRequest,
  AuthResponse,
  CurrentUserResponse,
  ApiError,
} from './auth';

// League types
export type {
  MemberRole,
  LeagueDto,
  LeagueSummaryDto,
  LeagueDetailDto,
  LeagueMemberDto,
  CreateLeagueRequest,
  UpdateLeagueRequest,
  JoinLeagueRequest,
  RegenerateInviteCodeRequest,
  InviteCodeResponse,
} from './league';

// Bet types
export type {
  MatchStatus,
  BetResultDto,
  BetDto,
  MyBetDto,
  MatchBetDto,
  LeagueStandingDto,
  UserStatsDto,
  PlaceBetRequest,
} from './bet';

// Match/Football types
export type {
  CompetitionSummaryDto,
  CompetitionDto,
  TeamSummaryDto,
  MatchDto,
  MatchDetailDto,
  PagedResponse,
  MatchesPagedResponse,
  MatchFilters,
} from './match';

// Bot types
export type {
  BotStrategy,
  StatsAnalystConfig,
  BotStats,
  BotDto,
  BotSummaryDto,
  LeagueBotDto,
  CreateBotRequest,
  UpdateBotRequest,
  CreateStatsAnalystBotRequest,
  AddBotToLeagueRequest,
  BotConfigurationDto,
  ConfigurationPreset,
} from './bot';

// Integration types
export type {
  IntegrationHealth,
  IntegrationStatus,
  DataAvailability,
} from './integration';

// ML Admin types
export type {
  MlModelVersionDto,
  MlStrategyAccuracyDto,
} from './ml';
