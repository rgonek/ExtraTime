# Test Coverage Gap Remediation Plan

> **Goal:** Achieve comprehensive test coverage across Domain, Unit, Integration, and API test layers.
> **Current State:** ~450+ tests across 110 files, significant gaps remain
> **Target:** 85%+ overall coverage with all critical paths tested

---

## Executive Summary

### Current Coverage Status

| Test Layer | Files | Tests | Coverage |
|------------|-------|-------|----------|
| Domain.Tests | 3 | ~30 | 20% (3/15 entities) |
| UnitTests | 50+ | ~200+ | 75% (handlers, validators, services) |
| IntegrationTests | 42 | ~150+ | 80% (major features) |
| NewIntegrationTests | 12 | ~60+ | 85% (high-level flows) |
| API.Tests | 2 | ~10 | 15% (major gaps) |
| **Total** | **~109** | **~450+** | **~65% overall** |

### Critical Gaps Identified

1. **Domain Layer:** 12/15 entities have no tests (User, Bot, BackgroundJob, RefreshToken, etc.)
2. **Bot Strategies:** ALL 6 betting strategies untested (Random, HomeFavorer, StatsAnalyst, etc.)
3. **API Endpoints:** Only 2/8 endpoint groups tested (missing Bet, Football, Admin, Bots)
4. **Infrastructure:** Football services, background services untested
5. **Value Objects:** No dedicated tests for Email, Username, InviteCode, etc.

---

## Phase 1: Domain Tests (Priority: Critical)

**Timeline:** 2-3 days
**Goal:** Add tests for all domain entities and value objects

### 1.1 Domain Entity Tests

#### User Entity Tests
**File:** `tests/ExtraTime.Domain.Tests/Entities/UserTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Register_WithValidData_CreatesUser` | Valid registration creates user with hashed password |
| `Register_WithInvalidEmail_ThrowsException` | Invalid email format rejected |
| `UpdateLastLogin_SetsLastLoginAt` | Last login timestamp updated |
| `AddRefreshToken_AddsTokenToCollection` | Token added successfully |
| `RevokeRefreshToken_MarksTokenAsRevoked` | Token revoked with timestamp |
| `UpdateProfile_UpdatesUsernameAndEmail` | Profile updates persist |
| `MarkAsBot_SetsIsBotAndBotId` | User marked as bot correctly |
| `Register_DuplicateEmail_NotAllowed` | Domain rule enforced |

#### Bot Entity Tests
**File:** `tests/ExtraTime.Domain.Tests/Entities/BotTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Create_WithValidData_CreatesBot` | Bot creation with strategy |
| `Activate_SetsIsActiveToTrue` | Bot activation |
| `Deactivate_SetsIsActiveToFalse` | Bot deactivation |
| `RecordBetPlaced_UpdatesLastBetPlacedAt` | Timestamp tracking |
| `UpdateConfiguration_UpdatesStrategyConfig` | Config changes persisted |
| `UpdateDetails_UpdatesNameAndAvatar` | Bot details update |
| `Create_WithInvalidStrategy_ThrowsException` | Invalid strategy rejected |

#### BackgroundJob Entity Tests
**File:** `tests/ExtraTime.Domain.Tests/Entities/BackgroundJobTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Create_WithValidData_CreatesJob` | Job creation with type and payload |
| `MarkAsProcessing_SetsStartedAtAndStatus` | Processing state transition |
| `MarkAsCompleted_SetsCompletedAtAndResult` | Completion with result |
| `MarkAsFailed_SetsErrorAndStatus` | Failure with error message |
| `Cancel_SetsStatusToCancelled` | Job cancellation |
| `Retry_IncrementsRetryCountAndResetsStatus` | Retry logic |
| `MarkAsFailed_MaxRetriesReached_NoRetry` | Max retry enforcement |
| `Create_WithScheduledAt_SetsScheduledTime` | Delayed job scheduling |

#### RefreshToken Entity Tests
**File:** `tests/ExtraTime.Domain.Tests/Entities/RefreshTokenTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Create_WithValidData_CreatesToken` | Token creation with expiration |
| `Revoke_SetsRevokedAt` | Token revocation |
| `IsValidForUse_UnexpiredAndNotRevoked_ReturnsTrue` | Valid token check |
| `IsValidForUse_Expired_ReturnsFalse` | Expired token rejected |
| `IsValidForUse_Revoked_ReturnsFalse` | Revoked token rejected |
| `ReplaceWith_CreatesNewTokenAndRevokesOld` | Token rotation |
| `Create_WithDifferentExpiration_SetsCorrectDate` | Custom expiration |

