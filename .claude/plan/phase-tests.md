# Backend Testing Plan

## Overview

Create comprehensive unit and integration tests for the ExtraTime .NET backend using **TUnit** testing framework. The codebase follows Clean Architecture with CQRS patterns, PostgreSQL database, and 32+ command/query handlers across 5 feature areas.

## Testing Framework: TUnit

This project uses **TUnit** (modern .NET testing framework) instead of xUnit.

### Key TUnit Concepts

**Test Attributes:**
- `[Test]` - Marks a test method
- `[Before(Test)]` - Runs before each test
- `[After(Test)]` - Runs after each test

**Async Assertions:**
All assertions are async and use fluent syntax:
```csharp
await Assert.That(actual).IsEqualTo(expected);
await Assert.That(value).IsNotNull();
await Assert.That(list).HasCount(3);
```

**Lifecycle Management:**
- No `IAsyncLifetime` or `ICollectionFixture`
- Use static fixtures with lazy initialization
- Use `[Before(Test)]` and `[After(Test)]` for setup/cleanup

## Test Project Structure

```
tests/
├── ExtraTime.UnitTests/           # Fast, isolated unit tests (NEW)
│   ├── Application/
│   │   └── Features/              # Handler & validator tests
│   └── Infrastructure/
│       └── Services/              # Service tests
├── ExtraTime.IntegrationTests/    # Database integration tests (NEW)
│   ├── Application/Features/
│   └── Fixtures/
└── ExtraTime.API.Tests/           # API endpoint tests (EXISTS - enhance)
    ├── Endpoints/
    └── Fixtures/
```

**Rationale:** Separate projects allow unit tests to run fast (<1s each) while integration tests use real PostgreSQL via Testcontainers.

---

## Phase 1: Add Test Packages & Create Projects

### 1.1 Update `Directory.Packages.props`

Add these packages under Test Projects section:

```xml
<PackageVersion Include="TUnit" Version="0.6.11" />
<PackageVersion Include="TUnit.Assertions" Version="0.6.11" />
<PackageVersion Include="NSubstitute" Version="5.3.0" />
<PackageVersion Include="Bogus" Version="35.6.1" />
<PackageVersion Include="Testcontainers.PostgreSql" Version="4.1.0" />
<PackageVersion Include="Respawn" Version="6.2.1" />
<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
```

### 1.2 Create `tests/ExtraTime.UnitTests/ExtraTime.UnitTests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="TUnit" />
    <PackageReference Include="TUnit.Assertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Bogus" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="TUnit.Core" />
    <Using Include="TUnit.Assertions" />
    <Using Include="TUnit.Assertions.Extensions" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ExtraTime.Application\ExtraTime.Application.csproj" />
    <ProjectReference Include="..\..\src\ExtraTime.Infrastructure\ExtraTime.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### 1.3 Create `tests/ExtraTime.IntegrationTests/ExtraTime.IntegrationTests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="TUnit" />
    <PackageReference Include="TUnit.Assertions" />
    <PackageReference Include="Bogus" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="Respawn" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="TUnit.Core" />
    <Using Include="TUnit.Assertions" />
    <Using Include="TUnit.Assertions.Extensions" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ExtraTime.Infrastructure\ExtraTime.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### 1.4 Update `tests/ExtraTime.API.Tests/ExtraTime.API.Tests.csproj`

Add packages:
```xml
<PackageReference Include="TUnit" />
<PackageReference Include="TUnit.Assertions" />
<PackageReference Include="Bogus" />
<PackageReference Include="NSubstitute" />
<PackageReference Include="Testcontainers.PostgreSql" />
<PackageReference Include="Respawn" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
```

And add global usings:
```xml
<ItemGroup>
  <Using Include="TUnit.Core" />
  <Using Include="TUnit.Assertions" />
  <Using Include="TUnit.Assertions.Extensions" />
</ItemGroup>
```

### 1.5 Add Projects to Solution

```powershell
dotnet sln add tests/ExtraTime.UnitTests/ExtraTime.UnitTests.csproj
dotnet sln add tests/ExtraTime.IntegrationTests/ExtraTime.IntegrationTests.csproj
```

---

## Phase 2: Test Infrastructure & Base Classes

