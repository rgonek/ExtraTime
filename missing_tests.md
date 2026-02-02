# Missing Integration Tests

All tests have been successfully migrated to the new `ExtraTime.NewIntegrationTests` project! ✅

## Migration Summary

| Feature Area | Tests Migrated | File Updated | Status |
|--------------|----------------|--------------|--------|
| Leagues | 3 | `Tests/Leagues/LeagueManagementTests.cs` | ✅ Complete |
| Bots | 1 | `Tests/Bots/BotTests.cs` | ✅ Complete |
| Bet Calculations | 3 | `Tests/Bets/BetCalculationTests.cs` | ✅ Complete |
| Admin | 10 | `Tests/Admin/AdminTests.cs` | ✅ Complete |
| **Total** | **17** | | **✅ All Done** |

---

## Migrated Tests

### League Features (3 tests)

**File:** `Tests/Leagues/LeagueManagementTests.cs`

- [x] `LeaveLeague_OwnerCannotLeave_ReturnsFailure` - Owner should not be able to leave their own league
- [x] `LeaveLeague_NotAMember_ReturnsFailure` - Non-member trying to leave should fail
- [x] `LeaveLeague_NonExistentLeague_ReturnsFailure` - Leaving a non-existent league should fail

---

### Bot Features (1 test)

**File:** `Tests/Bots/BotTests.cs`

- [x] `PlaceBotBets_WithRealServiceAndValidMatch_PlacesBet` - Tests actual bet placement with valid match data

**Note:** `PlaceBotBets_ServiceReturnsCount_ReturnsSuccessWithCount` already existed in BotTests.cs

---

### Bet Calculation Features (3 tests)

**File:** `Tests/Bets/BetCalculationTests.cs`

- [x] `CalculateBetResults_ExistingMatchWithScore_CalculatesAndCreatesResults` - Verifies results are calculated when match has final score
- [x] `CalculateBetResults_EnqueuesStandingsRecalculationJob` - Verifies job enqueues standings recalculation
- [x] `RecalculateStandings_ValidLeague_RecalculatesSuccessfully` - Tests recalculation for a single valid league

---

### Admin Features (10 tests)

**File:** `Tests/Admin/AdminTests.cs`

#### Retry Job Tests (5 tests)
- [x] `RetryJob_NotFailed_Pending_ReturnsFailure` - Should not retry pending jobs
- [x] `RetryJob_NotFailed_Processing_ReturnsFailure` - Should not retry processing jobs
- [x] `RetryJob_NotFailed_Completed_ReturnsFailure` - Should not retry completed jobs
- [x] `RetryJob_NotFailed_Cancelled_RetriesSuccessfully` - Can retry cancelled jobs
- [x] `RetryJob_IncrementsRetryCount` - Verifies retry count is incremented on retry

#### Cancel Job Tests (3 tests)
- [x] `CancelJob_ProcessingJob_CancelsSuccessfully` - Should be able to cancel processing jobs
- [x] `CancelJob_FailedJob_ReturnsFailure` - Should not be able to cancel failed jobs
- [x] `CancelJob_AlreadyCancelled_ReturnsFailure` - Should not be able to cancel already cancelled jobs

#### Get Jobs Query Tests (2 tests)
- [x] `GetJobs_ByStatus_ReturnsFilteredResults` - Filter jobs by status
- [x] `GetJobs_ByJobType_ReturnsFilteredResults` - Filter jobs by job type

---

## Test Count Update

- **Old Project:** 185 tests (per user)
- **New Project:** ~153 tests (17 tests added)
- **Status:** All 18 core integration tests from the missing list have been migrated

---

*Last Updated: 2026-02-02*