#### LeagueStanding Entity Tests
**File:** `tests/ExtraTime.Domain.Tests/Entities/LeagueStandingTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Create_WithValidData_CreatesStanding` | Standing creation |
| `ApplyBetResult_ExactMatch_UpdatesStatsAndStreak` | Exact match scoring |
| `ApplyBetResult_CorrectResult_UpdatesStatsAndStreak` | Correct result scoring |
| `ApplyBetResult_WrongResult_BreaksStreak` | Streak break on wrong |
| `Reset_ClearsAllStatsAndPoints` | Standing reset |
| `ApplyBetResult_MultipleBets_AccumulatesCorrectly` | Multiple results |

#### BetResult Entity Tests
**File:** `tests/ExtraTime.Domain.Tests/Entities/BetResultTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Create_WithValidData_CreatesResult` | Result creation |
| `CalculateFrom_ExactMatch_SetsIsExactMatch` | Exact calculation |
| `CalculateFrom_CorrectResult_SetsIsCorrectResult` | Correct result calc |
| `CalculateFrom_WrongResult_SetsZeroPoints` | Zero points for wrong |
| `Update_RecalculatesWithNewActualScore` | Recalculation allowed |
| `CalculateFrom_NullBet_ThrowsException` | Null check |

#### LeagueMember Entity Tests
**File:** `tests/ExtraTime.Domain.Tests/Entities/LeagueMemberTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Create_WithValidData_CreatesMember` | Member creation |
| `ChangeRole_ToAdmin_SetsRole` | Role change to admin |
| `ChangeRole_ToMember_SetsRole` | Role change to member |
| `IsOwner_WhenOwner_ReturnsTrue` | Owner check |
| `IsOwner_WhenNotOwner_ReturnsFalse` | Non-owner check |
| `Create_WithDuplicateUser_ThrowsException` | Duplicate prevention |

#### Match Entity Tests (Extend Existing)
**File:** `tests/ExtraTime.Domain.Tests/Entities/MatchTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `UpdateScore_UpdatesHomeAndAwayScore` | Score update |
| `UpdateMetadata_UpdatesVenueAndStatus` | Metadata update |
| `SyncDetails_UpdatesExternalData` | External sync |
| `IsOpenForBetting_MatchStarted_ReturnsFalse` | Betting closed after start |
| `IsOpenForBetting_MatchFinished_ReturnsFalse` | Betting closed after finish |
| `UpdateStatus_FromScheduledToLive_SetsCorrectly` | Status transition |
| `UpdateStatus_FromLiveToFinished_SetsCorrectly` | Match completion |

#### Team Entity Tests
**File:** `tests/ExtraTime.Domain.Tests/Entities/TeamTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Create_WithValidData_CreatesTeam` | Team creation |
| `UpdateDetails_UpdatesNameAndLogo` | Details update |
| `RecordSync_UpdatesLastSyncedAt` | Sync tracking |

#### Competition Entity Tests
**File:** `tests/ExtraTime.Domain.Tests/Entities/CompetitionTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Create_WithValidData_CreatesCompetition` | Competition creation |
| `UpdateDetails_UpdatesNameAndCountry` | Details update |
| `RecordSync_UpdatesLastSyncedAt` | Sync tracking |
| `UpdateCurrentSeason_UpdatesSeasonDates` | Season update |

#### TeamFormCache Entity Tests
**File:** `tests/ExtraTime.Domain.Tests/Entities/TeamFormCacheTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `GetFormScore_WithPositiveRecord_ReturnsHigherScore` | Form calculation |
| `GetHomeStrength_HomeWins_ReturnsHigherStrength` | Home strength calc |
| `GetAwayStrength_AwayWins_ReturnsHigherStrength` | Away strength calc |
| `GetAttackStrength_MoreGoals_ReturnsHigherScore` | Attack strength |
| `GetDefenseStrength_FewerConceded_ReturnsHigherScore` | Defense strength |
| `Calculate_WithInsufficientData_HandlesGracefully` | Edge case handling |

### 1.2 Value Object Tests

**File:** `tests/ExtraTime.Domain.Tests/ValueObjects/ValueObjectTests.cs`

| Value Object | Tests |
|--------------|-------|
| **Email** | Valid email creates, invalid throws, equality |
| **Username** | Valid username (3-50 chars), too short/long throws, equality |
| **InviteCode** | Generates correct length, no ambiguous chars, uniqueness |
| **MatchScore** | Valid scores create, negative throws, equality |
| **Score** | Valid score, negative throws, equality |
| **BettingDeadline** | Calculates correctly from match date, expired check |
| **CompetitionFilter** | Empty allows all, specific IDs filter correctly |
| **StatsAnalystConfig** | Valid config creates, invalid weights throw |