### 2.1 Async Query Provider Helper (for mocking DbSet)

**File:** `tests/ExtraTime.UnitTests/Common/TestAsyncQueryProvider.cs`

Provides `TestAsyncQueryProvider<T>` and `TestAsyncEnumerator<T>` to enable async LINQ operations on mocked DbSets.

### 2.2 Unit Test Base Classes

**File:** `tests/ExtraTime.UnitTests/Common/HandlerTestBase.cs`

```csharp
public abstract class HandlerTestBase
{
    protected readonly IApplicationDbContext Context;
    protected readonly ICurrentUserService CurrentUserService;
    protected readonly CancellationToken CancellationToken = CancellationToken.None;

    protected HandlerTestBase()
    {
        Context = Substitute.For<IApplicationDbContext>();
        CurrentUserService = Substitute.For<ICurrentUserService>();
    }

    protected void SetCurrentUser(Guid userId, string email = "test@example.com");
    protected static DbSet<T> CreateMockDbSet<T>(IQueryable<T> data) where T : class;
}
```

**File:** `tests/ExtraTime.UnitTests/Common/ValidatorTestBase.cs`

```csharp
public abstract class ValidatorTestBase<TValidator, TCommand>
    where TValidator : AbstractValidator<TCommand>, new()
{
    protected readonly TValidator Validator = new();
    protected void ShouldHaveValidationErrorFor(TCommand command, string propertyName);
}
```

### 2.3 Test Data Builders

**File:** `tests/ExtraTime.UnitTests/TestData/EntityBuilders.cs`

Fluent builders for test data:
- `UserBuilder` - creates User entities with defaults
- `LeagueBuilder` - creates League entities with scoring rules
- `MatchBuilder` - creates Match entities with status/scores
- `BetBuilder` - creates Bet entities with predictions
- `CompetitionBuilder`, `TeamBuilder`

### 2.4 Integration Test Fixtures

**File:** `tests/ExtraTime.IntegrationTests/Fixtures/DatabaseFixture.cs`

- Uses Testcontainers to spin up PostgreSQL
- Applies EF migrations
- Uses Respawn for fast database reset between tests
- Lazy initialization with `EnsureInitializedAsync()` and thread-safe singleton pattern

**File:** `tests/ExtraTime.IntegrationTests/Common/IntegrationTestBase.cs`

- Static DatabaseFixture with lazy initialization
- Uses TUnit `[Before(Test)]` and `[After(Test)]` attributes
- Auto-resets database before each test
- No `IAsyncLifetime` or `ICollectionFixture` needed

### 2.5 API Test Fixtures

**File:** `tests/ExtraTime.API.Tests/Fixtures/CustomWebApplicationFactory.cs`

- Extends `WebApplicationFactory<Program>`
- Replaces DbContext with Testcontainers PostgreSQL
- Mocks external services (IFootballDataService)
- Static container with lazy initialization using `EnsureInitializedAsync()`

**File:** `tests/ExtraTime.API.Tests/Fixtures/ApiTestBase.cs`

- Uses TUnit `[Before(Test)]` and `[After(Test)]` attributes
- Provides HttpClient with auth helpers
- `GetAuthTokenAsync()` - registers/logs in test user
- `SetAuthHeader()` - sets Bearer token
- No `IClassFixture` or `IAsyncLifetime` needed

---

## Phase 3: Unit Tests

**Note:** All tests use TUnit `[Test]` attribute and async assertions with `await Assert.That(...)`

### 3.1 Infrastructure Service Tests

| Service | File | Test Cases |
|---------|------|------------|
| BetCalculator | `Infrastructure/Services/BetCalculatorTests.cs` | Exact match scoring, correct result scoring, no score returns 0, custom league scoring |
| StandingsCalculator | `Infrastructure/Services/StandingsCalculatorTests.cs` | Points calculation, streak tracking, ranking |
| TokenService | `Infrastructure/Services/TokenServiceTests.cs` | JWT claims, refresh token generation, expiration |
| PasswordHasher | `Infrastructure/Services/PasswordHasherTests.cs` | Hash/verify, different passwords |
| InviteCodeGenerator | `Infrastructure/Services/InviteCodeGeneratorTests.cs` | Length, no ambiguous chars, uniqueness retry |

