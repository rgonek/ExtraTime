# FastEndpoints Migration Plan

## Executive Summary

**Goal**: Migrate from Minimal APIs + Mediator Source Generator to FastEndpoints to reduce boilerplate, improve maintainability, and consolidate endpoint logic.

**Scope**:
- 32 endpoints across 7 feature areas
- 27 command/query handlers
- 7 FluentValidation validators
- Remove Mediator Source Generator dependency
- Keep Result<T> pattern, Clean Architecture, and all domain logic intact

**Strategy**: Phased migration, feature-by-feature, with Auth as proof-of-concept. Full rollback capability at each phase.

**Expected Benefits**:
- 20-30% less code overall
- 75% fewer files per endpoint (one file per endpoint vs 4 separate files)
- Faster startup (no source generator reflection overhead)
- Better maintainability (all endpoint logic in one place)
- Auto-validation integration (no manual validator calls)

---

## Migration Phases

### Phase 0: Infrastructure Setup (1 hour)

**Goal**: Add FastEndpoints infrastructure without breaking existing code.

#### 0.1 Add NuGet Package

**File**: `D:\Dev\ExtraTime\Directory.Packages.props`

Add:
```xml
<PackageVersion Include="FastEndpoints" Version="5.31.0" />
<PackageVersion Include="FastEndpoints.Swagger.Swashbuckle" Version="5.31.0" />
```

Note: Keep Mediator packages until Phase 3.

#### 0.2 Update Program.cs

**File**: `D:\Dev\ExtraTime\src\ExtraTime.API\Program.cs`

After line 16 (AddInfrastructureServices), add:
```csharp
builder.Services.AddFastEndpoints();
```

After line 62 (UseAuthorization), add:
```csharp
app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";
});
```

Keep all existing endpoint mappings (lines 65-72).

#### 0.3 Verification

```bash
dotnet build
dotnet run --project src/ExtraTime.API
```

All existing endpoints should work unchanged. FastEndpoints middleware is loaded but no endpoints registered yet.

**Commit**: `git commit -m "Phase 0: Add FastEndpoints infrastructure"`

---

### Phase 1: Proof of Concept - Auth Feature (3-4 hours)

**Goal**: Migrate Auth feature (4 endpoints) end-to-end to validate approach.

#### 1.1 Create Endpoint Structure

Create directory: `D:\Dev\ExtraTime\src\ExtraTime.API\Features\Auth\Endpoints\`

#### 1.2 Migrate Endpoints

Create these 4 files:

**1. RegisterEndpoint.cs**
```csharp
using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Auth;
using ExtraTime.Application.Features.Auth.DTOs;
using ExtraTime.Domain.Entities;
using FastEndpoints;

namespace ExtraTime.API.Features.Auth.Endpoints;

public sealed class RegisterEndpoint : Endpoint<RegisterRequest, AuthResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public RegisterEndpoint(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public override void Configure()
    {
        Post("/auth/register");
        AllowAnonymous();
        Tags("Auth");
        Summary(s => s.Summary = "Register a new user");
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var normalizedEmail = req.Email.ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail, ct);

        if (emailExists)
        {
            await SendAsync(new { error = AuthErrors.EmailAlreadyExists }, 409, ct);
            return;
        }

        var usernameExists = await _context.Users
            .AnyAsync(u => u.Username == req.Username, ct);

        if (usernameExists)
        {
            await SendAsync(new { error = AuthErrors.UsernameAlreadyExists }, 409, ct);
            return;
        }

        var user = new User
        {
            Email = normalizedEmail,
            Username = req.Username,
            PasswordHash = _passwordHasher.Hash(req.Password),
            CreatedAt = DateTime.UtcNow
        };

        var refreshToken = new RefreshTokenEntity
        {
            Token = _tokenService.GenerateRefreshToken(),
            ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };

        user.RefreshTokens.Add(refreshToken);
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        var accessToken = _tokenService.GenerateAccessToken(user);

        var response = new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresAt: refreshToken.ExpiresAt,
            User: new UserDto(user.Id, user.Email, user.Username, user.Role.ToString())
        );

        await SendOkAsync(response, ct);
    }
}