---

## Phase 2: Bot Betting Strategy Tests (Priority: Critical)

**Timeline:** 3-4 days
**Goal:** Test ALL bot strategies and supporting services

### 2.1 Strategy Tests

#### Random Strategy
**File:** `tests/ExtraTime.UnitTests/Application/Services/BotStrategies/RandomStrategyTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Predict_ReturnsValidScore` | Returns score in reasonable range (0-5) |
| `Predict_MultipleCalls_ReturnsDifferentScores` | Randomness verified |
| `Predict_WithAnyMatch_ReturnsConsistentFormat` | Always valid tuple |
| `StrategyType_ReturnsRandom` | Correct type identifier |

#### Home Favorer Strategy
**File:** `tests/ExtraTime.UnitTests/Application/Services/BotStrategies/HomeFavorerStrategyTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Predict_HomeTeamFavored_ReturnsHigherHomeScore` | Home > Away |
| `Predict_WithStrongHomeAdvantage_BiasIncreases` | Bias scales with advantage |
| `Predict_WithNeutralVenue_MinimalBias` | Neutral = small bias |
| `StrategyType_ReturnsHomeFavorer` | Correct type identifier |

#### Draw Predictor Strategy
**File:** `tests/ExtraTime.UnitTests/Application/Services/BotStrategies/DrawPredictorStrategyTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Predict_CloselyMatchedTeams_ReturnsEqualOrNearEqual` | Close scores |
| `Predict_WithHistoricalDraws_IncreasesDrawProbability` | History influences |
| `Predict_WithTightlyContested_MinimalScoreDifference` | Low variance |
| `StrategyType_ReturnsDrawPredictor` | Correct type identifier |

#### Underdog Supporter Strategy
**File:** `tests/ExtraTime.UnitTests/Application/Services/BotStrategies/UnderdogSupporterStrategyTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Predict_IdentifiesUnderdog_FavorsAwayIfAwayWeaker` | Away underdog favored |
| `Predict_IdentifiesUnderdog_FavorsHomeIfHomeWeaker` | Home underdog favored |
| `Predict_WithClearFavorite_StrongUpsetBias` | Upset prediction |
| `StrategyType_ReturnsUnderdogSupporter` | Correct type identifier |

#### High Scorer Strategy
**File:** `tests/ExtraTime.UnitTests/Application/Services/BotStrategies/HighScorerStrategyTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Predict_ReturnsHighTotalGoals` | Total goals > 2.5 typically |
| `Predict_WithAttackingTeams_EvenHigherScores` | Attacking bonus |
| `Predict_ReturnsMinimumThreshold` | Always above minimum |
| `StrategyType_ReturnsHighScorer` | Correct type identifier |

#### Stats Analyst Strategy
**File:** `tests/ExtraTime.UnitTests/Application/Services/BotStrategies/StatsAnalystStrategyTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Predict_WithFormData_UsesRecentPerformance` | Form influences result |
| `Predict_WithHomeAdvantage_ConsidersHomeStrength` | Home weight applied |
| `Predict_WithStreak_DetectsMomentum` | Streak detection |
| `Predict_WithHighStakes_AdjustsForImportance` | Stakes awareness |
| `Predict_WithMissingData_FallsBackToDefaults` | Graceful degradation |
| `Configure_WithCustomWeights_UsesCustomWeights` | Configurable weights |
| `StrategyType_ReturnsStatsAnalyst` | Correct type identifier |

### 2.2 Supporting Service Tests

#### Bot Strategy Factory
**File:** `tests/ExtraTime.UnitTests/Application/Services/BotStrategies/BotStrategyFactoryTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Create_RandomStrategy_ReturnsRandomStrategyInstance` | Factory creates correct type |
| `Create_HomeFavorerStrategy_ReturnsHomeFavorerInstance` | Factory creates correct type |
| `Create_AllStrategies_ReturnsCorrectTypes` | All strategies creatable |
| `Create_InvalidStrategy_ThrowsNotSupportedException` | Invalid type rejected |
| `Create_WithConfiguration_AppliesConfiguration` | Config passed through |
| `GetAvailableStrategies_ReturnsAllStrategyTypes` | Lists all strategies |