### 3.2 Validator Tests

**Note:** Use TUnit assertions with async syntax

| Validator | File | Test Cases |
|-----------|------|------------|
| LoginCommandValidator | `Application/Features/Auth/Validators/LoginCommandValidatorTests.cs` | Valid command, empty email, invalid email format, empty password |
| RegisterCommandValidator | `Application/Features/Auth/Validators/RegisterCommandValidatorTests.cs` | Valid command, password requirements, username length |
| CreateLeagueValidator | `Application/Features/Leagues/Validators/CreateLeagueValidatorTests.cs` | Valid command, empty name, scoring rules |
| PlaceBetValidator | `Application/Features/Bets/Validators/PlaceBetValidatorTests.cs` | Valid command, negative scores, empty IDs |

### 3.3 Handler Unit Tests (Mocked DbContext)

**Auth Handlers:**
- `LoginCommandHandlerTests` - valid credentials, invalid email, wrong password
- `RegisterCommandHandlerTests` - success, duplicate email
- `RefreshTokenCommandHandlerTests` - valid token, expired token
- `GetCurrentUserQueryHandlerTests` - user found, not found

**League Handlers:**
- `CreateLeagueCommandHandlerTests` - creates league + owner membership
- `JoinLeagueCommandHandlerTests` - valid code, invalid code, already member, max members
- `LeaveLeagueCommandHandlerTests` - member leaves, owner cannot leave
- `KickMemberCommandHandlerTests` - owner kicks, non-owner cannot kick

**Bet Handlers:**
- `PlaceBetCommandHandlerTests` - valid bet, not member, match started, deadline passed
- `CalculateBetResultsCommandHandlerTests` - calculates results, enqueues standings job
- `GetLeagueStandingsQueryHandlerTests` - returns standings

---

## Phase 4: Integration Tests

### 4.1 Handler Integration Tests (Real Database)

**File:** `tests/ExtraTime.IntegrationTests/Application/Features/Leagues/CreateLeagueCommandIntegrationTests.cs`
- Creates league and verifies persistence
- Verifies owner membership created

**File:** `tests/ExtraTime.IntegrationTests/Application/Features/Bets/CalculateBetResultsIntegrationTests.cs`
- Full flow: user -> league -> match -> bet -> calculate results
- Verifies BetResult entity created with correct points

**File:** `tests/ExtraTime.IntegrationTests/Application/Features/Bets/RecalculateLeagueStandingsIntegrationTests.cs`
- Verifies LeagueStanding entities created/updated

### 4.2 DbContext Tests

**File:** `tests/ExtraTime.IntegrationTests/Infrastructure/Data/ApplicationDbContextTests.cs`
- Auditing: CreatedAt/UpdatedAt auto-populated
- Transaction rollback on failure

---

## Phase 5: API Integration Tests

### 5.1 Auth Endpoints

**File:** `tests/ExtraTime.API.Tests/Endpoints/AuthEndpointsTests.cs`

| Test | Expected |
|------|----------|
| Register with valid data | 200 OK + tokens |
| Register duplicate email | 409 Conflict |
| Login valid credentials | 200 OK + tokens |
| Login invalid credentials | 401 Unauthorized |
| Get current user without auth | 401 Unauthorized |
| Get current user with auth | 200 OK + user data |

### 5.2 League Endpoints

**File:** `tests/ExtraTime.API.Tests/Endpoints/LeagueEndpointsTests.cs`

| Test | Expected |
|------|----------|
| Create league (authenticated) | 200 OK + league with invite code |
| Create league (unauthenticated) | 401 Unauthorized |
| Join league with valid code | 200 OK |
| Join league with invalid code | 404 Not Found |
| Get user leagues | 200 OK + list |

### 5.3 Bet Endpoints

**File:** `tests/ExtraTime.API.Tests/Endpoints/BetEndpointsTests.cs`

| Test | Expected |
|------|----------|
| Place bet (valid) | 200 OK + bet |
| Place bet (not member) | 403 Forbidden |
| Get league standings | 200 OK + standings |

---

## Critical Files to Modify/Create

