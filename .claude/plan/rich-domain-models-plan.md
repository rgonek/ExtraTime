# Rich Domain Models - Implementation Plan

## Problem Statement
The current domain models are **anemic** - they contain only data (properties) with no behavior. Business logic is scattered across the Application layer (command handlers, services), violating Domain-Driven Design principles. This makes the domain layer weak and reduces encapsulation.

## Goal
Transform anemic domain models into **rich domain models** that:
- Encapsulate business logic and invariants
- Enforce business rules through their API
- Prevent invalid state through proper construction
- Use domain events for side effects
- Have comprehensive unit tests covering all business rules

## Approach
1. Move business logic from handlers into domain entities
2. Make constructors private, expose factory methods
3. Add domain events for important state changes
4. Add value objects for complex types
5. Write comprehensive unit tests for domain logic

---

## Workplan

### Phase 1: Setup Domain Testing Infrastructure
- [ ] Create `tests/ExtraTime.DomainTests` project
- [ ] Add xUnit, FluentAssertions, and NSubstitute packages
- [ ] Create test fixtures and builders for domain entities
- [ ] Set up base test classes for common patterns

### Phase 2: Enrich League Entity
**Current Problems:**
- Invite code generation happens in handler
- No validation of MaxMembers, scoring rules
- No business logic for member management
- AllowedCompetitionIds is just a JSON string

**Changes:**
- [ ] Add `League.Create()` factory method with validation
- [ ] Add `League.AddMember()` with capacity checks
- [ ] Add `League.RemoveMember()` with owner protection
- [ ] Add `League.RegenerateInviteCode()` method
- [ ] Add `League.UpdateSettings()` with validation
- [ ] Add `League.CanAcceptBet(Match)` to validate competition restrictions
- [ ] Add value object `CompetitionFilter` to replace JSON string
- [ ] Add `LeagueMemberAdded`, `LeagueMemberRemoved` domain events
- [ ] Write comprehensive tests for all League business rules

### Phase 3: Enrich Bet Entity
**Current Problems:**
- Bet placement logic in handler (deadline checks, member validation)
- Score calculation logic in separate service
- No protection against invalid scores

**Changes:**
- [ ] Add `Bet.Place()` factory method with validation
- [ ] Add `Bet.Update()` with deadline validation
- [ ] Add `Bet.CanBeModified(DateTime currentTime, int deadlineMinutes)` method
- [ ] Add `Bet.CalculatePoints(Match, int exactMatchPoints, int correctResultPoints)` method
- [ ] Add value object `Score` for predicted scores (validation: non-negative)
- [ ] Add `BetPlaced`, `BetUpdated`, `BetScored` domain events
- [ ] Write comprehensive tests for betting rules and scoring logic

### Phase 4: Enrich Match Entity
**Current Problems:**
- No business logic for match lifecycle
- Status transitions not validated
- Score updates not protected

**Changes:**
- [ ] Add `Match.Create()` factory method
- [ ] Add `Match.UpdateStatus()` with valid state transitions
- [ ] Add `Match.UpdateScore()` with validation (only for in-progress/finished)
- [ ] Add `Match.IsOpenForBetting(int deadlineMinutes)` method
- [ ] Add value object `MatchScore` with validation
- [ ] Add `MatchStatusChanged`, `MatchScoreUpdated` domain events
- [ ] Write tests for match lifecycle and state transitions

### Phase 5: Enrich User Entity
**Current Problems:**
- Password hashing in handler
- No business logic for refresh tokens
- No validation of email/username format

**Changes:**
- [ ] Add `User.Register()` factory method with validation
- [ ] Add `User.VerifyPassword(string password)` method
- [ ] Add `User.UpdateLastLogin()` method
- [ ] Add `User.AddRefreshToken()` with cleanup of old tokens
- [ ] Add `User.RevokeRefreshToken()` method
- [ ] Add value objects: `Email`, `Username`, `PasswordHash`
- [ ] Add `UserRegistered`, `UserLoggedIn` domain events
- [ ] Write tests for user authentication rules

### Phase 6: Enrich LeagueStanding Entity
**Current Problems:**
- All calculation logic in StandingsCalculator service
- No encapsulation of stats

**Changes:**
- [ ] Add `LeagueStanding.Create()` factory method
- [ ] Add `LeagueStanding.ApplyBetResult()` to update stats
- [ ] Add `LeagueStanding.UpdateStreak()` for streak tracking
- [ ] Add `LeagueStanding.Reset()` for new seasons
- [ ] Add value object `StandingStats` to group related stats
- [ ] Write tests for standings calculation logic

### Phase 6.5: Enrich Remaining Anemic Entities

**Priority: HIGH - These entities are currently pure data containers with no behavior:**