public sealed class RegisterRequestValidator : Validator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}
```

**2. LoginEndpoint.cs** (Pattern: command with validation, returns 401 on failure)
**3. RefreshTokenEndpoint.cs** (Pattern: command without validation)
**4. GetCurrentUserEndpoint.cs** (Pattern: query, uses `EndpointWithoutRequest<TResponse>`, requires auth)

#### 1.3 Disable Old Endpoints

**File**: `D:\Dev\ExtraTime\src\ExtraTime.API\Program.cs`

Comment out line 66:
```csharp
// app.MapAuthEndpoints();
```

#### 1.4 Testing

**Manual Tests** (via Swagger or Postman):
- POST /api/auth/register - Verify success and validation errors
- POST /api/auth/login - Verify success and 401 on bad credentials
- POST /api/auth/refresh - Verify token refresh works
- GET /api/auth/me - Verify requires Bearer token

**Checklist**:
- [ ] All 4 endpoints respond correctly
- [ ] Validation errors return 400 with expected format
- [ ] Authentication failures return 401
- [ ] Response DTOs match exactly
- [ ] Swagger UI shows endpoints

**Rollback**: If issues, uncomment line 66, delete `Features/Auth/Endpoints/` directory.

**Commit**: `git commit -m "Phase 1: Migrate Auth endpoints to FastEndpoints"`

---

### Phase 2: Migrate Remaining Features (15-20 hours)

**Goal**: Apply proven pattern to all remaining features.

**Order**: Leagues → Bets → Football → Admin → FootballSync → Health

#### Pattern Reference

**Endpoint with route parameters**:
```csharp
public sealed class GetLeagueEndpoint : Endpoint<GetLeagueRequest, LeagueDetailDto>
{
    public override void Configure()
    {
        Get("/leagues/{id}");
        Policies("UserOrAdmin"); // For authorized endpoints
        Tags("Leagues");
    }

    public override async Task HandleAsync(GetLeagueRequest req, CancellationToken ct)
    {
        // req.Id is auto-bound from route
    }
}

public sealed record GetLeagueRequest(Guid Id);
```

**Endpoint with nested routes**:
```csharp
public override void Configure()
{
    Post("/leagues/{leagueId}/bets");
    Tags("Bets");
}