#### Team Form Calculator
**File:** `tests/ExtraTime.UnitTests/Application/Services/BotStrategies/TeamFormCalculatorTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `CalculateForm_WithWinStreak_HighFormScore` | Streak = high form |
| `CalculateForm_WithLossStreak_LowFormScore` | Loss streak = low form |
| `CalculateForm_WithMixedResults_ModerateScore` | Mixed = moderate |
| `CalculateHomeStrength_MoreHomeWins_HigherStrength` | Home wins boost |
| `CalculateAwayStrength_MoreAwayWins_HigherStrength` | Away wins boost |
| `CalculateAttackStrength_MoreGoals_HigherStrength` | Goals = attack |
| `CalculateDefenseStrength_FewerConceded_HigherStrength` | Clean sheets |
| `Calculate_WithInsufficientMatches_HandlesGracefully` | Edge cases |
| `Calculate_WithNoMatches_ReturnsDefaultValues` | Empty handling |

#### Match Analysis
**File:** `tests/ExtraTime.UnitTests/Application/Services/BotStrategies/MatchAnalysisTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Analyze_CalculatesWinProbability` | Win % calculation |
| `Analyze_CalculatesDrawProbability` | Draw % calculation |
| `Analyze_DetectsHighStakes_Match` | Important match flag |
| `Analyze_ComparesHeadToHeadHistory` | H2H analysis |
| `Analyze_WithMissingData_ProvidesBestEffort` | Graceful handling |

### 2.3 Bot Betting Service Tests

**File:** `tests/ExtraTime.UnitTests/Application/Services/BotBettingServiceTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `PlaceBetsForLeague_WithActiveBots_PlacesBets` | Bets placed for bots |
| `PlaceBetsForLeague_NoActiveBots_NoBetsPlaced` | Empty handling |
| `PlaceBetsForLeague_MatchClosed_SkipsMatch` | Closed match handling |
| `PlaceBetsForLeague_AlreadyPlacedBet_Skips` | Duplicate prevention |
| `PlaceBetsForLeague_UsesCorrectStrategy` | Strategy selection |
| `PlaceBetsForLeague_LogsErrors_ContinuesProcessing` | Error resilience |
| `GetMatchesNeedingBotBets_ReturnsOnlyOpenMatches` | Match filtering |
| `ShouldPlaceBet_RespectsDeadline` | Deadline enforcement |

---

## Phase 3: API Endpoint Tests (Priority: Critical)

**Timeline:** 4-5 days
**Goal:** Complete API test coverage for all endpoint groups

### 3.1 Bet Endpoints Tests

**File:** `tests/ExtraTime.API.Tests/Endpoints/BetEndpointsTests.cs`

| Test Method | Endpoint | Expected |
|-------------|----------|----------|
| `PlaceBet_ValidRequest_ReturnsOk` | POST /api/leagues/{id}/bets | 200 OK + bet data |
| `PlaceBet_NotMember_ReturnsForbidden` | POST /api/leagues/{id}/bets | 403 Forbidden |
| `PlaceBet_MatchStarted_ReturnsBadRequest` | POST /api/leagues/{id}/bets | 400 Bad Request |
| `PlaceBet_InvalidScore_ReturnsValidationError` | POST /api/leagues/{id}/bets | 400 Validation |
| `PlaceBet_DeadlinePassed_ReturnsBadRequest` | POST /api/leagues/{id}/bets | 400 Bad Request |
| `DeleteBet_ValidRequest_ReturnsNoContent` | DELETE /api/leagues/{id}/bets/{betId} | 204 No Content |
| `DeleteBet_NotOwner_ReturnsForbidden` | DELETE /api/leagues/{id}/bets/{betId} | 403 Forbidden |
| `DeleteBet_MatchStarted_ReturnsBadRequest` | DELETE /api/leagues/{id}/bets/{betId} | 400 Bad Request |
| `GetMyBets_Authenticated_ReturnsBets` | GET /api/leagues/{id}/bets/my | 200 OK + list |
| `GetMyBets_NotMember_ReturnsForbidden` | GET /api/leagues/{id}/bets/my | 403 Forbidden |
| `GetMatchBets_Authenticated_ReturnsBets` | GET /api/leagues/{id}/matches/{matchId}/bets | 200 OK + list |
| `GetMatchBets_BeforeDeadline_ReturnsEmpty` | GET /api/leagues/{id}/matches/{matchId}/bets | 200 OK + empty |
| `GetMatchBets_NotMember_ReturnsForbidden` | GET /api/leagues/{id}/matches/{matchId}/bets | 403 Forbidden |
| `GetLeagueStandings_ReturnsStandings` | GET /api/leagues/{id}/standings | 200 OK + standings |
| `GetLeagueStandings_NotMember_ReturnsForbidden` | GET /api/leagues/{id}/standings | 403 Forbidden |
| `GetUserStats_ReturnsStats` | GET /api/leagues/{id}/users/{userId}/stats | 200 OK + stats |
| `GetUserStats_NotMember_ReturnsForbidden` | GET /api/leagues/{id}/users/{userId}/stats | 403 Forbidden |

### 3.2 Football Endpoints Tests