### New Files
- `tests/ExtraTime.UnitTests/ExtraTime.UnitTests.csproj`
- `tests/ExtraTime.IntegrationTests/ExtraTime.IntegrationTests.csproj`
- `tests/ExtraTime.UnitTests/Common/HandlerTestBase.cs`
- `tests/ExtraTime.UnitTests/Common/ValidatorTestBase.cs`
- `tests/ExtraTime.UnitTests/Common/TestAsyncQueryProvider.cs`
- `tests/ExtraTime.UnitTests/TestData/EntityBuilders.cs`
- `tests/ExtraTime.IntegrationTests/Fixtures/DatabaseFixture.cs`
- `tests/ExtraTime.API.Tests/Fixtures/CustomWebApplicationFactory.cs`
- All test class files listed in Phases 3-5

### Modify
- `Directory.Packages.props` - add test packages
- `ExtraTime.sln` - add new test projects
- `tests/ExtraTime.API.Tests/ExtraTime.API.Tests.csproj` - add packages
- Delete `tests/ExtraTime.API.Tests/UnitTest1.cs` (placeholder)

---

## Verification

### Run All Tests
```powershell
dotnet test
```

### Run Unit Tests Only (Fast)
```powershell
dotnet test tests/ExtraTime.UnitTests
```

### Run with Coverage
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

### Coverage Targets
| Component | Target |
|-----------|--------|
| BetCalculator | 100% |
| StandingsCalculator | 95% |
| Validators | 100% |
| Handlers | 90% |
| API Endpoints | 85% |
| **Overall** | **85%** |

---

## Implementation Order

1. **Phase 1:** Add packages, create projects, update solution ✅ COMPLETE
2. **Phase 2:** Create base classes and fixtures ✅ COMPLETE
3. **Phase 3:** Unit tests for services, then validators, then handlers
4. **Phase 4:** Integration tests with real database
5. **Phase 5:** API endpoint tests

Estimated test count: ~80-100 test methods covering all critical paths.

---

## TUnit Test Examples

### Unit Test Example
```csharp
public sealed class BetCalculatorTests
{
    [Test]
    public async Task CalculatePoints_ExactMatch_ReturnsFullPoints()
    {
        // Arrange
        var calculator = new BetCalculator();
        var prediction = (HomeScore: 2, AwayScore: 1);
        var actual = (HomeScore: 2, AwayScore: 1);
        var scoring = (ExactMatch: 3, CorrectResult: 1);

        // Act
        var points = calculator.CalculatePoints(prediction, actual, scoring);

        // Assert
        await Assert.That(points).IsEqualTo(3);
    }
}
```

### Handler Test Example
```csharp
public sealed class CreateLeagueCommandHandlerTests : HandlerTestBase
{
    [Test]
    public async Task Handle_ValidCommand_CreatesLeague()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetCurrentUser(userId);
        
        var command = new CreateLeagueCommand("Test League", null);
        var handler = new CreateLeagueCommandHandler(Context, CurrentUserService);

        // Setup mock DbSet
        var leagues = new List<League>().AsQueryable();
        Context.Leagues.Returns(CreateMockDbSet(leagues));
        
        // Act
        var result = await handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Context.Leagues.Received(1).Add(Arg.Any<League>());
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }
}
```

### Integration Test Example
```csharp
public sealed class CreateLeagueIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task CreateLeague_ValidData_PersistsToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var command = new CreateLeagueCommand("Integration League", "Test description");
        var handler = new CreateLeagueCommandHandler(Context, CurrentUserService);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        
        var league = await Context.Leagues
            .FirstOrDefaultAsync(l => l.Name == "Integration League");
        
        await Assert.That(league).IsNotNull();
        await Assert.That(league!.OwnerId).IsEqualTo(userId);
    }
}
```

### API Test Example
```csharp
public sealed class LeagueEndpointsTests : ApiTestBase
{
    [Test]
    public async Task CreateLeague_Authenticated_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var request = new { Name = "API Test League", Description = "Test" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/leagues", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var league = await response.Content.ReadFromJsonAsync<LeagueDto>();
        await Assert.That(league).IsNotNull();
        await Assert.That(league!.Name).IsEqualTo("API Test League");
    }
}
```
