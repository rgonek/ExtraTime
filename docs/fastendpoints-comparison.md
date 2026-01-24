# FastEndpoints vs Minimal API + Mediator Comparison

This document compares the current Minimal API + Mediator architecture with an alternative FastEndpoints implementation using 3 endpoint examples.

## Table of Contents
- [Current Architecture Overview](#current-architecture-overview)
- [FastEndpoints Overview](#fastendpoints-overview)
- [Example 1: Create League (POST with Validation)](#example-1-create-league-post-with-validation)
- [Example 2: Get League by ID (GET with Route Parameter)](#example-2-get-league-by-id-get-with-route-parameter)
- [Example 3: Login (POST Anonymous)](#example-3-login-post-anonymous)
- [Comparison Summary](#comparison-summary)
- [Recommendation](#recommendation)

---

## Current Architecture Overview

The current architecture uses:
- **Clean Architecture** (Domain, Application, Infrastructure, API layers)
- **Minimal APIs** with static endpoint classes and extension methods
- **Mediator Source Generator** for CQRS (Commands/Queries)
- **FluentValidation** for request validation
- **Vertical Slice** organization within Features folder

### Current Flow:
```
HTTP Request → Minimal API Endpoint → Validator → Mediator → Handler → Result → HTTP Response
```

---

## FastEndpoints Overview

FastEndpoints is an opinionated framework that:
- Provides a class-based endpoint structure
- Has built-in validation support (FluentValidation)
- Offers auto-discovery of endpoints
- Includes built-in request/response DTOs
- Supports dependency injection

### FastEndpoints Flow:
```
HTTP Request → FastEndpoint → Validator → Handler (in endpoint) → HTTP Response
```

---

## Example 1: Create League (POST with Validation)

### Current Implementation (Minimal API + Mediator)

**API Layer - `LeagueEndpoints.cs`:**
```csharp
public static class LeagueEndpoints
{
    public static void MapLeagueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leagues")
            .WithTags("Leagues")
            .RequireAuthorization();

        group.MapPost("/", CreateLeagueAsync)
            .WithName("CreateLeague");
        // ... other endpoints
    }

    private static async Task<IResult> CreateLeagueAsync(
        CreateLeagueRequest request,
        IMediator mediator,
        IValidator<CreateLeagueCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new CreateLeagueCommand(
            request.Name,
            request.Description,
            request.IsPublic,
            request.MaxMembers,
            request.ScoreExactMatch,
            request.ScoreCorrectResult,
            request.BettingDeadlineMinutes,
            request.AllowedCompetitionIds,
            request.InviteCodeExpiresAt);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Created($"/api/leagues/{result.Value!.Id}", result.Value);
    }
}
```

**Application Layer - Files:**
- `CreateLeagueCommand.cs` (record)
- `CreateLeagueCommandHandler.cs` (handler class)
- `CreateLeagueCommandValidator.cs` (validator class)
- `LeagueDtos.cs` (request/response DTOs)

### FastEndpoints Alternative

**Option A: FastEndpoints WITHOUT Mediator (handler logic in endpoint)**

```csharp
// Features/Leagues/Endpoints/CreateLeagueEndpoint.cs
public sealed class CreateLeagueRequest
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsPublic { get; init; }
    public int MaxMembers { get; init; }
    public int ScoreExactMatch { get; init; }
    public int ScoreCorrectResult { get; init; }
    public int BettingDeadlineMinutes { get; init; }
    public Guid[]? AllowedCompetitionIds { get; init; }
    public DateTime? InviteCodeExpiresAt { get; init; }
}

public sealed class CreateLeagueEndpoint : Endpoint<CreateLeagueRequest, LeagueDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IInviteCodeGenerator _inviteCodeGenerator;

    public CreateLeagueEndpoint(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IInviteCodeGenerator inviteCodeGenerator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _inviteCodeGenerator = inviteCodeGenerator;
    }

    public override void Configure()
    {
        Post("/api/leagues");
        Policies("Authenticated"); // or just remove AllowAnonymous()
        Description(b => b
            .WithName("CreateLeague")
            .WithTags("Leagues"));
    }

    public override async Task HandleAsync(CreateLeagueRequest req, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        
        // Business logic here (moved from handler)
        // ... validation, creation logic ...
        
        var league = new League { /* ... */ };
        _context.Leagues.Add(league);
        await _context.SaveChangesAsync(ct);

        await SendCreatedAtAsync<GetLeagueEndpoint>(
            new { id = league.Id },
            new LeagueDto(/* ... */),
            cancellation: ct);
    }
}

public sealed class CreateLeagueValidator : Validator<CreateLeagueRequest>
{
    public CreateLeagueValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("League name is required.")
            .MinimumLength(3).WithMessage("League name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("League name must not exceed 100 characters.");

        RuleFor(x => x.MaxMembers)
            .InclusiveBetween(2, 255).WithMessage("Max members must be between 2 and 255.");
        // ... rest of validation rules
    }
}
```

**Option B: FastEndpoints WITH Mediator (keeping current CQRS)**

```csharp
// Features/Leagues/Endpoints/CreateLeagueEndpoint.cs
public sealed class CreateLeagueEndpoint : Endpoint<CreateLeagueRequest, LeagueDto>
{
    private readonly IMediator _mediator;

    public CreateLeagueEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/leagues");
        Description(b => b
            .WithName("CreateLeague")
            .WithTags("Leagues"));
    }

    public override async Task HandleAsync(CreateLeagueRequest req, CancellationToken ct)
    {
        var command = new CreateLeagueCommand(
            req.Name,
            req.Description,
            req.IsPublic,
            req.MaxMembers,
            req.ScoreExactMatch,
            req.ScoreCorrectResult,
            req.BettingDeadlineMinutes,
            req.AllowedCompetitionIds,
            req.InviteCodeExpiresAt);

        var result = await _mediator.Send(command, ct);

        if (result.IsFailure)
        {
            await SendAsync(new { error = result.Error }, 400, ct);
            return;
        }

        await SendCreatedAtAsync<GetLeagueEndpoint>(
            new { id = result.Value!.Id },
            result.Value,
            cancellation: ct);
    }
}

// Validator can reuse existing CreateLeagueCommandValidator
// Or create a new one for CreateLeagueRequest
```

---

## Example 2: Get League by ID (GET with Route Parameter)

### Current Implementation (Minimal API + Mediator)

```csharp
// In LeagueEndpoints.cs
group.MapGet("/{id}", GetLeagueAsync)
    .WithName("GetLeague");

private static async Task<IResult> GetLeagueAsync(
    Guid id,
    IMediator mediator,
    CancellationToken cancellationToken)
{
    var query = new GetLeagueQuery(id);
    var result = await mediator.Send(query, cancellationToken);

    if (result.IsFailure)
    {
        if (result.Error == LeagueErrors.LeagueNotFound)
        {
            return Results.NotFound(new { error = result.Error });
        }
        if (result.Error == LeagueErrors.NotAMember)
        {
            return Results.Forbid();
        }
        return Results.BadRequest(new { error = result.Error });
    }

    return Results.Ok(result.Value);
}
```

### FastEndpoints Alternative

**Option A: FastEndpoints WITHOUT Mediator**

```csharp
public sealed class GetLeagueRequest
{
    public Guid Id { get; init; }
}

public sealed class GetLeagueEndpoint : Endpoint<GetLeagueRequest, LeagueDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetLeagueEndpoint(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("/api/leagues/{id}");
        Description(b => b
            .WithName("GetLeague")
            .WithTags("Leagues"));
    }

    public override async Task HandleAsync(GetLeagueRequest req, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;

        var league = await _context.Leagues
            .Include(l => l.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(l => l.Id == req.Id, ct);

        if (league == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var isMember = league.Members.Any(m => m.UserId == userId);
        if (!isMember && !league.IsPublic)
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var dto = MapToDetailDto(league);
        await SendOkAsync(dto, ct);
    }

    private static LeagueDetailDto MapToDetailDto(League league) => /* mapping logic */;
}
```

**Option B: FastEndpoints WITH Mediator**

```csharp
public sealed class GetLeagueEndpoint : Endpoint<GetLeagueRequest, LeagueDetailDto>
{
    private readonly IMediator _mediator;

    public GetLeagueEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/api/leagues/{id}");
        Description(b => b
            .WithName("GetLeague")
            .WithTags("Leagues"));
    }

    public override async Task HandleAsync(GetLeagueRequest req, CancellationToken ct)
    {
        var query = new GetLeagueQuery(req.Id);
        var result = await _mediator.Send(query, ct);

        if (result.IsFailure)
        {
            if (result.Error == LeagueErrors.LeagueNotFound)
            {
                await SendNotFoundAsync(ct);
                return;
            }
            if (result.Error == LeagueErrors.NotAMember)
            {
                await SendForbiddenAsync(ct);
                return;
            }
            await SendAsync(new { error = result.Error }, 400, ct);
            return;
        }

        await SendOkAsync(result.Value!, ct);
    }
}
```

---

## Example 3: Login (POST Anonymous)

### Current Implementation (Minimal API + Mediator)

```csharp
// In AuthEndpoints.cs
public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
{
    var group = app.MapGroup("/api/auth")
        .WithTags("Auth");

    group.MapPost("/login", LoginAsync)
        .WithName("Login")
        .AllowAnonymous();
    // ...
}

private static async Task<IResult> LoginAsync(
    LoginRequest request,
    IMediator mediator,
    IValidator<LoginCommand> validator,
    CancellationToken cancellationToken)
{
    var command = new LoginCommand(request.Email, request.Password);

    var validationResult = await validator.ValidateAsync(command, cancellationToken);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var result = await mediator.Send(command, cancellationToken);

    if (result.IsFailure)
    {
        if (result.Error == AuthErrors.InvalidCredentials)
        {
            return Results.Unauthorized();
        }
        return Results.BadRequest(new { error = result.Error });
    }

    return Results.Ok(result.Value);
}
```

### FastEndpoints Alternative

**Option A: FastEndpoints WITHOUT Mediator**

```csharp
public sealed class LoginRequest
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
}

public sealed class LoginEndpoint : Endpoint<LoginRequest, AuthResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginEndpoint(
        IApplicationDbContext context,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        IRefreshTokenService refreshTokenService)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _refreshTokenService = refreshTokenService;
    }

    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
        Description(b => b
            .WithName("Login")
            .WithTags("Auth"));
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant(), ct);

        if (user == null || !_passwordHasher.VerifyPassword(req.Password, user.PasswordHash))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var accessToken = _jwtService.GenerateToken(user);
        var refreshToken = await _refreshTokenService.CreateAsync(user.Id, ct);

        await SendOkAsync(new AuthResponse(accessToken, refreshToken), ct);
    }
}

public sealed class LoginValidator : Validator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
```

**Option B: FastEndpoints WITH Mediator**

```csharp
public sealed class LoginEndpoint : Endpoint<LoginRequest, AuthResponse>
{
    private readonly IMediator _mediator;

    public LoginEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
        Description(b => b
            .WithName("Login")
            .WithTags("Auth"));
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var command = new LoginCommand(req.Email, req.Password);
        var result = await _mediator.Send(command, ct);

        if (result.IsFailure)
        {
            if (result.Error == AuthErrors.InvalidCredentials)
            {
                await SendUnauthorizedAsync(ct);
                return;
            }
            await SendAsync(new { error = result.Error }, 400, ct);
            return;
        }

        await SendOkAsync(result.Value!, ct);
    }
}
```

---

## Comparison Summary

| Aspect | Current (Minimal API + Mediator) | FastEndpoints (without Mediator) | FastEndpoints (with Mediator) |
|--------|----------------------------------|----------------------------------|-------------------------------|
| **File Count** | Many (Request, Command, Handler, Validator, Endpoint) | Fewer (Request, Endpoint with Validator) | Same as current |
| **Code Organization** | Vertical slices in Application layer | Endpoints in API layer | Mixed |
| **Boilerplate** | Moderate | Low | Moderate |
| **Testability** | High (handlers are isolated) | Medium (endpoints include logic) | High |
| **Separation of Concerns** | Excellent | Good | Excellent |
| **Learning Curve** | Low (standard patterns) | Low (intuitive API) | Low |
| **Performance** | Excellent | Excellent | Excellent |
| **OpenAPI Support** | Built-in | Built-in | Built-in |
| **Validation** | FluentValidation | Built-in + FluentValidation | FluentValidation |
| **Auto-discovery** | Manual registration | Automatic | Automatic |
| **Type Safety** | High | High | High |

---

## Recommendation

### Does FastEndpoints Make Sense?

**For this project, I would recommend staying with the current Minimal API + Mediator architecture.** Here's why:

#### Pros of Current Architecture:
1. **Clean separation** - Business logic is in Application layer handlers, API layer only handles HTTP concerns
2. **Highly testable** - Handlers can be unit tested without HTTP infrastructure
3. **Consistent patterns** - CQRS with Mediator is well-established
4. **Flexibility** - Easy to add cross-cutting concerns via Mediator pipeline behaviors
5. **Already implemented** - No migration cost

#### When FastEndpoints Would Be Better:
1. **Simpler applications** - Where CQRS overhead isn't justified
2. **New projects** - Starting fresh without existing patterns
3. **Performance-critical** - Slightly less overhead (though marginal)
4. **Smaller teams** - Fewer files to manage

#### Hybrid Approach (If Desired):
If you want FastEndpoints' ergonomics while keeping CQRS:
- Use FastEndpoints for endpoint definition (auto-discovery, built-in features)
- Keep Mediator handlers for business logic
- This gives you the best of both worlds but adds a dependency

### Final Verdict

**Stick with Minimal API + Mediator** because:
1. The architecture is already clean and well-organized
2. The current pattern scales well with team size
3. Testing strategy is established
4. Migration would require significant refactoring with minimal benefit
5. Mediator pipeline behaviors (logging, validation, caching) are easier to implement

FastEndpoints is a great framework, but it's best suited for projects that don't already have a well-established CQRS pattern or that prioritize reducing the number of files over maximum separation of concerns.

---

## Setup (If You Want to Try FastEndpoints)

If you decide to experiment with FastEndpoints:

```xml
<!-- Add to Directory.Packages.props -->
<PackageVersion Include="FastEndpoints" Version="5.34.0" />
```

```xml
<!-- Add to ExtraTime.API.csproj -->
<PackageReference Include="FastEndpoints" />
```

```csharp
// Program.cs
using FastEndpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFastEndpoints();
// ... existing services

var app = builder.Build();
app.UseFastEndpoints();
// ... existing middleware
```

FastEndpoints will auto-discover and register all endpoint classes in the assembly.