**File:** `tests/ExtraTime.API.Tests/Endpoints/FootballEndpointsTests.cs`

| Test Method | Endpoint | Expected |
|-------------|----------|----------|
| `GetCompetitions_Anonymous_ReturnsList` | GET /api/competitions | 200 OK + list |
| `GetCompetitions_WithData_ReturnsPopulatedList` | GET /api/competitions | 200 OK + data |
| `GetMatches_Anonymous_ReturnsList` | GET /api/matches | 200 OK + list |
| `GetMatches_WithDateFilter_ReturnsFiltered` | GET /api/matches?date=... | 200 OK + filtered |
| `GetMatches_WithCompetitionFilter_ReturnsFiltered` | GET /api/matches?competition=... | 200 OK + filtered |
| `GetMatches_WithStatusFilter_ReturnsFiltered` | GET /api/matches?status=... | 200 OK + filtered |
| `GetMatchById_Existing_ReturnsMatch` | GET /api/matches/{id} | 200 OK + match |
| `GetMatchById_NotFound_Returns404` | GET /api/matches/{id} | 404 Not Found |
| `GetMatchById_InvalidId_Returns400` | GET /api/matches/invalid | 400 Bad Request |

### 3.3 Admin Endpoints Tests

**File:** `tests/ExtraTime.API.Tests/Endpoints/AdminEndpointsTests.cs`

| Test Method | Endpoint | Expected |
|-------------|----------|----------|
| `GetJobs_Admin_ReturnsJobs` | GET /api/admin/jobs | 200 OK + list |
| `GetJobs_NonAdmin_ReturnsForbidden` | GET /api/admin/jobs | 403 Forbidden |
| `GetJobs_Unauthenticated_ReturnsUnauthorized` | GET /api/admin/jobs | 401 Unauthorized |
| `GetJobStats_Admin_ReturnsStats` | GET /api/admin/jobs/stats | 200 OK + stats |
| `GetJobById_Admin_ReturnsJob` | GET /api/admin/jobs/{id} | 200 OK + job |
| `GetJobById_NotFound_Returns404` | GET /api/admin/jobs/{id} | 404 Not Found |
| `RetryJob_Admin_Success_ReturnsOk` | POST /api/admin/jobs/{id}/retry | 200 OK |
| `RetryJob_JobNotFailed_ReturnsBadRequest` | POST /api/admin/jobs/{id}/retry | 400 Bad Request |
| `RetryJob_NotFound_Returns404` | POST /api/admin/jobs/{id}/retry | 404 Not Found |
| `CancelJob_Admin_Success_ReturnsOk` | POST /api/admin/jobs/{id}/cancel | 200 OK |
| `CancelJob_JobNotPending_ReturnsBadRequest` | POST /api/admin/jobs/{id}/cancel | 400 Bad Request |
| `CancelJob_NotFound_Returns404` | POST /api/admin/jobs/{id}/cancel | 404 Not Found |

### 3.4 Bots Endpoints Tests

**File:** `tests/ExtraTime.API.Tests/Endpoints/BotsEndpointsTests.cs`

| Test Method | Endpoint | Expected |
|-------------|----------|----------|
| `GetBots_Authenticated_ReturnsBots` | GET /api/bots | 200 OK + list |
| `GetBots_Unauthenticated_ReturnsUnauthorized` | GET /api/bots | 401 Unauthorized |
| `GetLeagueBots_Member_ReturnsBots` | GET /api/leagues/{id}/bots | 200 OK + list |
| `GetLeagueBots_NotMember_ReturnsForbidden` | GET /api/leagues/{id}/bots | 403 Forbidden |
| `AddBotToLeague_Owner_ReturnsOk` | POST /api/leagues/{id}/bots | 200 OK |
| `AddBotToLeague_NonOwner_ReturnsForbidden` | POST /api/leagues/{id}/bots | 403 Forbidden |
| `AddBotToLeague_InvalidBot_ReturnsNotFound` | POST /api/leagues/{id}/bots | 404 Not Found |
| `RemoveBotFromLeague_Owner_ReturnsNoContent` | DELETE /api/leagues/{id}/bots/{botId} | 204 No Content |
| `RemoveBotFromLeague_NonOwner_ReturnsForbidden` | DELETE /api/leagues/{id}/bots/{botId} | 403 Forbidden |
| `CreateBot_Admin_ReturnsCreated` | POST /api/admin/bots | 201 Created |
| `CreateBot_NonAdmin_ReturnsForbidden` | POST /api/admin/bots | 403 Forbidden |

### 3.5 Admin Football Sync Endpoints Tests

**File:** `tests/ExtraTime.API.Tests/Endpoints/FootballSyncEndpointsTests.cs`