public sealed record PlaceBetRequest(
    Guid LeagueId, // From route
    Guid MatchId,  // From body
    int PredictedHomeScore,
    int PredictedAwayScore
);
```

**Endpoint with query parameters**:
```csharp
public sealed record GetMatchesRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? CompetitionId = null
);
// Query params auto-bound from URL
```

#### 2.1 Leagues Feature (4-5 hours)

**Directory**: `D:\Dev\ExtraTime\src\ExtraTime.API\Features\Leagues\Endpoints\`

**Endpoints to create** (9 total):
1. CreateLeagueEndpoint.cs - POST /leagues
2. GetUserLeaguesEndpoint.cs - GET /leagues
3. GetLeagueEndpoint.cs - GET /leagues/{id}
4. UpdateLeagueEndpoint.cs - PUT /leagues/{id}
5. DeleteLeagueEndpoint.cs - DELETE /leagues/{id}
6. JoinLeagueEndpoint.cs - POST /leagues/{id}/join
7. LeaveLeagueEndpoint.cs - DELETE /leagues/{id}/leave
8. KickMemberEndpoint.cs - DELETE /leagues/{id}/members/{userId}
9. RegenerateInviteCodeEndpoint.cs - POST /leagues/{id}/invite-code/regenerate

**Disable in Program.cs**: Comment out line 70 (`app.MapLeagueEndpoints();`)

**Test**: Verify all 9 endpoints work correctly

**Commit**: `git commit -m "Phase 2.1: Migrate Leagues endpoints"`

#### 2.2 Bets Feature (3-4 hours)

**Directory**: `D:\Dev\ExtraTime\src\ExtraTime.API\Features\Bets\Endpoints\`

**Endpoints** (6 total):
1. PlaceBetEndpoint.cs - POST /leagues/{leagueId}/bets
2. DeleteBetEndpoint.cs - DELETE /leagues/{leagueId}/bets/{betId}
3. GetMyBetsEndpoint.cs - GET /leagues/{leagueId}/bets/my
4. GetMatchBetsEndpoint.cs - GET /leagues/{leagueId}/matches/{matchId}/bets
5. GetLeagueStandingsEndpoint.cs - GET /leagues/{leagueId}/standings
6. GetUserStatsEndpoint.cs - GET /leagues/{leagueId}/users/{userId}/stats

**Disable**: Comment out line 71 (`app.MapBetEndpoints();`)

**Commit**: `git commit -m "Phase 2.2: Migrate Bets endpoints"`

#### 2.3 Football Feature (2-3 hours)

**Directory**: `D:\Dev\ExtraTime\src\ExtraTime.API\Features\Football\Endpoints\`

**Endpoints** (7 total - including sync):
1. GetCompetitionsEndpoint.cs - GET /competitions
2. GetMatchesEndpoint.cs - GET /matches (with query params)
3. GetMatchByIdEndpoint.cs - GET /matches/{id}
4. SyncCompetitionsEndpoint.cs - POST /admin/sync/competitions
5. SyncTeamsEndpoint.cs - POST /admin/sync/teams/{competitionId}
6. SyncMatchesEndpoint.cs - POST /admin/sync/matches
7. SyncLiveEndpoint.cs - POST /admin/sync/live

**Note**: Sync endpoints require `Policies("AdminOnly")`

**Disable**: Comment out lines 68-69 (`app.MapFootballEndpoints();` and `app.MapFootballSyncEndpoints();`)

**Commit**: `git commit -m "Phase 2.3: Migrate Football endpoints"`

#### 2.4 Admin Feature (2-3 hours)

**Directory**: `D:\Dev\ExtraTime\src\ExtraTime.API\Features\Admin\Endpoints\`

**Endpoints** (5 total):
1. GetJobsEndpoint.cs - GET /admin/jobs
2. GetJobStatsEndpoint.cs - GET /admin/jobs/stats
3. GetJobByIdEndpoint.cs - GET /admin/jobs/{id}
4. RetryJobEndpoint.cs - POST /admin/jobs/{id}/retry
5. CancelJobEndpoint.cs - POST /admin/jobs/{id}/cancel

**Authorization**: All use `Policies("AdminOnly")`

**Disable**: Comment out line 67 (`app.MapAdminEndpoints();`)

**Commit**: `git commit -m "Phase 2.4: Migrate Admin endpoints"`

#### 2.5 Health Feature (1 hour)

**Directory**: `D:\Dev\ExtraTime\src\ExtraTime.API\Features\Health\Endpoints\`

**Endpoint** (1 total):
1. HealthCheckEndpoint.cs - GET /health/check

**Disable**: Comment out line 65 (`app.MapHealthEndpoints();`)

**Note**: Line 72 (`app.MapHealthChecks("/health");`) stays - this is the built-in health check, not our custom endpoint.

**Commit**: `git commit -m "Phase 2.5: Migrate Health endpoint"`

---

### Phase 3: Cleanup - Remove Mediator (2 hours)

**Goal**: Remove all mediator-related code after all endpoints migrated.

#### 3.1 Delete Handler Files (27 files)

Delete these directories:
```
D:\Dev\ExtraTime\src\ExtraTime.Application\Features\Auth\Commands\
D:\Dev\ExtraTime\src\ExtraTime.Application\Features\Auth\Queries\
D:\Dev\ExtraTime\src\ExtraTime.Application\Features\Leagues\Commands\
D:\Dev\ExtraTime\src\ExtraTime.Application\Features\Leagues\Queries\
D:\Dev\ExtraTime\src\ExtraTime.Application\Features\Bets\Commands\
D:\Dev\ExtraTime\src\ExtraTime.Application\Features\Bets\Queries\
D:\Dev\ExtraTime\src\ExtraTime.Application\Features\Football\Queries\
D:\Dev\ExtraTime\src\ExtraTime.Application\Features\Admin\Commands\
D:\Dev\ExtraTime\src\ExtraTime.Application\Features\Admin\Queries\
```

**Verify**: No command/query files remain in Application layer.

#### 3.2 Delete Old Endpoint Files (7 files)

Delete:
```
D:\Dev\ExtraTime\src\ExtraTime.API\Features\Auth\AuthEndpoints.cs
D:\Dev\ExtraTime\src\ExtraTime.API\Features\Leagues\LeagueEndpoints.cs
D:\Dev\ExtraTime\src\ExtraTime.API\Features\Bets\BetEndpoints.cs
D:\Dev\ExtraTime\src\ExtraTime.API\Features\Football\FootballEndpoints.cs
D:\Dev\ExtraTime\src\ExtraTime.API\Features\Football\FootballSyncEndpoints.cs
D:\Dev\ExtraTime\src\ExtraTime.API\Features\Admin\AdminEndpoints.cs
D:\Dev\ExtraTime\src\ExtraTime.API\Features\Health\HealthEndpoints.cs
```

#### 3.3 Clean Program.cs

**File**: `D:\Dev\ExtraTime\src\ExtraTime.API\Program.cs`

Remove lines 4-9 (old endpoint imports):
```csharp
using ExtraTime.API.Features.Admin;
using ExtraTime.API.Features.Auth;
using ExtraTime.API.Features.Football;
using ExtraTime.API.Features.Health;
using ExtraTime.API.Features.Leagues;
using ExtraTime.API.Features.Bets;
```

Remove lines 65-71 (commented-out endpoint mappings):
```csharp
// app.MapHealthEndpoints();
// app.MapAuthEndpoints();
// app.MapAdminEndpoints();
// app.MapFootballEndpoints();
// app.MapFootballSyncEndpoints();
// app.MapLeagueEndpoints();
// app.MapBetEndpoints();
```

#### 3.4 Update DependencyInjection.cs

**File**: `D:\Dev\ExtraTime\src\ExtraTime.Application\DependencyInjection.cs`

Replace entire content with:
```csharp
using Microsoft.Extensions.DependencyInjection;