#### BetResult Entity
**Location:** `src/ExtraTime.Domain/Entities/BetResult.cs`
**Current Problems:**
- Pure data container with public setters
- Result creation logic in `CalculateBetResultsCommandHandler` (lines 62-83)
- No encapsulation of scoring result lifecycle

**Changes:**
- [ ] Add `BetResult.Create()` factory method
- [ ] Add `BetResult.Update()` for recalculation scenarios
- [ ] Add `BetResult.CalculateFrom()` static method
- [ ] Make properties private setters
- [ ] Add `BetResultCalculated` domain event
- [ ] Write tests for result creation and updates

#### Bot Entity
**Location:** `src/ExtraTime.Domain/Entities/Bot.cs`
**Current Problems:**
- All public setters, no encapsulation
- Creation logic scattered in `CreateBotCommandHandler` (lines 24-42)
- No lifecycle management (activation/deactivation)

**Changes:**
- [ ] Add `Bot.Create()` factory method with validation
- [ ] Add `Bot.Activate()` / `Bot.Deactivate()` methods
- [ ] Add `Bot.RecordBetPlaced()` to update timestamp
- [ ] Add `Bot.UpdateConfiguration()` method
- [ ] Add `BotCreated`, `BotStatusChanged` domain events
- [ ] Write tests for bot lifecycle

#### BackgroundJob Entity
**Location:** `src/ExtraTime.Domain/Entities/BackgroundJob.cs`
**Current Problems:**
- State transitions scattered across handlers
- Logic in `RetryJobCommandHandler` (lines 25-39)
- Logic in `CancelJobCommandHandler` (lines 25-31)
- No validation of state transitions

**Changes:**
- [ ] Add `BackgroundJob.Create()` factory method
- [ ] Add `BackgroundJob.MarkAsProcessing()` with validation
- [ ] Add `BackgroundJob.MarkAsCompleted()` method
- [ ] Add `BackgroundJob.MarkAsFailed()` with error capture
- [ ] Add `BackgroundJob.Cancel()` with validation (only Pending/Processing)
- [ ] Add `BackgroundJob.Retry()` with max retries check
- [ ] Add `JobStatusChanged`, `JobFailed` domain events
- [ ] Write tests for all state transitions

#### LeagueMember Entity
**Location:** `src/ExtraTime.Domain/Entities/LeagueMember.cs`
**Current Problems:**
- Pure data container with all public setters
- No behavior or lifecycle management
- Role change logic in `KickMemberCommandHandler` (lines 41-45)

**Changes:**
- [ ] Add `LeagueMember.Create()` factory method
- [ ] Add `LeagueMember.ChangeRole()` with validation (cannot demote Owner directly)
- [ ] Add `LeagueMember.IsOwner()` helper method
- [ ] Make properties private setters
- [ ] Write tests for member role management

#### RefreshToken Entity
**Location:** `src/ExtraTime.Domain/Entities/RefreshToken.cs`
**Current Problems:**
- Has computed properties but no behavior methods
- Revocation logic in `RefreshTokenCommandHandler` (lines 35-38)
- No encapsulation of token lifecycle

**Changes:**
- [ ] Add `RefreshToken.Create()` factory method
- [ ] Add `RefreshToken.Revoke()` method with reason tracking
- [ ] Add `RefreshToken.IsValidForUse()` validation method
- [ ] Add `RefreshToken.ReplaceWith()` for rotation
- [ ] Make properties private setters (except computed ones)
- [ ] Write tests for token lifecycle

#### Competition Entity
**Location:** `src/ExtraTime.Domain/Entities/Competition.cs`
**Current Problems:**
- Simple data container
- Sync logic scattered in sync services

**Changes:**
- [ ] Add `Competition.Create()` factory method
- [ ] Add `Competition.UpdateDetails()` for metadata changes
- [ ] Add `Competition.RecordSync()` to update sync timestamp
- [ ] Add `Competition.UpdateCurrentSeason()` method
- [ ] Write tests for competition lifecycle

#### Team Entity
**Location:** `src/ExtraTime.Domain/Entities/Team.cs`
**Current Problems:**
- Simple data container with no behavior
- No encapsulation of team data updates

**Changes:**
- [ ] Add `Team.Create()` factory method
- [ ] Add `Team.UpdateDetails()` for team info changes
- [ ] Add `Team.RecordSync()` to update sync timestamp
- [ ] Write tests for team data management

**Priority: MEDIUM - Minor improvements:**
- [ ] Add `TeamFormCache.Update()` method
- [ ] Add `CompetitionTeam.Create()` factory method

### Phase 7: Add Value Objects
- [ ] Create `Email` value object (validation, immutability)
- [ ] Create `Username` value object (min/max length, allowed characters)
- [ ] Create `InviteCode` value object (format, expiry)
- [ ] Create `Score` value object (non-negative validation)
- [ ] Create `MatchScore` value object (home/away scores)
- [ ] Create `CompetitionFilter` value object (replace JSON string)
- [ ] Create `BettingDeadline` value object (minutes before match)
- [ ] Write tests for all value objects

