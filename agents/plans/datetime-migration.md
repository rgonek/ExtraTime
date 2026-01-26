# DateTime Abstraction Migration Plan

## Problem Statement
Direct usage of `DateTime.UtcNow` is scattered throughout the codebase (54+ occurrences), making code difficult to test and control time-dependent behavior. Need to introduce a time abstraction that allows for:
- Deterministic testing with controlled time
- Consistent time handling across the application
- Better testability of time-dependent logic

## Proposed Approach: Static Ambient Context with IClock

Use `Clock.Current` static property backed by `AsyncLocal<IClock>` for thread-safe ambient context. This provides clean Domain code without parameter passing while maintaining testability and thread-safety.

**Key benefits:**
- No parameter passing through Domain entities
- Clean, readable code (`Clock.Current.UtcNow`)
- Thread-safe via `AsyncLocal<T>` (isolated per async context/test/request)
- Fully testable with `FakeClock`

**Implementation pattern:**
```csharp
public static class Clock
{
    private static readonly AsyncLocal<IClock?> _current = new();
    private static readonly IClock _default = new SystemClock();
    
    public static IClock Current
    {
        get => _current.Value ?? _default;
        set => _current.Value = value;
    }
    
    public static DateTime UtcNow => Current.UtcNow;
}
```

**Test usage:**
```csharp
var fakeClock = new FakeClock(new DateTime(2026, 1, 26));
Clock.Current = fakeClock;
// ... test code ...
Clock.Current = new SystemClock(); // reset in teardown
```

## Impact Analysis
**Files affected:** ~26 production files + ~15 test files

**Affected areas:**
- Domain Entities: User, RefreshToken, Match, LeagueStanding, League, Bet (9 files)
- Infrastructure Services: TokenService, FootballSyncService, RateLimitingHandler, InMemoryJobDispatcher (6 files)
- Application Handlers: Auth, Bets, Leagues, Admin commands and queries (8 files)
- API Layer: Health endpoints (1 file)
- Tests: All unit and domain tests (15 files)

## Implementation Plan

### Phase 1: Create Abstraction
- [ ] Create `IClock` interface in `Application/Common/Interfaces/`
  - Single property: `DateTime UtcNow { get; }`
- [ ] Create `SystemClock` sealed class in `Infrastructure/Services/`
  - Implements `IClock`
  - Returns `DateTime.UtcNow`
- [ ] Create `Clock` static class in `Application/Common/`
  - Uses `AsyncLocal<IClock?>` for thread-safe storage
  - Default fallback to `SystemClock` instance
  - Provides static `UtcNow` property for convenience
- [ ] Create `FakeClock` in test utilities (`tests/ExtraTime.UnitTests/Helpers/`)
  - Constructor accepts fixed `DateTime`
  - Method to advance time if needed
  - Optional: `IDisposable` pattern to auto-reset `Clock.Current`

### Phase 2: Update Domain Layer
Replace all `DateTime.UtcNow` with `Clock.UtcNow`:
- [ ] Update `User.cs` - 4 usages in RecordLogin, CleanupOldTokens, AddRefreshToken, RevokeToken
- [ ] Update `RefreshToken.cs` - 2 usages in property initialization and IsExpired property
- [ ] Update `Match.cs` - 5 usages in SetExternalMatchId, UpdateScore, UpdateStatus, SetFinished, AddEvent
- [ ] Update `LeagueStanding.cs` - 3 usages in Create, UpdateStandings, ResetStandings
- [ ] Update `League.cs` - 1 usage in AddMember
- [ ] Update `Bet.cs` - 1 usage in Create

**Note:** Entities use `Clock.UtcNow` directly - no constructor changes needed!

### Phase 3: Update Infrastructure Layer
Replace all `DateTime.UtcNow` with `Clock.UtcNow`:
- [ ] Update `TokenService.cs` - 2 usages in GenerateAccessToken, GetRefreshTokenExpiration
- [ ] Update `ApplicationDbContext.cs` - 1 usage in SaveChangesAsync for audit timestamps
- [ ] Update `InMemoryJobDispatcher.cs` - 1 usage in DispatchAsync
- [ ] Update `RateLimitingHandler.cs` - 3 usages in rate limiting logic
- [ ] Update `FootballSyncService.cs` - 8 usages in sync operations
- [ ] Update `FootballSyncHostedService.cs` - 1 usage in GetNextRunTime

