# Plan: Add API Rate Limiting

## Context

The ExtraTime API currently has no end-user rate limiting. Any authenticated or anonymous user can make unlimited requests, leaving the API vulnerable to abuse, scraping, and accidental overload. This plan adds a global per-user token bucket rate limiter using ASP.NET Core's built-in middleware — no extra NuGet packages required.

**Decisions made:**
- Algorithm: Token Bucket (allows burst, then steady refill)
- Scope: Global per-user (authenticated by UserId, anonymous by IP)
- Storage: In-memory only (single-instance deployment)

## Progress

- [x] Step 1: Create `RateLimitingSettings` configuration class
- [x] Step 2: Add configuration to `appsettings.json`
- [x] Step 3: Register rate limiter in `DependencyInjection.cs`
- [ ] Step 4: Add middleware to `Program.cs`

---

## Step 1: Create `RateLimitingSettings` configuration class ✅

**New file:** `src/ExtraTime.Infrastructure/Configuration/RateLimitingSettings.cs`

Follow the exact pattern from `JwtSettings.cs` (line 1–12):

```csharp
namespace ExtraTime.Infrastructure.Configuration;

public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public int TokenLimit { get; set; } = 100;
    public int TokensPerPeriod { get; set; } = 10;
    public int ReplenishPeriodSeconds { get; set; } = 1;
    public int QueueLimit { get; set; } = 0;
    public bool Enabled { get; set; } = true;
    public bool AutoReplenishment { get; set; } = true;
}
```

Defaults: 100-request burst capacity, refills at 10 tokens/second, no queuing (immediate 429).

---

## Step 2: Add configuration to `appsettings.json` ✅

**File:** `src/ExtraTime.API/appsettings.json` — add after the `Jwt` section (after line 18):

```json
"RateLimiting": {
  "Enabled": true,
  "TokenLimit": 100,
  "TokensPerPeriod": 10,
  "ReplenishPeriodSeconds": 1,
  "QueueLimit": 0,
  "AutoReplenishment": true
},
```

---

## Step 3: Register rate limiter in `DependencyInjection.cs` ✅

**File:** `src/ExtraTime.Infrastructure/DependencyInjection.cs`

**Add usings** at the top:
```csharp
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
```

**Add rate limiting block** after the authorization block (after line 92, before the `// Services` comment on line 94):

Key logic:
- Bind `RateLimitingSettings` via Options pattern
- If `Enabled`, register `AddRateLimiter` with a global `PartitionedRateLimiter`
- Partition key: `user:{userId}` for authenticated (from JWT `sub` / `NameIdentifier` claim), `ip:{address}` for anonymous
- Exclude health-check paths (`/health`, `/alive`, `/ready`) from rate limiting
- On rejection: return 429 with `Retry-After` header and JSON error body `{ "error": "Too many requests. Please try again later." }`

Extract partition key directly from `HttpContext.User` claims (same claim names as `CurrentUserService.cs`), not from the scoped `ICurrentUserService`.

---

## Step 4: Add middleware to `Program.cs`

**File:** `src/ExtraTime.API/Program.cs` — insert `app.UseRateLimiter();` between lines 122 and 123:

```csharp
app.UseAuthentication();
app.UseRateLimiter();     // <-- new
app.UseAuthorization();
```

**Pipeline order rationale:** After Authentication (so JWT claims are populated for partition key extraction), before Authorization (reject rate-limited requests before expensive auth checks).

---

## Files Changed Summary

| File | Action |
|------|--------|
| `src/ExtraTime.Infrastructure/Configuration/RateLimitingSettings.cs` | **Create** — settings class |
| `src/ExtraTime.API/appsettings.json` | **Edit** — add `RateLimiting` section |
| `src/ExtraTime.Infrastructure/DependencyInjection.cs` | **Edit** — register rate limiter services |
| `src/ExtraTime.API/Program.cs` | **Edit** — add `UseRateLimiter()` middleware |

No new NuGet packages needed — `Microsoft.AspNetCore.RateLimiting` and `System.Threading.RateLimiting` are part of .NET 10.

---

## Verification

1. `dotnet build` — confirm no compilation errors
2. Run the app and hit any endpoint rapidly (>100 times in a burst) — verify 429 responses appear with `Retry-After` header and JSON body
3. Verify health endpoints (`/health`, `/alive`) are **not** rate limited
4. Verify two different authenticated users get independent buckets
5. Set `"Enabled": false` in appsettings — verify rate limiting is fully bypassed