| Test Method | Endpoint | Expected |
|-------------|----------|----------|
| `SyncCompetitions_Admin_ReturnsAccepted` | POST /api/admin/sync/competitions | 202 Accepted |
| `SyncCompetitions_NonAdmin_ReturnsForbidden` | POST /api/admin/sync/competitions | 403 Forbidden |
| `SyncMatches_Admin_ReturnsAccepted` | POST /api/admin/sync/matches | 202 Accepted |
| `SyncMatches_NonAdmin_ReturnsForbidden` | POST /api/admin/sync/matches | 202 Accepted |
| `SyncLive_Admin_ReturnsAccepted` | POST /api/admin/sync/live | 202 Accepted |
| `SyncLive_NonAdmin_ReturnsForbidden` | POST /api/admin/sync/live | 403 Forbidden |

---

## Phase 4: Infrastructure Service Tests (Priority: High)

**Timeline:** 3-4 days
**Goal:** Test critical infrastructure services

### 4.1 Football Data Service Tests

**File:** `tests/ExtraTime.UnitTests/Infrastructure/Services/FootballDataServiceTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `GetCompetitionsAsync_ReturnsMappedCompetitions` | Maps external to domain |
| `GetMatchesAsync_ReturnsMappedMatches` | Maps matches correctly |
| `GetMatchByIdAsync_Existing_ReturnsMatch` | Fetches by ID |
| `GetMatchByIdAsync_NotFound_ReturnsNull` | Null on missing |
| `GetTeamsAsync_ReturnsMappedTeams` | Team mapping |
| `SyncCompetitions_CallsApi_PersistsToDb` | Full sync flow |
| `SyncMatches_CallsApi_PersistsToDb` | Match sync flow |
| `RateLimiting_RespectsApiLimits` | Rate limiting works |
| `RateLimiting_Handles429Retry` | Retry on rate limit |
| `ApiError_LogsError_ThrowsException` | Error handling |

### 4.2 Football Sync Service Tests

**File:** `tests/ExtraTime.UnitTests/Infrastructure/Services/FootballSyncServiceTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `SyncAllAsync_PerformsFullSync` | Full sync orchestration |
| `SyncCompetitions_OnlyNewCompetitions` | Incremental sync |
| `SyncMatches_WithinDateRange` | Date filtering |
| `SyncLive_OnlyLiveMatches` | Live filter |
| `FinishedMatches_TriggersBetCalculation` | Job enqueueing |
| `NoChanges_NoUnnecessaryUpdates` | Idempotency |
| `ApiFailure_LogsContinues` | Resilience |

### 4.3 Background Services Tests

#### Football Sync Hosted Service
**File:** `tests/ExtraTime.UnitTests/Infrastructure/Background/FootballSyncHostedServiceTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `StartAsync_TriggersInitialSync` | Initial sync on start |
| `ExecuteAsync_PeriodicSync` | Periodic execution |
| `StopAsync_GracefulShutdown` | Clean shutdown |
| `SyncFailure_LogsError_Continues` | Error resilience |

#### Bot Betting Background Service
**File:** `tests/ExtraTime.UnitTests/Infrastructure/Background/BotBettingBackgroundServiceTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `ExecuteAsync_PlacesBotBets` | Bet placement |
| `ExecuteAsync_NoActiveBots_NoAction` | Empty handling |
| `ExecuteAsync_OnlyDuringActiveHours` | Time window |
| `BetPlacementFailure_LogsError_Continues` | Error handling |
| `StopAsync_GracefulShutdown` | Clean shutdown |

#### Form Cache Background Service
**File:** `tests/ExtraTime.UnitTests/Infrastructure/Background/FormCacheBackgroundServiceTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `ExecuteAsync_UpdatesFormCache` | Cache updates |
| `ExecuteAsync_OnlyOnSchedule` | Scheduling |
| `UpdateFailure_LogsError_Continues` | Error resilience |

### 4.4 Supporting Infrastructure Tests

#### Current User Service
**File:** `tests/ExtraTime.UnitTests/Infrastructure/Services/CurrentUserServiceTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `UserId_Authenticated_ReturnsUserId` | Gets user ID from claims |
| `UserId_NotAuthenticated_ReturnsNull` | Null when no auth |
| `IsAdmin_AdminRole_ReturnsTrue` | Admin detection |
| `IsAdmin_UserRole_ReturnsFalse` | Non-admin detection |
| `Email_Authenticated_ReturnsEmail` | Email from claims |