namespace ExtraTime.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application services (if any) can be registered here
        // FastEndpoints auto-discovers validators
        return services;
    }
}
```

#### 3.5 Remove NuGet Packages

**File**: `D:\Dev\ExtraTime\Directory.Packages.props`

Remove:
```xml
<PackageVersion Include="Mediator.Abstractions" Version="3.0.1" />
<PackageVersion Include="Mediator.SourceGenerator" Version="3.0.1" />
<PackageVersion Include="FluentValidation" Version="12.1.1" />
<PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
```

Note: FastEndpoints includes FluentValidation.

#### 3.6 Clean Build

```bash
dotnet clean
dotnet build
```

Should compile with no errors.

**Commit**: `git commit -m "Phase 3: Remove Mediator infrastructure and old endpoint files"`

**WARNING**: This is the point of no return. After this commit, rollback requires git revert.

---

### Phase 4: Swagger Migration (1 hour)

**Goal**: Replace Swashbuckle with FastEndpoints.Swagger.

#### 4.1 Update Program.cs

**File**: `D:\Dev\ExtraTime\src\ExtraTime.API\Program.cs`

Remove lines 19-45 (old Swagger setup):
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => { ... });
```

Remove lines 53-57 (old Swagger UI):
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Add after line 15 (AddFastEndpoints):
```csharp
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "ExtraTime API";
        s.Version = "v1";
        s.AddAuth("Bearer", new()
        {
            Type = OpenApiSecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token"
        });
    };
});
```