### Phase 8: Add Domain Events Infrastructure
- [ ] Create `IDomainEvent` marker interface
- [ ] Create `BaseEntity.DomainEvents` collection
- [ ] Create `BaseEntity.AddDomainEvent()` method
- [ ] Create `BaseEntity.ClearDomainEvents()` method
- [ ] Update `ApplicationDbContext.SaveChangesAsync()` to dispatch events
- [ ] Create domain event handlers in Application layer
- [ ] Write tests for domain event dispatching

### Phase 9: Refactor Application Layer
**Remove business logic from handlers:**

#### Core Entity Handlers
- [ ] Update `CreateLeagueCommandHandler` to use `League.Create()`
- [ ] Update `PlaceBetCommandHandler` to use `Bet.Place()` and `Bet.Update()`
- [ ] Update `JoinLeagueCommandHandler` to use `League.AddMember()`
- [ ] Update `LeaveLeagueCommandHandler` to use `League.RemoveMember()`
- [ ] Update `CalculateBetResultsCommandHandler` to use `Bet.CalculatePoints()` and `BetResult.Create()`
- [ ] Update `RecalculateLeagueStandingsCommandHandler` to use `LeagueStanding.ApplyBetResult()`
- [ ] Update `RegisterCommandHandler` to use `User.Register()`
- [ ] Update `LoginCommandHandler` to use `User.VerifyPassword()`

#### Additional Entity Handlers (from Phase 6.5)
- [ ] Update `CreateBotCommandHandler` to use `Bot.Create()`
- [ ] Update `AddBotToLeagueCommandHandler` to use `Bot.Activate()` if needed
- [ ] Update `RemoveBotFromLeagueCommandHandler` to use `Bot.Deactivate()` if needed
- [ ] Update `PlaceBotBetsCommandHandler` to use `Bot.RecordBetPlaced()`
- [ ] Update `RetryJobCommandHandler` to use `BackgroundJob.Retry()`
- [ ] Update `CancelJobCommandHandler` to use `BackgroundJob.Cancel()`
- [ ] Update `RefreshTokenCommandHandler` to use `RefreshToken.Revoke()` and `RefreshToken.ReplaceWith()`
- [ ] Update `KickMemberCommandHandler` to use `LeagueMember.IsOwner()` check

#### Handler Refactoring Guidelines
- [ ] Remove all validation logic from handlers (move to entities)
- [ ] Remove state transition logic from handlers (move to entities)
- [ ] Keep only orchestration: fetch entity → call domain method → save
- [ ] Ensure all handlers use Result pattern consistently
- [ ] Add integration tests for all updated handlers

### Phase 10: Update Integration Tests
- [ ] Update existing integration tests to work with new domain model API
- [ ] Ensure all scenarios still pass
- [ ] Add new integration tests for domain events

---

## Testing Strategy

### Domain Tests (Unit Tests)
**Location:** `tests/ExtraTime.DomainTests/`

Each entity gets comprehensive tests:
1. **Factory Method Tests** - Valid/invalid construction
2. **Business Rule Tests** - All invariants enforced
3. **State Transition Tests** - Valid state changes
4. **Domain Event Tests** - Events raised correctly
5. **Value Object Tests** - Immutability, equality, validation

### Test Coverage Goals
- **Minimum 95% code coverage** for domain layer
- **100% coverage** for critical business rules:
  - Bet deadline validation
  - League capacity limits
  - Score calculation logic
  - Match state transitions
  - User authentication

### Example Test Structure
```csharp
public sealed class LeagueTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_WithValidData_ReturnsLeague() { }
        
        [Fact]
        public void Create_WithNullName_ThrowsArgumentException() { }
        
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidMaxMembers_ThrowsArgumentException(int max) { }
    }
    
    public class AddMemberTests
    {
        [Fact]
        public void AddMember_WhenNotFull_AddsMember() { }
        
        [Fact]
        public void AddMember_WhenFull_ThrowsInvalidOperationException() { }
        
        [Fact]
        public void AddMember_RaisesLeagueMemberAddedEvent() { }
    }
}
```

---

## Benefits

### Before (Anemic Model)
```csharp
// Domain/Entities/League.cs
public sealed class League : BaseAuditableEntity
{
    public required string Name { get; set; }
    public int MaxMembers { get; set; } = 255;
    // ... just properties
}

// Application/Features/Leagues/Commands/CreateLeagueCommandHandler.cs
public async ValueTask<Result<LeagueDto>> Handle(...)
{
    // All validation and business logic here
    if (string.IsNullOrWhiteSpace(request.Name))
        return Result.Failure("Name required");
    
    if (request.MaxMembers < 2 || request.MaxMembers > 1000)
        return Result.Failure("Invalid max members");
    
    var league = new League
    {
        Name = request.Name,
        MaxMembers = request.MaxMembers,
        // ... set all properties
    };
    // ...
}
```

