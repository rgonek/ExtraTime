# Integration Tests Migration Status

The following integration tests from `ExtraTime.IntegrationTests` still need to be fully moved to `ExtraTime.NewIntegrationTests`. 
"Fully moved" means ensuring all test cases (edge cases, failure modes, etc.) from the original files are represented in the new TUnit-based project.

## Auth Features
- [ ] `Application\Features\Auth\LoginCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Auth\RegisterCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Auth\RefreshTokenCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Auth\GetCurrentUserQueryIntegrationTests.cs` (Remaining edge cases)

## League Features
- [ ] `Application\Features\Leagues\CreateLeagueCommandIntegrationTests.cs` (Ensure all cases are covered)
- [ ] `Application\Features\Leagues\GetLeagueQueryIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Leagues\JoinLeagueCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Leagues\DeleteLeagueCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Leagues\KickMemberCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Leagues\LeaveLeagueCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Leagues\UpdateLeagueCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Leagues\RegenerateInviteCodeIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Leagues\GetUserLeaguesQueryIntegrationTests.cs` (Remaining edge cases)

## Bet Features
- [ ] `Application\Features\Bets\PlaceBetCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bets\DeleteBetCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bets\GetMatchBetsQueryIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bets\GetMyBetsQueryIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bets\GetUserStatsQueryIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bets\GetLeagueStandingsQueryIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bets\CalculateBetResultsIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bets\CalculateBetResultsJobIntegrationTests.cs` (NOT STARTED)
- [ ] `Application\Features\Bets\RecalculateStandingsJobIntegrationTests.cs` (NOT STARTED)

## Bot Features
- [ ] `Application\Features\Bots\AddBotToLeagueCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bots\CreateBotCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bots\GetBotsQueryIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bots\GetLeagueBotsQueryIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bots\PlaceBotBetsCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Bots\RemoveBotFromLeagueCommandIntegrationTests.cs` (Remaining edge cases)

## Football Features
- [ ] `Application\Features\Football\GetCompetitionsQueryIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Football\GetMatchesQueryIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Football\GetMatchByIdQueryIntegrationTests.cs` (Remaining edge cases)

## Admin Features
- [ ] `Application\Features\Admin\RetryJobCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Admin\CancelJobCommandIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Admin\GetJobStatsQueryIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Admin\GetJobByIdQueryIntegrationTests.cs` (Remaining edge cases)
- [ ] `Application\Features\Admin\GetJobsQueryIntegrationTests.cs` (Remaining edge cases)

## Infrastructure
- [ ] `Infrastructure\Data\ApplicationDbContextTests.cs` (Ensure all cases covered)