#### In-Memory Job Dispatcher
**File:** `tests/ExtraTime.UnitTests/Infrastructure/Background/InMemoryJobDispatcherTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Enqueue_CreatesJobRecord` | Job persistence |
| `Enqueue_ScheduledAt_SetsScheduledTime` | Delayed jobs |
| `Enqueue_WithCorrelationId_SetsId` | Correlation tracking |

---

## Phase 5: Integration Test Enhancements (Priority: Medium)

**Timeline:** 2-3 days
**Goal:** Fill gaps in integration test coverage

### 5.1 Missing Integration Tests

#### Update League Command Integration Tests
**File:** `tests/ExtraTime.NewIntegrationTests/Leagues/UpdateLeagueTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `UpdateLeague_ValidData_UpdatesLeague` | Update persists |
| `UpdateLeague_NotOwner_ReturnsForbidden` | Authorization |
| `UpdateLeague_InvalidName_ReturnsValidationError` | Validation |

#### Regenerate Invite Code Integration Tests
**File:** `tests/ExtraTime.NewIntegrationTests/Leagues/RegenerateInviteCodeTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `RegenerateInviteCode_GeneratesNewCode` | New code created |
| `RegenerateInviteCode_NotOwner_ReturnsForbidden` | Authorization |
| `RegenerateInviteCode_OldCodeInvalid` | Old code rejected |

#### Calculate Bet Results Integration Tests (Expand)
**File:** `tests/ExtraTime.IntegrationTests/Application/Features/Bets/CalculateBetResultsIntegrationTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `Calculate_ExactMatch_AwardsExactPoints` | Exact scoring |
| `Calculate_CorrectResult_AwardsResultPoints` | Result scoring |
| `Calculate_WrongResult_ZeroPoints` | Zero for wrong |
| `Calculate_CustomScoring_UsesLeagueRules` | Custom rules |
| `Calculate_MultipleBets_AllProcessed` | Batch processing |

### 5.2 End-to-End Flow Tests

#### Football Sync to Bet Calculation Flow
**File:** `tests/ExtraTime.NewIntegrationTests/Flows/FootballSyncToBetCalculationFlowTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `MatchSyncs_ThenFinishes_BetsCalculated` | Full flow |
| `LiveSync_UpdatesMatch_BetsRecalculated` | Live updates |

#### Bot Betting End-to-End Flow
**File:** `tests/ExtraTime.NewIntegrationTests/Flows/BotBettingFlowTests.cs`

| Test Method | Scenario |
|-------------|----------|
| `BotAddedToLeague_BetsPlacedAutomatically` | Auto betting |
| `MultipleBots_AllPlaceBets` | Multiple bots |
| `BotRemoved_NoLongerPlacesBets` | Removal stops betting |

---

## Phase 6: EF Core Configuration Tests (Priority: Low)

**Timeline:** 1-2 days
**Goal:** Verify database configurations and constraints

### 6.1 Configuration Validation Tests

**File:** `tests/ExtraTime.IntegrationTests/Infrastructure/Data/ConfigurationTests.cs`

| Configuration | Tests |
|---------------|-------|
| **LeagueConfiguration** | Table name, column types, indexes |
| **UserConfiguration** | Email unique, username unique |
| **BetConfiguration** | Composite indexes, foreign keys |
| **MatchConfiguration** | Status enum stored as string |
| **BotConfiguration** | Strategy enum stored as string |
| **All Configurations** | Relationships, cascade delete |

### 6.2 Constraint Tests

| Test Method | Scenario |
|-------------|----------|
| `League_InviteCode_UniqueConstraint` | Duplicate codes rejected |
| `User_Email_UniqueConstraint` | Duplicate emails rejected |
| `Bet_LeagueMatchUser_UniqueConstraint` | Duplicate bets rejected |
| `ForeignKey_DeleteBehavior` | Cascade vs restrict |

---

## Implementation Timeline

| Phase | Duration | Focus | Deliverables |
|-------|----------|-------|--------------|
| **Phase 1** | 2-3 days | Domain Tests | 12+ entity test files, value object tests |
| **Phase 2** | 3-4 days | Bot Strategies | 6 strategy tests, factory, calculator, service |
| **Phase 3** | 4-5 days | API Tests | 5+ endpoint test files, 80+ endpoint tests |
| **Phase 4** | 3-4 days | Infrastructure | Football services, background services |
| **Phase 5** | 2-3 days | Integration Gaps | Missing command tests, E2E flows |
| **Phase 6** | 1-2 days | EF Config Tests | Configuration validation |
| **Total** | **15-21 days** | | **~300-350 new tests** |

---

## Test Count Target

### Current vs Target