### After (Rich Model)
```csharp
// Domain/Entities/League.cs
public sealed class League : BaseAuditableEntity
{
    private readonly List<LeagueMember> _members = [];
    
    private League() { } // Private constructor
    
    public static League Create(
        string name, 
        Guid ownerId,
        int maxMembers = 255,
        ...)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name required", nameof(name));
        
        if (maxMembers < 2 || maxMembers > 1000)
            throw new ArgumentException("Max members must be 2-1000", nameof(maxMembers));
        
        var league = new League
        {
            Name = name,
            OwnerId = ownerId,
            MaxMembers = maxMembers,
            // ...
        };
        
        league.AddDomainEvent(new LeagueCreated(league.Id));
        return league;
    }
    
    public void AddMember(Guid userId, MemberRole role)
    {
        if (_members.Count >= MaxMembers)
            throw new InvalidOperationException("League is full");
        
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User already member");
        
        var member = new LeagueMember(Id, userId, role);
        _members.Add(member);
        
        AddDomainEvent(new LeagueMemberAdded(Id, userId));
    }
    
    public bool CanAcceptBet(Match match)
    {
        return CompetitionFilter.IsAllowed(match.CompetitionId);
    }
}

// Application/Features/Leagues/Commands/CreateLeagueCommandHandler.cs
public async ValueTask<Result<LeagueDto>> Handle(...)
{
    // Just orchestration, no business logic
    var league = League.Create(
        request.Name,
        currentUserId,
        request.MaxMembers,
        ...
    );
    
    context.Leagues.Add(league);
    await context.SaveChangesAsync(cancellationToken);
    
    return Result.Success(league.ToDto());
}
```

---

## Success Criteria
1. ✅ All business logic moved from Application to Domain layer
2. ✅ Handlers contain only orchestration (no validation/business rules)
3. ✅ All entities use factory methods (private constructors)
4. ✅ Domain events raised for important state changes
5. ✅ Value objects replace primitive obsession
6. ✅ 95%+ code coverage on Domain layer
7. ✅ All existing integration tests still pass
8. ✅ No breaking changes to API contracts
9. ✅ All entities enriched: League, Bet, Match, User, LeagueStanding, BetResult, Bot, BackgroundJob, LeagueMember, RefreshToken, Competition, Team
10. ✅ CompetitionFilter value object integrated into League entity
11. ✅ No anemic domain models remain in the codebase

---

## Implementation Notes

### Order of Implementation
Follow the phases in order because:
1. **Phase 1** sets up testing infrastructure needed for all phases
2. **Phases 2-6** can be done independently per entity
3. **Phase 7-8** are foundational for multiple entities
4. **Phase 9-10** require all domain changes to be complete

### Parallel Work Opportunities
After Phase 1, these can be done in parallel:
- Phases 2-6 (different entities)
- Phase 7 (value objects can be done alongside entity work)

### Breaking Changes
Minimal - internal implementation changes, but:
- Application layer handlers will change
- Some test setups will need updates
- No public API changes expected

### Estimated Effort
- **Phase 1:** 2-3 hours (testing infrastructure)
- **Phases 2-6:** 3-4 hours each (per entity)
- **Phase 6.5:** 6-8 hours (remaining anemic entities: BetResult, Bot, BackgroundJob, LeagueMember, RefreshToken, Competition, Team)
- **Phase 7:** 4-5 hours (all value objects)
- **Phase 8:** 3-4 hours (domain events)
- **Phase 9:** 8-10 hours (refactor all handlers - core + additional)
- **Phase 10:** 3-4 hours (update all tests)

**Total:** ~50-60 hours (updated to include additional entities)

---

## References

### Domain-Driven Design Principles
1. **Entities** - Objects with identity and lifecycle
2. **Value Objects** - Immutable objects without identity
3. **Aggregates** - Consistency boundaries (League is aggregate root)
4. **Domain Events** - Signal important state changes
5. **Invariants** - Business rules that must always be true

### Key Patterns
- **Factory Methods** - Control object creation
- **Tell, Don't Ask** - Objects do work, don't expose state
- **Encapsulation** - Hide implementation details
- **Rich Domain Model** - Business logic lives in domain
- **Domain Events** - Decouple side effects

### Testing Patterns
- **AAA Pattern** - Arrange, Act, Assert
- **Test Builders** - Fluent API for test data
- **Theory Tests** - Parameterized tests for multiple cases
- **Fixture Classes** - Group related tests