### Phase 4: Update Application Layer
Replace all `DateTime.UtcNow` with `Clock.UtcNow`:
- [ ] Update `JoinLeagueCommandHandler.cs` - 1 usage in invite code expiration check
- [ ] Update `RefreshTokenCommandHandler.cs` - 4 usages in token validation and refresh
- [ ] Update `PlaceBetCommandHandler.cs` - 1 usage in deadline check
- [ ] Update `DeleteBetCommandHandler.cs` - 1 usage in deadline check
- [ ] Update `GetMatchBetsQueryHandler.cs` - 1 usage in deadline check
- [ ] Update `CalculateBetResultsCommandHandler.cs` - 2 usages in result calculation
- [ ] Update `CancelJobCommandHandler.cs` - 1 usage in job completion
- [ ] Update validators:
  - [ ] `RegenerateInviteCodeCommandValidator.cs` - 1 usage
  - [ ] `CreateLeagueCommandValidator.cs` - 1 usage

**Note:** Validators can use `Clock.UtcNow` directly in rules - no special handling needed!

### Phase 5: Update API Layer
Replace all `DateTime.UtcNow` with `Clock.UtcNow`:
- [ ] Update `HealthEndpoints.cs` - 1 usage in health check response

### Phase 6: Update Tests
- [ ] Update tests to use `FakeClock`:
  - Set `Clock.Current = new FakeClock(fixedDateTime)` in test setup
  - Reset `Clock.Current = new SystemClock()` in teardown/finally
- [ ] Update `EntityBuilders.cs` in test data - 11 usages (replace with `Clock.UtcNow`)
- [ ] Update domain tests (BetTests, MatchTests) - 15 usages
  - Replace `DateTime.UtcNow` with `Clock.UtcNow`
  - Add FakeClock setup at test start
- [ ] Update unit tests:
  - [ ] `PlaceBetCommandHandlerTests.cs` - 2 usages + FakeClock setup
  - [ ] `RegisterCommandHandlerTests.cs` - 1 usage + FakeClock setup
  - [ ] `LoginCommandHandlerTests.cs` - 1 usage + FakeClock setup
  - [ ] `TokenServiceTests.cs` - 2 usages + FakeClock setup
  - [ ] `StandingsCalculatorTests.cs` - 3 usages + FakeClock setup

### Phase 7: Validation & Cleanup
- [ ] Run all tests to ensure no regressions
- [ ] Verify no remaining `DateTime.UtcNow` usages in production code (use grep)
- [ ] Consider: Add analyzer rule to prevent future `DateTime.UtcNow` usage
- [ ] Update any documentation referencing time handling
- [ ] Code review and cleanup

## Technical Considerations

### Thread Safety
Using `AsyncLocal<T>` makes `Clock.Current` safe for:
- ✅ Async/await operations (each async flow gets isolated value)
- ✅ Parallel test execution (xUnit/NUnit run tests in different execution contexts)
- ✅ Web requests (each request is a separate async context)
- ✅ Background jobs and hosted services

**Important:** Tests must reset `Clock.Current` in teardown to avoid test pollution.

### FluentValidation
Validators can use `Clock.UtcNow` directly in rules - no special handling needed.

### Domain Entity Design
Entities use `Clock.UtcNow` directly - keeps code clean without violating DDD too much. Time is a fundamental infrastructure concern, and ambient context is an acceptable tradeoff for cleaner code.

### Breaking Changes
- ✅ No method signature changes
- ✅ No constructor changes  
- ✅ Tests need to set `Clock.Current = new FakeClock(...)`
- ✅ No external API breaking changes (all internal)

## Success Criteria
- [ ] All production code uses `IDateTimeProvider` instead of `DateTime.UtcNow`
- [ ] All tests pass
- [ ] Tests can control time deterministically
- [ ] No regression in functionality
- [ ] Code follows Clean Architecture principles (domain remains infrastructure-agnostic)

## Estimated Effort
- Phase 1: 30-45 minutes (create abstractions and static class)
- Phase 2: 30-45 minutes (simple find/replace in domain)
- Phase 3: 30-45 minutes (simple find/replace in infrastructure)
- Phase 4: 30-45 minutes (simple find/replace in application)
- Phase 5: 5 minutes (one file)
- Phase 6: 1-2 hours (test updates + FakeClock setup)
- Phase 7: 30 minutes (validation)

**Total: 4-6 hours** (significantly faster than DI approach!)

## Notes
- Start with Phase 1 to establish pattern
- Phase 2 (Domain) is critical - must maintain DDD principles
- Consider doing Phase 6 (tests) in parallel with production code updates for faster feedback
- Use compiler errors to guide migration (won't compile until DateTime params are passed)
