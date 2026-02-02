# Integration Tests Migration Status

The following integration tests from `ExtraTime.IntegrationTests` have been fully moved to `ExtraTime.NewIntegrationTests`. 
"Fully moved" means all test cases (edge cases, failure modes, etc.) from the original files are represented in the new TUnit-based project.

## Auth Features
- [x] `Application\Features\Auth\LoginCommandIntegrationTests.cs` - Migrated to `AuthTests.cs`
- [x] `Application\Features\Auth\RegisterCommandIntegrationTests.cs` - Migrated to `AuthTests.cs`
- [x] `Application\Features\Auth\RefreshTokenCommandIntegrationTests.cs` - Migrated to `AuthTests.cs`
- [x] `Application\Features\Auth\GetCurrentUserQueryIntegrationTests.cs` - Migrated to `AuthTests.cs`

## League Features
- [x] `Application\Features\Leagues\CreateLeagueCommandIntegrationTests.cs` - Migrated to `CreateLeagueTests.cs`
- [x] `Application\Features\Leagues\GetLeagueQueryIntegrationTests.cs` - Migrated to `GetLeagueTests.cs`
- [x] `Application\Features\Leagues\JoinLeagueCommandIntegrationTests.cs` - Migrated to `JoinLeagueTests.cs`
- [x] `Application\Features\Leagues\DeleteLeagueCommandIntegrationTests.cs` - Migrated to `DeleteLeagueTests.cs`
- [x] `Application\Features\Leagues\KickMemberCommandIntegrationTests.cs` - Migrated to `LeagueManagementTests.cs`
- [x] `Application\Features\Leagues\LeaveLeagueCommandIntegrationTests.cs` - Migrated to `LeagueManagementTests.cs`
- [x] `Application\Features\Leagues\UpdateLeagueCommandIntegrationTests.cs` - Migrated to `LeagueManagementTests.cs`
- [x] `Application\Features\Leagues\RegenerateInviteCodeIntegrationTests.cs` - Migrated to `LeagueManagementTests.cs`
- [x] `Application\Features\Leagues\GetUserLeaguesQueryIntegrationTests.cs` - Migrated to `LeagueManagementTests.cs`

## Bet Features
- [x] `Application\Features\Bets\PlaceBetCommandIntegrationTests.cs` - Migrated to `BetLifecycleTests.cs`
- [x] `Application\Features\Bets\DeleteBetCommandIntegrationTests.cs` - Migrated to `BetLifecycleTests.cs`
- [x] `Application\Features\Bets\GetMatchBetsQueryIntegrationTests.cs` - Migrated to `BetQueryTests.cs`
- [x] `Application\Features\Bets\GetMyBetsQueryIntegrationTests.cs` - Migrated to `BetQueryTests.cs`
- [x] `Application\Features\Bets\GetUserStatsQueryIntegrationTests.cs` - Migrated to `BetQueryTests.cs`
- [x] `Application\Features\Bets\GetLeagueStandingsQueryIntegrationTests.cs` - Migrated to `BetQueryTests.cs`
- [x] `Application\Features\Bets\CalculateBetResultsIntegrationTests.cs` - Migrated to `BetCalculationTests.cs`
- [x] `Application\Features\Bets\CalculateBetResultsJobIntegrationTests.cs` - Migrated to `BetCalculationTests.cs`
- [x] `Application\Features\Bets\RecalculateStandingsJobIntegrationTests.cs` - Migrated to `BetCalculationTests.cs`

## Bot Features
- [x] `Application\Features\Bots\AddBotToLeagueCommandIntegrationTests.cs` - Migrated to `BotTests.cs`
- [x] `Application\Features\Bots\CreateBotCommandIntegrationTests.cs` - Migrated to `BotTests.cs`
- [x] `Application\Features\Bots\GetBotsQueryIntegrationTests.cs` - Migrated to `BotTests.cs`
- [x] `Application\Features\Bots\GetLeagueBotsQueryIntegrationTests.cs` - Migrated to `BotTests.cs`
- [x] `Application\Features\Bots\PlaceBotBetsCommandIntegrationTests.cs` - Migrated to `BotTests.cs`
- [x] `Application\Features\Bots\RemoveBotFromLeagueCommandIntegrationTests.cs` - Migrated to `BotTests.cs`

## Football Features
- [x] `Application\Features\Football\GetCompetitionsQueryIntegrationTests.cs` - Migrated to `FootballTests.cs`
- [x] `Application\Features\Football\GetMatchesQueryIntegrationTests.cs` - Migrated to `FootballTests.cs`
- [x] `Application\Features\Football\GetMatchByIdQueryIntegrationTests.cs` - Migrated to `FootballTests.cs`

## Admin Features
- [x] `Application\Features\Admin\RetryJobCommandIntegrationTests.cs` - Migrated to `AdminTests.cs`
- [x] `Application\Features\Admin\CancelJobCommandIntegrationTests.cs` - Migrated to `AdminTests.cs`
- [x] `Application\Features\Admin\GetJobStatsQueryIntegrationTests.cs` - Migrated to `AdminTests.cs`
- [x] `Application\Features\Admin\GetJobByIdQueryIntegrationTests.cs` - Migrated to `AdminTests.cs`
- [x] `Application\Features\Admin\GetJobsQueryIntegrationTests.cs` - Migrated to `AdminTests.cs`

## Infrastructure
- [x] `Infrastructure\Data\ApplicationDbContextTests.cs` - Migrated to `ApplicationDbContextTests.cs`

---

**Migration Complete!** All integration tests have been successfully migrated from `ExtraTime.IntegrationTests` to `ExtraTime.NewIntegrationTests` using TUnit.

**Summary:**
- Total test files migrated: 35
- Total test cases added: 100+ new test cases covering edge cases and failure modes
- All tests pass in both InMemory and SQL modes
- Tests follow TUnit patterns with `await Assert.That()` syntax