Add after UseFastEndpoints:
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}
```

#### 4.2 Remove Swashbuckle Package

**File**: `D:\Dev\ExtraTime\Directory.Packages.props`

Remove:
```xml
<PackageVersion Include="Swashbuckle.AspNetCore" Version="10.1.0" />
<PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
```

#### 4.3 Test Swagger UI

Start app and navigate to `/swagger`. Verify:
- All endpoints are documented
- JWT authorization works
- Request/response schemas are correct

**Commit**: `git commit -m "Phase 4: Migrate to FastEndpoints.Swagger"`

---

### Phase 5: Testing & Verification (4 hours)

**Goal**: Comprehensive testing to ensure migration is successful.

#### 5.1 Manual Testing Checklist

Test all 32 endpoints via Postman/Swagger:

**Auth** (4 endpoints):
- [ ] POST /api/auth/register - Success, validation errors, 409 conflict
- [ ] POST /api/auth/login - Success, 401 unauthorized
- [ ] POST /api/auth/refresh - Success, 401 invalid token
- [ ] GET /api/auth/me - Success with token, 401 without token

**Leagues** (9 endpoints):
- [ ] POST /api/leagues - Create league
- [ ] GET /api/leagues - List user's leagues
- [ ] GET /api/leagues/{id} - Get league details
- [ ] PUT /api/leagues/{id} - Update league
- [ ] DELETE /api/leagues/{id} - Delete league
- [ ] POST /api/leagues/{id}/join - Join with invite code
- [ ] DELETE /api/leagues/{id}/leave - Leave league
- [ ] DELETE /api/leagues/{id}/members/{userId} - Kick member
- [ ] POST /api/leagues/{id}/invite-code/regenerate - Regenerate code

**Bets** (6 endpoints):
- [ ] POST /api/leagues/{leagueId}/bets - Place bet
- [ ] DELETE /api/leagues/{leagueId}/bets/{betId} - Delete bet
- [ ] GET /api/leagues/{leagueId}/bets/my - Get my bets
- [ ] GET /api/leagues/{leagueId}/matches/{matchId}/bets - Get match bets
- [ ] GET /api/leagues/{leagueId}/standings - Get standings
- [ ] GET /api/leagues/{leagueId}/users/{userId}/stats - Get user stats

**Football** (3 endpoints):
- [ ] GET /api/competitions - List competitions
- [ ] GET /api/matches - List matches with filters
- [ ] GET /api/matches/{id} - Get match details

**Admin** (5 endpoints):
- [ ] GET /api/admin/jobs - List jobs (requires admin token)
- [ ] GET /api/admin/jobs/stats - Get job stats
- [ ] GET /api/admin/jobs/{id} - Get job details
- [ ] POST /api/admin/jobs/{id}/retry - Retry job
- [ ] POST /api/admin/jobs/{id}/cancel - Cancel job

**FootballSync** (4 endpoints):
- [ ] POST /api/admin/sync/competitions - Sync competitions
- [ ] POST /api/admin/sync/teams/{competitionId} - Sync teams
- [ ] POST /api/admin/sync/matches - Sync matches
- [ ] POST /api/admin/sync/live - Sync live matches

**Health** (1 endpoint):
- [ ] GET /api/health/check - Health status

#### 5.2 Automated Testing (Future Enhancement)

Create integration tests using FastEndpoints testing library:

```csharp
public class RegisterEndpointTests : TestBase<RegisterEndpoint>
{
    [Fact]
    public async Task Register_ValidRequest_ReturnsSuccess()
    {
        var (response, result) = await Endpoint.POSTAsync<RegisterRequest, AuthResponse>(
            new RegisterRequest("test@test.com", "testuser", "password123"));

        response.IsSuccessStatusCode.Should().BeTrue();
        result.Should().NotBeNull();
        result.User.Username.Should().Be("testuser");
    }
}
```

#### 5.3 Performance Comparison

Compare startup time and response times before/after migration:

**Metrics to track**:
- Application startup time
- First request latency
- Average response time (50th/95th/99th percentile)
- Memory usage

**Expected improvements**:
- ~10-20% faster startup (no source generator reflection)
- Similar or slightly better response times
- Similar memory usage

#### 5.4 Final Verification

- [ ] All 32 endpoints work correctly
- [ ] Authentication works (JWT tokens)
- [ ] Authorization works (policies, roles)
- [ ] Validation errors return proper format (400)
- [ ] Error responses match old format
- [ ] Success responses match old format
- [ ] Swagger documentation is complete
- [ ] No compilation errors or warnings
- [ ] No runtime errors in logs

**Commit**: `git commit -m "Phase 5: Add integration tests and verify migration"`

---

## Critical Files Reference

### To Create/Modify by Phase

**Phase 0**:
- `D:\Dev\ExtraTime\Directory.Packages.props` - Add FastEndpoints
- `D:\Dev\ExtraTime\src\ExtraTime.API\Program.cs` - Add middleware

**Phase 1** (Auth):
- `D:\Dev\ExtraTime\src\ExtraTime.API\Features\Auth\Endpoints\RegisterEndpoint.cs`
- `D:\Dev\ExtraTime\src\ExtraTime.API\Features\Auth\Endpoints\LoginEndpoint.cs`
- `D:\Dev\ExtraTime\src\ExtraTime.API\Features\Auth\Endpoints\RefreshTokenEndpoint.cs`
- `D:\Dev\ExtraTime\src\ExtraTime.API\Features\Auth\Endpoints\GetCurrentUserEndpoint.cs`

**Phase 2** (Remaining features):
- Similar pattern for Leagues (9 files), Bets (6 files), Football (7 files), Admin (5 files), Health (1 file)

**Phase 3** (Cleanup):
- `D:\Dev\ExtraTime\src\ExtraTime.Application\DependencyInjection.cs` - Remove mediator
- Delete all command/query handler directories
- Delete all old endpoint files

**Phase 4** (Swagger):
- `D:\Dev\ExtraTime\src\ExtraTime.API\Program.cs` - Migrate to FastEndpoints.Swagger

---

## Risk Mitigation

### Rollback Strategy

| After Phase | Rollback Method | Estimated Time |
|-------------|-----------------|----------------|
| Phase 0 | Revert NuGet, remove middleware | 5 minutes |
| Phase 1 | Uncomment old endpoints, delete new | 10 minutes |
| Phase 2 | Restore commented lines in Program.cs | 15 minutes |
| Phase 3 | **Git revert** (point of no return) | 30 minutes |
| Phase 4 | Git revert to Phase 3 | 10 minutes |

### Git Strategy

**Branch**: `feature/fastendpoints-migration`

**Commit after each phase** for easy rollback:
```bash
git checkout -b feature/fastendpoints-migration
# ... work on Phase 0 ...
git add . && git commit -m "Phase 0: Add FastEndpoints infrastructure"
# ... test ...
# ... work on Phase 1 ...
git add . && git commit -m "Phase 1: Migrate Auth endpoints"
# ... continue for each phase ...
```

**Test at each checkpoint**: If issues arise, `git revert HEAD` or `git reset --hard HEAD~1`.

### Breaking Changes

**What DOES NOT change**:
- API contracts (routes, DTOs, response formats)
- Database schema
- Authentication/authorization behavior
- External integrations
- Business logic

**What changes**:
- Internal endpoint implementation (Minimal APIs → FastEndpoints classes)
- Handler pattern (Mediator dispatch → direct endpoint logic)
- Validation location (separate validator class → nested/adjacent validator)

**Frontend/Client Impact**: **NONE** - All API contracts remain identical.

---

## Expected Outcomes

### Code Metrics

**Before**:
- 7 endpoint files (static classes)
- 27 handler files (commands + queries)
- 7 validator files (separate)
- ~100-130 lines per endpoint (across 4 files)

**After**:
- 32 endpoint files (one per endpoint)
- 0 handler files
- 0 separate validator files (nested in endpoints)
- ~90-110 lines per endpoint (in 1 file)

**Net Change**:
- 20-30% less code overall
- 75% fewer files per endpoint
- Better organization (one endpoint = one file)

### Performance

**Expected improvements**:
- 10-20% faster startup (no source generator reflection)
- 5-10% faster first request (no mediator warmup)
- Minimal change to average response time (< 5%)
- Similar memory footprint

### Maintainability

**Improvements**:
- Single file per endpoint (easier to locate and modify)
- No DTO → Command mapping (less boilerplate)
- Auto-validation (no manual validator calls)
- Better IDE navigation (jump-to-definition works better)
- Easier testing (endpoints are testable classes)

---

## Timeline

| Phase | Duration | Description |
|-------|----------|-------------|
| Phase 0 | 1 hour | Infrastructure setup |
| Phase 1 | 3-4 hours | Auth feature (proof of concept) |
| Phase 2.1 | 4-5 hours | Leagues (9 endpoints) |
| Phase 2.2 | 3-4 hours | Bets (6 endpoints) |
| Phase 2.3 | 2-3 hours | Football + Sync (7 endpoints) |
| Phase 2.4 | 2-3 hours | Admin (5 endpoints) |
| Phase 2.5 | 1 hour | Health (1 endpoint) |
| Phase 3 | 2 hours | Remove Mediator |
| Phase 4 | 1 hour | Swagger migration |
| Phase 5 | 4 hours | Testing & verification |
| **Total** | **23-28 hours** | **~3-4 days** |

---

## Post-Migration Tasks

### Documentation

- [ ] Update `CLAUDE.md` with FastEndpoints patterns
- [ ] Update README with new architecture notes
- [ ] Document endpoint testing approach

### Code Quality

- [ ] Add XML documentation to all endpoints
- [ ] Ensure consistent error handling
- [ ] Review authorization on all endpoints
- [ ] Add integration tests for critical flows

### Performance

- [ ] Run performance benchmarks
- [ ] Monitor production metrics after deployment
- [ ] Optimize any slow endpoints

---

## Conclusion

This migration plan provides a safe, phased approach to migrating from Minimal APIs + Mediator Source Generator to FastEndpoints. The proof-of-concept in Phase 1 validates the approach before full commitment. Each phase has clear rollback points and verification steps to ensure safety and correctness.

The migration will result in cleaner, more maintainable code with less boilerplate, while preserving all existing functionality and API contracts.