| Layer | Current | Target | Gap |
|-------|---------|--------|-----|
| Domain Tests | ~30 | ~150 | +120 |
| Unit Tests | ~200+ | ~300+ | +100 |
| Integration Tests | ~150+ | ~200+ | +50 |
| API Tests | ~10 | ~100 | +90 |
| **Total** | **~390** | **~750** | **+360** |

### Coverage Targets

| Component | Current | Target |
|-----------|---------|--------|
| Domain Entities | 20% | 95% |
| Value Objects | 0% | 100% |
| Bot Strategies | 0% | 100% |
| Handlers | 75% | 90% |
| Validators | 85% | 100% |
| API Endpoints | 15% | 85% |
| Infrastructure | 40% | 70% |
| **Overall** | **65%** | **85%** |

---

## New Files to Create

### Domain Tests (15 files)
```
tests/ExtraTime.Domain.Tests/Entities/
├── UserTests.cs
├── BotTests.cs
├── BackgroundJobTests.cs
├── RefreshTokenTests.cs
├── LeagueStandingTests.cs
├── BetResultTests.cs
├── LeagueMemberTests.cs
├── MatchTests.cs (extend)
├── TeamTests.cs
├── CompetitionTests.cs
├── TeamFormCacheTests.cs
└── ValueObjects/
    └── ValueObjectTests.cs
```

### Strategy Tests (9 files)
```
tests/ExtraTime.UnitTests/Application/Services/BotStrategies/
├── RandomStrategyTests.cs
├── HomeFavorerStrategyTests.cs
├── DrawPredictorStrategyTests.cs
├── UnderdogSupporterStrategyTests.cs
├── HighScorerStrategyTests.cs
├── StatsAnalystStrategyTests.cs
├── BotStrategyFactoryTests.cs
├── TeamFormCalculatorTests.cs
├── MatchAnalysisTests.cs
└── BotBettingServiceTests.cs
```

### API Tests (6 files)
```
tests/ExtraTime.API.Tests/Endpoints/
├── BetEndpointsTests.cs
├── FootballEndpointsTests.cs
├── AdminEndpointsTests.cs
├── BotsEndpointsTests.cs
├── FootballSyncEndpointsTests.cs
└── HealthEndpointsTests.cs
```

### Infrastructure Tests (8 files)
```
tests/ExtraTime.UnitTests/Infrastructure/
├── Services/
│   ├── FootballDataServiceTests.cs
│   ├── FootballSyncServiceTests.cs
│   └── CurrentUserServiceTests.cs
└── Background/
    ├── FootballSyncHostedServiceTests.cs
    ├── BotBettingBackgroundServiceTests.cs
    ├── FormCacheBackgroundServiceTests.cs
    └── InMemoryJobDispatcherTests.cs
```

### Integration Tests (5 files)
```
tests/ExtraTime.NewIntegrationTests/
├── Leagues/
│   ├── UpdateLeagueTests.cs
│   └── RegenerateInviteCodeTests.cs
└── Flows/
    ├── FootballSyncToBetCalculationFlowTests.cs
    └── BotBettingFlowTests.cs
```

### EF Configuration Tests (1 file)
```
tests/ExtraTime.IntegrationTests/Infrastructure/Data/
└── ConfigurationTests.cs
```

---

## Success Criteria

1. **All new tests pass** - Zero failing tests before merging
2. **Coverage targets met** - 85% overall coverage minimum
3. **CI/CD integration** - Tests run automatically in pipeline
4. **Test reliability** - Flaky tests < 1% (no random failures)
5. **Test speed** - Unit tests < 100ms each, integration < 5s each
6. **Documentation** - All test files follow naming conventions

---

## Verification Commands

```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test projects
dotnet test tests/ExtraTime.Domain.Tests
dotnet test tests/ExtraTime.UnitTests
dotnet test tests/ExtraTime.IntegrationTests
dotnet test tests/ExtraTime.API.Tests

# Run specific test class
dotnet test --filter "FullyQualifiedName~UserTests"

# Check coverage report
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet tool run reportgenerator -reports:./TestResults/**/coverage.cobertura.xml -targetdir:./CoverageReport
```

---

## Notes

### TUnit Framework Reminders
- All assertions are async: `await Assert.That(value).IsEqualTo(expected)`
- Use `[Test]` attribute for test methods
- Use `[Before(Test)]` and `[After(Test)]` for setup/cleanup
- No `IAsyncLifetime` - use TUnit lifecycle attributes

### Naming Conventions
- Test files: `{Component}Tests.cs`
- Test methods: `{Action}_{Condition}_{ExpectedResult}`
- Arrange/Act/Assert comments for clarity

### Mocking Guidelines
- Use NSubstitute for mocking
- Mock external dependencies only
- Don't mock domain entities
- Use Testcontainers for real database in integration tests
